using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Cell
{
    public int X;
    public int Y;
    public int ID;
    public int Dir;
    public int Mult;
    public List<Cell> Prev;
    public List<Cell> Next;
    public List<Cell> PrevFull = new List<Cell>();
    public List<Cell> NextFull = new List<Cell>();
    public List<Cell> PrevReduced;
    public List<Cell> NextReduced;

    public Cell(int x, int y, int dir, int mult)
    {
        this.X = x;
        this.Y = y;
        this.Dir = dir;
        this.Mult = mult;
        this.ID = x + y * Board.Size;
        Prev = PrevFull;
        Next = NextFull;
    }

    private static int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
    public void MakeNeighbors()
    {
        int x = X + dx[Dir];
        int y = Y + dy[Dir];
        while (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size)
        {
            Next.Add(Board.Grid[x, y]);
            Board.Grid[x, y].Prev.Add(this);
            x += dx[Dir];
            y += dy[Dir];
        }
    }

    public void Cleanup()
    {
        if (Next.Count == 1 && Next[0].Prev.Count(p => p.Next.Count == 1) == 1)
        {
            foreach (Cell c in Next[0].Prev)
            {
                if (c != this) c.Next.Remove(Next[0]);
            }
            Next[0].Prev = new List<Cell> { this };
        }
        if (Prev.Count == 1 && Prev[0].Next.Count(n => n.Prev.Count == 1) == 1)
        {
            foreach (Cell c in Prev[0].Next)
            {
                if (c != this) c.Prev.Remove(Prev[0]);
            }
            Prev[0].Next = new List<Cell> { this };
        }

        if (Next.Count == 1)
        {
            Cell n = Next[0];
            foreach (Cell p in Prev)
            {
                if (!p.Next.Contains(n)) continue;
                p.Next.Remove(n);
                n.Prev.Remove(p);
            }
        }
        if (Prev.Count == 1)
        {
            Cell p = Prev[0];
            foreach (Cell n in Next)
            {
                if (!n.Prev.Contains(p)) continue;
                p.Next.Remove(n);
                n.Prev.Remove(p);
            }
        }
    }

    public override string ToString() => X + "/" + Y;

    public void ConnectWith(List<Cell> toReset)
    {
        foreach (Cell r in toReset)
        {
            if (Next.Contains(r)) continue;
            int delta = Math.Max(Math.Abs(X - r.X), Math.Abs(Y - r.Y));
            if (X + delta * dx[Dir] == r.X && Y + delta * dy[Dir] == r.Y)
            {
                Next.Add(r);
                r.Prev.Add(this);
            }
        }
    }
}