using System;
using System.Collections.Generic;

public struct Bridge
{
    public Point From;
    public Point To;
    public int Count;

    public int Index => 60 * Math.Min(From.X, To.X) + 2 * Math.Min(From.Y, To.Y) + (From.X == To.X ? 0 : 1);

    public Bridge(Point from, Point to, int count)
    {
        From = from;
        To = to;
        if (from.X > to.X || from.X == to.X && from.Y > to.Y)
        {
            From = to;
            To = from;
        }
        Count = count;
    }

    public override string ToString() => $"{From.Y} {From.X} {To.Y} {To.X} {Count}";

    public bool HasPoint(Point p) => From.Equals(p) || To.Equals(p);

    public Point Partner(Point p) => p.Equals(From) ? To : From;

    public IEnumerable<Point> Middle()
    {
        int dx = Math.Sign(To.X - From.X);
        int dy = Math.Sign(To.Y - From.Y);
        for (int i = 1; ; i++)
        {
            int x = From.X + i * dx;
            int y = From.Y + i * dy;
            if (x == To.X && y == To.Y) break;
            yield return new Point(x, y);
        }
    }

    public override int GetHashCode() => Index;
}