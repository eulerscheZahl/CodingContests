using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

public class ArenaStrategy : Strategy
{
    private static List<Row> rows = new List<Row>();
    private static List<Row>[,] rowsByCell;
    private static List<Row> rowsLong = new List<Row>();
    private static List<Row>[,] rowsByCellLong;
    private static List<Row> rowsShort = new List<Row>();
    private static List<Row>[,] rowsByCellShort;
    private static List<Point> cells = new List<Point>();
    public override void Init(int n, int c)
    {
        base.Init(n, c);
        visited = new int[n * n];
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < N; y++)
            {
                cells.Add(new Point(x, y));
            }
        }
        int rowLength = 5;
        if (c == 3 && n == 7) rowLength = 6;
        if (c == 3 && n == 8) rowLength = 7;
        if (c == 3 && n == 9) rowLength = 7;
        if (c == 3 && n == 10) rowLength = 8;
        if (c == 3 && n == 11) rowLength = 8;

        if (c == 4 && n == 9) rowLength = 6;
        if (c == 4 && n == 10) rowLength = 6;
        if (c == 4 && n == 11) rowLength = 6;

        InitRows(rowLength);
        rowsLong = rows;
        rowsByCellLong = rowsByCell;
        InitRows(5);
        rowsShort = rows;
        rowsByCellShort = rowsByCell;

        if (c == 3 && false)
        {
            MakeStarRows();
            rowsLong = rows;
            rowsByCellLong = rowsByCell;
        }

    }

    private void InitRows(int rowLength)
    {
        rowsByCell = new List<Row>[N, N];
        foreach (Point p in cells) rowsByCell[p.X, p.Y] = new List<Row>();
        rows = new List<Row>();
        foreach (Point p in cells)
        {
            for (int dir = 0; dir < 4; dir++)
            {
                Row row = new Row(p.X, p.Y, dir, rowLength, N);
                if (!row.InGrid()) continue;
                rows.Add(row);
                foreach (Point r in row.Points) rowsByCell[r.X, r.Y].Add(row);
            }
        }
    }

    private void MakeStarRows()
    {
        rowsByCell = new List<Row>[N, N];
        foreach (Point p in cells) rowsByCell[p.X, p.Y] = new List<Row>();
        List<Point> points = new List<Point>
        {
            new Point(4,4),
            new Point(0,0),
            new Point(1,1),
            new Point(2,2),
            new Point(3,3),
            new Point(0,4),
            new Point(1,4),
            new Point(2,4),
            new Point(3,4),
            new Point(4,0),
            new Point(4,1),
            new Point(4,2),
            new Point(4,3),
        };
        Row row = new Row(points, N);
        foreach (Point p in points) rowsByCell[p.X, p.Y].Add(row);
        rows = new List<Row> { row };
        return;


        foreach (Point p in cells)
        {
            List<Row> rows1 = rowsByCellShort[p.X, p.Y].Where(r => r.rowDir == 0).ToList();
            List<Row> rows2 = rowsByCellShort[p.X, p.Y].Where(r => r.rowDir == 2).ToList();
            List<Row> rows3 = rowsByCellShort[p.X, p.Y].Where(r => (r.rowDir == 1 || r.rowDir == 3) && (p == r.Points[0] || p == r.Points.Last())).ToList();
            foreach (Row r1 in rows1)
            {
                foreach (Row r2 in rows2)
                {
                    Row r12 = Row.Merge(r1, r2);
                    foreach (Row r3 in rows3)
                    {
                        Row r = Row.Merge(r12, r3);
                        foreach (Point pt in r.Points) rowsByCell[pt.X, pt.Y].Add(r);
                        Console.Error.WriteLine(string.Join(" ", r.Points));
                    }
                }
            }
        }
    }

    public override (Point p1, Point p2, double score) Turn(int[] grid, int[] nextBalls, int elapsedTime)
    {
        int free = cells.Count(c => grid[c.X * N + c.Y] == EMPTY);
        if (free > 10 * (C - 1))
        {
            rows = rowsLong;
            rowsByCell = rowsByCellLong;
        }
        else
        {
            rows = rowsShort;
            rowsByCell = rowsByCellShort;
        }

        double[] colorDistribution = new double[1 + C];
        List<Point>[] PointsByColor = Enumerable.Range(0, C + 1).Select(i => new List<Point>()).ToArray();
        foreach (Point p in cells)
        {
            colorDistribution[grid[p.X * N + p.Y]]++;
            PointsByColor[grid[p.X * N + p.Y]].Add(p);
        }
        foreach (int c in nextBalls) colorDistribution[c]++;
        colorDistribution[0] = 0;
        double total = colorDistribution.Sum();
        for (int i = 1; i < colorDistribution.Length; i++) colorDistribution[i] = Math.Sqrt(colorDistribution[i] * C / total);

        rows.ForEach(r => r.MemorizeScore(grid, colorDistribution));
        double initialScore = rows.Sum(r => r.Score);
        List<(Point from, Point to, double score)> actions = new List<(Point from, Point to, double score)>();
        List<(Point from, Point to)> moves = GenerateMoves(grid).ToList();
        Dictionary<Point, HashSet<Point>> moveGroup = new Dictionary<Point, HashSet<Point>>();
        foreach (var move in moves)
        {
            if (!moveGroup.ContainsKey(move.from)) moveGroup[move.from] = new HashSet<Point>();
            moveGroup[move.from].Add(move.to);
        }
        Dictionary<(Point from, Point to), double> bonusScores = new Dictionary<(Point from, Point to), double>();
        if (N - C <= 3
#if !DEBUG
 && elapsedTime > 9000
#endif
            )
        {
            List<(Row row, int color, int completion)> completionStates = rows.Select(r => r.CompletionState(grid)).OrderByDescending(x => x.completion).ToList();
            foreach (var state in completionStates)
            {
                if (state.completion < 3) break;
                List<Point> toFix = state.row.Points.Where(p => grid[p.X * N + p.Y] != state.color).ToList();
                if (state.completion == 4)
                {
                    Point f = toFix[0];
                    if (moves.Any(m => m.to == f && grid[m.from.X * N + m.from.Y] == state.color && !state.row.Points.Contains(m.from)))
                    {
                        completionStates.Clear();
                        break;
                    }
                    List<Point> source = PointsByColor[state.color].Except(state.row.Points).ToList();
                    HashSet<Point> sourceNeighbors = new HashSet<Point>(source.SelectMany(s => new[] { new Point(s.X + 1, s.Y), new Point(s.X - 1, s.Y), new Point(s.X, s.Y + 1), new Point(s.X, s.Y - 1) }));

                    // find way to unblock path and complete row in 2 actions
                    foreach (Point from in moveGroup.Keys)
                    {
                        if (!moveGroup[from].Contains(f)) continue;
                        if (!sourceNeighbors.Intersect(moveGroup[from]).Any()) continue;

                        // test actual move
                        // if same color: from => f. now there most be a path from source to `from`
                        if (grid[from.X * N + from.Y] == state.color)
                        {
                            if (!bonusScores.ContainsKey((from, f))) bonusScores[(from, f)] = 0;
                            bonusScores[(from, f)] += 1e5;
                        }
                        else // different color: just move away and check for free path
                        {
                            foreach (Point to in moveGroup[from])
                            {
                                grid[to.X * N + to.Y] = grid[from.X * N + from.Y];
                                grid[from.X * N + from.Y] = EMPTY;
                                if (HasPath(grid, source, f))
                                {
                                    if (!bonusScores.ContainsKey((from, to))) bonusScores[(from, to)] = 0;
                                    bonusScores[(from, to)] += 1e5;
                                }
                                grid[from.X * N + from.Y] = grid[to.X * N + to.Y];
                                grid[to.X * N + to.Y] = EMPTY;
                            }
                        }

                    }
                }
                /*
                if (state.completion == 3)
                {
                    List<Point> source = PointsByColor[state.color].Except(state.row.Points).Where(s => moveGroup.ContainsKey(s)).ToList();
                    foreach (Point from1 in source.ToList())
                    {
                        foreach (Point to1 in toFix)
                        {
                            if (!moveGroup[from1].Contains(to1)) continue;
                            Point to2 = toFix[0];
                            if (to1 == to2) to2 = toFix[1];
                            source.Remove(from1);
                            grid[to1.X * N + to1.Y] = state.color;
                            grid[from1.X * N + from1.Y] = EMPTY;
                            if (HasPath(grid, source, to2))
                            {
                                if (!bonusScores.ContainsKey((from1, to1))) bonusScores[(from1, to1)] = 0;
                                bonusScores[(from1, to1)] += 1e5;
                            }
                            source.Add(from1);
                            grid[from1.X * N + from1.Y] = state.color;
                            grid[to1.X * N + to1.Y] = EMPTY;
                        }
                    }
                }
                */
            }
        }
        if (C == 3 && free > 20
#if !DEBUG
&& elapsedTime < 9000
#endif
        ) // remove actions that take away exactly 5 tiles - what a waste!
        {
            var oldMoves = moves.ToList();
            moves.Clear();
            foreach (var move in oldMoves)
            {
                int cl = grid[move.from.X * N + move.from.Y];
                grid[move.to.X * N + move.to.Y] = cl;
                grid[move.from.X * N + move.from.Y] = EMPTY;
                int complete = rowsByCellShort[move.to.X, move.to.Y].Count(r => r.IsFilled(grid, cl));
                if (complete != 1) moves.Add(move);
                grid[move.from.X * N + move.from.Y] = grid[move.to.X * N + move.to.Y];
                grid[move.to.X * N + move.to.Y] = EMPTY;
            }
        }
        foreach (var move in moves)
        {
#if !DEBUG
if (elapsedTime > 9800) return (move.from, move.to, -1);
#endif
            int color = grid[move.from.X * N + move.from.Y];
            //if (move.to == new Point(1, 1) && move.from == new Point(6,7)) System.Diagnostics.Debugger.Break();
            grid[move.from.X * N + move.from.Y] = EMPTY;
            double score = initialScore;
            if (bonusScores.ContainsKey((move.from, move.to))) score += bonusScores[(move.from, move.to)];
            foreach (Row r in rowsByCell[move.from.X, move.from.Y])
            {
                r.Affected = true;
                score += r.ComputeScore(grid, colorDistribution, move.from) - r.Score;
            }
            grid[move.to.X * N + move.to.Y] = color;
            foreach (Row r in rowsByCell[move.to.X, move.to.Y])
            {
                if (r.Affected)
                {
                    score -= r.ComputeScore(grid, colorDistribution, move.from);
                    r.ResetCache(grid, move.from);
                    score += r.ComputeScore(grid, colorDistribution);
                }
                else score += r.ComputeScore(grid, colorDistribution, move.to) - r.Score;
            }
            foreach (Row r in rowsByCell[move.from.X, move.from.Y]) r.Affected = false;

            score += CellScore(move.from) - CellScore(move.to);
            grid[move.from.X * N + move.from.Y] = color;
            grid[move.to.X * N + move.to.Y] = EMPTY;
            actions.Add((move.from, move.to, score));
        }

        actions = actions.OrderByDescending(a => a.score).ToList();
        var allActions = actions;
        if (actions.Count > 10) actions = actions.Take(10).ToList();
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            int color = grid[action.from.X * N + action.from.Y];
            grid[action.from.X * N + action.from.Y] = EMPTY;
            grid[action.to.X * N + action.to.Y] = color;
            // check if rowLength-1 aligned? => bonus score if last can be placed too
            foreach (Row row in rowsByCell[action.to.X, action.to.Y])
            {
                (Point missing, int rowColor) = row.LastMissing(grid);
                if (rowColor == -1) continue;
                foreach (Point p in FindSources(missing, grid, rowColor))
                {
                    if (!row.Points.Contains(p))
                    {
                        actions[i] = (action.from, action.to, actions[i].score + 1e5);
                        break;
                    }
                }
            }

            grid[action.from.X * N + action.from.Y] = color;
            grid[action.to.X * N + action.to.Y] = EMPTY;
        }

        var result = actions.OrderByDescending(a => a.score).First();
        int actionColor = grid[result.from.X * N + result.from.Y];
        if (free > 10 * (C - 1) && C < 7)
        {
            grid[result.to.X * N + result.to.Y] = actionColor;
            List<Row> targetRows = rowsByCellShort[result.to.X, result.to.Y]
                .Where(r => r.Points.All(p => grid[p.X * N + p.Y] == actionColor || grid[p.X * N + p.Y] == EMPTY))
                .OrderBy(r => r.Points.Count(p => grid[p.X * N + p.Y] == EMPTY)).ToList();
            HashSet<Point> cantMove = new HashSet<Point>();
            int rowIndex = 0;
            for (; rowIndex < targetRows.Count; rowIndex++)
            {
                if (targetRows[rowIndex].IsFilled(grid, actionColor)) cantMove.UnionWith(targetRows[rowIndex].Points);
                else break;
            }
            if (rowIndex == 0) return result; // won't finish anything, play normal

            grid[result.to.X * N + result.to.Y] = EMPTY;
            HashSet<Point> cantFill = new HashSet<Point>();
            for (; rowIndex < targetRows.Count; rowIndex++)
            {
                List<Point> shallFill = targetRows[rowIndex].Points.Where(p => grid[p.X * N + p.Y] == EMPTY && p != result.to).ToList();
                if (shallFill.Count > 1) break; // TODO: allow longer?
                if (shallFill.Any(f => cantFill.Contains(f))) continue;
                foreach (Point f in shallFill) grid[f.X * N + f.Y] = actionColor;
                foreach (Point f in shallFill)
                {
                    grid[f.X * N + f.Y] = actionColor;
                    if (rowsByCellShort[f.X, f.Y].Any(r => r.IsFilled(grid, actionColor)))
                        cantFill.Add(f);
                }
                foreach (Point f in shallFill) grid[f.X * N + f.Y] = EMPTY;
                if (shallFill.Any(f => cantFill.Contains(f))) continue;
                foreach (Point f in shallFill)
                {
                    var fillers = allActions.Where(m => m.to == f && grid[m.from.X * N + m.from.Y] == actionColor && m.from != result.from && !cantMove.Contains(m.from)).ToList();
                    if (fillers.Count > 0)
                    {
                        //Console.Error.WriteLine(fillers.First());
                        return fillers.First();
                    }
                }
            }
        }

        grid[result.from.X * N + result.from.Y] = EMPTY;
        grid[result.to.X * N + result.to.Y] = actionColor;
        int removeCount = rowsByCellShort[result.to.X, result.to.Y].Where(r => r.IsFilled(grid, actionColor)).SelectMany(r => r.Points).Distinct().Count();
        grid[result.from.X * N + result.from.Y] = actionColor;
        grid[result.to.X * N + result.to.Y] = EMPTY;
        if (removeCount > 0)
        {
            foreach (var move in moves)
            {
                if (grid[move.from.X * N + move.from.Y] != actionColor) continue;
                //if (move.from != result.from && move.to != result.to) continue;
                grid[move.from.X * N + move.from.Y] = EMPTY;
                grid[move.to.X * N + move.to.Y] = actionColor;
                int thisRemoveCount = rowsByCellShort[move.to.X, move.to.Y].Where(r => r.IsFilled(grid, actionColor)).SelectMany(r => r.Points).Distinct().Count();
                grid[move.from.X * N + move.from.Y] = actionColor;
                grid[move.to.X * N + move.to.Y] = EMPTY;
                if (thisRemoveCount > removeCount)
                {
                    removeCount = thisRemoveCount;
                    result = (move.from, move.to, result.score);
                }
            }
        }
        return result;
    }

    private double CellScore(Point p)
    {
        int borderX = Math.Min(p.X, N - 1 - p.X);
        int borderY = Math.Min(p.Y, N - 1 - p.Y);
        return 10.1 * (borderX + borderY);
    }

    private static int[] visited;
    private static int visitIndex = 1;
    private static Point[] queue = new Point[200];
    private static Point[] freeList = new Point[200];
    private static Point[] fullList = new Point[200];
    private IEnumerable<(Point from, Point to)> GenerateMoves(int[] grid)
    {
        visitIndex++;
        foreach (Point cell in cells)
        {
            if (visited[cell.X * N + cell.Y] == visitIndex || grid[cell.X * N + cell.Y] != EMPTY) continue;
            int queueWrite = 0;
            int queueRead = 0;
            queue[queueWrite++] = cell;
            visited[cell.X * N + cell.Y] = visitIndex;
            int freeCount = 0;
            int fullCount = 0;
            freeList[freeCount++] = cell;

            while (queueRead < queueWrite)
            {
                Point p = queue[queueRead++];
                for (int dir = 0; dir < 4; dir++)
                {
                    Point q = new Point(p.X + dx[dir], p.Y + dy[dir]);
                    if (q.X < 0 || q.X >= N || q.Y < 0 || q.Y >= N || visited[q.X * N + q.Y] == visitIndex) continue;
                    visited[q.X * N + q.Y] = visitIndex;
                    if (grid[q.X * N + q.Y] == EMPTY)
                    {
                        queue[queueWrite++] = q;
                        freeList[freeCount++] = q;
                    }
                    else fullList[fullCount++] = q;
                }
            }

            for (int fromIdx = 0; fromIdx < fullCount; fromIdx++)
            {
                Point from = fullList[fromIdx];
                visited[from.X * N + from.Y] = 0;
                for (int toIdx = 0; toIdx < freeCount; toIdx++)
                {
                    yield return (from, freeList[toIdx]);
                }
            }
        }
    }

    static int[] dx = { 0, 1, 0, -1 };
    static int[] dy = { -1, 0, 1, 0 };

    private IEnumerable<Point> FindReachable(Point from, int[,] grid)
    {
        bool[,] visited = new bool[N, N];
        Queue<Point> queue = new Queue<Point>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            for (int dir = 0; dir < 4; dir++)
            {
                Point q = new Point(p.X + dx[dir], p.Y + dy[dir]);
                if (q.X < 0 || q.X >= N || q.Y < 0 || q.Y >= N || visited[q.X, q.Y] || grid[q.X, q.Y] != EMPTY) continue;
                visited[q.X, q.Y] = true;
                queue.Enqueue(q);
                yield return q;
            }
        }
    }

    private bool HasPath(int[] grid, List<Point> source, Point f)
    {
        if (source.Count == 0) return false;
        foreach (Point p in FindSources(f, grid, grid[source[0].X * N + source[0].Y]))
        {
            if (source.Contains(p)) return true;
        }
        return false;
    }

    private IEnumerable<Point> FindSources(Point missing, int[] grid, int color)
    {
        visitIndex++;
        Queue<Point> queue = new Queue<Point>();
        queue.Enqueue(missing);
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            for (int dir = 0; dir < 4; dir++)
            {
                Point q = new Point(p.X + dx[dir], p.Y + dy[dir]);
                if (q.X < 0 || q.X >= N || q.Y < 0 || q.Y >= N || visited[q.X * N + q.Y] == visitIndex) continue;
                visited[q.X * N + q.Y] = visitIndex;
                if (grid[q.X * N + q.Y] == color) yield return q;
                if (grid[q.X * N + q.Y] != EMPTY) continue;
                queue.Enqueue(q);
            }
        }
    }
}