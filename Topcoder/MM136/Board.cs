using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    public static int Size;
    public static int Colors;
    public static int[,] D;
    public static Cell[,] Cells;
    public int[,] Grid;
    public static Board ReadBoard()
    {
        Board result = new Board();
        Size = HungryKnights.ReadInt();
        Cells = new Cell[Size, Size];
        Colors = HungryKnights.ReadInt();
        result.Grid = new int[Size, Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                result.Grid[x, y] = HungryKnights.ReadInt();
                Cells[x, y] = new Cell(x, y);
            }
        }

        D = new int[Colors, Colors];
        for (int from = 0; from < Colors; from++)
        {
            for (int to = 0; to < Colors; to++)
            {
                D[from, to] = HungryKnights.ReadInt();
            }
            D[from, from] = 1;
        }

        foreach (Cell cell in Cells) cell.InitNeighbors(Cells);
        return result;
    }

    public static int[] penalties;
    int timeLimit = 9000;
    public List<Move> Solve()
    {
        Stopwatch sw = Stopwatch.StartNew();
        int bestScore = 0;
        List<Move> result = new List<Move>();
        int[,] initialGrid = (int[,])Grid.Clone();
        for (int simulation = 1; sw.ElapsedMilliseconds < timeLimit; simulation++)
        {
            Grid = (int[,])initialGrid.Clone();
            int currentScore = 0;
            double limit = 2 + 3 * random.NextDouble();
            penalties = new int[Colors];
            bool[,] visited = new bool[Size, Size];
            List<Move> currentResult = new List<Move>();
            HashSet<int> taboo = new HashSet<int>();
            for (int run = 0; run < 10 * Colors && sw.ElapsedMilliseconds < timeLimit; run++)
            {
                List<Cell>[] byColor = Enumerable.Range(0, Colors).Select(i => new List<Cell>()).ToArray();
                foreach (Cell cell in Cells)
                {
                    if (visited[cell.X, cell.Y] || taboo.Contains(Grid[cell.X, cell.Y])) continue;
                    byColor[Grid[cell.X, cell.Y]].Add(cell);
                }
                byColor = byColor.OrderByDescending(l => l.Count + random.Next(run)).ToArray();
                for (int i = 0; i < byColor.Length; i++)
                {
                    if (byColor[i].Count == 0) continue;
                    penalties[Grid[byColor[i][0].X, byColor[i][0].Y]] = 1 << (Colors - i);
                }

                List<Cell> cells = byColor[0].Where(c => !visited[c.X, c.Y]).ToList();
                List<Move>[] moves = new[] { new List<Move>(), new List<Move>() };
                int best = 0;
                List<Move> colorPath = new List<Move>();
                for (int attempt = 0; attempt < 10 && moves[1].Count < cells.Count / 2; attempt++)
                {
                    moves = FindPath(cells, visited, sw);
                    int current = moves[1].Count * moves[1].Count - moves[0].Count;
                    if (current > best)
                    {
                        best = current;
                        colorPath = moves[0].Concat(moves[1]).ToList();
                    }
                }
                int[,] gridBackup = (int[,])Grid.Clone();
                int score = 0;
                Cell lastMoved = null;
                int chainPower = 1;
                foreach (Move move in colorPath)
                {
                    if (move.From != lastMoved || gridBackup[move.From.X, move.From.Y] != gridBackup[move.To.X, move.To.Y]) chainPower = 1;
                    if (gridBackup[move.From.X, move.From.Y] == gridBackup[move.To.X, move.To.Y])
                    {
                        score += chainPower;
                        chainPower++;
                    }
                    else score += D[gridBackup[move.From.X, move.From.Y], gridBackup[move.To.X, move.To.Y]];
                    lastMoved = move.To;
                    gridBackup[move.To.X, move.To.Y] = gridBackup[move.From.X, move.From.Y];
                }
                if ((double)score / colorPath.Count < limit)
                {
                    taboo.Add(Grid[byColor[0][0].X, byColor[0][0].Y]);
                    continue;
                }

                currentScore += score;
                currentResult.AddRange(colorPath);
                foreach (Move move in colorPath)
                {
                    visited[move.From.X, move.From.Y] = true;
                    Grid[move.To.X, move.To.Y] = Grid[move.From.X, move.From.Y];
                }
            }

            Console.Error.Write(currentScore + " => ");
            currentResult.AddRange(Finalize(visited, ref currentScore, sw, simulation == 1));
            Console.Error.WriteLine(currentScore + " @" + sw.ElapsedMilliseconds);
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                result = currentResult;
            }
        }
        return result;
    }

    private class ScoredMove : IComparable<ScoredMove>
    {
        public Move Move;
        public int Color1;
        public int Color2;
        public double Score;
        public bool Initial;

        public int CompareTo(ScoredMove other) => this.Score.CompareTo(other.Score);
    }

    private List<Move> Finalize(bool[,] visited, ref int totalScore, Stopwatch sw, bool first)
    {
        List<Move> finalizers = new List<Move>();
        List<Move> result = new List<Move>();
        foreach (Cell cell in Cells)
        {
            if (visited[cell.X, cell.Y]) continue;
            foreach (Cell next in cell.Neighbors.Where(n => !visited[n.X, n.Y])) finalizers.Add(new Move(cell, next));
        }

        int score = 0;
        int[,] gridBackup = (int[,])Grid.Clone();
        bool[,] visitedBackup = (bool[,])visited.Clone();
        for (int run = 0; run < 1000; run++)
        {
            double rnd = 5 * random.NextDouble();
            if (run==1 && first || sw.ElapsedMilliseconds > timeLimit) break;
            MaxHeap<ScoredMove> currentFinalizers = new MaxHeap<ScoredMove>();
            foreach (Move move in finalizers) currentFinalizers.Add(new ScoredMove { Initial = true, Move = move, Color1 = Grid[move.From.X, move.From.Y], Color2 = Grid[move.To.X, move.To.Y], Score = move.Score(Grid, D) + rnd * random.NextDouble() });
            int currentScore = 0;
            List<Move> currentResult = new List<Move>();
            Grid = (int[,])gridBackup.Clone();
            visited = (bool[,])visitedBackup.Clone();
            int chainPower = 1;
            Cell lastMoved = null;
            while (currentFinalizers.Count > 0)
            {
                ScoredMove scoredMove = currentFinalizers.ExtractDominating();
                Move move = scoredMove.Move;
                if (visited[move.From.X, move.From.Y] || visited[move.To.X, move.To.Y] ||
                    Grid[move.From.X, move.From.Y] != scoredMove.Color1 || Grid[move.To.X, move.To.Y] != scoredMove.Color2) continue;
                if (move.From == lastMoved && Grid[move.To.X, move.To.Y] == Grid[move.From.X, move.From.Y])
                {
                    currentScore += chainPower;
                }
                else
                {
                    currentScore += D[Grid[move.From.X, move.From.Y], Grid[move.To.X, move.To.Y]];
                    chainPower = 1;
                }
                currentResult.Add(move);
                visited[move.From.X, move.From.Y] = true;
                if (Grid[move.To.X, move.To.Y] == Grid[move.From.X, move.From.Y]) chainPower++;
                Grid[move.To.X, move.To.Y] = Grid[move.From.X, move.From.Y];
                foreach (Cell cell in move.To.Neighbors)
                {
                    if (visited[cell.X, cell.Y]) continue;
                    Move newMove = new Move(move.To, cell);
                    ScoredMove newScoredMove = new ScoredMove { Move = newMove, Color1 = Grid[move.To.X, move.To.Y], Color2 = Grid[cell.X, cell.Y], Score = newMove.Score(Grid, D) + rnd * random.NextDouble() };
                    currentFinalizers.Add(newScoredMove);
                    newMove = new Move(cell, move.To);
                    newScoredMove = new ScoredMove { Move = newMove, Color2 = Grid[move.To.X, move.To.Y], Color1 = Grid[cell.X, cell.Y], Score = newMove.Score(Grid, D) + rnd * random.NextDouble() };
                    currentFinalizers.Add(newScoredMove);
                }
                lastMoved = move.To;

                if (currentScore > score)
                {
                    score = currentScore;
                    result = currentResult.ToList();
                }
            }
        }
        totalScore += score;
        return result;
    }

    private static Random random = new Random(0);
    private List<Move>[] FindPath(List<Cell> cells, bool[,] initialVisited, Stopwatch sw)
    {
        List<Move> prepareMoves = new List<Move>();
        List<Move> chainMoves = new List<Move>();
        if (cells.Count == 0) return new[] { prepareMoves, chainMoves };
        int color = Grid[cells[0].X, cells[0].Y];
        List<Cell> chainPath = new List<Cell>();
        List<Cell>[,] preparePath = new List<Cell>[Size, Size];
        bool[,] visited = (bool[,])initialVisited.Clone();

        List<BeamNode> beam = new List<BeamNode>();
        for (int i = 0; cells.Count > 0 && i < 1; i++)
        {
            Cell start = cells[random.Next(cells.Count)];
            beam.Add(new BeamNode { End1 = start, End2 = start, Visited = new HashSet<Cell> { start }, Path = new List<Cell> { start } });
        }
        int beamWidth = 250;
        int beamDepth = 30;
        for (int i = 0; i < beamDepth && sw.ElapsedMilliseconds < timeLimit; i++)
        {
            List<BeamNode> next = beam.SelectMany(b => b.Expand(color, Grid, visited)).OrderBy(b => b.Penalty).ToList();
            if (next.Count > 0) beam = next;
            else break;
            if (beam.Count > beamWidth) beam = beam.Take(beamWidth).ToList();
        }
        beam = beam.OrderBy(b => b.Penalty).ToList();

        while (beam.Count > 0 && sw.ElapsedMilliseconds < timeLimit)
        {
            BeamNode root = beam[0].Root();
            beam = beam.Where(b => b.Root() == root).ToList();
            beam.ForEach(b => b.RemoveRoot(root));
            List<BeamNode> next = beam.SelectMany(b => b.Expand(color, Grid, visited)).OrderBy(b => b.Penalty).ToList();
            if (next.Count > 0) beam = next;
            if (beam.Count > beamWidth) beam = beam.Take(beamWidth).ToList();

            List<Cell> path = (root?.Path ?? beam[0].Path);
            Cell toAdd = path.Last();
            if (chainPath.Count > 0 && toAdd.HasNeighbor(chainPath[0])) chainPath.Insert(0, toAdd);
            else chainPath.Add(toAdd);
            foreach (Cell cell in path)
            {
                visited[cell.X, cell.Y] = true;
                foreach (BeamNode b in beam) b.Visited.Remove(cell);
            }
            preparePath[toAdd.X, toAdd.Y] = path;
            if (root == null) break;
        }

        for (int dist = 0; dist < 7; dist++)
        {
            for (int i = 1; i < chainPath.Count && sw.ElapsedMilliseconds < timeLimit; i++)
            {
                Cell prev = chainPath[i - 1];
                Cell next = chainPath[i];
                List<Cell>[] prevN = new List<Cell>[dist + 1];
                List<Cell>[] nextN = new List<Cell>[dist + 1];
                prevN[0] = prev.Neighbors.Where(n => !visited[n.X, n.Y]).ToList();
                nextN[0] = next.Neighbors.Where(n => !visited[n.X, n.Y]).ToList();
                for (int d = 1; d <= dist; d++)
                {
                    prevN[d] = prevN[d - 1].SelectMany(c => c.Neighbors.Where(n => !visited[n.X, n.Y])).Distinct().ToList();
                    nextN[d] = nextN[d - 1].SelectMany(c => c.Neighbors.Where(n => !visited[n.X, n.Y])).Distinct().ToList();
                }

                for (int totDist = 0; totDist <= dist; totDist++)
                {
                    for (int dist1 = 0; dist1 <= totDist; dist1++)
                    {
                        foreach (Cell p in prevN[dist1].Where(c => Grid[c.X, c.Y] == color))
                        {
                            List<Cell> pathP = new List<Cell> { p };
                            for (int j = dist1 - 1; j >= 0; j--) pathP.Add(prevN[j].First(c => pathP.Last().HasNeighbor(c)));
                            foreach (Cell n in nextN[totDist - dist1].Where(c => Grid[c.X, c.Y] == color))
                            {
                                if (pathP.Contains(n)) continue;
                                List<Cell> pathN = new List<Cell> { n };
                                bool valid = true;
                                for (int j = totDist - dist1 - 1; j >= 0; j--)
                                {
                                    pathN.Add(nextN[j].FirstOrDefault(c => !pathP.Contains(c) && pathN.Last().HasNeighbor(c)));
                                    if (pathN.Last() == null)
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                                if (valid && pathP.Last().HasNeighbor(pathN.Last()))
                                {
                                    chainPath.Insert(i, pathN.Last());
                                    chainPath.Insert(i, pathP.Last());

                                    foreach (Cell cell in pathN.Concat(pathP)) visited[cell.X, cell.Y] = true;
                                    preparePath[pathP.Last().X, pathP.Last().Y] = pathP;
                                    preparePath[pathN.Last().X, pathN.Last().Y] = pathN;
                                    goto didAdd;
                                }
                            }
                        }
                    }
                }

            didAdd:;
            }
        }

        foreach (Cell cell in chainPath)
        {
            List<Cell> prepare = preparePath[cell.X, cell.Y];
            for (int i = 1; i < prepare.Count; i++)
            {
                prepareMoves.Add(new Move(prepare[i - 1], prepare[i]));
            }
        }

        for (int i = 1; i < chainPath.Count; i++)
        {
            chainMoves.Add(new Move(chainPath[i - 1], chainPath[i]));
        }
        //Console.Error.WriteLine(string.Join(" - ", prepareMoves));
        //Console.Error.WriteLine(string.Join(" - ", chainMoves));
        return new[] { prepareMoves, chainMoves };
    }
}