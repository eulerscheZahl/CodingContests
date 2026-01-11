using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;

public class BeamNode : IEquatable<BeamNode>
{
    public Cell Cell;
    public int[] Visited;
    public BeamNode Parent;
    public int Score;
    public int PathLength;
    public int Mult;
    public double SortScore;

    public BeamNode() { }
    public BeamNode(Cell cell)
    {
        this.Cell = cell;
        this.PathLength = 1;
        this.Mult = cell.Mult;
        this.Score = cell.Mult;
        this.Visited = new int[Board.Size];
        this.Visited[cell.Y] |= 1 << cell.X;
    }

    public BeamNode(Cell cell, BeamNode plan, bool copyVisited)
    {
        this.Cell = cell;
        this.Parent = plan;
        this.PathLength = plan.PathLength + 1;
        if (copyVisited)
        {
            this.Visited = plan.Visited.ToArray();
            this.Visited[cell.Y] |= 1 << cell.X;
        }
        this.Mult = plan.Mult + cell.Mult;
        this.Score = plan.Score + Mult;
    }

    public double EstimateScore(int startIndex) => Score + startIndex * Mult;

    static int[] factors = { 1033, 1039, 1049, 1051, 1061, 1063, 1069, 1087, 1091, 1093, 1097, 1103, 1109, 1117, 1123, 1129, 1151, 1153, 1163, 1171, 1181, 1187, 1193, 1201, 1213, 1217, 1223, 1229, 1231, 1237 };
    public override int GetHashCode()
    {
        long result = Cell.ID;
        for (int i = 0; i < Visited.Length; i++) result ^= factors[i] * Visited[i];
        return result.GetHashCode();
    }

    public bool Equals(BeamNode other)
    {
        return this.Cell == other.Cell && this.Visited.SequenceEqual(other.Visited);
    }

    public IEnumerable<BeamNode> ExpandBefore(bool copyVisited = true)
    {
        foreach (Cell c in Cell.Prev)
        {
            if ((this.Visited[c.Y] & (1 << c.X)) != 0) continue;
            yield return new BeamNode(c, this, copyVisited);
        }
    }

    public Plan GetPlan()
    {
        List<Cell> path = new List<Cell>();
        BeamNode plan = this;
        while (plan != null)
        {
            path.Add(plan.Cell);
            plan = plan.Parent;
        }
        return new Plan(path, Visited);
    }
}