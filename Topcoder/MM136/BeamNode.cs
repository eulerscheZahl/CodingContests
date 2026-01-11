using System;
using System.Collections.Generic;
using System.Linq;

public class BeamNode
{
    public Cell End1;
    public Cell End2;
    public BeamNode Parent;
    public double Penalty;
    public List<Cell> Path;
    public HashSet<Cell> Visited = new HashSet<Cell>();

    private const int maxMoveDist = 5;
    private static int counter = 0;
    public int ID;
    private static Random random = new Random(0);

    public BeamNode() { ID = ++counter; }
    public IEnumerable<BeamNode> Expand(int color, int[,] grid, bool[,] visited)
    {
        HashSet<Cell>[] ends = new HashSet<Cell>[1 + maxMoveDist];
        ends[0] = new HashSet<Cell>(new Cell[] { End1, End2 });
        int maxDepth = maxMoveDist;
        for (int i = 1; i <= maxDepth; i++)
        {
            ends[i] = new HashSet<Cell>();
            foreach (Cell prev in ends[i - 1])
            {
                foreach (Cell n in prev.Neighbors)
                {
                    if (!visited[n.X, n.Y] && !Visited.Contains(n)) ends[i].Add(n);
                }
            }
            List<Cell> sameColor = ends[i].Where(e => grid[e.X, e.Y] == color).ToList();
            if (sameColor.Count > 0)
            {
                List<List<Cell>> result = sameColor.Select(s => new List<Cell> { s }).ToList();
                for (int j = i; j > 1; j--)
                {
                    List<List<Cell>> newResult = new List<List<Cell>>();
                    foreach (List<Cell> list in result)
                    {
                        foreach (Cell cell in ends[j - 1])
                        {
                            if (!list.Last().HasNeighbor(cell)) continue;
                            List<Cell> next = list.ToList();
                            next.Add(cell);
                            newResult.Add(next);
                        }
                    }
                    result = newResult;
                }
                foreach (List<Cell> cells in result)
                {
                    int penalty = 10000 * cells.Count;
                    foreach (Cell c in cells)
                    {
                        penalty += 10 * Board.penalties[grid[c.X, c.Y]];
                        foreach (Cell n in c.Neighbors)
                        {
                            if (visited[n.X, n.Y] || Visited.Contains(n)) continue;
                            penalty += Board.penalties[grid[n.X, n.Y]];
                        }
                    }
                    HashSet<Cell> newVisited = new HashSet<Cell>(Visited);
                    foreach (Cell cell in cells) newVisited.Add(cell);
                    BeamNode node = new BeamNode { Parent = this, Path = cells, End1 = End1, End2 = End2, Visited = newVisited, Penalty = Penalty + penalty + 0.001 * random.NextDouble() };
                    if (End1.HasNeighbor(cells.Last())) node.End1 = cells.Last();
                    else node.End2 = cells.Last();
                    yield return node;
                }
                break;
                maxDepth = Math.Min(3, Math.Min(maxDepth, i + 1));
            }
        }
    }

    public override string ToString()
    {
        string result = string.Join("..", Path);
        BeamNode root = this;
        while (root.Parent != null)
        {
            root = root.Parent;
            result = string.Join("..", root.Path) + " -> " + result;
        }
        return result;
    }

    public BeamNode Root()
    {
        BeamNode root = Parent;
        while (root?.Parent != null) root = root.Parent;
        return root;
    }

    public void RemoveRoot(BeamNode root)
    {
        BeamNode current = this;
        while (current.Parent?.Parent != null) current = current.Parent;
        if (current.Parent == root) current.Parent = null;
    }
}