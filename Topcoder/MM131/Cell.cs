using System;
using System.Collections.Generic;

public class Cell : IEquatable<Cell>
{
    public int X;
    public int Y;
    public Tile Tile;
    public List<Cell> Neighbors = new List<Cell>();
    public bool CanPlaceBox;
    public bool Edge;

    public Cell(int x, int y, int tile)
    {
        this.X = x;
        this.Y = y;
        this.Tile = (Tile)tile;
        Edge = X == 0 || X + 1 == Board.Size || Y == 0 || Y + 1 == Board.Size;
        CanPlaceBox = Tile == Tile.EMPTY && !Edge;
    }

    static int[] dx = { 0, 1, 0, -1 };
    static int[] dy = { 1, 0, -1, 0 };
    public void InitNeighbors(Board board)
    {
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = this.X + dx[dir];
            int y = this.Y + dy[dir];
            if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size) Neighbors.Add(board.Grid[x, y]);
        }
    }

    public override string ToString() => $"{Y} {X}";

    public bool Equals(Cell other)
    {
        return this.X == other.X && this.Y == other.Y;
    }

    public override int GetHashCode() => (X << 16) + Y;

    public double CenterDist()
    {
        double dx = Board.Size / 2.0 - X;
        double dy = Board.Size / 2.0 - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public double Dist(Cell cell) => Dist(cell.X, cell.Y);

    public double Dist(double x, double y)
    {
        double dx = X - x;
        double dy = X - y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public int DistToEdge()
    {
        int dx = Math.Min(X, Board.Size - 1 - X);
        int dy = Math.Min(Y, Board.Size - 1 - Y);
        return Math.Min(dx, dy);
    }
}