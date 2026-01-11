using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board : IEquatable<Board>
{
    public static int Size;
    public static int ColorCount;
    public static int Penalty;
    public static int Area => Size * Size;

    public int[] Tiles;
    public static int[] targetTiles;
    public static int[,,] zobrist;
    private static Stopwatch sw;

    public Board Parent;
    private int Action;
    public int Score;

    public Board() { }

    public void ReadInitial()
    {
        Size = int.Parse(Console.ReadLine().Split().Last());
        ColorCount = int.Parse(Console.ReadLine().Split().Last());
        Penalty = int.Parse(Console.ReadLine().Split().Last());
        Console.Error.WriteLine(Size + "\n" + ColorCount + "\n" + Penalty);

        Tiles = new int[Size * (Size + 1)];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                string line = Console.ReadLine();
                Console.Error.WriteLine(line);
                Tiles[(Size + 1) * x + y] = int.Parse(line.Split()[0]);
                Tiles[(Size + 1) * x + y] |= int.Parse(line.Split()[1]) << 4;
            }
        }
        string freeLine = Console.ReadLine();
        Console.Error.WriteLine(freeLine);
        Tiles[Size] = int.Parse(freeLine.Split()[0]);
        Tiles[Size] |= int.Parse(freeLine.Split()[1]) << 4;

        sw = Stopwatch.StartNew();
        zobrist = new int[Size, Size, ColorCount * 16];
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int c = 0; c < ColorCount * 16; c++) zobrist[x, y, c] = random.Next();
            }
        }
    }

    private static int[] dx = { 0, 1, 0, -1, 0, 1, 0, -1, 0, 1, 0, -1 };
    private static int[] dy = { -1, 0, 1, 0, -1, 0, 1, 0, -1, 0, 1, 0 };
    public static string dirs = "URDLURDL";
    private static int[] masks = { 1, 2, 4, 8, 1, 2, 4, 8, 1, 2, 4, 8 };
    private static Random random = new Random(1);
    private static Func<int, int[], int> columnScoreFunction;
    private void PrintState(int[] tiles)
    {
        for (int y = 0; y <= Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Console.ForegroundColor = new[] { ConsoleColor.Green, ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.Yellow }[tiles[(Size + 1) * x + y] >> 4];
                Console.Error.Write(" ╵╶└╷│┌├╴┘─┴┐┤┬┼"[tiles[(Size + 1) * x + y] & 0xf]);
            }
            Console.Error.WriteLine();
        }
        Console.Error.WriteLine();
        Console.ResetColor();
    }

    private int columnSolved;
    public Board(Board parent, int action)
    {
        this.Parent = parent;
        this.Action = action;
        this.Score = Parent.Score;
        this.hash = parent.hash;
        this.columnSolved = parent.columnSolved;
        Tiles = (int[])parent.Tiles.Clone();

        int dir = action & 3;
        action >>= 2;
        Score -= columnScoreFunction(action, Tiles);
        if (dir == UP || dir == DOWN)
        {
            for (int y = 0; y < Size; y++) hash ^= zobrist[action, y, Tiles[(Size + 1) * action + y]];
        }
        else
        {
            for (int x = 0; x < Size; x++) hash ^= zobrist[x, action, Tiles[(Size + 1) * x + action]];
        }
        if (dir == UP)
        {
            int free = Tiles[(Size + 1) * action + 0];
            Tiles[(Size + 1) * action + Size] = Tiles[Size];
            for (int y = 0; y < Size; y++) Tiles[(Size + 1) * action + y] = Tiles[(Size + 1) * action + y + 1];
            Tiles[Size] = free;
        }
        else if (dir == DOWN)
        {
            int free = Tiles[(Size + 1) * action + Size - 1];
            for (int y = Size - 1; y > 0; y--) Tiles[(Size + 1) * action + y] = Tiles[(Size + 1) * action + y - 1];
            Tiles[(Size + 1) * action + 0] = Tiles[Size];
            Tiles[Size] = free;
        }
        else if (dir == RIGHT)
        {
            int free = Tiles[(Size + 1) * (Size - 1) + action];
            for (int x = Size - 1; x > 0; x--) Tiles[(Size + 1) * x + action] = Tiles[(Size + 1) * (x - 1) + action];
            Tiles[(Size + 1) * 0 + action] = Tiles[Size];
            Tiles[Size] = free;
        }
        else if (dir == LEFT)
        {
            int free = Tiles[(Size + 1) * 0 + action];
            for (int x = 0; x < Size - 1; x++) Tiles[(Size + 1) * x + action] = Tiles[(Size + 1) * (x + 1) + action];
            Tiles[(Size + 1) * (Size - 1) + action] = Tiles[Size];
            Tiles[Size] = free;
        }
        int score = columnScoreFunction(action, Tiles);
        Score += score;
        if (score == 1000000) columnSolved |= 1 << action;
        if (dir == UP || dir == DOWN)
        {
            for (int y = 0; y < Size; y++) hash ^= zobrist[action, y, Tiles[(Size + 1) * action + y]];
        }
        else
        {
            for (int x = 0; x < Size; x++) hash ^= zobrist[x, action, Tiles[(Size + 1) * x + action]];
        }
    }

    public List<string> GetActions()
    {
        List<string> actions = new List<string>();
        Board b = this;
        while (b.Parent != null)
        {
            actions.Add(dirs[b.Action & 3] + " " + (b.Action >> 2));
            b = b.Parent;
        }
        actions.Reverse();
        return actions;
    }

    public const int UP = 0;
    public const int RIGHT = 1;
    public const int DOWN = 2;
    public const int LEFT = 3;
    public IEnumerable<Board> Expand(bool horizontal = false)
    {
        for (int i = 0; i < Size; i++)
        {
            if (!horizontal && (columnSolved & (1 << i)) != 0) continue;
            yield return new Board(this, (i << 2) + UP);
            yield return new Board(this, (i << 2) + DOWN);
            if (!horizontal) continue;
            yield return new Board(this, (i << 2) + RIGHT);
            yield return new Board(this, (i << 2) + LEFT);
        }
    }

    private static int[][] columnTargets;
    public Board SolveGroups()
    {
        columnScoreFunction = MultishiftColumnScore;
        columnTargets = new int[Size][];
        int[] junk = new int[16 * ColorCount];
        for (int x = 0; x < Size; x++)
        {
            int tmp = targetTiles[(Size + 1) * x + 0];
            for (int y = 0; y < Size - 1; y++) targetTiles[(Size + 1) * x + y] = targetTiles[(Size + 1) * x + y + 1];
            targetTiles[(Size + 1) * x + Size - 1] = tmp;
            columnTargets[x] = new int[16 * ColorCount];
            for (int y = 0; y < Size - 1; y++)
            {
                columnTargets[x][targetTiles[(Size + 1) * x + y]]++;
            }
            junk[targetTiles[(Size + 1) * x + Size - 1]]++;
        }
        junk[targetTiles[Size]]++;

        // TODO max(top, bottom)
        int[] filledWithoutJunk = new int[Size];
        int[] filledWithJunk = new int[Size];
        for (int x = 0; x < Size; x++)
        {
            int filled = 0;
            int[] missing = columnTargets[x].ToArray();
            while (missing[Tiles[(Size + 1) * x + filled]] > 0)
            {
                missing[Tiles[(Size + 1) * x + filled]]--;
                filled++;
            }
            filledWithoutJunk[x] = filled;
            filledWithJunk[x] = filled;
            if (junk[Tiles[(Size + 1) * x + filled]] > 0)
            {
                filledWithJunk[x]++;
                filled++;
                while (filled < Size && missing[Tiles[(Size + 1) * x + filled]] > 0)
                {
                    missing[Tiles[(Size + 1) * x + filled]]--;
                    filled++;
                }
                filledWithJunk[x] = filled;
            }
        }

        List<int> byGain = Enumerable.Range(0, Size).OrderByDescending(x => filledWithJunk[x] - filledWithoutJunk[x]).ToList();
        foreach (int x in byGain.ToList())
        {
            if (junk[Tiles[(Size + 1) * x + filledWithoutJunk[x]]] > 0)
            {
                junk[Tiles[(Size + 1) * x + filledWithoutJunk[x]]]--;
                byGain.Remove(x);
                columnTargets[x][Tiles[(Size + 1) * x + filledWithoutJunk[x]]]++;
            }
        }
        foreach (int x in byGain)
        {
            int j = Enumerable.Range(0, junk.Length).First(k => junk[k] > 0);
            junk[j]--;
            columnTargets[x][j]++;
        }

        this.Score = Enumerable.Range(0, Size).Sum(i => columnScoreFunction(i, Tiles));
        HashSet<Board> boards = new HashSet<Board> { this };
        while (true)
        {
            if (sw.ElapsedMilliseconds > 7500)
                throw new Exception();
            HashSet<Board> next = new HashSet<Board>(boards.SelectMany(b => b.Expand()));
            int max = next.Max(n => n.Score);
            if (max > boards.Max(n => n.Score))
                boards = new HashSet<Board>(next.Where(n => n.Score == max).Take(1));
            else
                boards = new HashSet<Board>(next.Where(n => n.Score == max).OrderBy(n => random.Next()).Take(15));
            if (boards.First().Score == Area - Size)
                return boards.First(b => b.Score == Area - Size);
        }
    }

    public int MultishiftColumnScore(int x, int[] tiles)
    {
        if (x == Size - 1) return 0;
        int fromTop = 0;
        int[] missing = columnTargets[x].ToArray();
        while (fromTop < Size && missing[tiles[(Size + 1) * x + fromTop]] > 0)
        {
            missing[tiles[(Size + 1) * x + fromTop]]--;
            fromTop++;
        }
        int fromBottom = 0;
        missing = columnTargets[x].ToArray();
        while (fromBottom < Size && missing[tiles[(Size + 1) * x + Size - 1 - fromBottom]] > 0)
        {
            missing[tiles[(Size + 1) * x + Size - 1 - fromBottom]]--;
            fromBottom++;
        }
        return Math.Max(fromTop, fromBottom);
    }

    private int BeamColumnScore(int x, int[] tiles)
    {
        int colScore = 0;
        for (int yTarget = 0; yTarget < Size; yTarget++)
        {
            for (int yTile = 0; yTile < Size; yTile++)
            {
                if (targetTiles[(Size + 1) * x + yTarget] != tiles[(Size + 1) * x + yTile]) continue;
                int len = 1;
                while (yTarget + len <= Size && yTile + len <= Size && targetTiles[(Size + 1) * x + yTarget + len - 1] == tiles[(Size + 1) * x + yTile + len - 1]) len++;
                len--;
                int diff1 = yTarget + yTile;
                int diff2 = 2 * Size - 2 * len - yTarget - yTile;
                colScore = Math.Max(colScore, 1000 - Size + len - diff1 - diff2);
            }
        }
        return colScore * colScore;
    }

    private Board SolveMultishift()
    {
        Board result = this;
        int[] target = (int[])targetTiles.Clone();
        try
        {
            for (int run = 0; run < 10; run++)
            {
                Board board = this.SolveGroups();
                Console.Error.WriteLine("setup: " + board.GetActions().Count + " @" + sw.ElapsedMilliseconds);
                for (int x = Size - 1; x >= 0; x--) board = board.SolveColumn(x, false);
                targetTiles = (int[])target.Clone();
                board = board.SolveRow(0);
                int tmpScore = board.GetActions().Count + ComputeLayoutScore(board.Tiles);
                Console.Error.WriteLine("multi-run: " + tmpScore + " @" + sw.ElapsedMilliseconds);
                if (tmpScore < result.GetActions().Count + ComputeLayoutScore(result.Tiles)) result = board;
            }
        }
        catch (Exception) { }

        targetTiles = (int[])target.Clone();
        return result;
    }

    private int shiftedMask;
    private int shiftedCount;
    private int correctedMask;
    private int correctedCount;
    private int depth;
    private Board SolveColumn(int x, bool solveLast)
    {
        Board result = this;
        HashSet<int> shifted = new HashSet<int>(Enumerable.Range(0, Size));
        int resultDepth = 0;
        while (shifted.Count > 0)
        {
            int y = shifted.Last();
            if (shifted.Any(s => targetTiles[(Size + 1) * x + s] == result.Tiles[Size])) y = shifted.First(s => targetTiles[(Size + 1) * x + s] == result.Tiles[Size]);
            shifted.Remove(y);
            result = new Board(result, (y << 2) + RIGHT);
            resultDepth++;
        }

        for (int y = 0; y < Size; y++)
        {
            if (result.Tiles[(Size + 1) * 0 + y] == targetTiles[(Size + 1) * x + y]) continue;
            if (y + 1 < Size && result.Tiles[Size] == targetTiles[(Size + 1) * x + y])
            {
                int y2 = y + 1;
                result = new Board(result, (y2 << 2) + LEFT);
                result = new Board(result, (y << 2) + LEFT);
                result = new Board(result, (y2 << 2) + RIGHT);
                result = new Board(result, (y << 2) + RIGHT);
                resultDepth += 4;
                continue;
            }
            for (int y2 = y + 1; y2 < Size; y2++)
            {
                if (result.Tiles[(Size + 1) * 0 + y2] != targetTiles[(Size + 1) * x + y]) continue;
                result = new Board(result, (y << 2) + LEFT);
                result = new Board(result, (y2 << 2) + LEFT);
                result = new Board(result, (y << 2) + RIGHT);
                result = new Board(result, (y2 << 2) + RIGHT);
                resultDepth += 4;
                break;
            }
        }
        result.depth = resultDepth;
        if (solveLast && result.Tiles[Size - 1] != targetTiles[(Size + 1) * x + Size - 1]) result.depth += 4;

        columnScoreFunction = (a, b) => 0;
        Board board = this;
        this.depth = 0;
        this.shiftedMask = 0;
        this.shiftedCount = 0;
        this.correctedMask = 0;
        this.correctedCount = 0;
        Board d = new Board(this, ((Size - 1) << 2) + DOWN);
        Board d2 = new Board(d, ((Size - 1) << 2) + DOWN);
        Board u = new Board(this, ((Size - 1) << 2) + UP);
        Board u2 = new Board(u, ((Size - 1) << 2) + UP);
        u.depth = 1;
        u2.depth = 2;
        d.depth = 1;
        d2.depth = 2;
        int solveTarget = solveLast ? Size : Size - 1;
        foreach (Board start in new[] { board, d, u, d2, u2 })
        {
            if (sw.ElapsedMilliseconds > 9500 || result.depth <= Size + start.depth) return result;
            List<HashSet<Board>> boards = new List<HashSet<Board>>();
            boards.Add(new HashSet<Board> { start });
            for (int depth = 0; depth < result.depth; depth++)
            {
                while (boards.Count <= depth + 4) boards.Add(new HashSet<Board>());
                foreach (Board b in boards[depth].OrderByDescending(b => 2 * b.correctedCount + b.shiftedCount).Take(10))
                {
                    if (b.depth + solveTarget - b.correctedCount >= result.depth) continue;
                    if (b.correctedCount == Size - 1 && b.shiftedCount == Size)
                    {
                        if (solveLast && b.Tiles[Size - 1] != targetTiles[(Size + 1) * x + Size - 1])
                        {
                            b.depth += 4; // penalty for incomplete
                            if (result.depth > b.depth) result = b;
                            continue;
                        }
                        result = b;
                        break;
                    }
                    if (b.correctedCount == solveTarget && b.shiftedCount == Size)
                    {
                        result = b;
                        break;
                    }
                    for (int shift = 0; shift < Size; shift++)
                    {
                        if ((b.shiftedMask & (1 << shift)) != 0) continue;
                        Board s = new Board(b, (shift << 2) + RIGHT);
                        s.depth = b.depth + 1;
                        s.shiftedMask = b.shiftedMask | (1 << shift);
                        s.shiftedCount = b.shiftedCount + 1;
                        s.correctedMask = b.correctedMask;
                        s.correctedCount = b.correctedCount;
                        if ((solveLast || shift != Size - 1) && s.Tiles[(Size + 1) * 0 + shift] == targetTiles[(Size + 1) * x + shift])
                        {
                            s.correctedMask |= 1 << shift;
                            s.correctedCount++;
                        }
                        boards[s.depth].Add(s);
                    }

                    for (int y1 = 0; y1 < Size - 1; y1++)
                    {
                        if ((b.shiftedMask & (1 << y1)) == 0 || (b.correctedMask & (1 << y1)) != 0) continue;
                        for (int y2 = 0; y2 < Size; y2++)
                        {
                            if (y1 == y2 || (b.shiftedMask & (1 << y2)) == 0 || (b.correctedMask & (1 << y2)) != 0) continue;
                            if (b.Tiles[Size] == targetTiles[(Size + 1) * x + y1] || y2 != Size - 1 && b.Tiles[(Size + 1) * x + y1] == targetTiles[(Size + 1) * x + y2])
                            {
                                Board s = new Board(b, (y2 << 2) + LEFT);
                                s = new Board(s, (y1 << 2) + LEFT);
                                s = new Board(s, (y2 << 2) + RIGHT);
                                s = new Board(s, (y1 << 2) + RIGHT);
                                s.depth = b.depth + 4;
                                s.shiftedMask = b.shiftedMask;
                                s.shiftedCount = b.shiftedCount;
                                s.correctedMask = b.correctedMask;
                                s.correctedCount = b.correctedCount;
                                if (s.Tiles[(Size + 1) * 0 + y1] == targetTiles[(Size + 1) * x + y1])
                                {
                                    s.correctedMask |= 1 << y1;
                                    s.correctedCount++;
                                }
                                if (y2 < Size - 1 && s.Tiles[(Size + 1) * 0 + y2] == targetTiles[(Size + 1) * x + y2])
                                {
                                    s.correctedMask |= 1 << y2;
                                    s.correctedCount++;
                                }
                                boards[s.depth].Add(s);
                            }
                            if (b.Tiles[Size] == targetTiles[(Size + 1) * x + y2] || y1 != Size - 1 && b.Tiles[(Size + 1) * x + y2] == targetTiles[(Size + 1) * x + y1])
                            {
                                Board s = new Board(b, (y1 << 2) + LEFT);
                                s = new Board(s, (y2 << 2) + LEFT);
                                s = new Board(s, (y1 << 2) + RIGHT);
                                s = new Board(s, (y2 << 2) + RIGHT);
                                s.depth = b.depth + 4;
                                s.shiftedMask = b.shiftedMask;
                                s.shiftedCount = b.shiftedCount;
                                s.correctedMask = b.correctedMask;
                                s.correctedCount = b.correctedCount;
                                if (s.Tiles[(Size + 1) * 0 + y1] == targetTiles[(Size + 1) * x + y1])
                                {
                                    s.correctedMask |= 1 << y1;
                                    s.correctedCount++;
                                }
                                if (y2 < Size - 1 && s.Tiles[(Size + 1) * 0 + y2] == targetTiles[(Size + 1) * x + y2])
                                {
                                    s.correctedMask |= 1 << y2;
                                    s.correctedCount++;
                                }
                                boards[s.depth].Add(s);
                            }
                        }
                    }
                }
            }
        }
        return result;
    }

    private Board SolveRow(int y)
    {
        Board result = this;
        HashSet<int> shifted = new HashSet<int>(Enumerable.Range(0, Size));
        int resultDepth = 0;
        while (shifted.Count > 0)
        {
            int x = shifted.First();
            if (shifted.Any(s => targetTiles[(Size + 1) * s + y] == result.Tiles[Size])) x = shifted.First(s => targetTiles[(Size + 1) * s + y] == result.Tiles[Size]);
            shifted.Remove(x);
            result = new Board(result, (x << 2) + DOWN);
            resultDepth++;
        }

        for (int x = 0; x < Size; x++)
        {
            if (result.Tiles[(Size + 1) * x + 0] == targetTiles[(Size + 1) * x + y]) continue;
            if (x + 1 < Size && result.Tiles[Size] == targetTiles[(Size + 1) * x + y])
            {
                int x2 = x + 1;
                result = new Board(result, (x2 << 2) + UP);
                result = new Board(result, (x << 2) + UP);
                result = new Board(result, (x2 << 2) + DOWN);
                result = new Board(result, (x << 2) + DOWN);
                resultDepth += 4;
                continue;
            }
            for (int x2 = x + 1; x2 < Size; x2++)
            {
                if (result.Tiles[(Size + 1) * x2 + 0] != targetTiles[(Size + 1) * x + y]) continue;
                result = new Board(result, (x << 2) + UP);
                result = new Board(result, (x2 << 2) + UP);
                result = new Board(result, (x << 2) + DOWN);
                result = new Board(result, (x2 << 2) + DOWN);
                resultDepth += 4;
                break;
            }
        }
        result.depth = resultDepth;
        if (result.Tiles[(Size + 1) * (Size - 1)] != targetTiles[(Size + 1) * (Size - 1) + y]) result.depth += 4;

        columnScoreFunction = (a, b) => 0;
        Board board = this;
        this.depth = 0;
        this.shiftedMask = 0;
        this.shiftedCount = 0;
        this.correctedMask = 0;
        this.correctedCount = 0;
        Board r = new Board(this, ((Size - 1) << 2) + RIGHT);
        Board r2 = new Board(r, ((Size - 1) << 2) + RIGHT);
        Board l = new Board(this, ((Size - 1) << 2) + LEFT);
        Board l2 = new Board(l, ((Size - 1) << 2) + LEFT);
        l.depth = 1;
        l2.depth = 2;
        r.depth = 1;
        r2.depth = 2;
        foreach (Board start in new[] { board, r, l, r2, l2 })
        {
            if (sw.ElapsedMilliseconds > 9500 || result.depth <= Size + start.depth) return result;
            List<HashSet<Board>> boards = new List<HashSet<Board>>();
            boards.Add(new HashSet<Board> { start });
            for (int depth = 0; depth < result.depth; depth++)
            {
                while (boards.Count <= depth + 4) boards.Add(new HashSet<Board>());
                foreach (Board b in boards[depth].OrderByDescending(b => 2 * b.correctedCount + b.shiftedCount).Take(10))
                {
                    if (b.depth + Size - b.correctedCount >= result.depth) continue;
                    if (b.correctedCount == Size - 1 && b.shiftedCount == Size)
                    {
                        if (b.Tiles[Size - 1] != targetTiles[(Size + 1) * (Size - 1) + y])
                        {
                            b.depth += 4; // penalty for incomplete
                            if (result.depth > b.depth) result = b;
                            continue;
                        }
                    }
                    if (b.correctedCount == Size)
                    {
                        result = b;
                        break;
                    }
                    for (int shift = 0; shift < Size; shift++)
                    {
                        if ((b.shiftedMask & (1 << shift)) != 0) continue;
                        Board s = new Board(b, (shift << 2) + DOWN);
                        s.depth = b.depth + 1;
                        s.shiftedMask = b.shiftedMask | (1 << shift);
                        s.shiftedCount = b.shiftedCount + 1;
                        s.correctedMask = b.correctedMask;
                        s.correctedCount = b.correctedCount;
                        if (s.Tiles[(Size + 1) * shift + 0] == targetTiles[(Size + 1) * shift + y])
                        {
                            s.correctedMask |= 1 << shift;
                            s.correctedCount++;
                        }
                        boards[s.depth].Add(s);
                    }

                    for (int x1 = 0; x1 < Size - 1; x1++)
                    {
                        if ((b.shiftedMask & (1 << x1)) == 0 || (b.correctedMask & (1 << x1)) != 0) continue;
                        for (int x2 = 0; x2 < Size; x2++)
                        {
                            if (x1 == x2 || (b.shiftedMask & (1 << x2)) == 0 || (b.correctedMask & (1 << x2)) != 0) continue;
                            if (b.Tiles[Size] == targetTiles[(Size + 1) * x1 + y] || x2 != Size - 1 && b.Tiles[(Size + 1) * x1 + y] == targetTiles[(Size + 1) * x2 + y])
                            {
                                Board s = new Board(b, (x2 << 2) + UP);
                                s = new Board(s, (x1 << 2) + UP);
                                s = new Board(s, (x2 << 2) + DOWN);
                                s = new Board(s, (x1 << 2) + DOWN);
                                s.depth = b.depth + 4;
                                s.shiftedMask = b.shiftedMask;
                                s.shiftedCount = b.shiftedCount;
                                s.correctedMask = b.correctedMask;
                                s.correctedCount = b.correctedCount;
                                if (s.Tiles[(Size + 1) * x1 + 0] == targetTiles[(Size + 1) * x1 + y])
                                {
                                    s.correctedMask |= 1 << x1;
                                    s.correctedCount++;
                                }
                                if (x2 < Size - 1 && s.Tiles[(Size + 1) * x2 + 0] == targetTiles[(Size + 1) * x2 + y])
                                {
                                    s.correctedMask |= 1 << x2;
                                    s.correctedCount++;
                                }
                                boards[s.depth].Add(s);
                            }
                            if (b.Tiles[Size] == targetTiles[(Size + 1) * x2 + y] || x1 != Size - 1 && b.Tiles[(Size + 1) * x2 + y] == targetTiles[(Size + 1) * x1 + y])
                            {
                                Board s = new Board(b, (x1 << 2) + LEFT);
                                s = new Board(s, (x2 << 2) + LEFT);
                                s = new Board(s, (x1 << 2) + RIGHT);
                                s = new Board(s, (x2 << 2) + RIGHT);
                                s.depth = b.depth + 4;
                                s.shiftedMask = b.shiftedMask;
                                s.shiftedCount = b.shiftedCount;
                                s.correctedMask = b.correctedMask;
                                s.correctedCount = b.correctedCount;
                                if (s.Tiles[(Size + 1) * x1 + 0] == targetTiles[(Size + 1) * x1 + y])
                                {
                                    s.correctedMask |= 1 << x1;
                                    s.correctedCount++;
                                }
                                if (x2 < Size - 1 && s.Tiles[(Size + 1) * x2 + 0] == targetTiles[(Size + 1) * x2 + y])
                                {
                                    s.correctedMask |= 1 << x2;
                                    s.correctedCount++;
                                }
                                boards[s.depth].Add(s);
                            }
                        }
                    }
                }
            }
        }
        return result;
    }

    public List<string> Solve()
    {
        Board multishift = SolveMultishift();
        List<string> result = multishift.GetActions();
        return result;
        int best = result.Count + ComputeLayoutScore(multishift.Tiles);
        Console.Error.WriteLine("multishift: " + best);
        List<Board> initial = new List<Board> { this };
        initial.AddRange(Expand(true).Where(b => "RL".Contains(b.GetActions()[0][0])));
        foreach (Board ini in initial)
        {
            foreach (ColumnBoard colBoard in new ColumnBoard(ini).Solve(sw).Take(20))
            {
                List<string> actions = colBoard.GetActions();
                Board board = colBoard.ToBoard().SolveColumn(0, true);
                actions.AddRange(board.GetActions());
                actions.InsertRange(0, ini.GetActions());
                int score = actions.Count + ComputeLayoutScore(board.Tiles);
                if (score < best)
                {
                    best = score;
                    result = actions;
                }
            }
        }

        Board beam = BeamSolve();
        if (beam != null && beam.GetActions().Count + ComputeLayoutScore(beam.Tiles) < result.Count) result = beam.GetActions();

        return result;
    }

    private Board BeamSolve()
    {
        columnScoreFunction = BeamColumnScore;

        Console.Error.WriteLine("start beam at " + sw.ElapsedMilliseconds);
        Score = 0;
        for (int i = 0; i < Size; i++)
        {
            int score = BeamColumnScore(i, Tiles);
            if (score == 1000000) columnSolved |= 1 << i;
            Score += score;
        }
        List<Board> boards = new List<Board> { this };
        Board best = this;
        int lastImprove = 0;
        for (int i = 0; sw.ElapsedMilliseconds < 9000 && i < Size * Size * Size; i++)
        {
            boards = boards.SelectMany(b => b.Expand()).OrderByDescending(b => b.Score).Distinct().Take(500).ToList();
            if (boards[0].Score > best.Score) { best = boards[0]; lastImprove = i; }
            if (lastImprove + 10 < i) break;
        }

        return best;
    }

    private static void Swap(int[] array, int x1, int y1, int x2, int y2)
    {
        int tmp = array[(Size + 1) * x1 + y1];
        array[(Size + 1) * x1 + y1] = array[(Size + 1) * x2 + y2];
        array[(Size + 1) * x2 + y2] = tmp;
    }

    public void BuildTrees()
    {
        bfsTurn = new int[Size, Size];
        int[] tiles = (int[])Tiles.Clone();

        List<List<int>> tilesPerColor = Enumerable.Range(0, ColorCount).Select(i => new List<int>()).ToList();
        List<int[]> tileCounts = Enumerable.Range(0, ColorCount).Select(i => new int[16]).ToList();
        int[,] targetColor = new int[Size, Size + 1];
        int[] totals = new int[100];
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                totals[tiles[(Size + 1) * x + y]]++;
                tilesPerColor[tiles[(Size + 1) * x + y] >> 4].Add(tiles[(Size + 1) * x + y]);
                tileCounts[tiles[(Size + 1) * x + y] >> 4][tiles[(Size + 1) * x + y] & 0xf]++;
                targetColor[x, y] = -1;
            }
        }
        tilesPerColor = tilesPerColor.OrderByDescending(c => c.Count).ToList();
        for (int group = 0; group < tilesPerColor.Count; group++)
        {
            int groupIndex = 0;
            if (group % 2 == 0)
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        if (targetColor[x, y] != -1 || groupIndex == tilesPerColor[group].Count) continue;
                        int place = tilesPerColor[group][groupIndex++];
                        tiles[(Size + 1) * x + y] = place;
                        targetColor[x, y] = place >> 4;
                    }
                }
            }
            else
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        if (targetColor[x, y] != -1 || groupIndex == tilesPerColor[group].Count) continue;
                        int place = tilesPerColor[group][groupIndex++];
                        tiles[(Size + 1) * x + y] = place;
                        targetColor[x, y] = place >> 4;
                    }
                }
            }
        }
        tileCounts[tiles[Size] >> 4][tiles[Size] & 0xf]++;
        totals[tiles[Size]]++;

        int score = ComputeLayoutScore(tiles);
        int[] bestColorScores = Enumerable.Range(0, ColorCount).Select(i => 1000000000).ToArray();
        int[] treeLayout = new int[Size * (Size + 1)];
        for (int run = 0; sw.ElapsedMilliseconds < 3000; run++)
        {
            if (ColorCount > 1 && run % 200 == 199)
            {
                bestColorScores = Enumerable.Range(0, ColorCount).Select(i => 1000000000).ToArray();
                if (ComputeLayoutScore(treeLayout) < score)
                {
                    score = ComputeLayoutScore(treeLayout);
                    tiles = (int[])treeLayout.Clone();
                    treeLayout = new int[Size * (Size + 1)];
                }
                targetColor = GenerateColorLayout(tileCounts.Select(c => c.Sum()).ToList());
            }
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    int color = targetColor[x, y];
                    int[] layout = new int[(Size + 1) * Size];
                    int treeScore = BuildTree(x, y, tileCounts[color].ToArray(), targetColor, layout);
                    if (treeScore < bestColorScores[color])
                    {
                        bestColorScores[color] = treeScore;
                        for (int x_ = 0; x_ < Size; x_++)
                        {
                            for (int y_ = 0; y_ < Size; y_++)
                            {
                                if (targetColor[x_, y_] == color) treeLayout[(Size + 1) * x_ + y_] = layout[(Size + 1) * x_ + y_] | (color << 4);
                            }
                        }
                    }
                }
            }
        }

        if (ComputeLayoutScore(treeLayout) < score)
        {
            score = ComputeLayoutScore(treeLayout);
            tiles = (int[])treeLayout.Clone();
            treeLayout = new int[Size * (Size + 1)];
        }

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                totals[tiles[(Size + 1) * x + y]]--;
            }
        }
        tiles[Size] = Enumerable.Range(0, totals.Length).First(i => totals[i] > 0);

        PrintState(tiles);
        score = ComputeLayoutScore(tiles);
        int best = score;
        targetTiles = (int[])tiles.Clone();
#if DEBUG
        Console.Error.WriteLine("score: " + score);
#endif
        int stuck = 0;
        bool stickToColors = true;
        int stuckReset = 10 * Board.Area;

        List<List<Point>> components = FindComponents(tiles);
        List<Point> smallComps = new List<Point>();
        foreach (var group in components.GroupBy(c => targetColor[c[0].X, c[0].Y]))
        {
            var exceptLargest = group.OrderByDescending(g => g.Count).Skip(1);
            smallComps.AddRange(exceptLargest.SelectMany(c => c));
        }
        smallComps.Add(new Point(0, Size));
        bool small = smallComps.Count > 1;

        int mutations = 0;
        while (best > ColorCount)
        {
            mutations++;
            if (sw.ElapsedMilliseconds > 3500) break;
            int x1 = random.Next(Size);
            int y1 = random.Next(Size);
            int x2 = random.Next(Size);
            int y2 = random.Next(Size);
            if (small)
            {
                small &= stuck < 100;
                int a = random.Next(smallComps.Count);
                int b = random.Next(smallComps.Count);
                x1 = smallComps[a].X;
                y1 = smallComps[a].Y;
                x2 = smallComps[b].X;
                y2 = smallComps[b].Y;
            }
            if (!small && stickToColors && targetColor[x1, y1] != targetColor[x2, y2]) continue;
            if (x1 == y1 && x2 == y2)
            {
                x1 = 0; y1 = Size;
                if (!small && stickToColors && (tiles[(Size + 1) * x1 + y1] >> 4) != targetColor[x2, y2]) continue;
            }
            Swap(tiles, x1, y1, x2, y2);
            int tmp = ComputeLayoutScore(tiles);
            if (tmp <= score || ++stuck > stuckReset)
            {
                if (stuck > stuckReset) stickToColors = false;
                if (tmp < score) stuck = 0;
                if (tmp < best)
                {
                    best = tmp;
                    targetTiles = (int[])tiles.Clone();
#if DEBUG
                    Console.Error.WriteLine("score: " + tmp + " @" + sw.ElapsedMilliseconds + "ms / " + bfsCount);
                    PrintState(tiles);
#endif
                }
                score = tmp;
                continue;
            }
            Swap(tiles, x1, y1, x2, y2);
        }
        Console.Error.WriteLine("mutations: " + mutations);
    }

    private int[,] GenerateColorLayout(List<int> counts)
    {
        List<int> backup = counts.ToList();
        while (true)
        {
            counts = backup.ToList();
            int[,] result = new int[Size, Size + 1];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++) result[x, y] = -1;
            }

            int missing = Size * Size;
            while (missing > 0)
            {
                int color = random.Next(ColorCount);
                if (counts[color] == 0) continue;
                int count = counts[color];
                counts[color] = 0;
                int pointsCount = 0;
                for (int x = 0; x < Size && pointsCount == 0; x++)
                {
                    for (int y = 0; y < Size && pointsCount == 0; y++)
                    {
                        if (result[x, y] == -1) points[pointsCount++] = new Point(x, y);
                    }
                }
                while (count > 0 && pointsCount > 0)
                {
                    int index = random.Next(pointsCount);
                    Point p = points[index];
                    int offset = random.Next(4);
                    bool extended = false;
                    for (int dir = 0; dir < 4; dir++)
                    {
                        int d = dir + offset;
                        int nx = p.X + dx[d];
                        int ny = p.Y + dy[d];
                        if (nx < 0 || nx >= Size || ny < 0 || ny >= Size || result[nx, ny] >= 0) continue;

                        extended = true;
                        result[nx, ny] = color;
                        points[pointsCount++] = new Point(nx, ny);
                        count--;
                        missing--;
                        break;
                    }
                    if (!extended) points[index] = points[--pointsCount];
                }
                if (count > 0) break;
            }
            if (missing == 0) return result;
        }
    }

    struct Point
    {
        public int X, Y;

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString() => X + "/" + Y;
    }

    private static int[][] extends = {
        new[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15}, // 0
        new[]{1,3,5,7,9,11,13,15}, // 1
        new[]{2,3,6,7,10,11,14,15}, // 2
        new[]{3,7,11,15}, // 3 = 1|2
        new[]{4,5,6,7,12,13,14,15}, // 4
        new[]{5,7,13,15}, // 5 = 1|4
        new[]{6,7,14,15}, // 6 = 2|4
        new[]{7,15}, // 7 = 1|2|4
        new[]{8,9,10,11,12,13,14,15}, // 8
        new[]{9,11,13,15}, // 9 = 1|8
        new[]{10,11,14,15}, // 10 = 2|8
        new[]{11,15}, // 11 = 1|2|8
        new[]{12,13,14,15}, // 12 = 4|8
        new[]{13,15}, // 13 = 1|4|8
        new[]{14,15}, // 14 = 2|4|8
        new int[]{15} // 15
    };

    static Point[] points = new Point[1000];
    static Point[] retry = new Point[1000];
    private int BuildTree(int x, int y, int[] tileCounts, int[,] targetColor, int[] tiles, bool recursive = true)
    {
        int color = targetColor[x, y];
        tileCounts[0]--;
        points[0] = new Point(x, y);
        int pointsCount = 1;
        int retryCount = 0;
        for (int run = 0; run < 3; run++)
        {
            while (pointsCount > 0)
            {
                int index = random.Next(pointsCount);
                Point p = points[index];
                int offset = random.Next(4);
                bool extended = false;
                bool putBack = false;
                for (int dir = 0; !extended && dir < 4; dir++)
                {
                    int d = dir + offset;
                    int nx = p.X + dx[d];
                    int ny = p.Y + dy[d];
                    if (nx < 0 || nx >= Size || ny < 0 || ny >= Size || tiles[(Size + 1) * nx + ny] > 0 || targetColor[nx, ny] != color) continue;
                    putBack = true;

                    // can I extend current tile in that direction?
                    int extendCapacity = tileCounts[tiles[(Size + 1) * p.X + p.Y] | masks[d]];
                    bool needsAddition = (tiles[(Size + 1) * p.X + p.Y] & masks[d]) == 0;
                    if (run == 2) extendCapacity = extends[tiles[(Size + 1) * p.X + p.Y] | masks[d]].Max(e => tileCounts[e]);
                    if (needsAddition && extendCapacity <= 0) continue;

                    int extendTile = tiles[(Size + 1) * p.X + p.Y] | masks[d];
                    if (needsAddition && tileCounts[extendTile] <= 0) extendTile = extends[tiles[(Size + 1) * p.X + p.Y] | masks[d]].First(e => tileCounts[e] > 0);
                    tileCounts[extendTile]--;

                    // can I connect the partner tile?
                    int connectCapacity = tileCounts[masks[d + 2]];
                    if (run == 2) connectCapacity = extends[masks[d + 2]].Max(e => tileCounts[e]);
                    tileCounts[extendTile]++;
                    if (connectCapacity <= 0) continue;

                    extended = true;
                    points[pointsCount++] = new Point(nx, ny);
                    if (needsAddition)
                    {
                        tileCounts[tiles[(Size + 1) * p.X + p.Y]]++;
                        if (tileCounts[tiles[(Size + 1) * p.X + p.Y] | masks[d]] > 0)
                        {
                            tileCounts[tiles[(Size + 1) * p.X + p.Y] | masks[d]]--;
                            tiles[(Size + 1) * p.X + p.Y] |= masks[d];
                        }
                        else
                        {
                            foreach (int e in extends[tiles[(Size + 1) * p.X + p.Y] | masks[d]])
                            {
                                if (tileCounts[e] > 0)
                                {
                                    tileCounts[e]--;
                                    tiles[(Size + 1) * p.X + p.Y] = e;
                                    break;
                                }
                            }
                        }
                    }
                    if (tileCounts[masks[d + 2]] > 0)
                    {
                        tileCounts[masks[d + 2]]--;
                        tiles[(Size + 1) * nx + ny] = masks[d + 2];
                    }
                    else
                    {
                        foreach (int e in extends[masks[d + 2]])
                        {
                            if (tileCounts[e] > 0)
                            {
                                tileCounts[e]--;
                                tiles[(Size + 1) * nx + ny] = e;
                                break;
                            }
                        }
                    }
                }
                if (!extended)
                {
                    points[index] = points[--pointsCount];
                    if (putBack) retry[retryCount++] = p;
                }
            }
            Point[] tmp = points;
            points = retry;
            retry = tmp;
            pointsCount = retryCount;
            retryCount = 0;
        }
        if (tileCounts[0] == -1)
        {
            tileCounts[0] = 0;
            int t = Enumerable.Range(0, tileCounts.Length).FirstOrDefault(v => tileCounts[v] > 0);
            tiles[(Size + 1) * x + y] = t;
            tileCounts[t]--;
        }

        if (recursive)
        {
            for (int x_ = 0; x_ < Size; x_++)
            {
                for (int y_ = 0; y_ < Size; y_++)
                {
                    if (targetColor[x_, y_] != color || tiles[(Size + 1) * x_ + y_] != 0) continue;
                    BuildTree(x_, y_, tileCounts, targetColor, tiles, false);
                }
            }
        }

        if (recursive)
            return ComputeLayoutScore(tiles);
        return 0;
    }

    private static int[,] bfsTurn;
    private static int bfsCount;
    private static int[] queue = new int[30 * 30 * 2];
    private static int ComputeLayoutScore(int[] tiles)
    {
        bfsCount++;
        int score = 0;
        int[] comps = new int[ColorCount];
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (bfsTurn[x, y] == bfsCount) continue;
                comps[tiles[(Size + 1) * x + y] >> 4]++;
                queue[0] = x;
                queue[1] = y;
                bfsTurn[x, y] = bfsCount;
                int readIndex = 0;
                int writeIndex = 2;
                while (readIndex < writeIndex)
                {
                    int qx = queue[readIndex++];
                    int qy = queue[readIndex++];
                    for (int dir = 0; dir < 4; dir++)
                    {
                        if ((tiles[(Size + 1) * qx + qy] & masks[dir]) == 0) continue;
                        int x_ = qx + dx[dir];
                        int y_ = qy + dy[dir];
                        if (x_ < 0 || x_ >= Size || y_ < 0 || y_ >= Size) { score += Penalty; continue; }
                        if ((tiles[(Size + 1) * x_ + y_] & masks[dir + 2]) == 0) { score += Penalty; continue; }
                        if ((tiles[(Size + 1) * qx + qy] & 0x30) != (tiles[(Size + 1) * x_ + y_] & 0x30))
                        {
                            if (bfsTurn[x_, y_] != bfsCount) score += Penalty;
                            continue;
                        }
                        if (bfsTurn[x_, y_] == bfsCount) continue;
                        queue[writeIndex++] = x_;
                        queue[writeIndex++] = y_;
                        bfsTurn[x_, y_] = bfsCount;
                    }
                }
            }
        }
        foreach (int c in comps) score += c * c;
        return score;
    }

    private List<List<Point>> FindComponents(int[] tiles)
    {
        List<List<Point>> result = new List<List<Point>>();
        bfsCount++;
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (bfsTurn[x, y] == bfsCount) continue;
                List<Point> comp = new List<Point>();
                result.Add(comp);
                queue[0] = x;
                queue[1] = y;
                bfsTurn[x, y] = bfsCount;
                int readIndex = 0;
                int writeIndex = 2;
                while (readIndex < writeIndex)
                {
                    int qx = queue[readIndex++];
                    int qy = queue[readIndex++];
                    comp.Add(new Point(qx, qy));
                    for (int dir = 0; dir < 4; dir++)
                    {
                        if ((tiles[(Size + 1) * qx + qy] & masks[dir]) == 0) continue;
                        int x_ = qx + dx[dir];
                        int y_ = qy + dy[dir];
                        if (x_ < 0 || x_ >= Size || y_ < 0 || y_ >= Size) continue;
                        if ((tiles[(Size + 1) * x_ + y_] & masks[dir + 2]) == 0) continue;
                        if ((tiles[(Size + 1) * qx + qy] & 0x30) != (tiles[(Size + 1) * x_ + y_] & 0x30)) continue;
                        if (bfsTurn[x_, y_] == bfsCount) continue;
                        queue[writeIndex++] = x_;
                        queue[writeIndex++] = y_;
                        bfsTurn[x_, y_] = bfsCount;
                    }
                }
            }
        }
        return result;
    }
    private int hash;
    public override int GetHashCode() => hash;

    public bool Equals(Board board)
    {
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (Tiles[(Size + 1) * x + y] != board.Tiles[(Size + 1) * x + y]) return false;
            }
        }
        return true;
    }
}