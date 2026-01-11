using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

internal class Row
{
    public Point[] Points;
    private int[] pointIndices;
    public double Score { get; set; }
    public bool Affected { get; set; }

    private double rowFactor = 1;
    public int rowDir;
    private int n;

    private static double[][] posWeights = {
        new double[] { },
        new double[] { },
        new double[] { },
        new double[] { },
        new double[] { },
        new double[] { 1, 1, 1, 1, 1 },
        new double[] { 1, 1, 0.7, 0.7, 1, 1 },
        new double[] { 1, 1, 1, 0.5, 1, 1, 1 },
        new double[] { 1, 1, 1, 0.7, 0.7, 1, 1, 1 },
        new double[] { 1, 1, 1, 1, 0.5, 1, 1, 1, 1 },
    };
    private double[] posWeight;
    public Row(int startX, int startY, int dir, int length, int n)
    {
        this.rowDir = dir;
        this.n = n;
        posWeight = posWeights[length];
        Points = new Point[length];
        for (int i = 0; i < length; i++)
        {
            Points[i] = new Point(startX, startY);
            startX += dx[dir];
            startY += dy[dir];
        }
        pointIndices = Points.Select(p => p.X * n + p.Y).ToArray();
    }

    public Row(List<Point> points, int n)
    {
        this.Points = points.ToArray();
        posWeight = Enumerable.Range(0, points.Count).Select(i => 1.0).ToArray();
        posWeight[0] = 0.5;
        rowDir = -1;
        this.n = n;
    }

    public static Row Merge(Row r1, Row r2)
    {
        Point intersect = r1.Points.Intersect(r2.Points).First();
        List<Point> union = r1.Points.Union(r2.Points).ToList();
        union.Remove(intersect);
        union.Insert(0, intersect);
        Row row = new Row(0, 0, -1, 0, r1.n);
        row.rowDir = -1;
        row.Points = union.ToArray();
        row.posWeight = Enumerable.Range(0, union.Count).Select(i => 1.0).ToArray();
        row.posWeight[0] = 0.5;
        return row;
    }

    private bool InGrid(int x, int y) => x >= 0 && x < n && y >= 0 && y < n;

    internal bool InGrid()
    {
        return Points.All(p => InGrid(p.X, p.Y));
    }

    private double[] scoreCache = new double[200];
    public double ComputeScore(int[] grid, double[] colorDistribution, Point changed)
    {
        //return ComputeScore(grid, colorDistribution);
        int key = GetKey(grid, changed);
        //if (Points.Contains(new Point(7, 0)) && Points.Contains(new Point(3, 0)) && grid[changed.X * n + changed.Y] == 7) System.Diagnostics.Debugger.Break();
        if (scoreCache[key] == int.MinValue)
            scoreCache[key] = ComputeScore(grid, colorDistribution);
        return scoreCache[key];
    }

    private int GetKey(int[] grid, Point changed)
    {
        int changedIndex = (Points[0].X == Points[1].X) ? Math.Abs(changed.Y - Points[0].Y) : Math.Abs(changed.X - Points[0].X);
        if (rowDir == -1)
        {
            changedIndex = 0;
            while (Points[changedIndex] != changed) changedIndex++;
        }
        int newColor = grid[changed.X * n + changed.Y];
        int key = 10 * changedIndex + newColor;
        return key;
    }

    internal void ResetCache(int[] grid, Point changed)
    {
        int key = GetKey(grid, changed);
        scoreCache[key] = int.MinValue;
    }

    private static double[] color = new double[10];
    private static int[] dx = { 1, 1, 1, 0 };
    private static int[] dy = { 1, 0, -1, -1 };
    private static int[] n4x = { 0, 1, 0, -1 };
    private static int[] n4y = { 1, 0, -1, 0 };
    internal double ComputeScore(int[] grid, double[] colorDistribution)
    {
        Array.Clear(color, 0, color.Length);
        int mostFreqColor = 0;
        double total = 0;
        for (int i = 0; i < Points.Length; i++)
        {
            int cl = grid[pointIndices[i]];
            if (cl != 0)
            {
                color[cl] += posWeight[i];
                if (color[cl] > color[mostFreqColor]) mostFreqColor = cl;
                total += posWeight[i];
            }
        }
        if (color[mostFreqColor] > Points.Length - 1) return 1e7;

        double stepsTaken = 2 * color[mostFreqColor] - total;// - 0.1 * Penalty;
        if (stepsTaken <= 0) return 0;


        int minAccessible = 2;
        for (int i = 0; i < Points.Length; i++)
        {
            int cl = grid[pointIndices[i]];
            if (cl == 0)
            {
                Point p = Points[i];
                int currentAccessible = 0;
                for (int dir = 0; currentAccessible < minAccessible && dir < 4; dir++)
                {
                    if (rowDir == 1 && dir == 1 && i + 1 < Points.Length) continue;
                    if (rowDir == 1 && dir == 3 && i > 0) continue;
                    if (rowDir == 3 && dir == 2 && i + 1 < Points.Length) continue;
                    if (rowDir == 3 && dir == 0 && i > 0) continue;
                    int qx = p.X + n4x[dir];
                    int qy = p.Y + n4y[dir];
                    if (!InGrid(qx, qy)) continue;
                    if (grid[qx * n + qy] == mostFreqColor) currentAccessible = 2;
                    else if (grid[qx * n + qy] == 0) currentAccessible++;
                }
                // TODO: caching breaks here :(
                minAccessible = Math.Min(minAccessible, currentAccessible);
            }
        }
        return rowFactor * (0.6 + 0.2 * minAccessible) * colorDistribution[mostFreqColor] * stepsTaken * stepsTaken * stepsTaken * stepsTaken;
    }

    internal (Row row, int color, int completion) CompletionState(int[] grid)
    {
        Array.Clear(color, 0, color.Length);
        int mostFreqColor = 0;
        double total = 0;
        for (int i = 0; i < Points.Length; i++)
        {
            int cl = grid[pointIndices[i]];
            if (cl != 0)
            {
                color[cl]++;
                if (color[cl] > color[mostFreqColor]) mostFreqColor = cl;
                total++;
            }
        }
        int stepsTaken = (int)(2 * color[mostFreqColor] - total);
        return (this, mostFreqColor, stepsTaken);
    }

    internal void MemorizeScore(int[] grid, double[] colorDistribution)
    {
        Array.Fill(scoreCache, int.MinValue);
        Score = ComputeScore(grid, colorDistribution);
    }

    internal (Point, int) LastMissing(int[] grid)
    {
        int color = -1;
        Point result = new Point(-1, -1);
        foreach (Point p in Points)
        {
            if (grid[p.X * n + p.Y] == 0)
            {
                if (result.X != -1) return (new Point(-1, -1), -1);
                result = p;
            }
            else
            {
                if (color == -1) color = grid[p.X * n + p.Y];
                if (grid[p.X * n + p.Y] != color) return (new Point(-1, -1), -1);
                color = grid[p.X * n + p.Y];
            }
        }
        return (result, color);
    }

    internal bool IsFilled(int[] grid, int color)
    {
        for (int i = 0; i < Points.Length; i++)
        {
            if (grid[Points[i].X * n + Points[i].Y] != color) return false;
        }
        return true;
    }
}