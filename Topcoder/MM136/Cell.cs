using System;
using System.Collections.Generic;

public class Cell
{
    public int X, Y;
    public List<Cell> Neighbors = new List<Cell>();

    public Cell(int x, int y)
    {
        X = x;
        Y = y;
    }

    private static int[] dx = { -2, -2, -1, -1, 1, 1, 2, 2 };
    private static int[] dy = { -1, 1, -2, 2, -2, 2, -1, 1 };
    public void InitNeighbors(Cell[,] cells)
    {
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = this.X + dx[dir];
            int y = this.Y + dy[dir];
            if (x < 0 || x >= Board.Size || y < 0 || y >= Board.Size) continue;
            Neighbors.Add(cells[x, y]);
            neighbors[x + 30 * y] = true;
        }
    }

    public override string ToString() => $"{X}/{Y}";
    public string Print() => $"{Y} {X}";

    private bool[] neighbors = new bool[900];
    public bool HasNeighbor(Cell cell)
    {
        return neighbors[cell.X + 30 * cell.Y];
    }
}