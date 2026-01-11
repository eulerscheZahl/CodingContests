#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

class Solution
{
    static int ReadNumber()
    {
        int num = int.Parse(Console.ReadLine());
#if DEBUG
        //Console.Error.WriteLine(num);
#endif
        return num;
    }

    static int[] ReadLine()
    {
        int[] num = Console.ReadLine().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
#if DEBUG
        //Console.Error.WriteLine(string.Join("\n", num));
#endif
        return num;
    }

    static void TestBeamSize()
    {
        n = 5;
        Random random = new Random();
        while (true)
        {
            List<int> nums = Enumerable.Range(0, n * n).ToList();
            for (int i = 0; i < nums.Count - 1; i++)
            {
                int index = random.Next(i + 1, n * n);
                int tmp = nums[i];
                nums[i] = nums[index];
                nums[index] = tmp;
            }
            int[,] grid = new int[n, n];
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    grid[x, y] = nums[x + n * y];
                }
            }
            p = random.Next(1, 11);
            Console.Error.Write("size = 5: ");
            Plan.randomSize = 5;
            Plan plan = new Plan((int[,])grid.Clone());
            plan.Solve(false);
            int score5 = plan.Score();
            Console.Error.Write(plan.Score());
            Console.Error.Write("\t\tsize = 4: ");
            Plan.randomSize = 4;
            plan = new Plan((int[,])grid.Clone());
            plan.Solve(false);
            int score4 = plan.Score();
            Console.Error.Write(plan.Score());
            Console.Error.WriteLine();

            if (score5 - 20 > score4)
            {
                Console.Error.WriteLine(n + " " + p);
                for (int x = 0; x < n; x++)
                {
                    for (int y = 0; y < n; y++)
                    {
                        Console.Error.Write((grid[x, y] + 1) + " ");
                    }
                    Console.Error.WriteLine();
                }
            }
        }
    }

    static int n, p;
    static void Main(string[] args)
    {
        //TestBeamSize();
        n = ReadNumber();
        p = ReadNumber();
        int[,] grid = new int[n, n];
        for (int i = 0; i < n * n; i++)
            grid[i % n, i / n] = ReadNumber() - 1;

        Plan best = new Plan((int[,])grid.Clone());
        best.Solve(true);
        Console.WriteLine(best.Print());
    }

    class Point : IEquatable<Point>
    {
        public int X, Y;

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString()
        {
            return X + "/" + Y;
        }

        public int Dist(Point p)
        {
            return Math.Abs(X - p.X) + Math.Abs(Y - p.Y);
        }

        public bool Equals(Point other)
        {
            return this.X == other.X && this.Y == other.Y;
        }

        public override int GetHashCode()
        {
            return X * n + Y;
        }

        internal int EdgeDist()
        {
            int dx = Math.Min(X, n - 1 - X);
            int dy = Math.Min(Y, n - 1 - Y);
            return dx + dy;
        }

        internal IEnumerable<Point> Neighbors()
        {
            if (X > 0) yield return new Point(X - 1, Y);
            if (Y > 0) yield return new Point(X, Y - 1);
            if (X + 1 < n) yield return new Point(X + 1, Y);
            if (Y + 1 < n) yield return new Point(X, Y + 1);
        }
    }

    class Rectangle
    {
        public int Xmin, Xmax, Ymin, Ymax;

        public Rectangle(int xmin, int xmax, int ymin, int ymax)
        {
            this.Xmin = xmin;
            this.Xmax = xmax;
            this.Ymin = ymin;
            this.Ymax = ymax;
        }

        public List<Point> GetPoints()
        {
            List<Point> result = new List<Point>();
            for (int x = Xmin; x <= Xmax; x++)
            {
                for (int y = Ymin; y <= Ymax; y++)
                {
                    result.Add(new Point(x, y));
                }
            }
            return result;
        }
    }

    class State : IEquatable<State>
    {
        public double Score;
        public int MoveCost;
        public State Parent;
        public Rotation Rotation;
        public List<State> Childs;
        private int[,] grid;

        public double ScoreBoard()
        {
            double result = 0;
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    int val = grid[x, y];
                    int targetX = val % n;
                    int targetY = val / n;
                    result += Math.Abs(x - targetX) + Math.Abs(y - targetY);
                }
            }
            result *= p;
            return result;
        }

        public State(int[,] grid)
        {
            this.grid = grid;
            Score = ScoreBoard();
        }

        public State(State parent, Rotation rot)
        {
            MoveCost = parent.MoveCost + rot.Cost;
            this.grid = (int[,])parent.grid.Clone();
            rot.Apply(this.grid);
            Score = ScoreBoard() + MoveCost;
            this.Parent = parent;
            this.Rotation = rot;
        }

        public void Expand()
        {
            Childs = new List<State>();
            foreach (Rotation rot in Plan.validRotations) Childs.Add(new State(this, rot));
        }

        internal List<Rotation> GetRotations()
        {
            List<Rotation> result = new List<Rotation>();
            State state = this;
            while (state.Rotation != null)
            {
                result.Add(state.Rotation);
                state = state.Parent;
            }
            result.Reverse();
            return result;
        }

        public bool Equals(State other)
        {
            if (this.MoveCost != other.MoveCost) return false;
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    if (this.grid[x, y] != other.grid[x, y]) return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    sb.Append(grid[x, y] + " ");
                }
            }
            return sb.ToString().GetHashCode();
        }
    }

    class Plan
    {
        private static Random random = new Random();
        public int[,] grid;
        private int MoveScore;
        public int Score() => MoveScore + BoardScore();
        private List<Rotation> rotations = new List<Rotation>();
        public static List<Rotation> validRotations = new List<Rotation>();

        public static void GenerateRotations()
        {
            validRotations.Clear();
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    for (int w = 2; w + Math.Max(x, y) <= n && w <= 3; w++)
                    {
                        validRotations.Add(new Rotation(x, y, w, true));
                        validRotations.Add(new Rotation(x, y, w, false));
                    }
                }
            }
        }

        public Plan(int[,] grid)
        {
            this.grid = grid;
        }

        public static int randomSize = 5;
        public void SolveRandom(int toBeat)
        {
            List<Rotation> rots = validRotations.ToList();
            int maxSize = n;
            int noImprovement = 0;
            int bestScore = (int)1e9;
            int rotCount = 0;
            while (MoveScore < toBeat)
            {
                noImprovement++;
                if (noImprovement > 100) break;
                Rotation rot = rots[random.Next(validRotations.Count)];
                int before = BoardScore(rot);
                rot.Apply(grid);
                int after = rot.Cost + BoardScore(rot);
                if (after <= before || random.Next(400) == 0)
                {
                    rotations.Add(rot);
                    int currentScore = BoardScore();
                    if (currentScore < bestScore)
                    {
                        bestScore = currentScore;
                        rotCount = rotations.Count;
                    }
                    MoveScore += rot.Cost;
                    noImprovement = 0;
                    if (maxSize > 4)
                    {
                        maxSize--;
                        rots = rots.Where(r => r.W <= maxSize).ToList();
                    }
                }
                else
                {
                    rot.Revert(grid);
                }
            }
            while (rotations.Count > rotCount)
            {
                rotations.Last().Revert(grid);
                rotations.RemoveAt(rotations.Count - 1);
            }
        }

        public void SolveBeam()
        {
            int beamSize = 1500;
            State root = new State((int[,])grid.Clone());
            HashSet<State> beam = new HashSet<State> { root };
            State best = root;
            int stuck = 0;
            while (beam.Count > 0 && stuck < 3)
            {
                foreach (State state in beam.ToList())
                {
                    state.Expand();
                    foreach (State s2 in state.Childs)
                    {
                        beam.Add(s2);
                    }
                }
                if (beam.Count > beamSize) beam = new HashSet<State>(beam.OrderBy(b => b.Score).Take(beamSize));
                State currentBest = beam.OrderBy(b => b.Score).First();

                if (currentBest.Score < best.Score) { best = currentBest; stuck = 0; }
                else stuck++;
                beam = new HashSet<State>(beam.Where(b => b.MoveCost < best.Score));
            }

            Plan greedy = null;
            if (randomSize == 5)
            {
                randomSize = 4;
                greedy = new Plan((int[,])grid.Clone());
                greedy.Solve(false);
                randomSize = 5;
                //Console.Error.WriteLine(best.ScoreBoard(false) + " -> " + greedy.Score() + "   ");
            }
            if (greedy != null && greedy.Score() < best.ScoreBoard())
                this.rotations = greedy.rotations;
            else
                this.rotations = best.GetRotations();
            rotations.ForEach(r => r.Apply(grid));
            if (rotations.Count > 0) MoveScore = rotations.Sum(r => r.Cost);
        }

        public int BoardScore(Rotation rot = null)
        {
            int xMin = 0, xMax = n - 1, yMin = 0, yMax = n - 1;
            if (rot != null)
            {
                xMin = rot.X;
                xMax = rot.X + rot.W - 1;
                yMin = rot.Y;
                yMax = rot.Y + rot.W - 1;
            }
            int result = 0;
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    int val = grid[x, y];
                    Point target = targets[val];
                    result += Math.Abs(x - target.X) + Math.Abs(y - target.Y);
                }
            }
            return result * p;
        }

        public static double[] weight;
        public double BoardScoreWeighted(Rotation rot = null)
        {
            int xMin = 0, xMax = n - 1, yMin = 0, yMax = n - 1;
            if (rot != null)
            {
                xMin = rot.X;
                xMax = rot.X + rot.W - 1;
                yMin = rot.Y;
                yMax = rot.Y + rot.W - 1;
            }
            double result = 0;
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    int val = grid[x, y];
                    Point target = targets[val];
                    double dx = x - target.X;
                    double dy = y - target.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    result += Math.Pow(dist, 1.2) * weight[val];
                }
            }
            return result;
        }

        public override string ToString()
        {
            string result = "MoveScore: " + MoveScore + "\n";
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    result += (grid[x, y] + 1).ToString("00") + " ";
                }
                result += Environment.NewLine;
            }
            return result;
        }

        public string Print()
        {
            List<string> result = new List<string> { rotations.Count.ToString() };
            foreach (Rotation r in rotations) result.Add(r.ToString());
            return string.Join(Environment.NewLine, result);
        }

        private Point FindValue(Point p)
        {
            int target = solved[p.X, p.Y];
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    if (grid[x, y] == target) return new Point(x, y);
                }
            }
            return null;
        }

        private int[,] BFS(Point p, bool[,] blocked)
        {
            Queue<Point> points = new Queue<Point>();
            points.Enqueue(p);
            int[,] result = new int[n, n];
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++) result[x, y] = n * n;
            }
            result[p.X, p.Y] = 0;
            while (points.Count > 0)
            {
                p = points.Dequeue();
                foreach (Point p2 in p.Neighbors())
                {
                    if (blocked[p2.X, p2.Y] || result[p2.X, p2.Y] < n * n) continue;
                    result[p2.X, p2.Y] = 1 + result[p.X, p.Y];
                    points.Enqueue(p2);
                }
            }
            return result;
        }

        private void PrintBlocked(bool[,] blocked)
        {
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Console.Error.Write(blocked[x, y] ? '#' : '.');
                }
                Console.Error.WriteLine();
            }
            Console.Error.WriteLine();
            Console.Error.WriteLine();
        }

        private void PrintGrid(int[,] grid)
        {
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Console.Error.Write(grid[x, y].ToString("0000") + " ");
                }
                Console.Error.WriteLine();
            }
        }

        private void SolveReverse(bool[,] blocked)
        {
            if (n < 10) return;
            int frac = 8;
            int size = n / frac * 2;
            if (size < 4) size = 4;

            foreach (Point p in new Rectangle(0, size / 2 - 1, 0, size - 1).GetPoints()) weight[solved[p.X, p.Y]] = 1.8;
            foreach (Point p in new Rectangle(n - size / 2, n - 1, n - size, n - 1).GetPoints()) weight[solved[p.X, p.Y]] = 1.7;
            foreach (Point p in new Rectangle(0, size - 1, n - size / 2, n - 1).GetPoints()) weight[solved[p.X, p.Y]] = 1.6;
            foreach (Point p in new Rectangle(n - size, n - 1, 0, size / 2 - 1).GetPoints()) weight[solved[p.X, p.Y]] = 1.5;

            Rotation topLeft = new Rotation(0, 0, size, true);
            Rotation bottomLeft = new Rotation(0, n - size, size, true);
            Rotation topRight = new Rotation(n - size, 0, size, true);
            Rotation bottomRight = new Rotation(n - size, n - size, size, true);
            Rotation top = new Rotation(size / 2, 0, size, false);
            Rotation left = new Rotation(0, n - size - size / 2, size, false);
            Rotation bottom = new Rotation(n - size - size / 2, n - size, size, false);
            Rotation right = new Rotation(n - size, size / 2, size, false);

            Rectangle toFixTopLeft = new Rectangle(size / 2, size - 1, size / 2, size - 1);
            Rectangle toFixBottomLeft = new Rectangle(size / 2, size - 1, n - size, n - 1 - size / 2);
            Rectangle toFixTopRight = new Rectangle(n - size, n - 1 - size / 2, size / 2, size - 1);
            Rectangle toFixBottomRight = new Rectangle(n - size, n - 1 - size / 2, n - size, n - 1 - size / 2);
            Rectangle toFixTop = new Rectangle(size, size / 2 + size - 1, size / 2, size - 1);
            Rectangle toFixLeft = new Rectangle(size / 2, size - 1, n - size - size / 2, n - 1 - size);
            Rectangle toFixBottom = new Rectangle(n - size - size / 2, n - 1 - size, n - size, n - 1 - size / 2);
            Rectangle toFixRight = new Rectangle(n - size, n - 1 - size / 2, size, size + size / 2 - 1);
            List<Rectangle> fixes = new List<Rectangle> { toFixTopLeft, toFixBottomRight, toFixBottomLeft, toFixTopRight };
            List<Rotation> rots = new List<Rotation> { topLeft, bottomRight, bottomLeft, topRight };

            if (n >= 12)
            {
                foreach (Point p in new Rectangle(size / 2, size + size / 2 - 1, 0, size / 2 - 1).GetPoints()) weight[solved[p.X, p.Y]] = 1.4;
                foreach (Point p in new Rectangle(n - size - size / 2, n - 1 - size / 2, n - size / 2, n - 1).GetPoints()) weight[solved[p.X, p.Y]] = 1.3;
                foreach (Point p in new Rectangle(0, size / 2 - 1, n - size - size / 2, n - 1 - size / 2).GetPoints()) weight[solved[p.X, p.Y]] = 1.2;
                foreach (Point p in new Rectangle(n - size / 2, n - 1, size / 2, size + size / 2 - 1).GetPoints()) weight[solved[p.X, p.Y]] = 1.1;
                rots.AddRange(new[] { top, bottom, left, right });
                fixes.AddRange(new[] { toFixTop, toFixBottom, toFixLeft, toFixRight });
            }
            RemapTargets();

            foreach (Rotation r in rots.Reverse<Rotation>())
            {
                r.Apply(solved); r.Apply(solved);
            }
            for (int index = 0; index < rots.Count; index++)
            {
                Rectangle toFix = fixes[index];
                Rotation rot = rots[index];
                List<Point> points = toFix.GetPoints().OrderBy(p => p.EdgeDist()).ToList();
                for (int i = 0; i < 2; i++)
                {
                    List<Point> sources = points.Select(p => FindValue(p)).ToList();
                    List<Rotation> hill = Hillclimbing(toFix, points, sources);
                    foreach (Point p in points)
                    {
                        Point from = FindValue(p);
                        int[,] dist = BFS(p, blocked);
                        MoveGreedy(from, p, blocked, false, true, 2, 3, 1, dist);
                        blocked[p.X, p.Y] = true;
                    }
                    foreach (Rotation h in hill)
                    {
                        h.Revert(solved);
                        h.Revert(grid);
                        rotations.Add(new Rotation(h.X, h.Y, h.W, !h.Right));
                    }
                    rot.Apply(solved);
                    rot.Apply(grid);
                    rot.Apply(blocked);
                    rotations.Add(rot);
                    RemapTargets();
                }
            }

            //PrintBlocked(blocked);
            //PrintGrid(grid);
        }

        private List<Rotation> Hillclimbing(Rectangle rect, List<Point> points, List<Point> sources)
        {
            List<Rotation> options = new List<Rotation>();
            for (int w = 2; w < 3; w++)
            {
                for (int x = rect.Xmin; x + w - 1 <= rect.Xmax; x++)
                {
                    for (int y = rect.Ymin; y + w - 1 <= rect.Ymax; y++)
                    {
                        options.Add(new Rotation(x, y, w, true));
                        options.Add(new Rotation(x, y, w, false));
                    }
                }
            }

            List<Rotation> result = new List<Rotation>();
            int saving = 0;
            for (int i = 0; i < 1000; i++)
            {
                Rotation test = options[random.Next(options.Count)];
                List<Point> to = points.Where(p => p.X >= test.X && p.X < test.X + test.W && p.X >= test.Y && p.Y < test.Y + test.W).ToList();
                List<Point> from = to.Select(a => sources.First(s => grid[s.X, s.Y] == solved[a.X, a.Y])).ToList();
                int oldCost = Enumerable.Range(0, to.Count).Sum(idx => to[idx].Dist(from[idx]));
                int newCost = Enumerable.Range(0, to.Count).Sum(idx => test.Map(to[idx]).Dist(from[idx])) + test.Cost;
                if (newCost < oldCost + 1)
                {
                    saving += oldCost - newCost;
                    result.Add(test);
                    test.Apply(solved);
                }
            }

            result.Reverse();
            return result;
        }

        private bool MergeEdges(HashSet<Point> points, HashSet<Point> toTest, bool[,] blocked)
        {
            int xMin = toTest.Min(p => p.X);
            int xMax = toTest.Max(p => p.X);
            int yMin = toTest.Min(p => p.Y);
            int yMax = toTest.Max(p => p.Y);
            int pointCount = toTest.Count;

            List<Point> edgePoints = toTest.Where(p => p.X == xMin).ToList();
            if (edgePoints.Count == 2 && edgePoints.All(p => points.Contains(p))) MergeEdge(edgePoints, blocked, toTest);
            edgePoints = toTest.Where(p => p.X == xMax).ToList();
            if (edgePoints.Count == 2 && edgePoints.All(p => points.Contains(p))) MergeEdge(edgePoints, blocked, toTest);
            edgePoints = toTest.Where(p => p.Y == yMin).ToList();
            if (edgePoints.Count == 2 && edgePoints.All(p => points.Contains(p))) MergeEdge(edgePoints, blocked, toTest);
            edgePoints = toTest.Where(p => p.Y == yMax).ToList();
            if (edgePoints.Count == 2 && edgePoints.All(p => points.Contains(p))) MergeEdge(edgePoints, blocked, toTest);

            return toTest.Count < pointCount;
        }

        internal void SolveGreedy()
        {
            bool[,] blocked = new bool[n, n];
            SolveReverse(blocked);
            // PrintGrid();
            Point from = null, to = null;
            Rectangle rectangle = new Rectangle(0, n - 1, 0, n - 1);
            Rectangle center = new Rectangle((n - randomSize) / 2, (n - randomSize) / 2 + randomSize - 1, (n - randomSize) / 2, (n - randomSize) / 2 + randomSize - 1);
            HashSet<Point> centerPoints = new HashSet<Point>(center.GetPoints());
            HashSet<Point> points = new HashSet<Point>(rectangle.GetPoints().Where(p => !blocked[p.X, p.Y]));
            HashSet<Point> toTest = new HashSet<Point>(points);
            points.ExceptWith(center.GetPoints());
            while (MergeEdges(points, toTest, blocked))
            {
                // PrintBlocked(blocked);
            }

            for (int edgeDist = 0; edgeDist < n; edgeDist++)
            {
                HashSet<Point> next = new HashSet<Point>(points.Where(p => !blocked[p.X, p.Y] && p.EdgeDist() == edgeDist));
                while (next.Count > 0)
                {
                    List<Point> cands = next.OrderBy(p => p.Dist(FindValue(p))).ToList();
                    to = cands.First();
                    if (to.Dist(FindValue(to)) > 0) to = cands.Last();
                    if (blocked[to.X, to.Y])
                    {
                        next.Remove(to);
                        continue;
                    }
                    from = FindValue(to);
                    bool success = MoveGreedy(from, to, blocked, false);
                    if (!success) continue;
                    blocked[to.X, to.Y] = true;
                    next.Remove(to);
                    points.Remove(to);
                    toTest.Remove(to);
                    //PrintBlocked(blocked);
                    MoveScore = rotations.Sum(r => r.Cost);

                    //for (int x = 0; x < n; x++)
                    //{
                    //    for (int y = 0; y < n; y++)
                    //    {
                    //        if (blocked[x, y]) continue;
                    //        Point p = new Point(x, y);
                    //        //if (x >= n - randomSize && y >= n - randomSize) continue;
                    //        if (BlockedAbove(p, blocked) && BlockedRightShort(p, blocked)) next.Add(p);
                    //        if (BlockedAboveShort(p, blocked) && BlockedRight(p, blocked)) next.Add(p);
                    //        if (BlockedAbove(p, blocked) && BlockedLeftShort(p, blocked)) next.Add(p);
                    //        if (BlockedAboveShort(p, blocked) && BlockedLeft(p, blocked)) next.Add(p);
                    //        if (BlockedBelow(p, blocked) && BlockedRightShort(p, blocked)) next.Add(p);
                    //        if (BlockedBelowShort(p, blocked) && BlockedRight(p, blocked)) next.Add(p);
                    //        if (BlockedBelow(p, blocked) && BlockedLeftShort(p, blocked)) next.Add(p);
                    //        if (BlockedBelowShort(p, blocked) && BlockedLeft(p, blocked)) next.Add(p);
                    //    }
                    //}

                    //for (int y = 0; y < n; y++)
                    //{
                    //    for (int x = 0; x < n; x++)
                    //    {
                    //        Console.Error.Write(blocked[x, y] ? '#' : '.');
                    //    }
                    //    Console.Error.WriteLine();
                    //}
                    //Console.Error.WriteLine(); Console.Error.WriteLine();

                    MergeEdges(points, toTest, blocked);
                }
            }

            MoveScore = rotations.Sum(r => r.Cost);
        }

        int mergeCount = 0;
        private void MergeEdge(List<Point> points, bool[,] blocked, HashSet<Point> allPoints)
        {
            mergeCount++;
            allPoints.ExceptWith(points);
            int xMin = points.Min(p => p.X);
            int yMin = points.Min(p => p.Y);

            List<Point> sources = points.Select(p => FindValue(p)).ToList();
            List<Point> rotationArea = points.ToList();
            if (points[0].Y == points[1].Y)
            {
                int dy = points[0].Y < n / 2 ? 1 : -1;
                rotationArea.Add(new Point(xMin, points[0].Y + 1 * dy));
                rotationArea.Add(new Point(xMin, points[0].Y + 2 * dy));
                rotationArea.Add(new Point(xMin + 1, points[0].Y + 1 * dy));
                rotationArea.Add(new Point(xMin + 1, points[0].Y + 2 * dy));
            }
            if (points[0].X == points[1].X)
            {
                int dx = points[0].X < n / 2 ? 1 : -1;
                rotationArea.Add(new Point(points[0].X + 1 * dx, yMin));
                rotationArea.Add(new Point(points[0].X + 2 * dx, yMin));
                rotationArea.Add(new Point(points[0].X + 1 * dx, yMin + 1));
                rotationArea.Add(new Point(points[0].X + 2 * dx, yMin + 1));
            }

            int inArea = sources.Count(s => rotationArea.Contains(s));
            if (inArea == 0)
            {
                Point s0 = FindValue(points[0]);
                MoveGreedy(s0, points[1], blocked, false);
                sources = points.Select(p => FindValue(p)).ToList();
                inArea = sources.Count(s => rotationArea.Contains(s));
            }

            if (inArea == 1)
            {
                Point there = sources.First(s => rotationArea.Contains(s));
                Point target = there == sources[0] ? points[1] : points[0];
                MoveGreedy(there, target, blocked, false);
                sources = points.Select(p => FindValue(p)).ToList();
                Point neighbor = target.Neighbors().First(n => !blocked[n.X, n.Y] && !points.Contains(n));
                Point other = sources.First(s => !s.Equals(target));
                blocked[target.X, target.Y] = true;
                MoveGreedy(other, neighbor, blocked, false);
                blocked[target.X, target.Y] = false;
                sources = points.Select(p => FindValue(p)).ToList();
                inArea = sources.Count(s => rotationArea.Contains(s));
            }

            xMin = rotationArea.Min(p => p.X);
            yMin = rotationArea.Min(p => p.Y);
            if (inArea == 2)
            {
                List<Rotation> options = new List<Rotation>
                {
                    new Rotation(xMin, yMin, 2, true),
                    new Rotation(xMin, yMin, 2, false),
                };
                if (points[0].X == points[1].X)
                {
                    options.Add(new Rotation(xMin + 1, yMin, 2, true));
                    options.Add(new Rotation(xMin + 1, yMin, 2, false));
                }
                else
                {
                    options.Add(new Rotation(xMin, yMin + 1, 2, true));
                    options.Add(new Rotation(xMin, yMin + 1, 2, false));
                }

                List<Rotation> solution = Bruteforce(points, sources, options);
                foreach (Rotation rot in solution)
                {
                    rot.Apply(grid);
                    rotations.Add(rot);
                }
            }

            if (!points[0].Equals(FindValue(points[0]))) throw new Exception();
            if (!points[1].Equals(FindValue(points[1]))) throw new Exception();

            foreach (Point p in points) blocked[p.X, p.Y] = true;
        }

        class BruteforceState
        {
            public List<Point> Sources;
            public BruteforceState Parent;
            public Rotation Action;
            public static List<Point> Solution;

            public override string ToString()
            {
                return Sources[0] + " " + Sources[1];
            }

            public IEnumerable<BruteforceState> Expand(List<Rotation> options)
            {
                foreach (Rotation rot in options)
                {
                    List<Point> current = Sources.Select(s => rot.Map(s)).ToList();
                    yield return new BruteforceState { Sources = current, Parent = this, Action = rot };
                }
            }

            public bool IsSolved()
            {
                for (int i = 0; i < Sources.Count; i++)
                {
                    if (!Sources[i].Equals(Solution[i])) return false;
                }
                return true;
            }

            internal List<Rotation> Path()
            {
                List<Rotation> result = new List<Rotation>();
                BruteforceState state = this;
                while (state.Action != null)
                {
                    result.Add(state.Action);
                    state = state.Parent;
                }
                result.Reverse();
                return result;
            }
        }

        private List<Rotation> Bruteforce(List<Point> points, List<Point> sources, List<Rotation> options)
        {
            BruteforceState.Solution = points;
            BruteforceState state = new BruteforceState { Sources = sources };
            if (state.IsSolved()) return new List<Rotation>();
            Queue<BruteforceState> queue = new Queue<BruteforceState>();
            queue.Enqueue(state);
            HashSet<string> visited = new HashSet<string> { state.ToString() };
            while (true)
            {
                BruteforceState s = queue.Dequeue();
                foreach (BruteforceState t in s.Expand(options))
                {
                    if (visited.Contains(t.ToString())) continue;
                    visited.Add(t.ToString());
                    queue.Enqueue(t);
                    if (t.IsSolved()) return t.Path();
                }
            }
        }

        private bool BlockedRight(Point p, bool[,] blocked)
        {
            if (p.X + 1 < n && p.Y > 1 && !blocked[p.X + 1, p.Y - 2]) return false;
            if (p.X + 1 < n && p.Y > 0 && !blocked[p.X + 1, p.Y - 1]) return false;
            if (p.X + 1 < n && !blocked[p.X + 1, p.Y]) return false;
            if (p.X + 1 < n && p.Y + 1 < n && !blocked[p.X + 1, p.Y + 1]) return false;
            if (p.X + 1 < n && p.Y + 2 < n && !blocked[p.X + 1, p.Y + 2]) return false;
            return true;
        }

        private bool BlockedBelow(Point p, bool[,] blocked)
        {
            if (p.Y + 1 < n && p.X > 1 && !blocked[p.X - 2, p.Y + 1]) return false;
            if (p.Y + 1 < n && p.X > 0 && !blocked[p.X - 1, p.Y + 1]) return false;
            if (p.Y + 1 < n && !blocked[p.X, p.Y + 1]) return false;
            if (p.Y + 1 < n && p.X + 1 < n && !blocked[p.X + 1, p.Y + 1]) return false;
            if (p.Y + 1 < n && p.X + 2 < n && !blocked[p.X + 2, p.Y + 1]) return false;
            return true;
        }

        private bool BlockedLeft(Point p, bool[,] blocked)
        {
            if (p.X > 0 && p.Y > 1 && !blocked[p.X - 1, p.Y - 2]) return false;
            if (p.X > 0 && p.Y > 0 && !blocked[p.X - 1, p.Y - 1]) return false;
            if (p.X > 0 && !blocked[p.X - 1, p.Y]) return false;
            if (p.X > 0 && p.Y + 1 < n && !blocked[p.X - 1, p.Y + 1]) return false;
            if (p.X > 0 && p.Y + 2 < n && !blocked[p.X - 1, p.Y + 2]) return false;
            return true;
        }

        private bool BlockedAbove(Point p, bool[,] blocked)
        {
            if (p.Y > 0 && p.X > 1 && !blocked[p.X - 2, p.Y - 1]) return false;
            if (p.Y > 0 && p.X > 0 && !blocked[p.X - 1, p.Y - 1]) return false;
            if (p.Y > 0 && !blocked[p.X, p.Y - 1]) return false;
            if (p.Y > 0 && p.X + 1 < n && !blocked[p.X + 1, p.Y - 1]) return false;
            if (p.Y > 0 && p.X + 2 < n && !blocked[p.X + 2, p.Y - 1]) return false;
            return true;
        }

        private bool BlockedRightShort(Point p, bool[,] blocked)
        {
            if (p.X + 1 < n && !blocked[p.X + 1, p.Y]) return false;
            return true;
        }

        private bool BlockedBelowShort(Point p, bool[,] blocked)
        {
            if (p.Y + 1 < n && !blocked[p.X, p.Y + 1]) return false;
            return true;
        }

        private bool BlockedLeftShort(Point p, bool[,] blocked)
        {
            if (p.X > 0 && !blocked[p.X - 1, p.Y]) return false;
            return true;
        }

        private List<Rotation> GenerateRotations(Point from, Point to, bool[,] blocked, int wMin, int wMax, int raster, int[,] dist = null)
        {
            List<Rotation> candidates = new List<Rotation>();
            for (int w = wMin; w <= wMax; w++)
            {
                if (w % raster != 0) continue;
                for (int x = from.X - w + 1; x < from.X + w; x++)
                {
                    if (x % raster != 0) continue;
                    for (int y = from.Y - w + 1; y < from.Y + w; y++)
                    {
                        if (y % raster != 0) continue;
                        candidates.Add(new Rotation(x, y, w, true));
                        candidates.Add(new Rotation(x, y, w, false));
                    }
                }
            }
            candidates = candidates.Where(c => c.InGrid() && !c.Collide(blocked)).ToList();
            if (dist == null) candidates = candidates.Where(c => from.Dist(to) - c.W + 1 == c.Map(from).Dist(to)).ToList();
            else candidates = candidates.Where(c => dist[from.X, from.Y] > dist[c.Map(from).X, c.Map(from).Y]).ToList();
            candidates = candidates.OrderBy(c => c.ExpectedCost(this)).ToList();
            if (candidates.Count > 10) candidates = candidates.Take(10).ToList();
            return candidates;
        }

        private bool MoveGreedy(Point from, Point to, bool[,] blocked, bool allowCancel, bool ply2 = true, int wMin = 2, int wMax = 3, int raster = 1, int[,] dist = null)
        {
            bool moved = false;
            while (!from.Equals(to))
            {
                List<Rotation> candidates = GenerateRotations(from, to, blocked, wMin, wMax, raster, dist);
                Rotation bestRot = candidates[0];
                if (allowCancel && moved && bestRot.ExpectedCost(this) > 0 && random.Next(5) == 0) return false;
                if (ply2 && !bestRot.Map(from).Equals(to))
                {
                    double bestCost = 1e9;
                    foreach (Rotation rot1 in candidates)
                    {
                        if (rot1.Map(from).Equals(to)) continue;
                        double currentCost = rot1.ExpectedCost(this);
                        rot1.Apply(grid);
                        Point from2 = rot1.Map(from);
                        List<Rotation> candidates2 = GenerateRotations(from2, to, blocked, wMin, wMax, raster, dist);
                        if (candidates2.Count == 0) continue;
                        currentCost += candidates2[0].ExpectedCost(this);
                        if (currentCost < bestCost)
                        {
                            bestCost = currentCost;
                            bestRot = rot1;
                        }
                        rot1.Revert(grid);
                    }
                }

                from = bestRot.Map(from);
                bestRot.Apply(grid);
                rotations.Add(bestRot);
                moved = true;
            }
            return true;
        }
        private void GenerateTargets()
        {
            solved = new int[n, n];
            targets = new Point[n * n];
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    solved[x, y] = x + n * y;
                }
            }
            RemapTargets();
        }

        private void RemapTargets()
        {
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    targets[solved[x, y]] = new Point(x, y);
                }
            }
        }

        static int[,] solved;
        static Point[] targets;
        public void Solve(bool random)
        {
            GenerateTargets();
            weight = new double[n * n];
            foreach (Point p in new Rectangle(0, n - 1, 0, n - 1).GetPoints()) weight[solved[p.X, p.Y]] = Math.Pow(0.95, p.EdgeDist());


            if (randomSize > n) randomSize = n;
            Stopwatch sw = Stopwatch.StartNew();
            SolveGreedy();
#if DEBUG
            Console.Error.WriteLine("greedy score = " + Score());
            //Console.Error.WriteLine(best.Print());
            //Console.Error.WriteLine(best);
#endif
            int[,] sub = new int[randomSize, randomSize];
            Rectangle center = new Rectangle((n - randomSize) / 2, (n - randomSize) / 2 + randomSize - 1, (n - randomSize) / 2, (n - randomSize) / 2 + randomSize - 1);
            List<int> numbers = center.GetPoints().Select(p => grid[p.X, p.Y]).OrderBy(x => x).ToList();
            for (int x = 0; x < randomSize; x++)
            {
                for (int y = 0; y < randomSize; y++)
                {
                    int val = grid[center.Xmin + x, center.Ymin + y];
                    sub[x, y] = numbers.IndexOf(val);
                }
            }

            n = randomSize;
            Plan.GenerateRotations();
            GenerateTargets();

            int runs = 0;
            Plan best = new Plan((int[,])sub.Clone());
            best.SolveBeam();
            int toBeat = best.Score();
            int maxTime = 0 * 8_000;
            if (!random) maxTime = 0;
#if DEBUG
            maxTime = 0;
            Console.Error.WriteLine("beam score: " + best.Score());
#endif
            while (sw.ElapsedMilliseconds < maxTime)
            {
                runs++;
                Plan plan = new Plan((int[,])sub.Clone());
                plan.SolveRandom(toBeat);
                if (best == null || toBeat > plan.Score())
                {
                    best = plan;
                    toBeat = best.Score();
                }
            }
#if DEBUG
            Console.Error.WriteLine("random score: " + best.Score());
#endif

            n = grid.GetLength(1);
            if (best != null)
            {
                foreach (Rotation rot in best.rotations)
                {
                    Rotation transpose = new Rotation(rot.X + center.Xmin, rot.Y + center.Ymin, rot.W, rot.Right);
                    transpose.Apply(grid);
                    rotations.Add(transpose);
                    MoveScore += transpose.Cost;
                }
            }
            GenerateTargets();
#if DEBUG
            Console.Error.WriteLine("runs: " + runs);
            Console.Error.WriteLine("score = " + Score());
            Console.Error.WriteLine("move cost = " + MoveScore);
            //Console.Error.WriteLine(best.Print());
            //Console.Error.WriteLine(best);
#endif
        }
    }

    class Rotation
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public bool Right { get; private set; }
        public readonly int Cost;

        public Rotation(int x, int y, int w, bool right)
        {
            this.X = x;
            this.Y = y;
            this.W = w;
            this.Right = right;
            Cost = (int)Math.Pow(w - 1, 1.5);
        }

        public Point Map(Point point)
        {
            if (point.X < X || point.X >= X + W || point.Y < Y || point.Y >= Y + W) return point;
            // transpose
            int y = point.X - this.X;
            int x = point.Y - this.Y;
            // mirror
            if (Right) x = W - 1 - x;
            else y = W - 1 - y;
            return new Point(X + x, Y + y);
        }

        public bool Collide(bool[,] blocked)
        {
            for (int x = X; x < X + W; x++)
            {
                for (int y = Y; y < Y + W; y++)
                {
                    if (blocked[x, y]) return true;
                }
            }
            return false;
        }

        public bool InGrid()
        {
            return X >= 0 && X + W <= n && Y >= 0 && Y + W <= n;
        }

        public void Apply<T>(T[,] grid)
        {
            if (Right) RotateRight(grid);
            else RotateLeft(grid);
        }

        public void Revert<T>(T[,] grid)
        {
            if (Right) RotateLeft(grid);
            else RotateRight(grid);
        }

        private void RotateRight<T>(T[,] grid)
        {
            for (int x = 0; x < W / 2; x++)
            {
                for (int y = x; y < W - x - 1; y++)
                {
                    T temp = grid[x + X, y + Y];
                    grid[x + X, y + Y] = grid[y + X, W - 1 - x + Y];
                    grid[y + X, W - 1 - x + Y] = grid[W - 1 - x + X, W - 1 - y + Y];
                    grid[W - 1 - x + X, W - 1 - y + Y] = grid[W - 1 - y + X, x + Y];
                    grid[W - 1 - y + X, x + Y] = temp;
                }
            }
        }

        private void RotateLeft<T>(T[,] grid)
        {
            for (int x = 0; x < W / 2; x++)
            {
                for (int y = x; y < W - x - 1; y++)
                {
                    T temp = grid[W - 1 - y + X, x + Y];
                    grid[W - 1 - y + X, x + Y] = grid[W - 1 - x + X, W - 1 - y + Y];
                    grid[W - 1 - x + X, W - 1 - y + Y] = grid[y + X, W - 1 - x + Y];
                    grid[y + X, W - 1 - x + Y] = grid[x + X, y + Y];
                    grid[x + X, y + Y] = temp;
                }
            }
        }

        public override string ToString()
        {
            string dir = Right ? "R" : "L";
            return $"{Y} {X} {W} {dir}";
        }

        internal double ExpectedCost(Plan plan)
        {
            double before = plan.BoardScoreWeighted(this);
            Apply(plan.grid);
            double after = plan.BoardScoreWeighted(this);
            Revert(plan.grid);
            double result = after - before + Cost;
            return result / Cost;
        }
    }
}
