using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

public class Ring
{
    enum CellGroup
    {
        OUTER,
        BORDER,
        INNER
    }

    public HashSet<Cell> Presents = new HashSet<Cell>();
    public HashSet<Cell> Border = new HashSet<Cell>();

    private static Random random = new Random(0);
    private static Ring[] bestRings;

    public static List<Ring> GenerateRings(Board board, int time)
    {
        if (bestRings == null)
        {
            bestRings = new Ring[Board.Turns];
            bestRings[0] = new Ring();
        }
        List<Cell> edges = new List<Cell>();
        List<Cell> presents = new List<Cell>();
        foreach (Cell cell in board.Grid)
        {
            if (cell.Edge && cell.Tile != Tile.TREE) edges.Add(cell);
            if (cell.Tile == Tile.PRESENT) presents.Add(cell);
        }

        Stopwatch sw = Stopwatch.StartNew();
        int flows = 0;
        while (sw.ElapsedMilliseconds < time)
        {
            GenerateFlowRing(board, edges, presents);
            flows++;
        }
        //Console.Error.WriteLine("flows: " + flows);

        // remove rings that cost more boxes without benefit
        for (int i = bestRings.Length - 1; i > 0; i--)
        {
            Ring ring = bestRings[i];
            if (ring == null) continue;
            ring.Presents = new HashSet<Cell>(ring.Presents.Where(p => board.Grid[p.X, p.Y].Tile == Tile.PRESENT));
            if (bestRings.Take(i).Any(r => r != null && r.Presents.Count >= ring.Presents.Count)) bestRings[i] = null;
            else ring.Deflate(board, edges);
        }
        return bestRings.Where(r => r != null).ToList();
    }

    public void Deflate(Board board, List<Cell> edges)
    {
        MaxFlow flow = new MaxFlow(board, this, edges);
        flow.InvertDirection();
        this.Border = flow.Border(board, this);
    }

    private static void GenerateFlowRing(Board board, List<Cell> edges, List<Cell> presents)
    {
        Ring ring = new Ring();
        presents = presents.ToList();
        while (presents.Count > 0)
        {
            Cell present = presents[random.Next(presents.Count)];
            if (ring.Presents.Count > 0 && ring.Border.Count > 0 && random.Next(3) == 0)
            {
                double[] presentScore = presents.Select(p => ring.Border.Min(b => b.Dist(p)) + p.CenterDist() + 5 * random.NextDouble()).ToArray();
                Cell[] presentArray = presents.ToArray();
                Array.Sort(presentScore, presentArray);
                present = presentArray[0];
            }
            ring.Presents.Add(present);
            MaxFlow flow = new MaxFlow(board, ring, edges);
            ring.Border = flow.Border(board, ring);
            bool[,] reachable = BFS(edges, new HashSet<Cell>(ring.Border.Where(b => b.Tile != Tile.TREE)));
            foreach (Cell cell in board.Grid)
            {
                if (!reachable[cell.X, cell.Y] && cell.Tile == Tile.PRESENT) ring.Presents.Add(cell);
            }

            presents = presents.Except(ring.Presents).ToList();
            ring.Snapshot();
            if (ring.Presents.Count + 6 < bestRings[ring.Border.Count].Presents.Count) ring = new Ring(bestRings[ring.Border.Count]);
            if (!board.CanDefend(bestRings[ring.Border.Count])) break;
        }
    }

    private static bool[,] BFS(List<Cell> edges, HashSet<Cell> newBorder)
    {
        bool[,] reachable = new bool[Board.Size, Board.Size];
        foreach (Cell c in edges) reachable[c.X, c.Y] = true;

        Queue<Cell> queue = new Queue<Cell>(edges);
        while (queue.Count > 0)
        {
            Cell c = queue.Dequeue();
            foreach (Cell c2 in c.Neighbors)
            {
                if (reachable[c2.X, c2.Y]) continue;
                reachable[c2.X, c2.Y] = true;
                if (c2.Tile == Tile.TREE || newBorder.Contains(c2)) continue;
                queue.Enqueue(c2);
            }
        }

        return reachable;
    }

    public void Smoothen(Board board, List<Cell> edges)
    {
        bool[,] reachable = BFS(edges, this.Border);
        foreach (Cell cell in this.Border.ToList())
        {
            List<Cell> neighbors = cell.Neighbors.Where(n => n.Tile != Tile.TREE && reachable[n.X, n.Y] && !Border.Contains(n)).ToList();
            if (neighbors.Count < 3) continue;
            Console.Error.WriteLine(cell + ": " + string.Join("  -  ", neighbors));
            foreach (Cell neighbor in neighbors)
            {
                HashSet<Cell> newBorder = new HashSet<Cell>(this.Border);
                newBorder.Add(neighbor);
                bool[,] newReachable = BFS(edges, newBorder);
                List<Cell> nneighbors = neighbor.Neighbors.Where(n => n.Tile != Tile.TREE && reachable[n.X, n.Y] && Border.Contains(n)).ToList();
                if (nneighbors.Count > 2) continue;
                Cell toRemove = Border.FirstOrDefault(b => !reachable[b.X, b.Y]);
                if (toRemove != null)
                {
                    Console.Error.WriteLine("reshaping");
                    this.Border.Remove(toRemove);
                    this.Border.Add(neighbor);
                }
            }

        }
    }

    public static void UpdateInner(Board board, List<Cell> edges)
    {
        List<Ring> rings = bestRings.Where(r => r != null).ToList();
        bestRings = new Ring[bestRings.Length];
        foreach (Ring ring in bestRings.Where(r => r != null))
        {
            ring.Presents = new HashSet<Cell>(ring.Presents.Where(p => board.Grid[p.X, p.Y].Tile == Tile.PRESENT));
            ring.Deflate(board, edges);
            ring.Snapshot();
        }
    }

    private void Snapshot()
    {
        if (bestRings[Border.Count] != null && bestRings[Border.Count].Presents.Count >= this.Presents.Count) return;
        bestRings[Border.Count] = new Ring(this);
    }

    public Ring() { }

    private Ring(Ring ring)
    {
        this.Border = new HashSet<Cell>(ring.Border);
        this.Presents = new HashSet<Cell>(ring.Presents);
    }

    public void PrintRing(Board board)
    {
        char[,] grid = new char[Board.Size * 2 + 5, Board.Size];
        for (int y = 0; y < Board.Size; y++)
        {
            for (int x = 0; x < 5; x++) grid[x + Board.Size, y] = ' ';
            for (int x = 0; x < Board.Size; x++)
            {
                grid[x, y] = (char)board.Grid[x, y].Tile;
                grid[x + Board.Size + 5, y] = (char)board.Grid[x, y].Tile;
            }
        }
        foreach (Cell b in Border) grid[b.X + Board.Size + 5, b.Y] = '*';

        Console.Error.WriteLine("Presents: " + Presents.Count);
        Console.Error.WriteLine("Boxes: " + Border.Count);
        for (int y = 0; y < Board.Size; y++)
        {
            for (int x = 0; x < 2 * Board.Size + 5; x++)
            {
                Console.Error.Write(grid[x, y]);
            }
            Console.Error.WriteLine();
        }
        Console.Error.WriteLine();

#if DEBUG
        int cellSize = 128;
        using System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(cellSize * Board.Size, cellSize * Board.Size + 50);
        using System.Drawing.Image tree = System.Drawing.Bitmap.FromFile("images/tree.png");
        using System.Drawing.Image present = System.Drawing.Bitmap.FromFile("images/present.png");
        using System.Drawing.Image box = System.Drawing.Bitmap.FromFile("images/box.png");
        using System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.White);
        g.DrawString($"Boxes: {Border.Count}   Presents: {Presents.Count}", new System.Drawing.Font(new System.Drawing.FontFamily("Arial"), 20), System.Drawing.Brushes.Black, cellSize, cellSize * Board.Size + cellSize / 10);
        foreach (Cell cell in board.Grid)
        {
            if (cell.Tile == Tile.TREE) g.DrawImage(tree, cell.X * cellSize, cell.Y * cellSize);
            if (cell.Tile == Tile.PRESENT) g.DrawImage(present, cell.X * cellSize, cell.Y * cellSize);
            if (Border.Contains(cell)) g.DrawImage(box, cell.X * cellSize + cellSize / 4, cell.Y * cellSize + cellSize / 4, cellSize / 2, cellSize / 2);
        }
        bmp.Save("ring" + Border.Count + ".png");
#endif
    }
}