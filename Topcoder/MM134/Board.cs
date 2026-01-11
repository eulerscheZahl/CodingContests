using System;
using System.Collections.Generic;
using System.Linq;

public class Board
{
    public static int Size;
    public static Cell[,] Grid;
    public static List<Barn> Barns;
    public List<Farmer> Farmers;
    public List<Sheep> Sheep;

    public Board()
    {
        bool init = Grid == null;
        if (init) Grid = new Cell[Board.Size, Board.Size];
        Farmers = new List<Farmer>();
        Sheep = new List<Sheep>();
        Barns = new List<Barn>();
        Queue<char> chars = new Queue<char>();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                while (chars.Count == 0)
                {
                    foreach (char c in Console.ReadLine().Replace(" ", "")) chars.Enqueue(c);
                }
                char ch = chars.Dequeue();
                if (init) Grid[x, y] = new Cell(x, y);
                Grid[x, y].SetChar(ch);
                Grid[x, y].Farmer = null;
                Grid[x, y].Sheep = null;
                if (ch >= 'a' && ch <= 'z') Farmers.Add(new Farmer(Grid[x, y], ch - 'a'));
                if (ch >= 'A' && ch <= 'D') Sheep.Add(new Sheep(Grid[x, y], ch - 'A'));
                if (ch == 'X') Barns.Add(new Barn(Grid[x, y]));
            }
        }
        if (init)
        {
            foreach (Cell cell in Grid) cell.MakeNeighbors(Grid);
        }
        ShearTheSheep.Debug(this);
    }

    private static Cell[] queue = new Cell[30 * 30];
    public static int[,] BFS(List<Cell> cells)
    {
        int[,] dist = new int[Board.Size, Board.Size];
        for (int x = 0; x < Board.Size; x++)
        {
            for (int y = 0; y < Board.Size; y++) dist[x, y] = Board.Size * Board.Size;
        }
        int queueStart = 0;
        int queueEnd = 0;
        foreach (Cell cell in cells) queue[queueEnd++] = cell;
        foreach (Cell cell in cells) dist[cell.X, cell.Y] = 0;
        while (queueStart < queueEnd)
        {
            Cell q = queue[queueStart++];
            foreach (Cell n in q.Neighbors)
            {
                if (dist[n.X, n.Y] < Board.Size * Board.Size) continue;
                if (!n.Empty && n.Farmer == null) continue;
                dist[n.X, n.Y] = 1 + dist[q.X, q.Y];
                queue[queueEnd++] = n;
            }
        }
        return dist;
    }

    public override string ToString()
    {
        string result = "";
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++) result += Grid[x, y].Char;
            result += "\n";
        }
        return result.Trim();
    }
}
