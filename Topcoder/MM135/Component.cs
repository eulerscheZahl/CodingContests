using System;
using System.Linq;
using System.Collections.Generic;

public class Component : IEquatable<Component>
{
    public int Score;
    public List<Bridge> Bridges = new List<Bridge>();
    public HashSet<Point> Covered;
    public HashSet<Point> IslandCovered;
    private int hash;
    private static int idCounter;
    private int id;


    public Component() { this.id = idCounter++; }
    public Component(Component component, Point open)
    {
        this.id = idCounter++;
        this.Bridges = component.Bridges.ToList();
        this.Covered = new HashSet<Point>();
        this.IslandCovered = new HashSet<Point>();
        foreach (Bridge bridge in Bridges)
        {
            IslandCovered.Add(bridge.From);
            IslandCovered.Add(bridge.To);
            Covered.Add(bridge.From);
            Covered.Add(bridge.To);
            foreach (Point p in bridge.Middle()) Covered.Add(p);
        }
        Covered.Remove(open);
        IslandCovered.Remove(open);

        foreach (Point island in IslandCovered) hash ^= island.GetHashCode();
    }

    public bool CheckParallel(Component other)
    {
        foreach (Point p in this.Covered)
        {
            if (other.Covered.Contains(p)) return false;
        }
        return true;
    }

    public void AddBridge(Bridge bridge, int[,] grid)
    {
        Bridges.Add(bridge);

        grid[bridge.From.X, bridge.From.Y] -= bridge.Count;
        grid[bridge.To.X, bridge.To.Y] -= bridge.Count;
        if (grid[bridge.From.X, bridge.From.Y] == 0) grid[bridge.From.X, bridge.From.Y] = -1;
        if (grid[bridge.To.X, bridge.To.Y] == 0) grid[bridge.To.X, bridge.To.Y] = -1;
        int dx = Math.Sign(bridge.To.X - bridge.From.X);
        int dy = Math.Sign(bridge.To.Y - bridge.From.Y);
        for (int i = 1; ; i++)
        {
            int x = bridge.From.X + i * dx;
            int y = bridge.From.Y + i * dy;
            if (x == bridge.To.X && y == bridge.To.Y) break;
            grid[x, y] = -1;
        }
    }

    static bool[,] scored = new bool[30, 30];
    public void FinalizeComponent(int[,] initialGrid)
    {
        Covered = new HashSet<Point>();
        IslandCovered = new HashSet<Point>();
        foreach (Bridge bridge in Bridges)
        {
            int dx = Math.Sign(bridge.To.X - bridge.From.X);
            int dy = Math.Sign(bridge.To.Y - bridge.From.Y);
            for (int i = 1; ; i++)
            {
                int x = bridge.From.X + i * dx;
                int y = bridge.From.Y + i * dy;
                if (x == bridge.To.X && y == bridge.To.Y) break;
                Covered.Add(new Point(x, y));
            }

            Covered.Add(bridge.From);
            Covered.Add(bridge.To);
            if (!IslandCovered.Contains(bridge.From))
            {
                IslandCovered.Add(bridge.From);
                hash ^= bridge.From.GetHashCode();
            }
            if (!IslandCovered.Contains(bridge.To))
            {
                IslandCovered.Add(bridge.To);
                hash ^= bridge.To.GetHashCode();
            }
        }

        Score = IslandCovered.Count * IslandCovered.Count;
        foreach (Bridge b in Bridges)
        {
            if (!scored[b.From.X, b.From.Y])
            {
                scored[b.From.X, b.From.Y] = true;
                Score += initialGrid[b.From.X, b.From.Y] * initialGrid[b.From.X, b.From.Y];
            }
            if (!scored[b.To.X, b.To.Y])
            {
                scored[b.To.X, b.To.Y] = true;
                Score += initialGrid[b.To.X, b.To.Y] * initialGrid[b.To.X, b.To.Y];
            }
        }
        foreach (Bridge b in Bridges)
        {
            scored[b.From.X, b.From.Y] = false;
            scored[b.To.X, b.To.Y] = false;
        }

        //Validate(initialGrid);
    }

    public void Validate(int[,] initialGrid, bool[,] checkVisited = null)
    {
        int[,] covered = new int[Board.Size, Board.Size];
        bool[,] bridgeCenter = new bool[Board.Size, Board.Size];
        foreach (Bridge b in Bridges)
        {
            if (b.Count < 0 || b.Count > Board.BridgeCount) throw new Exception();
            covered[b.From.X, b.From.Y] += b.Count;
            covered[b.To.X, b.To.Y] += b.Count;

            foreach (Point p in b.Middle())
            {
                if (bridgeCenter[p.X, p.Y]) throw new Exception();
                bridgeCenter[p.X, p.Y] = true;
                if (checkVisited != null && !checkVisited[p.X, p.Y]) throw new Exception();
            }
        }
        for (int x = 0; x < Board.Size; x++)
        {
            for (int y = 0; y < Board.Size; y++)
            {
                if (covered[x, y] != 0 && covered[x, y] != initialGrid[x, y]) throw new Exception();
            }
        }
    }

    public override int GetHashCode() => hash;

    public bool Equals(Component other)
    {
        return this.hash == other.hash; // my hashing is perfect
        return this.IslandCovered.SetEquals(other.IslandCovered); // && this.Covered.SetEquals(other.Covered);
    }

    public override string ToString()
    {
        HashSet<Point> points = new HashSet<Point>(Bridges.SelectMany(b => new[] { b.From, b.To }));
        return string.Join(" ", points) + ": " + Score;
    }

    public void SetCount(Bridge path, int count)
    {
        int index = Bridges.IndexOf(path);
        Bridges[index] = new Bridge(path.From, path.To, count);
    }
}