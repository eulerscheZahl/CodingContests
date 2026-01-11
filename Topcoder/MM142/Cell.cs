using System;
using System.Collections.Generic;
using System.Linq;

public enum CellType
{
    EMPTY = '.',
    TREE = 'T',
    PRESENT = 'P',
    BOX = 'b',
    ELF = 'e',
    ELF_WITH_PRESENT = 'E',
    ELF_WITH_BOX = 'B'
}

public class Cell
{
    public int X;
    public int Y;
    public int ID;
    public Elf[] Elf = new Elf[1 + Board.SEARCH_DEPTH];
    public CellType CellType;
    public bool IsEdge;
    public bool HasTree => CellType == CellType.TREE;
    public bool HasBox => CellType == CellType.BOX;
    public bool HasPresent => CellType == CellType.PRESENT;
    public bool IsSolid => HasTree || HasBox || HasPresent;
    public bool IsNotBlocked => !IsSolid;
    public bool IsEmpty => CellType == CellType.EMPTY;
    public Cell[] Neighbors;

    public Cell(int x, int y)
    {
        this.X = x;
        this.Y = y;
        this.ID = x + y * Board.Size;
        IsEdge = X == 0 || Y == 0 || X == Board.Size - 1 || Y == Board.Size - 1;
    }

    private static int[] dx = { 1, 0, -1, 0 };
    private static int[] dy = { 0, 1, 0, -1 };
    public static string[] dir = { "R", "D", "L", "U" };
    public void MakeNeighbors()
    {
        List<Cell> neighbors = new List<Cell>();
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = X + dx[dir];
            int y = Y + dy[dir];
            if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size) neighbors.Add(Board.Grid[x, y]);
        }
        Neighbors = neighbors.ToArray();
    }

    public void SetType(char c)
    {
        CellType = (CellType)c;
        IsEdge &= CellType != CellType.TREE;
    }

    public override string ToString() => X + "/" + Y + ": " + CellType;

    public string GetDir(Cell next)
    {
        for (int i = 0; i < 4; i++)
        {
            int x = X + dx[i];
            int y = Y + dy[i];
            if (next == null && (x < 0 || x >= Board.Size || y < 0 || y >= Board.Size)) return dir[i];
            if (next != null && x == next.X && y == next.Y) return dir[i];
        }
        return "";
    }
}