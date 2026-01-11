using System;
using System.Collections.Generic;
using System.Linq;

public class Cell
{
    public int X, Y, ID;
    public int Color;
    public int Value;
    public Cell[] Neighbors;
    public List<Path> Paths = new List<Path>();

    public Cell(int x, int y, int value, int color)
    {
        X = x;
        Y = y;
        ID = X + Board.Size * Y;
        Color = color;
        Value = value;
    }

    public static int[] dx = { 0, 1, 0, -1 };
    public static int[] dy = { 1, 0, -1, 0 };
    public void InitNeighbors(Cell[,] grid)
    {
        List<Cell> neighbors = new List<Cell>();
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = X + dx[dir];
            int y = Y + dy[dir];
            if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size) neighbors.Add(grid[x, y]);
        }
        this.Neighbors = neighbors.ToArray();
    }

    struct BfsState
    {
        public Cell Cell;
        public int Crossings;
        public BfsState(Cell cell, int crossings)
        {
            this.Cell = cell;
            this.Crossings = crossings;
        }
    }

    public static int BFSCount;
    private static BfsState[] queue = new BfsState[10 * 30 * 30];
    public int[] BFS(int crossings = 0)
    {
        BFSCount++;
        int[] result = new int[Board.Area * (crossings + 1)];
        int readIndex = 0;
        int writeIndex = 1;
        queue[0] = new BfsState(this, 0);
        while (readIndex < writeIndex)
        {
            BfsState q = queue[readIndex++];
            if (q.Cell != this && q.Cell.Value > 0) continue;
            foreach (Cell n in q.Cell.Neighbors)
            {
                int c = q.Crossings + n.Paths.Count;
                if (c > crossings || result[n.ID + Board.Area * c] > 0) continue;
                result[n.ID + Board.Area * c] = 1 + result[q.Cell.ID + Board.Area * q.Crossings];
                for (int i = c + 1; i <= crossings; i++)
                {
                    if (result[n.ID + Board.Area * i] == 0) result[n.ID + Board.Area * i] = result[n.ID + Board.Area * c];
                }
                queue[writeIndex++] = new BfsState(n, c);
            }
        }
        result[this.ID + Board.Area * 0] = 0;
        return result;
    }

    public string Print() => Y + " " + X;

    public override string ToString() => X + "/" + Y;

    public Path BuildPath(Cell toConnect, int[] dist, int dir = -1)
    {
        int c = 0;
        while (dist[toConnect.ID + Board.Area * c] == 0) c++;
        Path path = new Path();
        while (dist[toConnect.ID + Board.Area * c] > 1)
        {
            path.Cells.Add(toConnect);
            int newC = c - toConnect.Paths.Count;
            int off = dir == -1 ? Board.random.Next(toConnect.Neighbors.Length) : dir;
            for (int i = 0; i < toConnect.Neighbors.Length; i++)
            {
                Cell next = toConnect.Neighbors[(i + off) % toConnect.Neighbors.Length];
                if (next.Value == 0 && dist[next.ID + Board.Area * newC] == dist[toConnect.ID + Board.Area * c] - 1)
                {
                    toConnect = next;
                    break;
                }
            }
            c = newC;
        }
        path.Cells.Add(toConnect);
        path.Cells.Add(this);

        path.Apply();
        return path;
    }
}