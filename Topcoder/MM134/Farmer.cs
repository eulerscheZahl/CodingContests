using System;

public class Farmer
{
    public static int Capacity;
    public int Wool;
    public int InitialWool;
    public Cell Cell;
    public Cell InitialCell;
    public Cell Target;
    public double[] SheepAttractor;
    public double[] BarnAttractor;

    public Farmer(Cell cell, int wool)
    {
        this.Cell = cell;
        this.InitialCell = cell;
        this.Wool = wool;
        this.InitialWool = wool;
        cell.Farmer = this;
    }

    public string PrintMove()
    {
        if (Target.X > this.InitialCell.X) return $"{InitialCell.Y} {InitialCell.X} R";
        if (Target.X < this.InitialCell.X) return $"{InitialCell.Y} {InitialCell.X} L";
        if (Target.Y < this.InitialCell.Y) return $"{InitialCell.Y} {InitialCell.X} U";
        if (Target.Y > this.InitialCell.Y) return $"{InitialCell.Y} {InitialCell.X} D";
        return $"{InitialCell.Y} {InitialCell.X} N";
    }

    public int MoveTo(Cell target)
    {
        if (target.Barn)
        {
            int result = this.Wool;
            this.Wool = 0;
            return result;
        }
        if (target.Sheep != null)
        {
            if (target.Sheep.Wool > 0 && this.Wool < Capacity)
            {
                target.Sheep.Wool--;
                this.Wool++;
            }
            return 0;
        }
        if (target.Farmer != null)
        {
            int passing = Math.Min(this.Wool, Capacity - target.Farmer.Wool);
            this.Wool -= passing;
            target.Farmer.Wool += passing;
            return 0;
        }
        Cell.Farmer = null;
        target.Farmer = this;
        Cell = target;
        return 0;
    }

    public override string ToString()
    {
        return InitialCell + ": " + PrintMove();
    }

    public double GetSheepAttractor(int x, int y)
    {
        for (int i = 0; i < InitialCell.Neighbors.Count; i++)
        {
            if (InitialCell.Neighbors[i].X == x && InitialCell.Neighbors[i].Y == y) return SheepAttractor[i];
        }
        if (InitialCell.X == x && InitialCell.Y == y) return SheepAttractor[4];
        return 0;
    }
}
