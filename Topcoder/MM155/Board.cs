using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    public static int Size;
    public static int Area => Size * Size;
    public static Cell[,] Grid;
    public static bool[,] NextLookup;

    public Board() { }

    public void ReadInitial(bool debug)
    {
        Size = int.Parse(Console.ReadLine().Split().Last());
        if (debug) Console.Error.WriteLine(Size);

        Grid = new Cell[Size, Size];
        NextLookup = new bool[Area, Area];
        int x = 0, y = 0;
        while (y < Size)
        {
            string line = Console.ReadLine();
            if (debug) Console.Error.WriteLine(line);
            int[] nums = line.Split().Select(int.Parse).ToArray();
            Grid[x, y] = new Cell(x, y, nums[0], nums[1]);
            x++;
            if (x == Size)
            {
                x = 0;
                y++;
            }
        }
        foreach (Cell cell in Grid) cell.MakeNeighbors();
        foreach (Cell cell in Grid)
        {
            foreach (Cell n in cell.Next) NextLookup[cell.ID, n.ID] = true;
            cell.NextReduced = cell.Next.ToList();
            cell.PrevReduced = cell.Prev.ToList();
            cell.Next = cell.NextReduced;
            cell.Prev = cell.PrevReduced;
        }
        for (int i = 0; i < 10; i++) foreach (Cell cell in Grid) cell.Cleanup();
        HashSet<Cell> tested = new HashSet<Cell>();
        List<List<Cell>> components = new List<List<Cell>>();
        foreach (Cell cell in Grid)
        {
            if (tested.Contains(cell)) continue;
            tested.Add(cell);
            List<Cell> comp = new List<Cell> { cell };
            components.Add(comp);
            Queue<Cell> queue = new Queue<Cell>();
            queue.Enqueue(cell);
            while (queue.Count > 0)
            {
                Cell c = queue.Dequeue();
                foreach (Cell n in c.Next)
                {
                    if (tested.Contains(n)) continue;
                    tested.Add(n);
                    comp.Add(n);
                    queue.Enqueue(n);
                }
            }
        }
        components = components.OrderByDescending(c => c.Count).ToList();
        List<Cell> toReset = components.Skip(1).SelectMany(c => c).ToList();
        foreach (Cell c in toReset)
        {
            c.Prev.Clear();
            c.Next.Clear();
        }
        foreach (Cell c in toReset) c.MakeNeighbors();
        foreach (Cell c in components[0]) c.ConnectWith(toReset);
    }

    public static void ExtendEdges()
    {
        foreach (Cell cell in Grid)
        {
            cell.Next = cell.NextFull;
            cell.Prev = cell.PrevFull;
        }
    }

    public static void ReduceEdges()
    {
        foreach (Cell cell in Grid)
        {
            cell.Next = cell.NextReduced;
            cell.Prev = cell.PrevReduced;
        }
    }
}