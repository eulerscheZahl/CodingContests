using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

class Solution
{
    public static Hexagon hex = new Hexagon();
    public const int targetK = 10;
    static void Main(string[] args)
    {
        TryDegenerate(new List<int>());
        Try9WithoutTriangle();
        TryLayouts();
    }

    private static int bestDegenerate = 0;
    private static void TryDegenerate(List<int> list)
    {
        if (list.Count == 5)
        {
            List<int> diffs = new List<int>();
            for (int start = 0; start < list.Count; start++)
            {
                int running = 0;
                for (int end = start; end < list.Count; end++)
                {
                    running += list[end];
                    diffs.Add(running);
                }
            }
            int missing = Enumerable.Range(1, 16).First(m => !diffs.Contains(m));
            if (missing > bestDegenerate)
            {
                bestDegenerate = missing;
                Console.WriteLine(missing - 1 + ": " + string.Join(",", list));
            }
        }
        for (int i = 1; i + list.Sum() <= 15; i++)
        {
            list.Add(i);
            TryDegenerate(list);
            list.RemoveAt(list.Count - 1);
        }
    }

    private static void Try9WithoutTriangle()
    {
        List<int> edges = hex.Triangles.SelectMany(h => h.Edges).Distinct().ToList();
        for (int i = 0; i < (1 << edges.Count); i++)
        {
            List<int> set = new List<int>();
            for (int j = 0; j < edges.Count; j++)
            {
                if ((i >> j) % 2 == 1) set.Add(edges[j]);
            }
            if (set.Count >= 9 && !hex.Triangles.Any(t => t.Edges.All(e => set.Contains(e)))) Console.WriteLine("set: " + string.Join(" ", set));
        }
    }

    private static void TryLayouts()
    {
        bool[] usedSides = new bool[targetK + 1];
        foreach (Triangle triangle in hex.Triangles)
        {
            if (triangle.A != hex.Points[0]) continue;
            for (int a = 1; a < usedSides.Length; a++)
            {
                for (int b = 1; b < usedSides.Length; b++)
                {
                    for (int c = 1; c < usedSides.Length; c++)
                    {
                        if (a == b || a == c || b == c) continue;
                        int max = Math.Max(a, Math.Max(b, c));
                        if (max >= a + b + c - max) continue;
                        usedSides[a] = true;
                        usedSides[b] = true;
                        usedSides[c] = true;
                        triangle.A.X = 0;
                        triangle.A.Y = 0;
                        triangle.A.Constrained = true;
                        triangle.B.X = a;
                        triangle.B.Y = 0;
                        triangle.B.Constrained = true;
                        triangle.ComputePos(b, c);
                        List<Point> constrained = new List<Point> { triangle.A, triangle.B, triangle.C };
                        ConpleteHex(constrained, usedSides);

                        usedSides[a] = false;
                        usedSides[b] = false;
                        usedSides[c] = false;
                        triangle.A.Constrained = false;
                        triangle.B.Constrained = false;
                        triangle.C.Constrained = false;
                    }
                }
            }
        }
    }

    private static void ConpleteHex(List<Point> constrained, bool[] usedSides)
    {
        if (hex.Triangles.Any(t => t.IsLinear())) return;
        if (constrained.Count == 6)
        {
            List<double> distances = new List<double>();
            for (int i = 0; i < constrained.Count; i++)
            {
                for (int j = i + 1; j < constrained.Count; j++)
                {
                    distances.Add(constrained[i].Dist(constrained[j]));
                }
            }
            distances.Sort();
            if (!Enumerable.Range(1, targetK).All(dist => distances.Any(d => Math.Abs(d - dist) < 1e-9))) return;
            //if (hex.Triangles.Any(t => !t.CCW())) return;
            Console.WriteLine(string.Join(" ; ", constrained));
            return;
        }
        foreach (Point point in hex.Points)
        {
            if (point.Constrained) continue;
            for (int c1Index = 0; c1Index < constrained.Count; c1Index++)
            {
                for (int c2Index = c1Index + 1; c2Index < constrained.Count; c2Index++)
                {
                    int a = (int)Math.Round(constrained[c1Index].Dist(constrained[c2Index]));
                    for (int b = 1; b < usedSides.Length; b++)
                    {
                        for (int c = 1; c < usedSides.Length; c++)
                        {
                            if (usedSides[b] || usedSides[c] || b == c) continue;
                            int max = Math.Max(a, Math.Max(b, c));
                            if (max >= a + b + c - max) continue;

                            usedSides[b] = true;
                            usedSides[c] = true;
                            Triangle.ComputePos(constrained[c1Index], constrained[c2Index], point, b, c, true);
                            constrained.Add(point);
                            ConpleteHex(constrained, usedSides);
                            point.Constrained = false;
                            Triangle.ComputePos(constrained[c1Index], constrained[c2Index], point, b, c, false);
                            ConpleteHex(constrained, usedSides);
                            constrained.RemoveAt(constrained.Count - 1);
                            point.Constrained = false;
                            usedSides[b] = false;
                            usedSides[c] = false;
                        }
                    }
                }
            }
        }
    }
}

class Hexagon
{
    public Point[] Points = new Point[6];
    public Triangle[] Triangles = new Triangle[20];
    public Hexagon()
    {
        for (int i = 0; i < 6; i++) Points[i] = new Point { ID = i + 1 };
        int triangleIndex = 0;
        for (int i = 0; i < 6; i++)
        {
            for (int j = i + 1; j < 6; j++)
            {
                for (int k = j + 1; k < 6; k++)
                {
                    Triangles[triangleIndex++] = new Triangle(Points[i], Points[j], Points[k]);
                }
            }
        }
    }
}

class Point
{
    public int ID;
    public bool Constrained;
    public double X, Y;

    internal double Dist(Point point)
    {
        double dx = this.X - point.X;
        double dy = this.Y - point.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public override string ToString()
    {
        return X + "/" + Y;
    }
}

class Triangle
{
    public Point A, B, C;
    public List<int> Edges = new List<int>();

    public Triangle(Point a, Point b, Point c)
    {
        A = a;
        B = b;
        C = c;

        Edges.Add(10 * Math.Min(A.ID, B.ID) + Math.Max(A.ID, B.ID));
        Edges.Add(10 * Math.Min(A.ID, C.ID) + Math.Max(A.ID, C.ID));
        Edges.Add(10 * Math.Min(B.ID, C.ID) + Math.Max(B.ID, C.ID));
    }

    public void ComputePos(int len1, int len2)
    {
        if (!A.Constrained) ComputePos(B, C, A, len1, len2, true);
        if (!B.Constrained) ComputePos(C, A, B, len1, len2, true);
        if (!C.Constrained) ComputePos(A, B, C, len1, len2, true);
    }

    // with known location of pointA an pointB as well as 2 side lengths, compute the location of the 3rd point
    public static void ComputePos(Point pointA, Point pointB, Point pointC, int len1, int len2, bool ccw)
    {
        /* sagemath:
        ax,ay,bx,by,cx,cy,len1,len2=var('ax,ay,bx,by,cx,cy,len1,len2')
        eqs = [
            (ax-cx)^2+(ay-cy)^2 == len1*len1,
            (bx-cx)^2+(by-cy)^2 == len2*len2
        ]
        solve(eqs, cx, cy)
        */

        double ax = pointA.X;
        double ay = pointA.Y;
        double bx = pointB.X;
        double by = pointB.Y;
        double cx1 = 1.0 / 2 * (ax * ax * ax + ax * ay * ay - ax * bx * bx + bx * bx * bx + (ax + bx) * by * by - (ax - bx) * len1 * len1 + (ax - bx) * len2 * len2 - (ax * ax - ay * ay) * bx - 2 * (ax * ay + ay * bx) * by + Math.Sqrt(-ax * ax * ax * ax - 2 * ax * ax * ay * ay - ay * ay * ay * ay + 4 * ax * bx * bx * bx - bx * bx * bx * bx + 4 * ay * by * by * by - by * by * by * by - len1 * len1 * len1 * len1 - len2 * len2 * len2 * len2 - 2 * (3 * ax * ax + ay * ay) * bx * bx - 2 * (ax * ax + 3 * ay * ay - 2 * ax * bx + bx * bx) * by * by + 2 * (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by) * len1 * len1 + 2 * (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by + len1 * len1) * len2 * len2 + 4 * (ax * ax * ax + ax * ay * ay) * bx + 4 * (ax * ax * ay + ay * ay * ay - 2 * ax * ay * bx + ay * bx * bx) * by) * (ay - by)) / (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by);
        double cy1 = 1.0 / 2 * (ax * ax * ay + ay * ay * ay - 2 * ax * ay * bx + ay * bx * bx - ay * by * by + by * by * by - (ay - by) * len1 * len1 + (ay - by) * len2 * len2 + (ax * ax - ay * ay - 2 * ax * bx + bx * bx) * by - Math.Sqrt(-ax * ax * ax * ax - 2 * ax * ax * ay * ay - ay * ay * ay * ay + 4 * ax * bx * bx * bx - bx * bx * bx * bx + 4 * ay * by * by * by - by * by * by * by - len1 * len1 * len1 * len1 - len2 * len2 * len2 * len2 - 2 * (3 * ax * ax + ay * ay) * bx * bx - 2 * (ax * ax + 3 * ay * ay - 2 * ax * bx + bx * bx) * by * by + 2 * (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by) * len1 * len1 + 2 * (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by + len1 * len1) * len2 * len2 + 4 * (ax * ax * ax + ax * ay * ay) * bx + 4 * (ax * ax * ay + ay * ay * ay - 2 * ax * ay * bx + ay * bx * bx) * by) * (ax - bx)) / (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by);
        double cx2 = 1.0 / 2 * (ax * ax * ax + ax * ay * ay - ax * bx * bx + bx * bx * bx + (ax + bx) * by * by - (ax - bx) * len1 * len1 + (ax - bx) * len2 * len2 - (ax * ax - ay * ay) * bx - 2 * (ax * ay + ay * bx) * by - Math.Sqrt(-ax * ax * ax * ax - 2 * ax * ax * ay * ay - ay * ay * ay * ay + 4 * ax * bx * bx * bx - bx * bx * bx * bx + 4 * ay * by * by * by - by * by * by * by - len1 * len1 * len1 * len1 - len2 * len2 * len2 * len2 - 2 * (3 * ax * ax + ay * ay) * bx * bx - 2 * (ax * ax + 3 * ay * ay - 2 * ax * bx + bx * bx) * by * by + 2 * (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by) * len1 * len1 + 2 * (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by + len1 * len1) * len2 * len2 + 4 * (ax * ax * ax + ax * ay * ay) * bx + 4 * (ax * ax * ay + ay * ay * ay - 2 * ax * ay * bx + ay * bx * bx) * by) * (ay - by)) / (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by);
        double cy2 = 1.0 / 2 * (ax * ax * ay + ay * ay * ay - 2 * ax * ay * bx + ay * bx * bx - ay * by * by + by * by * by - (ay - by) * len1 * len1 + (ay - by) * len2 * len2 + (ax * ax - ay * ay - 2 * ax * bx + bx * bx) * by + Math.Sqrt(-ax * ax * ax * ax - 2 * ax * ax * ay * ay - ay * ay * ay * ay + 4 * ax * bx * bx * bx - bx * bx * bx * bx + 4 * ay * by * by * by - by * by * by * by - len1 * len1 * len1 * len1 - len2 * len2 * len2 * len2 - 2 * (3 * ax * ax + ay * ay) * bx * bx - 2 * (ax * ax + 3 * ay * ay - 2 * ax * bx + bx * bx) * by * by + 2 * (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by) * len1 * len1 + 2 * (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by + len1 * len1) * len2 * len2 + 4 * (ax * ax * ax + ax * ay * ay) * bx + 4 * (ax * ax * ay + ay * ay * ay - 2 * ax * ay * bx + ay * bx * bx) * by) * (ax - bx)) / (ax * ax + ay * ay - 2 * ax * bx + bx * bx - 2 * ay * by + by * by);

        double c1Sum = (bx - ax) * (by + ay) + (cx1 - bx) * (cy1 + by) + (ax - cx1) * (ay + cy1);
        if (!ccw) c1Sum = -c1Sum;
        if (c1Sum < 0)
        {
            pointC.X = cx1;
            pointC.Y = cy1;
        }
        else
        {
            pointC.X = cx2;
            pointC.Y = cy2;
        }
        pointC.Constrained = true;
    }

    internal bool CCW()
    {
        double sum = (B.X - A.X) * (B.Y + A.Y) + (C.X - B.X) * (C.Y + B.Y) + (A.X - C.X) * (A.Y + C.Y);
        return sum < 0;
    }

    internal bool IsLinear()
    {
        if (!A.Constrained || !B.Constrained || !C.Constrained) return false;
        double d1 = A.Dist(B);
        double d2 = A.Dist(C);
        double d3 = B.Dist(C);
        double max = Math.Max(d1, Math.Max(d2, d3));
        if (max + 1e-3 > d1 + d2 + d3 - max)
            return true;
        return false;
    }
}
