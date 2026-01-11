using System;
using System.Collections.Generic;
using System.Linq;

public class Plan
{
    public Board Board;
    public double Points;
    public int RemainingTurns;
    List<Farmer> Farmers;

    private int[,] barnDist;
    private static Random random = new Random(0);

    public Plan(Board board, int remainingTurns)
    {
        this.RemainingTurns = remainingTurns;
        random = new Random(0);
        this.Board = board;
        Farmers = board.Farmers.ToList();
        foreach (Barn barn in Board.Barns) barn.Dist = Board.BFS(new List<Cell> { barn.Cell });

        barnDist = Board.BFS(Board.Barns.Select(b => b.Cell).ToList());
        double[,] barnAttractor = new double[Board.Size, Board.Size];
        for (int x = 0; x < Board.Size; x++)
        {
            for (int y = 0; y < Board.Size; y++) barnAttractor[x, y] = 1.0 / barnDist[x, y];
        }
        foreach (Sheep sheep in board.Sheep.Where(s => s.Wool > 0)) sheep.Dist = Board.BFS(new List<Cell> { sheep.Cell });
        foreach (Barn barn in Board.Barns)
        {
            foreach (Sheep sheep in Board.Sheep)
            {
                if (sheep.Wool == 0) continue;
                barn.SheepAttractor += 1.0 / ComputeDist(sheep.Cell, barn.Cell, sheep.Dist);
            }
        }

        // greedy initial
        foreach (Farmer farmer in Farmers)
        {
            List<Farmer> otherFarmers = board.Farmers.Where(f => f.Wool != Farmer.Capacity && f != farmer).ToList();
            ComputeAttractors(farmer, otherFarmers);
            if (farmer.Wool == Farmer.Capacity)
            {
                farmer.Target = farmer.Cell.Neighbors.OrderBy(n => barnAttractor[n.X, n.Y]).Last();
            }
            else
            {
                farmer.Target = farmer.Cell.Neighbors[Enumerable.Range(0, farmer.Cell.Neighbors.Count).OrderBy(i => farmer.SheepAttractor[i]).Last()];
            }
        }

        int result = ApplyPlans(Farmers);
        Points = Score(result);
        UndoPlans(Farmers);

        foreach (Farmer farmer in Farmers.ToList())
        {
            List<Cell> targetCandidates = farmer.Cell.Neighbors.ToList();
            foreach (Cell target in targetCandidates)
            {
                if (target.Barn && farmer.Target.Sheep != null && farmer.Target.Sheep.Wool > 0 && farmer.Wool < Farmer.Capacity) continue;
                foreach (int farmerIndex in new int[] { 0, Farmers.Count - 1 })
                {
                    Cell oldTarget = farmer.Target;
                    int oldIndex = Farmers.IndexOf(farmer);
                    Farmers.Remove(farmer);
                    Farmers.Insert(farmerIndex, farmer);
                    farmer.Target = target;
                    int score = ApplyPlans(Farmers);
                    double points = Score(score);
                    if (points > Points)
                    {
                        Points = points;
                        Score(score);
                    }
                    else
                    {
                        farmer.Target = oldTarget;
                        Farmers.Remove(farmer);
                        Farmers.Insert(oldIndex, farmer);
                    }
                    UndoPlans(Farmers);

                    // mutate farmer at target as well
                    if (target.Farmer != null && target.Farmer != farmer)
                    {
                        Farmer farmer2 = target.Farmer;
                        List<Cell> targetCandidates2 = farmer2.Cell.Neighbors.ToList();
                        targetCandidates2.Remove(farmer.Cell);
                        foreach (Cell target2 in targetCandidates2)
                        {
                            oldTarget = farmer.Target;
                            farmer.Target = target;
                            oldIndex = Farmers.IndexOf(farmer);
                            Farmers.Remove(farmer);
                            Farmers.Insert(farmerIndex, farmer);
                            Cell oldTarget2 = farmer2.Target;
                            farmer2.Target = target2;
                            score = ApplyPlans(Farmers);
                            points = Score(score);
                            if (points > Points)
                            {
                                Points = points;
                                Score(score);
                            }
                            else
                            {
                                farmer.Target = oldTarget;
                                farmer2.Target = oldTarget2;
                                Farmers.Remove(farmer);
                                Farmers.Insert(oldIndex, farmer);
                            }
                            UndoPlans(Farmers);
                        }
                    }
                }
            }
        }

        // find diagonal neighbors, move to same cell
        foreach (Cell cell in Board.Grid)
        {
            if (!cell.Empty) continue;
            List<Farmer> farmers = cell.Neighbors.Select(n => n.Farmer).Distinct().ToList();
            farmers.Remove(null);
            foreach (Farmer f1 in farmers)
            {
                foreach (Farmer f2 in farmers)
                {
                    if (f1 == f2) continue;
                    List<Farmer> backup = Farmers.ToList();
                    Cell f1Target = f1.Target;
                    Cell f2Target = f2.Target;
                    Farmers.Remove(f1);
                    Farmers.Remove(f2);
                    f1.Target = cell;
                    f2.Target = cell;
                    Farmers.Add(f1);
                    Farmers.Add(f2);
                    int score = ApplyPlans(Farmers);
                    double points = Score(score);
                    if (points > Points)
                    {
                        Points = points;
                        Score(score);
                    }
                    else
                    {
                        f1.Target = f1Target;
                        f2.Target = f2Target;
                        Farmers = backup;
                    }
                    UndoPlans(Farmers);
                }
            }
        }

        ShearTheSheep.Debug(this);
    }

    private void ComputeAttractors(Farmer farmer, List<Farmer> otherFarmers)
    {
        farmer.SheepAttractor = new double[5];
        farmer.BarnAttractor = new double[5];
        foreach (Sheep sheep in Board.Sheep)
        {
            if (sheep.Wool == 0) continue;
            double blockingFactor = 1;
            if (otherFarmers.Any(f => f.InitialWool < Farmer.Capacity && sheep.Dist[f.InitialCell.X, f.InitialCell.Y] < sheep.Dist[farmer.InitialCell.X, farmer.InitialCell.Y]))
                blockingFactor = 1.0 / (3 + Farmer.Capacity);
            blockingFactor *= Math.Pow(sheep.Wool, 1.5 + 2.5 / (2 + Farmer.Capacity));
            for (int i = 0; i < farmer.Cell.Neighbors.Count; i++)
            {
                Cell n = farmer.Cell.Neighbors[i];
                if (sheep.Dist[n.X, n.Y] == Board.Size * Board.Size) continue;
                farmer.SheepAttractor[i] += blockingFactor / ComputeDist(sheep.Cell, n, sheep.Dist);
            }
            if (sheep.Dist[farmer.Cell.X, farmer.Cell.Y] == Board.Size * Board.Size) continue;
            farmer.SheepAttractor[4] += blockingFactor / ComputeDist(sheep.Cell, farmer.Cell, sheep.Dist);
        }

        double pow = 0.00194805 * Farmer.Capacity * Farmer.Capacity - 0.0967532 * Farmer.Capacity + 2.27273;
        foreach (Barn barn in Board.Barns)
        {
            for (int i = 0; i < farmer.Cell.Neighbors.Count; i++)
            {
                Cell n = farmer.Cell.Neighbors[i];
                farmer.BarnAttractor[i] = Math.Max(farmer.BarnAttractor[i], 1.0 / Math.Pow(barn.Dist[n.X, n.Y], pow));
            }
            farmer.BarnAttractor[4] = Math.Max(farmer.BarnAttractor[4], 1.0 / Math.Pow(barn.Dist[farmer.Cell.X, farmer.Cell.Y], pow));
        }
    }

    private static double ComputeDist(Cell from, Cell to, int[,] bfsDist)
    {
        // parabola through (3,2) (14,1.3) (21,1.1)
        double pow = 0.00194805 * Farmer.Capacity * Farmer.Capacity - 0.0967532 * Farmer.Capacity + 2.27273;
        return ComputeDist(from, to, bfsDist, pow);
    }

    private static double ComputeDist(Cell from, Cell to, int[,] bfsDist, double pow)
    {
        int initialDist = bfsDist[to.X, to.Y];
        if (initialDist == 0) return 0.01;
        int dx = Math.Abs(from.X - to.X);
        int dy = Math.Abs(from.Y - to.Y);
        double result = initialDist * (dx + dy + Math.Sqrt(dx * dx + dy * dy)) / (2 * dx + 2 * dy);
        return Math.Pow(result, pow);
    }

    public override string ToString()
    {
        return string.Join(" ", Farmers.Select(f => f.PrintMove()));
    }

    private int ApplyPlans(List<Farmer> farmers)
    {
        int points = 0;
        foreach (Farmer farmer in farmers) points += farmer.MoveTo(farmer.Target);
        return points;
    }

    private void UndoPlans(List<Farmer> farmers)
    {
        foreach (Farmer farmer in farmers) farmer.Wool = farmer.InitialWool;
        foreach (Farmer farmer in farmers) farmer.Cell.Farmer = null;
        foreach (Farmer farmer in farmers) farmer.Cell = farmer.InitialCell;
        foreach (Farmer farmer in farmers) farmer.Cell.Farmer = farmer;
        foreach (Sheep sheep in Board.Sheep) sheep.Wool = sheep.InitialWool;
    }

    private double Score(int points)
    {
        double result = 101 * points;
        foreach (Farmer farmer in Farmers)
        {
            int neighborIndex = 4;
            if (farmer.Cell != farmer.InitialCell) neighborIndex = farmer.InitialCell.Neighbors.IndexOf(farmer.Cell);
            double woolFactor = barnDist[farmer.Cell.X, farmer.Cell.Y] == Board.Size * Board.Size || barnDist[farmer.Cell.X, farmer.Cell.Y] <= RemainingTurns - 2 ? 1 : 0;
            if (barnDist[farmer.Cell.X, farmer.Cell.Y] == RemainingTurns - 1) woolFactor = 0.5;
            if (barnDist[farmer.Cell.X, farmer.Cell.Y] == RemainingTurns - 2) woolFactor = 0.75;
            result += 100 * woolFactor * farmer.Wool;
            result += woolFactor * (Farmer.Capacity - farmer.Wool) * farmer.SheepAttractor[neighborIndex];
            result += farmer.Wool * farmer.BarnAttractor[neighborIndex];

            result += 0.000001 * farmer.SheepAttractor[neighborIndex];
            result += 0.000001 * farmer.BarnAttractor[neighborIndex];
        }
        return result;
    }
}