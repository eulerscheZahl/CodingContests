using System;

public class Move
{
    public Cell From;
    public Cell To;

    public Move(Cell from, Cell to)
    {
        From = from;
        To = to;
    }

    public override string ToString() => From + " => " + To;
    public string Print() => From.Print() + " " + To.Print();

    public int Score(int[,] grid, int[,] d)
    {
        return d[grid[From.X, From.Y], grid[To.X, To.Y]];
    }
}