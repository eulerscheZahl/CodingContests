using System;

public struct Point
{
    public int X, Y;
    public int Index => 30*X+Y;
    public Point(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    private static int[,] zobrist = new int[30, 30];
    static Point()
    {
        Random random = new Random(0);
        for (int x = 0; x < 30; x++)
        {
            for (int y = 0; y < 30; y++) zobrist[x, y] = random.Next();
        }
    }

    public override int GetHashCode() => zobrist[X, Y];

    public override string ToString() => $"({X}/{Y})";
}