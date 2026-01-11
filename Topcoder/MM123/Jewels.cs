using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Diagnostics;

class State
{
    private static Random random = new Random(0);
    public static int N;
    public static int C;
    public int[,] Grid;
    public double Score;
    public State Parent;
    public List<Point> Action;
    static Stopwatch sw = new Stopwatch();

    public State(int[,] grid)
    {
        this.Grid = grid;
    }

    public State(State parent, List<Point> action)
    {
        this.Parent = parent;
        this.Action = action.ToList();
        this.Grid = (int[,])parent.Grid.Clone();
        this.Score = 3 * parent.Score + Apply(action);
    }

    public State(string filename)
    {
        string[] lines = File.ReadAllLines(filename);
        N = lines[0].Length;
        visitedHor = new int[N, N];
        visitedVer = new int[N, N];
        Grid = new int[N, N];
        List<Point> points = new List<Point>();
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                Grid[x, y] = lines[N - 1 - y][x];
                if (Grid[x, y] == '.') Grid[x, y] = 0;
                points.Add(new Point(x, y));
            }
        }
        double score = RemoveRows(points);
    }

    public double Apply(List<Point> action)
    {
        int tmp = Grid[action[0].X, action[0].Y];
        Grid[action[0].X, action[0].Y] = Grid[action[1].X, action[1].Y];
        Grid[action[1].X, action[1].Y] = tmp;

        return RemoveRows(action);
    }

    public static int[,] visitedHor;
    public static int[,] visitedVer;
    private static int visitCounter;
    private static int[] lowest = new int[16];
    private static int[] dropList = new int[16 * 16 * 4];
    private static int[] actionList = new int[16 * 16 * 4];

    public double RemoveRows(List<Point> action)
    {
        int score = 0;
        int combo = 0;

        for (int i = 0; i < action.Count; i++)
        {
            actionList[2 * i] = action[i].X;
            actionList[2 * i + 1] = action[i].Y;
        }
        int actionCount = 2 * action.Count;


        while (actionCount > 0)
        {
            visitCounter++;
            //Console.Error.WriteLine(score + " x " + combo + " = " + score * combo);
            //PrintGrid();
            for (int i = 0; i < N; i++) lowest[i] = N;
            int dropIndex = 0;
            for (int pointIndex = 0; pointIndex < actionCount; pointIndex += 2)
            {
                int px = actionList[pointIndex];
                int py = actionList[pointIndex + 1];
                int c = Grid[px, py];
                if (c == 0) continue;
                if (visitedHor[px, py] != visitCounter && (
                    px >= 2 && Grid[px - 2, py] == c && Grid[px - 1, py] == c ||
                    px >= 1 && px + 1 < N && Grid[px - 1, py] == c && Grid[px + 1, py] == c ||
                    px + 2 < N && Grid[px + 1, py] == c && Grid[px + 2, py] == c))
                {
                    int hits = 1;
                    dropList[dropIndex++] = px;
                    dropList[dropIndex++] = py;
                    visitedHor[px, py] = visitCounter;
                    for (int x = px + 1; x < N && c == Grid[x, py]; x++) { dropList[dropIndex++] = x; dropList[dropIndex++] = py; hits++; visitedHor[x, py] = visitCounter; }
                    for (int x = px - 1; x >= 0 && c == Grid[x, py]; x--) { dropList[dropIndex++] = x; dropList[dropIndex++] = py; hits++; visitedHor[x, py] = visitCounter; }
                    score += (hits - 2) * (hits - 2);
                }

                if (visitedVer[px, py] != visitCounter && (
                    py >= 2 && Grid[px, py - 2] == c && Grid[px, py - 1] == c ||
                    py >= 1 && py + 1 < N && Grid[px, py - 1] == c && Grid[px, py + 1] == c ||
                    py + 2 < N && Grid[px, py + 1] == c && Grid[px, py + 2] == c))
                {
                    int hits = 1;
                    dropList[dropIndex++] = px;
                    dropList[dropIndex++] = py;
                    visitedVer[px, py] = visitCounter;
                    for (int y = py + 1; y < N && c == Grid[px, y]; y++) { dropList[dropIndex++] = px; dropList[dropIndex++] = y; hits++; visitedVer[px, y] = visitCounter; }
                    for (int y = py - 1; y >= 0 && c == Grid[px, y]; y--) { dropList[dropIndex++] = px; dropList[dropIndex++] = y; hits++; visitedVer[px, y] = visitCounter; }
                    score += (hits - 2) * (hits - 2);
                }
            }
            if (dropIndex == 0) break;
            for (int drop = 0; drop < dropIndex; drop += 2)
            {
                int px = dropList[drop];
                int py = dropList[drop + 1];
                lowest[px] = Math.Min(lowest[px], py);
                Grid[px, py] = 0;
            }

            actionCount = 0;
            for (int x = 0; x < N; x++)
            {
                if (lowest[x] == N) continue;
                int gap = lowest[x];
                int stone = gap;
                while (stone < N && Grid[x, stone] == 0) stone++;
                while (stone < N)
                {
                    Grid[x, gap] = Grid[x, stone];
                    Grid[x, stone] = 0;
                    if (Grid[x, gap] != 0)
                    {
                        actionList[actionCount++] = x;
                        actionList[actionCount++] = gap;
                    }
                    gap++;
                    while (stone < N && Grid[x, stone] == 0) stone++;
                }
            }

            combo++;
        }

        return score * combo;// (combo + 0.5);
    }

    public HashSet<long>[] GenerateTabooList(int[,] mapping, List<Point> action, int n, List<int> alphabet)
    {
        mapping = (int[,])mapping.Clone();
        int id = -1;
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                if (mapping[x, y] == 0) mapping[x, y] = id--;
            }
        }
        action = action.ToList();
        HashSet<long>[] taboo = Enumerable.Range(0, alphabet.Count).Select(i => new HashSet<long>()).ToArray();
        taboo[alphabet.IndexOf(mapping[action[0].X, action[0].Y])].Add((1L << alphabet.IndexOf(mapping[action[0].X, action[0].Y])) | (1L << alphabet.IndexOf(mapping[action[1].X, action[1].Y])));
        taboo[alphabet.IndexOf(mapping[action[1].X, action[1].Y])].Add((1L << alphabet.IndexOf(mapping[action[0].X, action[0].Y])) | (1L << alphabet.IndexOf(mapping[action[1].X, action[1].Y])));
        int tmp = mapping[action[0].X, action[0].Y];
        mapping[action[0].X, action[0].Y] = mapping[action[1].X, action[1].Y];
        mapping[action[1].X, action[1].Y] = tmp;
        while (true)
        {
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n - 2; y++)
                {
                    HashSet<int> set = new HashSet<int>();
                    set.Add(mapping[x, y]);
                    set.Add(mapping[x, y + 1]);
                    set.Add(mapping[x, y + 2]);
                    set.Remove((char)0);
                    if (set.Count > 1 && set.All(s => s > 0))
                    {
                        foreach (int s in set)
                            taboo[alphabet.IndexOf(s)].Add(set.Select(a => 1L << alphabet.IndexOf(a)).Aggregate((a, b) => a | b));
                    }

                    set = new HashSet<int>();
                    set.Add(mapping[y, x]);
                    set.Add(mapping[y + 1, x]);
                    set.Add(mapping[y + 2, x]);
                    set.Remove(0);
                    if (set.Count > 1 && set.All(s => s > 0))
                    {
                        foreach (int s in set)
                            taboo[alphabet.IndexOf(s)].Add(set.Select(a => 1L << alphabet.IndexOf(a)).Aggregate((a, b) => a | b));
                    }
                }
            }


            visitCounter++;
            //Console.Error.WriteLine(score + " x " + combo + " = " + score * combo);
            //PrintGrid();
            int[] lowest = new int[n];
            for (int i = 0; i < n; i++) lowest[i] = n;
            HashSet<Point> toDrop = new HashSet<Point>();
            foreach (Point p in action)
            {
                if (mapping[p.X, p.Y] == 0) continue;
                if (visitedHor[p.X, p.Y] != visitCounter)
                {
                    List<Point> hor = new List<Point> { p };
                    visitedHor[p.X, p.Y] = visitCounter;
                    for (int x = p.X + 1; x < n && mapping[p.X, p.Y] == mapping[x, p.Y]; x++) { hor.Add(new Point(x, p.Y)); visitedHor[x, p.Y] = visitCounter; }
                    for (int x = p.X - 1; x >= 0 && mapping[p.X, p.Y] == mapping[x, p.Y]; x--) { hor.Add(new Point(x, p.Y)); visitedHor[x, p.Y] = visitCounter; }
                    if (hor.Count >= 3)
                    {
                        toDrop.UnionWith(hor);
                    }
                }

                if (visitedVer[p.X, p.Y] != visitCounter)
                {
                    List<Point> ver = new List<Point> { p };
                    visitedVer[p.X, p.Y] = visitCounter;
                    for (int y = p.Y + 1; y < n && mapping[p.X, p.Y] == mapping[p.X, y]; y++) { ver.Add(new Point(p.X, y)); visitedVer[p.X, y] = visitCounter; }
                    for (int y = p.Y - 1; y >= 0 && mapping[p.X, p.Y] == mapping[p.X, y]; y--) { ver.Add(new Point(p.X, y)); visitedVer[p.X, y] = visitCounter; }
                    if (ver.Count >= 3)
                    {
                        toDrop.UnionWith(ver);
                    }
                }
            }
            if (toDrop.Count == 0) break;
            foreach (Point p in toDrop)
            {
                lowest[p.X] = Math.Min(lowest[p.X], p.Y);
                mapping[p.X, p.Y] = 0;
            }

            action.Clear();
            for (int x = 0; x < n; x++)
            {
                if (lowest[x] == n) continue;
                int gap = lowest[x];
                int stone = gap;
                while (stone < n && mapping[x, stone] == 0) stone++;
                while (stone < n)
                {
                    mapping[x, gap] = mapping[x, stone];
                    mapping[x, stone] = 0;
                    if (mapping[x, gap] != 0) action.Add(new Point(x, gap));
                    gap++;
                    while (stone < n && mapping[x, stone] == 0) stone++;
                }
            }
        }

        return taboo;
    }

    public State ExpandRandom()
    {
        List<Point> swap = new List<Point>();
        while (true)
        {
            swap.Add(new Point(random.Next(N), random.Next(N)));
            swap.Add(new Point(random.Next(N), random.Next(N)));
            if (Grid[swap[0].X, swap[0].Y] != Grid[swap[1].X, swap[1].Y]) break;
            swap.Clear();
        }
        return new State(this, swap);
    }

    private bool HasRow(int[,] mapping, Point p)
    {
        int c = mapping[p.X, p.Y];
        if (p.X >= 2 && mapping[p.X - 2, p.Y] == c && mapping[p.X - 1, p.Y] == c) return true;
        if (p.X >= 1 && p.X + 1 < N && mapping[p.X - 1, p.Y] == c && mapping[p.X + 1, p.Y] == c) return true;
        if (p.X + 2 < N && mapping[p.X + 1, p.Y] == c && mapping[p.X + 2, p.Y] == c) return true;
        if (p.Y >= 2 && mapping[p.X, p.Y - 2] == c && mapping[p.X, p.Y - 1] == c) return true;
        if (p.Y >= 1 && p.Y + 1 < N && mapping[p.X, p.Y - 1] == c && mapping[p.X, p.Y + 1] == c) return true;
        if (p.Y + 2 < N && mapping[p.X, p.Y + 1] == c && mapping[p.X, p.Y + 2] == c) return true;
        return false;
    }

    public IEnumerable<State> ExpandAll(int limit = 1000000)
    {
        List<Point> points = GetPoints();
        bool[,,] causesRow = new bool[N, N, 11];
        foreach (Point p in points)
        {
            for (int c = 1; c <= 10; c++)
            {
                causesRow[p.X, p.Y, c] |= p.X >= 2 && Grid[p.X - 2, p.Y] == c && Grid[p.X - 1, p.Y] == c;
                causesRow[p.X, p.Y, c] |= p.X >= 1 && p.X + 1 < N && Grid[p.X - 1, p.Y] == c && Grid[p.X + 1, p.Y] == c;
                causesRow[p.X, p.Y, c] |= p.X + 2 < N && Grid[p.X + 1, p.Y] == c && Grid[p.X + 2, p.Y] == c;
                causesRow[p.X, p.Y, c] |= p.Y >= 2 && Grid[p.X, p.Y - 2] == c && Grid[p.X, p.Y - 1] == c;
                causesRow[p.X, p.Y, c] |= p.Y >= 1 && p.Y + 1 < N && Grid[p.X, p.Y - 1] == c && Grid[p.X, p.Y + 1] == c;
                causesRow[p.X, p.Y, c] |= p.Y + 2 < N && Grid[p.X, p.Y + 1] == c && Grid[p.X, p.Y + 2] == c;
            }
        }

        bool found = false;
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                if (Grid[points[i].X, points[i].Y] == Grid[points[j].X, points[j].Y]) continue;
                if (!causesRow[points[i].X, points[i].Y, Grid[points[j].X, points[j].Y]] && !causesRow[points[j].X, points[j].Y, Grid[points[i].X, points[i].Y]]) continue;
                found = true;
                if (found && --limit <= 0) yield break;
                yield return new State(this, new List<Point> { points[i], points[j] });
            }
        }
        if (!found) yield return ExpandRandom();
    }

    public static List<Point> GetPoints()
    {
        List<Point> points = new List<Point>();
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++) points.Add(new Point(x, y));
        }

        return points;
    }

    public void PrintGrid()
    {
        for (int y = N - 1; y >= 0; y--)
        {
            //for (int x = 0; x < N; x++) Console.Error.Write(Grid[x, y] == 0 ? "." : Grid[x, y].ToString());
            for (int x = 0; x < N; x++)
            {
                if (Grid[x, y] == 0) Console.Error.Write(".");
                else if (Grid[x, y] <= 10) Console.Error.Write(Grid[x, y] % 10);
                else Console.Error.Write((char)(Grid[x, y]));
            }
            if (y > 0) Console.Error.Write("|");
        }
        Console.Error.WriteLine();
    }

    static int exported = 0;
    static string[] layout;
    static List<Point> trigger;

    public static List<Point>[] triggers = new List<Point>[17];
    static string[][] layouts = new string[17][];
    static State()
    {
        triggers[16] = new List<Point> { new Point(4, 0), new Point(11, 0) };
        layouts[16] = "....Ü.Ö....w....|..ö.ÖÜÄ...xvv...|..ääpÖyÄzzwtuv..|..oÜoÄrzyxtftu..|.önomqqyxwseed..|.ämmjpprssgcdcu.|önlkIjiqrgfIcbd.|nlkjHihihfeHbab.|lkIIGIIhgIIGIIa.|IIHHFHHIIHHFHHII|HHGGEGGHHGGEGGHH|GGFFDFFGGFFDFFGG|FFEECEEFFEECEEFF|EEDDADDEEDDBDDEE|DDCCACCDDCCBCCDD|CCAABAACCBBABBCa".Split('|');
        triggers[15] = new List<Point> { new Point(4, 0), new Point(10, 0) };
        layouts[15] = "..p....y...w...|..o.q.yx.xwv...|..nppystxwuuv..|.omnjsrstueed..|.nlliqqrrtcdcv.|omkjIihhgfIcbd.|mljiHhggfeHbab.|kkIIGIIfIIGIIa.|IIHHFHHIHHFHHII|HHGGEGGHGGEGGHH|GGFFDFFGFFDFFGG|FFEECEEFEECEEFF|EEDDADDEDDBDDEE|DDCCACCDCCBCCDD|CCAABAACBBABBCa".Split('|');
        triggers[14] = new List<Point> { new Point(4, 0), new Point(9, 0) };
        layouts[14] = "....r....vw...|..opps...uvw..|..nonrttufuv..|..mnlqssteed..|.olliprqgcdcw.|.mkjHiqhfHcbd.|mkjiGhhgeGbab.|kjHHFHgfHFHHa.|HHGGEGHHGEGGHH|GGFFDFGGFDFFGG|FFEECEFFECEEFF|EEDDADEEDBDDEE|DDCCACDDCBCCDD|CCAABACCBABBCa".Split('|');
        triggers[13] = new List<Point> { new Point(4, 0), new Point(8, 0) };
        layouts[13] = "....n...qr...|..l.mp.qoqr..|..kmhnppeed..|.ljlgmoocdcr.|.kijHgnfHcbd.|kjhhGfgeGbab.|iiHHFHfHFHHa.|HHGGEGHGEGGHH|GGFFDFGFDFFGG|FFEECEFECEEFF|EEDDADEDBDDEE|DDCCACDCBCCDD|CCAABACBABBCa".Split('|');
        triggers[12] = new List<Point> { new Point(3, 0), new Point(7, 0) };
        layouts[12] = "..mmn.ng....|..lkm.heed..|.lkiingcdc..|.kjHhhfHcbd.|ljiGfgeGbab.|jHHFHfHFHHa.|HGGEGHGEGGHH|GFFDFGFDFFGG|FEECEFECEEFF|EDDADEDBDDEE|DCCACDCBCCDD|CAABACBABBCa".Split('|');
        triggers[11] = new List<Point> { new Point(4, 0), new Point(6, 1) };
        layouts[11] = "..jk.llm...|..ijkkdlm..|.jhhfecdc..|.igfedGcb..|ihfeGGFbam.|ggGGFFEGGb.|GGFFEEDFFGG|FFEEDDCEEFF|EEDDCCBDDEE|DDCCBBABBDD|CCAABABCCaa".Split('|');
        triggers[10] = new List<Point> { new Point(4, 0), new Point(6, 1) };
        layouts[10] = "..i.j.....|..hiijkk..|..ggedjc..|.hfedccb..|hgedFFbak.|ffFFEEFFb.|FFEEDDEEFF|EEDDCCDDEE|DDCCBBABDD|CCAABACCaa".Split('|');
        triggers[9] = new List<Point> { new Point(4, 0), new Point(6, 1) };
        layouts[9] = "..h.ij...|..gifij..|..fhdcc..|.hefcbbj.|ggddEEab.|eeEEDDEE.|EEDDCCDDE|DDCCBBABD|CCAABACaa".Split('|');
        triggers[8] = new List<Point> { new Point(4, 0), new Point(5, 1) };
        layouts[8] = "..g.dc..|..ffcb..|.gedbac.|gfdbEEa.|eeEEDDEE|EEDDCCDD|DDCCBABB|CCAABACa".Split('|');
    }

    public static List<List<Point>> cachedPlan = new List<List<Point>>();
    internal List<Point> BuildBoard(int remainingTurns)
    {
        if (cachedPlan.Count > 0)
        {
            List<Point> result = cachedPlan[0];
            cachedPlan.RemoveAt(0);
            return result;
        }

        List<Point> points = GetPoints();
        int[] available = new int[11];
        foreach (int g in Grid) available[g]++;

        //sw.Restart();
        int[,] mapping = null;
        List<List<Point>> symbols = null;
        for (int layoutSize = N; layoutSize >= 8; layoutSize--)
        {
            layout = layouts[layoutSize];
            trigger = triggers[layoutSize];
            List<int> alphabet = string.Join("", layout).Select(c => (int)c).Distinct().ToList();
            alphabet.Remove('.');
            alphabet.Sort();
            int[,] targetLayout = new int[N, N];
            for (int x = 0; x < layoutSize; x++)
            {
                for (int y = 0; y < layoutSize; y++)
                {
                    targetLayout[x, y] = layout[layoutSize - 1 - y][x] == '.' ? 0 : layout[layoutSize - 1 - y][x];
                }
            }
            symbols = alphabet.Select(c => points.Where(p => targetLayout[p.X, p.Y] == c).ToList()).ToList();
            mapping = (int[,])targetLayout.Clone();
            HashSet<long>[] taboo = GenerateTabooList(mapping, trigger, layoutSize, alphabet);
            double score = new State(new State(mapping), trigger.ToList()).Score;
            symbols = alphabet.Select(c => points.Where(p => mapping[p.X, p.Y] == c).ToList()).ToList();
            int lifetime = 10000;
            int take = FindMapping(mapping, symbols, available.ToArray(), points, trigger, taboo, new long[11], 0, ref lifetime);
            for (int i = take; i < symbols.Count; i++)
            {
                foreach (Point p in symbols[i]) mapping[p.X, p.Y] = 0;
            }
            symbols = symbols.Take(take).ToList();
            score = new State(new State(mapping), trigger).Score;
            lifetime = 100000;
            int[] remainingAvailable = available.ToArray();
            long[] colorPicked = new long[11];
            FindMapping(mapping, symbols, remainingAvailable, points, trigger, taboo, colorPicked, 0, ref lifetime);
            int[] symbolAvailable = new int[available.Length];
            foreach (Point p in symbols.SelectMany(s => s))
            {
                symbolAvailable[Grid[p.X, p.Y]]++;
                symbolAvailable[mapping[p.X, p.Y]]--;
            }
            OptimizeMapping(Grid, mapping, symbols, remainingAvailable, symbolAvailable, trigger, score);
            int stepsNeeded = symbols.SelectMany(s => s).Count(p => mapping[p.X, p.Y] != 0 && mapping[p.X, p.Y] != Grid[p.X, p.Y]) * 2 / 3;
            if (stepsNeeded <= remainingTurns) break;
        }
        LogTime("mapping");
        Debug("new mapping");
        DumpState("_mapping" + (++exported) + ".txt", mapping);

        double targetScore = new State(new State(mapping), trigger).Score;
        (double waveScore, List<List<Point>> plan) = MakePlan(symbols, mapping, remainingTurns, targetScore);
        double bestAverage = waveScore / plan.Count;
        List<List<Point>> bestPlan = plan;
        int runs = 0;
        while (waveScore < targetScore || ++runs < 5)
        {
            List<Point> lastSymbol = symbols.Last();
            symbols.Remove(lastSymbol);
            if (symbols.Count == 0) break;
            foreach (Point p in lastSymbol) mapping[p.X, p.Y] = 0;
            targetScore = new State(new State(mapping), trigger).Score;
            (waveScore, plan) = MakePlan(symbols, mapping, remainingTurns, targetScore);
            if (waveScore / plan.Count > bestAverage)
            {
                bestAverage = waveScore / plan.Count;
                bestPlan = plan;
            }
        }

        cachedPlan = bestPlan;
        return BuildBoard(remainingTurns);
    }

    private (double waveScore, List<List<Point>> plan) MakePlan(List<List<Point>> symbols, int[,] mapping, int remainingTurns, double targetScore)
    {
        int totalWalks = (int)(((16 - N) / 4.0 + 1) * ((C - 1) * 4.0 / 9 + 1));
        totalWalks = (1 + totalWalks) / 2;
        (double waveScore, List<List<Point>> plan) result = (0, null);
        for (int randomWalk = 0; randomWalk < totalWalks; randomWalk++)
        {
            List<Point> points = GetPoints();
            for (int j = 0; j < points.Count - 1; j++)
            {
                int idx = random.Next(j, points.Count);
                Point tmp = points[j];
                points[j] = points[idx];
                points[idx] = tmp;
            }
            List<Point> toFix = points.Where(p => mapping[p.X, p.Y] != 0 && mapping[p.X, p.Y] != Grid[p.X, p.Y]).ToList();
            List<Point> keep = points.Where(p => mapping[p.X, p.Y] == 0).ToList();

            int[,] planGrid = (int[,])Grid.Clone();
            List<List<Point>> plan = new List<List<Point>>();
            while (true)
            {
                var v = new State((int[,])planGrid.Clone()).MakeAction(symbols, mapping, remainingTurns, toFix, keep, targetScore);
                plan.Add(v.action);
                int tmp = planGrid[v.action[0].X, v.action[0].Y];
                planGrid[v.action[0].X, v.action[0].Y] = planGrid[v.action[1].X, v.action[1].Y];
                planGrid[v.action[1].X, v.action[1].Y] = tmp;
                foreach (Point p in v.action)
                {
                    if (planGrid[p.X, p.Y] == mapping[p.X, p.Y]) toFix.Remove(p);
                }
                if (v.finish) break;
            }
            //int toFixCount = GetPoints().Count(p => mapping[p.X, p.Y] != 0 && mapping[p.X, p.Y] != Grid[p.X, p.Y]);
            //int colorFix = Enumerable.Range(1, 10).Select(c => Math.Max(GetPoints().Count(p => mapping[p.X, p.Y] == c && Grid[p.X, p.Y] != c),
            //                                                            GetPoints().Count(p => mapping[p.X, p.Y] != 0 && mapping[p.X, p.Y] != c && Grid[p.X, p.Y] == c))).Sum();
            //Console.Error.WriteLine("to fix: " + toFixCount + "    turns: " + plan.Count + "   min turns: " + colorFix / 2);
            (double waveScore, List<List<Point>> plan) turnResult = (new State(planGrid).RemoveRows(GetPoints()), plan);
            if (result.plan == null || turnResult.waveScore / turnResult.plan.Count > result.waveScore / result.plan.Count) result = turnResult;
        }

        return result;
    }

    private (List<Point> action, bool finish) MakeAction(List<List<Point>> symbols, int[,] mapping, int remainingTurns, List<Point> toFix, List<Point> keep, double targetScore)
    {
        if (toFix.Count == 0)
        {
            //sw.Restart();
            State finish = new State(this, trigger);
            State best = new State(new State(mapping), trigger);
            State toReturn = null;
            double returnScore = finish.Score;
            if (finish.Score < targetScore)
            {
                for (int i = 0; i < keep.Count; i++)
                {
                    Point p1 = keep[i];
                    int val1 = Grid[p1.X, p1.Y];
                    Grid[p1.X, p1.Y] = -1;
                    double gapScore = new State(this, trigger).Score;
                    Grid[p1.X, p1.Y] = val1;
                    if (gapScore <= finish.Score) continue;

                    for (int j = 0; j < keep.Count; j++)
                    {
                        Point p2 = keep[j];
                        int val2 = Grid[p2.X, p2.Y];
                        if (val1 == val2) continue;
                        if (HasRow(Grid, p1, p2)) continue;
                        Grid[p2.X, p2.Y] = val1;
                        Grid[p1.X, p1.Y] = val2;
                        State swap = new State(this, trigger);
                        Grid[p2.X, p2.Y] = val2;
                        Grid[p1.X, p1.Y] = val1;
                        if (swap.Score > returnScore)
                        {
                            //this.DumpState("adjustA" + finish.Score + "_" + swap.Score + ".txt", mapping);
                            Debug("ADJUST: " + finish.Score + " => " + swap.Score + "   " + string.Join(" : ", new List<Point> { p1, p2 }));
                            toReturn = new State(this, new List<Point> { p1, p2 });
                            returnScore = swap.Score;
                            //LogTime("adjust");
                            //return (new List<Point> { p1, p2 }, false);
                        }
                    }
                }
            }
            LogTime("adjust");
            if (toReturn != null) return (toReturn.Action, false);

            /*foreach (Point p1 in keep)
            {
                foreach (Point p2 in keep)
                {
                    State state = new State((int[,])Grid.Clone());
                    state.Apply(new List<Point> { p1, p2 });
                    state.Action = new List<Point> { p1, p2 };
                    if (state.Score == 0 && new State(state, trigger).Score > finish.Score + 500) return state;
                }
            }*/

            //Console.Error.WriteLine(finish.Score + " of " + targetScore + " @" + remainingTurns);
            if (finish.Score < new State(new State(mapping), trigger).Score) DumpState(finish.Score + ".txt", mapping);
            mapping = null;
            return (finish.Action, true);
        }
        //if (prevDiff == toFix.Count) DumpState(prevDiff + "_" + (++exported) + ".txt", mapping);

        //sw.Restart();
        // find pair of 2 points that reduces the dist and doesn't trigger early
        foreach (Point p1 in toFix)
        {
            foreach (Point p2 in toFix)
            {
                if (mapping[p1.X, p1.Y] == Grid[p2.X, p2.Y] && mapping[p2.X, p2.Y] == Grid[p1.X, p1.Y])
                {
                    State next = new State(this, new List<Point> { p1, p2 });
                    if (next.Score == 0)
                    {
                        LogTime("swap");
                        return (next.Action, false);
                    }
                }
            }
        }

        // three points circular change
        foreach (Point p1 in toFix)
        {
            foreach (Point p2 in toFix)
            {
                if (mapping[p1.X, p1.Y] != Grid[p2.X, p2.Y] || HasRow(Grid, p1, p2)) continue;
                foreach (Point p3 in toFix)
                {
                    if (mapping[p1.X, p1.Y] == Grid[p2.X, p2.Y] && mapping[p2.X, p2.Y] == Grid[p3.X, p3.Y] && mapping[p3.X, p3.Y] == Grid[p1.X, p1.Y])
                    {
                        Debug("ring-swap");
                        LogTime("swap");
                        return (new List<Point> { p1, p2 }, false);
                    }
                }
            }
        }

        foreach (Point p1 in toFix)
        {
            foreach (Point p2 in toFix)
            {
                if (mapping[p1.X, p1.Y] == Grid[p2.X, p2.Y] || mapping[p2.X, p2.Y] == Grid[p1.X, p1.Y])
                {
                    if (!HasRow(Grid, p1, p2))
                    {
                        LogTime("swap");
                        return (new List<Point> { p1, p2 }, false);
                    }
                }
            }
        }

        List<List<Point>> shallSwap = new List<List<Point>>();
        foreach (Point p1 in keep)
        {
            foreach (Point p2 in toFix)
            {
                if (mapping[p2.X, p2.Y] == Grid[p1.X, p1.Y])
                {
                    if (!HasRow(Grid, p1, p2))
                    {
                        LogTime("swap");
                        return (new List<Point> { p1, p2 }, false);
                    }
                    shallSwap.Add(new List<Point> { p1, p2 });
                }
            }
        }

        foreach (List<Point> pair in shallSwap)
        {
            foreach (Point middle in keep)
            {
                if (middle == pair[0] || middle == pair[1]) continue;
                State next = new State(new State((int[,])Grid.Clone()), new List<Point> { pair[0], middle });
                if (next.Score > 0) continue;
                State next2 = new State(next, new List<Point> { pair[1], middle });
                if (next2.Score > 0) continue;
                LogTime("swap");
                return (next.Action, false);
            }
        }

        Debug("early trigger");
        DumpState("early" + (++exported) + ".txt", mapping);
        mapping = null;

        LogTime("swap");
        return (trigger, true);
    }

    private bool HasRow(int[,] grid, Point p1, Point p2)
    {

        int val1 = grid[p1.X, p1.Y];
        int val2 = grid[p2.X, p2.Y];
        grid[p2.X, p2.Y] = val1;
        grid[p1.X, p1.Y] = val2;
        bool result = HasRow(grid, p1) || HasRow(grid, p2);
        Grid[p2.X, p2.Y] = val2;
        Grid[p1.X, p1.Y] = val1;
        return result;
    }

    private static Dictionary<string, long> times = new Dictionary<string, long>();
    private void LogTime(string category)
    {
        //if (times.ContainsKey(category)) times[category] += sw.ElapsedMilliseconds;
        //else times[category] = sw.ElapsedMilliseconds;
    }

    public static void PrintTimes()
    {
        foreach (var pair in times)
        {
            Console.Error.WriteLine(pair.Key + ": " + pair.Value + "ms");
        }
    }

    private void OptimizeMapping(int[,] grid, int[,] mapping, List<List<Point>> symbols, int[] available, int[] symbolAvailable, List<Point> trigger, double score)
    {
        List<Point> points = GetPoints();

        int prevWrong = symbols.SelectMany(s => s).Count(p => grid[p.X, p.Y] != mapping[p.X, p.Y]);
        for (int run = 0; run < 1000; run++)
        {
            int symbolIndex = random.Next(symbols.Count);
            int newColor = 1 + random.Next(C);
            List<Point> group = symbols[symbolIndex];
            int oldColor = mapping[group[0].X, group[0].Y];
            if (oldColor == newColor) continue;
            if (available[newColor] < group.Count) continue;
            int swapPoints = SetGroupColor(mapping, grid, group, oldColor, newColor, available, symbolAvailable);
            if (swapPoints < 0 ||
                    new State((int[,])mapping.Clone()).Apply(points) > 0 ||
                    score > new State(new State(mapping), trigger).Score)
            {
                SetGroupColor(mapping, grid, group, newColor, oldColor, available, symbolAvailable); // undo
                //DumpState("run" + run + "same.txt", mapping);
            }
            else
            {
                //DumpState("run" + run + "mod.txt", mapping);
            }
        }

        int postWrong = symbols.SelectMany(s => s).Count(p => grid[p.X, p.Y] != mapping[p.X, p.Y]);
        Debug("improved mapping: " + prevWrong + " => " + postWrong);
    }

    private int SetGroupColor(int[,] mapping, int[,] grid, List<Point> group, int oldColor, int newColor, int[] available, int[] symbolAvailable)
    {
        int result = Math.Abs(symbolAvailable[oldColor]) + Math.Abs(symbolAvailable[newColor]);
        foreach (Point p in group)
        {
            if (grid[p.X, p.Y] == oldColor) result -= 2;
            if (grid[p.X, p.Y] == newColor) result += 2;
            mapping[p.X, p.Y] = newColor;
        }
        available[oldColor] += group.Count;
        available[newColor] -= group.Count;
        symbolAvailable[oldColor] += group.Count;
        symbolAvailable[newColor] -= group.Count;
        result -= Math.Abs(symbolAvailable[oldColor]) + Math.Abs(symbolAvailable[newColor]);
        return result;
    }

    private void Debug(string message)
    {
        return;
        Console.Error.WriteLine(message);
    }

    private int FindMapping(int[,] mapping, List<List<Point>> symbols, int[] available, List<Point> points, List<Point> trigger, HashSet<long>[] taboo, long[] colorTaken, int index, ref int lifetime)
    {
        if (lifetime <= 0) return index;
        lifetime--;
        if (index == symbols.Count) return index;
        List<Point> usages = symbols[index];
        // order by: in place, then total available
        List<int> preferences = Enumerable.Range(0, available.Length)
            .OrderByDescending(c => usages.Count(u => Grid[u.X, u.Y] == c))
            .ThenByDescending(c => available[c]).ToList();

        // make sure the color is possible without causing an early trigger
        int initialColor = mapping[usages[0].X, usages[0].Y];
        int best = index;
        foreach (int pref in preferences)
        {
            if (available[pref] < usages.Count) continue;
            long step = colorTaken[pref] | (1L << index);
            if (taboo[index].Any(t => (t & step) == t)) continue;
            foreach (Point u in usages) mapping[u.X, u.Y] = pref;

            available[pref] -= usages.Count;
            colorTaken[pref] ^= 1L << index;
            int tmp = FindMapping(mapping, symbols, available, points, trigger, taboo, colorTaken, index + 1, ref lifetime);
            colorTaken[pref] ^= 1L << index;
            best = Math.Max(best, tmp);
            if (best == symbols.Count) return best;
            available[pref] += usages.Count;
        }
        foreach (Point u in usages) mapping[u.X, u.Y] = initialColor;
        return best;
    }

    public void DumpState(string name, int[,] mapping)
    {
        return;
        List<string> lines = new List<string>();
        lines.Add(N.ToString());

        List<int> nums = new List<int>();
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++) nums.Add(Grid[x, y]);
        }
        lines.Add(nums.Distinct().Count().ToString());
        lines.AddRange(nums.Select(n => n.ToString()));
        File.WriteAllLines(name, lines.ToArray());
        DrawGrid(name.Replace("txt", "png"), mapping);
    }

    public void DrawGrid(string name, int[,] mapping)
    {
        Bitmap bmp = new Bitmap(N * 128, N * 128);
        Bitmap[] jewels = Enumerable.Range(1, 10).Select(i => (Bitmap)Bitmap.FromFile("images/jewel" + i + ".png")).ToArray();
        Graphics g = Graphics.FromImage(bmp);
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                if (Grid[x, y] > 0) g.DrawImage(jewels[Grid[x, y] - 1], x * 128, (N - 1 - y) * 128, 128, 128);
                if (mapping != null && mapping[x, y] > 0 && mapping[x, y] <= 10) g.DrawImage(jewels[mapping[x, y] - 1], x * 128 + 64, (N - 1 - y) * 128 + 64, 64, 64);
                g.DrawString(x + "/" + y, new Font(new FontFamily("Arial"), 20), Brushes.Black, x * 128, (N - 1 - y) * 128);
            }
        }
        g.Dispose();
        foreach (Bitmap j in jewels) j.Dispose();
        bmp.Save(name);
        bmp.Dispose();
    }
}

public class Jewels
{
    static void Main(string[] args)
    {
        //BuildChain(File.ReadAllLines("board.txt"));

        int N = int.Parse(Console.ReadLine());
        State.N = N;
        State.visitedHor = new int[N, N];
        State.visitedVer = new int[N, N];
        State.C = int.Parse(Console.ReadLine());
        int[,] grid = ReadGrid(N);

        for (int turn = 0; turn < 1000; turn++)
        {
            State current = new State(grid);
            //current.PrintGrid();
            List<Point> best = PlayTurn(current, N, State.C, 1000 - turn);
            Console.WriteLine(string.Join(" ", best.Select(p => p.Y + " " + p.X)));
            grid = ReadGrid(N);
            //State.PrintTimes();
            int runtime = int.Parse(Console.ReadLine());
            //Console.Error.WriteLine("time = " + runtime + "    score = " + expectedScore);
        }
    }

    static void BuildChain(string[] layout)
    {
        layout = layout.Take(layout[0].Length).ToArray();
        List<Point> trigger = State.triggers[layout.Length];
        State.N = layout.Length;
        State.visitedHor = new int[State.N, State.N];
        State.visitedVer = new int[State.N, State.N];
        int[] initialHeight = new int[State.N];
        string alphabet = "abcdefghijklmnopqrstuvwxyzÄÖÜäöü";
        List<int> alphabetList = ("ABCDEFGHIJKLM" + alphabet).Select(c => (int)c).ToList();
        int[,] targetLayout = new int[State.N, State.N];
        for (int x = 0; x < State.N; x++)
        {
            for (int y = 0; y < State.N; y++)
            {
                targetLayout[x, y] = layout[State.N - 1 - y][x] == '.' ? 0 : layout[State.N - 1 - y][x];
                if (targetLayout[x, y] != 0) initialHeight[x] = y;
            }
        }
        List<State> states = new List<State> { new State(targetLayout) };
        for (int depth = 1; depth < alphabet.Length; depth++)
        {
            List<State> next = new List<State>();
            foreach (State s in states)
            {
                int prefStart = 0;
                while (!Enumerable.Range(0, State.N).Any(y => s.Grid[prefStart, y] == alphabet[depth - 1])) prefStart++;
                for (int nextStart = Math.Max(0, prefStart - 2); nextStart <= prefStart + 2 && nextStart + 3 < State.N; nextStart++)
                {
                    State s2 = new State((int[,])s.Grid.Clone());
                    bool valid = true;
                    for (int x = nextStart; x <= nextStart + 2; x++)
                    {
                        int y = State.N;
                        while (s2.Grid[x, y - 1] == 0) y--;
                        if (y < State.N) s2.Grid[x, y] = alphabet[depth];
                        else valid = false;
                    }
                    valid &= s2.RemoveRows(State.GetPoints()) == 0;
                    if (valid) next.Add(s2);
                }
            }

            State[] stateArray = next.ToArray();
            double[] scores = next.Select(n => new State(n, trigger).Score).ToArray();
            for (int i = 0; i < next.Count; i++)
            {
                for (int x = 0; x < State.N; x++)
                {
                    int height = State.N;
                    while (next[i].Grid[x, height - 1] == 0) height--;
                    scores[i] -= Math.Pow(height - initialHeight[x], 1.1);
                    //var taboo = next[i].GenerateTabooList(next[i].Grid, trigger, State.N, alphabetList);
                    //scores[i] -= 0.01 * taboo.SelectMany(t => t).Distinct().Count();
                }
            }
            Array.Sort(scores, stateArray);
            states = stateArray.Reverse().ToList();
            if (states.Count > 1000) states = states.Take(1000).ToList();
            if (states.Count == 0) break;

            Console.Error.WriteLine(new State(states[0], trigger).Score);
            states[0].PrintGrid();
        }
    }

    private static List<Point> PlayTurn(State current, int N, int C, int remainingTurns)
    {
        if (State.cachedPlan != null && remainingTurns < State.cachedPlan.Count)
        {
            List<State> next = current.ExpandAll().ToList();
            State best = next[0];
            foreach (State n in next)
            {
                if (n.Score > best.Score) best = n;
            }

            return best.Action;
        }

        return current.BuildBoard(remainingTurns);
    }

    private static Random random = new Random();
    private static List<Point> FindRandomPair(int[,] grid, int n)
    {
        List<Point> result = new List<Point>();
        while (true)
        {
            result.Add(new Point(random.Next(n), random.Next(n)));
            result.Add(new Point(random.Next(n), random.Next(n)));
            if (grid[result[0].X, result[0].Y] != grid[result[1].X, result[1].Y]) return result;
            result.Clear();
        }
    }

    private static int[,] ReadGrid(int n)
    {
        int[,] grid = new int[n, n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++) grid[x, y] = int.Parse(Console.ReadLine());
        }

        // PrintGrid(grid, n);
        return grid;
    }
}