using System;

public class Point : IEquatable<Point>
{
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public double Dist2(Point p)
    {
        int dx = X - p.X;
        int dy = Y - p.Y;
        return dx * dx + dy * dy;
    }

    public double Dist(Point p) => Math.Sqrt(Dist2(p));

    public override int GetHashCode() => (X << 16) + Y;
    public bool Equals(Point p) => X == p.X && Y == p.Y;

    public override string ToString() => X + "/" + Y;

    public bool InRange(Point p, int range)
    {
        int dx = X - p.X;
        if (Math.Abs(dx) > range) return false;
        int dy = Y - p.Y;
        if (Math.Abs(dy) > range) return false;
        return dx * dx + dy * dy <= range * range;
    }
}
