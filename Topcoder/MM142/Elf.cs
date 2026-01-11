using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Elf
{
    public Cell[] Plan = new Cell[1 + Board.SEARCH_DEPTH];
    private Cell[] planBackup = new Cell[1 + Board.SEARCH_DEPTH];
    public bool Moved;
    public bool HasPresent;
    public bool HasBox;
    public bool IsFree => !HasPresent && !HasBox;

    public Elf(Cell cell)
    {
        for (int i = 0; i < Plan.Length; i++)
        {
            this.Plan[i] = cell;
            this.planBackup[i] = cell;
            cell.Elf[i] = this;
        }
        if (cell.CellType == CellType.ELF_WITH_BOX) HasBox = true;
        if (cell.CellType == CellType.ELF_WITH_PRESENT) HasPresent = true;
    }

    public string PrintAction()
    {
        if (Plan[0] == Plan[1]) return null;
        return Plan[0].Y + " " + Plan[0].X + " " + Plan[0].GetDir(Plan[1]);
    }

    public override string ToString() => string.Join(" - ", Plan.Select(c => c == null ? "null" : (c.X + "/" + c.Y)));

    public void Escape(int depth)
    {
        for (int t = depth; t < Plan.Length; t++) Plan[depth].Elf[t] = null;
        Plan[depth] = null;
    }

    public int Dist(int[,] dist, int t = Board.SEARCH_DEPTH) => dist[Plan[t].X, Plan[t].Y];

    public Cell RandomTarget(Random random, int depth, int[,] targetDist, Cell exclude)
    {
        if (IsFree && Plan[depth].HasBox && exclude == null) return Plan[depth];
        List<Cell> targets = new List<Cell>();
        if (exclude == null) targets.Add(Plan[depth]);
        int initialDist = targetDist[Plan[depth].X, Plan[depth].Y];
        foreach (Cell neighbor in Plan[depth].Neighbors)
        {
            if (neighbor == exclude || neighbor.HasTree) continue;
            if (!IsFree && neighbor.IsSolid) continue;
            if (exclude == null && targetDist[neighbor.X, neighbor.Y] > initialDist && !AiTracker.BlockworthyGrid[neighbor.X, neighbor.Y]) continue;
            targets.Add(neighbor);
        }
        if (targets.Count == 0) return null;
        return targets[random.Next(targets.Count)];
    }

    public void Backup()
    {
        for (int i = 0; i < Plan.Length; i++)
        {
            planBackup[i] = Plan[i];
        }
    }

    public void Restore()
    {
        for (int i = 0; i < Plan.Length; i++)
        {
            if (Plan[i] != null && Plan[i].Elf[i] == this) Plan[i].Elf[i] = null;
            Plan[i] = planBackup[i];
            Plan[i].Elf[i] = this;
        }
    }
}