using System;
using System.Collections.Generic;
using System.Drawing;

public abstract class Strategy
{
    public Random random = new Random(0);
    public int N { get; private set; }
    public int C { get; private set; }
    protected const int EMPTY = 0;

    public virtual void Init(int n, int c)
    {
        this.N = n;
        this.C = c;
    }

    public abstract (Point p1, Point p2, double score) Turn(int[] grid, int[] nextBalls, int elapsedTime);
}