using System;
using System.Collections.Generic;
using System.Linq;

public class Cell
{
    public int X;
    public int Y;
    public int ID;
    public Cell[] Neighbors = new Cell[4];

    public Cell(int x, int y, char c)
    {
        this.X = x;
        this.Y = y;
        this.ID = x + y * Board.Size;
    }

    private static int[] dx = { 1, 0, -1, 0 };
    private static int[] dy = { 0, 1, 0, -1 };
    public void MakeNeighbors()
    {
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = X + dx[dir];
            int y = Y + dy[dir];
            if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size) Neighbors[dir] = Board.Grid[x, y];
        }
    }

    public override string ToString() => X + "/" + Y;
}   