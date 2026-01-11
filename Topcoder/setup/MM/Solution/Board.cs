using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    public static int Size;
    public static int Area => Size * Size;
    public static Cell[,] Grid;

    public Board() { }

    public void ReadInitial()
    {
        Size = int.Parse(Console.ReadLine().Split().Last());

        Grid = new Cell[Size, Size];
        int x = 0, y = 0;
        while (y < Size)
        {
            string line = Console.ReadLine();
            if (line.StartsWith("Grid")) line = Console.ReadLine();
            foreach (char c in line)
            {
                Grid[x, y] = new Cell(x, y, c);

                x++;
                if (x == Size)
                {
                    x = 0;
                    y++;
                }
            }
        }
        foreach (Cell cell in Grid) cell.MakeNeighbors();
    }
}