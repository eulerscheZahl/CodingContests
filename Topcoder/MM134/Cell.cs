using System.Collections.Generic;

public class Cell
{
    public int X;
    public int Y;
    public bool Tree;
    public bool Barn;
    public Sheep Sheep;
    public Farmer Farmer;
    public bool Empty;
    public char Char;
    public int ID => X * Board.Size + Y;
    public List<Cell> Neighbors = new List<Cell>();
    public Cell(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public void SetChar(char c)
    {
        this.Tree = c == '#';
        this.Barn = c == 'X';
        this.Empty = c == '.';
        this.Char = c;
    }

    private static int[] dx = { 0, 1, 0, -1 };
    private static int[] dy = { 1, 0, -1, 0 };

    public void MakeNeighbors(Cell[,] grid)
    {
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = this.X + dx[dir];
            int y = this.Y + dy[dir];
            if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size && !grid[x, y].Tree) Neighbors.Add(grid[x, y]);
        }
    }

    public override string ToString()
    {
        return $"({X}/{Y})";
    }
}
