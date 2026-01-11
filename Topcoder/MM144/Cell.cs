using System;
using System.Collections.Generic;
using System.Linq;

public class Cell
{
    public int X;
    public int Y;
    public int ID;
    public Cell[] Neighbors;
    public int[] Dist;
    public bool Flower;
    public bool Wall;
    public bool Hydrant;
    public bool NearHydrant;
    public bool Builder;
    public char C;

    public Cell(int x, int y, char c)
    {
        this.X = x;
        this.Y = y;
        this.ID = x + y * Board.Width;
        this.C = c;
        Wall = c == '#';
        Flower = c == '*';
        Hydrant = c == 'T';
        Builder = c == 'B';
    }

    private static int[] dx = { 1, 0, -1, 0 };
    private static int[] dy = { 0, 1, 0, -1 };
    public void MakeNeighbors()
    {
        List<Cell> neighbors = new List<Cell>();
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = X + dx[dir];
            int y = Y + dy[dir];
            if (x >= 0 && x < Board.Width && y >= 0 && y < Board.Height && !Board.Grid[x, y].Wall) neighbors.Add(Board.Grid[x, y]);
        }
        NearHydrant = Hydrant || neighbors.Any(n => n.Hydrant);
        Neighbors = neighbors.ToArray();
    }

    public void BFS()
    {
        Dist = new int[Board.Area];
        for (int i = 0; i < Dist.Length; i++) Dist[i] = 1000;
        if (Hydrant || Wall) return;
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(this);
        Dist[ID] = 0;
        while (queue.Count > 0)
        {
            Cell c = queue.Dequeue();
            foreach (Cell n in c.Neighbors)
            {
                if (Dist[n.ID] < 1000 || n.Hydrant) continue;
                Dist[n.ID] = 1 + Dist[c.ID];
                queue.Enqueue(n);
            }
        }
    }

    public override string ToString() => X + "/" + Y;
}