using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    public static int Size;
    public static int Area;
    public static int Colors;
    public static int Penalty;
    public static Cell[,] Grid;
    public void ReadInput()
    {
        Size = PipeConnector.ReadInt();
        Colors = PipeConnector.ReadInt();
        Penalty = PipeConnector.ReadInt();
        Area = Size * Size;

        Grid = new Cell[Size, Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Grid[x, y] = new Cell(x, y, PipeConnector.ReadInt(), PipeConnector.ReadInt());
            }
        }
        foreach (Cell cell in Grid) cell.InitNeighbors(Grid);
    }

    public List<Path> Solve()
    {
        List<Cell> withValue = new List<Cell>();
        int[] freq = new int[Colors + 1];
        foreach (Cell cell in Grid)
        {
            if (cell.Value > 0) withValue.Add(cell);
            freq[cell.Color]++;
        }
        withValue = withValue.Where(c => freq[c.Color] > 1).ToList();

        Stopwatch sw = Stopwatch.StartNew();
        int bestScore = 0;
        List<Path> result = new List<Path>();
        int attempts = 5;
        int totalTime = 9800;
        int intervalTime = totalTime / attempts;
        for (int i = 0; i < attempts; i++)
        {
            List<Path> current = GreedyInit(withValue);
            current = Mutate(current, withValue, sw, i * intervalTime, intervalTime);
            int score = 0;
            foreach (Path path in current) score += path.Remove();
            if (score > bestScore)
            {
                bestScore = score;
                result = current;
            }
        }
        return result;
    }

    private static List<Path> GreedyInit(List<Cell> withValue)
    {

        List<Path> result = new List<Path>();
        foreach (Cell cell in withValue.OrderByDescending(c => c.Value))
        {
            if (cell.Paths.Count > 0) continue;
            int[] dist = cell.BFS();
            List<Cell> partners = withValue.Where(c => c != cell && c.Color == cell.Color && dist[c.ID] > 0).ToList();
            if (partners.Count == 0) continue;
            Cell toConnect = partners.OrderByDescending(p => p.Value / Math.Sqrt(dist[p.ID])).First();
            result.Add(cell.BuildPath(toConnect, dist));
        }
        return result;
    }

    public static Random random = new Random();
    private static List<Path> Mutate(List<Path> result, List<Cell> withValue, Stopwatch sw, int startTime, int maxTime)
    {
        int totalScore = result.Sum(r => r.Score());
        int randomRange = 2;
        int toUndo = 8;
        List<Cell> empty = withValue.Where(v => v.Paths.Count == 0).ToList();
        int runs = 0;
        while (true)
        {
            int time = (int)sw.ElapsedMilliseconds - startTime;
            if (time > maxTime) break;
            runs++;
            int crossings = 30 * time / (Math.Max(4, Penalty) * maxTime);
            // undo some paths near each other
            List<Path> undone = new List<Path>();
            int x = random.Next(Size);
            int y = random.Next(Size);
            int oldScore = 0;
            int maxColors = random.Next(2, 1 + Colors);
            int attempts = 0;
            HashSet<int> usedColors = new HashSet<int>();
            while (undone.Count < toUndo && undone.Count < result.Count)
            {
                attempts++;
                if (Grid[x, y].Paths.Count > 0)
                {
                    Path toRemove = Grid[x, y].Paths[random.Next(Grid[x, y].Paths.Count)];
                    if (usedColors.Count < maxColors || attempts > 1000 || usedColors.Contains(toRemove.Color))
                    {
                        oldScore += toRemove.Remove();
                        undone.Add(toRemove);
                        usedColors.Add(toRemove.Color);
                        int r = random.Next(5);
                        if (r == 0) { x = toRemove.Start.X; y = toRemove.Start.Y; }
                        if (r == 1) { x = toRemove.End.X; y = toRemove.End.Y; }
                    }
                }
                x = Math.Max(0, Math.Min(Size - 1, x + random.Next(2 * randomRange + 1) - randomRange));
                y = Math.Max(0, Math.Min(Size - 1, y + random.Next(2 * randomRange + 1) - randomRange));
            }

            // redo connections with some randomness
            List<Cell> ends = undone.Select(u => u.Start).Concat(undone.Select(u => u.End)).Concat(empty).ToList();
            ends = ends.OrderByDescending(e => e.Value + random.Next(3)).ToList();
            List<Path> replacements = new List<Path>();
            List<Path> cross = new List<Path>();
            int newScore = 0;
            foreach (Cell cell in ends)
            {
                if (cell.Paths.Count > 0) continue;
                if (!ends.Any(e => e != cell && e.Color == cell.Color && e.Paths.Count == 0)) continue;
                int[] dist = cell.BFS(crossings);
                Cell[] partners = ends.Where(c => c != cell && c.Color == cell.Color && c.Paths.Count == 0 && dist[c.ID + Board.Area * crossings] > 0).ToArray();
                if (partners.Length == 0) continue;
                double pow = random.NextDouble();
                double[] scores = new double[partners.Length];
                for (int p = 0; p < partners.Length; p++)
                {
                    int c = 0;
                    while (dist[partners[p].ID + Board.Area * c] == 0) c++;
                    scores[p] = cell.Value * partners[p].Value + random.Next(5) - Penalty * c;
                    scores[p] = scores[p] / Math.Pow(dist[partners[p].ID + Board.Area * c], pow);
                }
                Array.Sort(scores, partners);
                Cell toConnect = partners[partners.Length - 1];
                Path path = cell.BuildPath(toConnect, dist);
                int pathScore = path.Score();
                if (pathScore > 0)
                {
                    replacements.Add(path);
                    newScore += pathScore;
                    foreach (Cell c in path.Cells)
                    {
                        for (int i = 0; i < c.Paths.Count - 1; i++)
                        {
                            if (!replacements.Contains(c.Paths[i]) && !cross.Contains(c.Paths[i])) cross.Add(c.Paths[i]);
                        }
                    }
                }
                else path.Remove();
            }

            // redo crossings between old and new paths
            foreach (Path p in cross)
            {
                oldScore += p.Remove();
                int[] dist = p.Start.BFS(crossings);
                if (dist[p.End.ID + crossings * Area] == 0)
                {
                    p.Apply();
                    oldScore -= p.Score();
                    continue;
                }
                undone.Add(p);
                Path r = p.Start.BuildPath(p.End, dist);
                newScore += r.Score();
                replacements.Add(r);
            }

            // keep the better
            if (newScore < oldScore)
            {
                foreach (Path p in replacements) p.Remove();
                foreach (Path p in undone) p.Apply();
            }
            else
            {
                foreach (Path p in undone) result.Remove(p);
                foreach (Path p in replacements) result.Add(p);
                empty = ends.Where(e => e.Paths.Count == 0).ToList();
                totalScore += newScore - oldScore;
                //if (newScore > oldScore) Console.Error.WriteLine(time + ": " + totalScore);
            }
        }

        if (runs % 64 == 0)
        {
            int dir = (runs / 64) % 4;
            foreach (Path path in result.OrderBy(r => -r.Start.X * Cell.dx[dir] - r.Start.Y * Cell.dy[dir]).ToList())
            {
                path.Remove();
                int[] dist = path.Start.BFS(5);
                if (dist[path.End.ID + 5 * Area] == 0)
                {
                    totalScore += path.Score();
                    path.Apply();
                    continue;
                }
                Path newPath = path.Start.BuildPath(path.End, dist, dir);
                result.Remove(path);
                result.Add(newPath);
            }
            totalScore = result.Sum(r => r.Start.Value * r.End.Value);
            foreach (Cell cell in Grid) totalScore -= Math.Max(Penalty * (cell.Paths.Count - 1), 0);
        }

        Console.Error.WriteLine("runs: " + runs + "   score: " + totalScore + "   BFS total: " + Cell.BFSCount + " @" + (sw.ElapsedMilliseconds - startTime));
        return result;
    }
}
