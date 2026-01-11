using System;
using System.Collections.Generic;
using System.Linq;

public class MemoryStrategy : Strategy
{
    private int empty = 0;
    private int stop = 0;
    public MemoryStrategy(int stop)
    {
        this.stop = stop;
    }

    private int REALITY_LIMIT = 2500;
    private int plannedId = -1;
    private int Score = 0;
    private int turn = 0;
    public override (int take, int next) Turn(int treasureCount, int pathCount, int time, int chamberId = -1)
    {
        turn++;
        if (Score > 0 && Realities[0].AllChambers.Count > 5 && Realities[0].AllChambers.Sum(c => c.TreasureCount) == 0) return (-1, 0);
        double expected = Realities[0].AllChambers.Sum(c => Math.Min(c.TreasureCount, MaxTreasure));
        if (Realities[0].AllChambers.Count > NumChambers) expected = Realities[0].AllChambers.OrderBy(c => c.TreasureCount).Take(NumChambers).Sum(c => Math.Min(c.TreasureCount, MaxTreasure));
        expected *= (double)TreasureValue / Math.Max(1, Math.Min(NumChambers, Realities[0].AllChambers.Count));
        expected -= StepCost;
        if (treasureCount == 0) empty++;
        else empty = 0;

        if (Score > 0 && empty > stop) return (-1, 0);
        int stepsToCompensateEmpty = 1;
        while (StepCost != TreasureValue && stepsToCompensateEmpty * TreasureValue * MaxTreasure < (1 + stepsToCompensateEmpty) * StepCost) stepsToCompensateEmpty++;
        if (Score > 0 && empty > 10 && expected < 0) return (-1, 0);

        int take = Math.Min(treasureCount, this.MaxTreasure);
        Realities = Realities.SelectMany(r => r.AddMove(this, treasureCount, pathCount, chamberId)).ToList();
        //Realities = Realities.Where(r => r.CurrentChamber.RealId == chamberId).ToList();
        Reality fewestChambers = Realities.OrderBy(r => r.AllChambers.Count).First();
        if (Realities.Any(r => !r.Failed))
            Realities = Realities.Where(r => !r.Failed).ToList();
        else REALITY_LIMIT = 25;
        if (Realities.Count > REALITY_LIMIT) REALITY_LIMIT = 25;
        if (Realities.Count > REALITY_LIMIT) Realities = Realities.OrderBy(r => r.AllChambers.Count).Take(REALITY_LIMIT).ToList();
        if (Realities.Count > REALITY_LIMIT)
        { // shuffle and remove some
            Realities = Realities.OrderBy(r => r.AllChambers.Count).ToList();
            List<Reality> reduced = Realities.Where(r => r.AllChambers.Count <= Realities[REALITY_LIMIT - 1].AllChambers.Count).ToList();
            Realities.Clear();
            while (Realities.Count < REALITY_LIMIT)
            {
                int idx = random.Next(reduced.Count);
                Realities.Add(reduced[idx]);
                reduced.RemoveAt(idx);
            }
        }
        if (Realities.All(r => r.AllChambers.Count > fewestChambers.AllChambers.Count)) Realities.Insert(0, fewestChambers);

        int[] next = new int[pathCount + 1];
        foreach (Reality stepReality in Realities)
        {
            Chamber currentChamber = stepReality.CurrentChamber;
            int stepPath = random.Next(pathCount);
            Chamber nextChamber = currentChamber.Neighbors[stepPath];
            // avoid empty chambers
            while (nextChamber != null && nextChamber.TreasureCount == 0 && currentChamber.Neighbors.Any(n => n == null || n.TreasureCount > 0))
            {
                stepPath = random.Next(pathCount);
                nextChamber = currentChamber.Neighbors[stepPath];
            }
            // all chambers known? try to get some points
            while ((nextChamber == null || nextChamber.TreasureCount == 0) && stepReality.AllChambers.Count == NumChambers && currentChamber.Neighbors.Any(n => n != null && n.TreasureCount > 0))
            {
                stepPath = random.Next(pathCount);
                nextChamber = currentChamber.Neighbors[stepPath];
            }

            if (/*random.Next(2) == 0 &&*/ nextChamber != null && nextChamber.TreasureCount > 0 ||
                stepReality.AllChambers.Count > NumChambers - 5 && currentChamber.Neighbors.Any(n => n != null && n.TreasureCount > 0))
            {
                stepPath = Enumerable.Range(0, currentChamber.Neighbors.Length).Where(i => currentChamber.Neighbors[i] != null).OrderByDescending(i => currentChamber.Neighbors[i].TreasureCount).First();
                nextChamber = currentChamber.Neighbors[stepPath];
            }

            while (turn < NumChambers * 20 / MaxTreasure && nextChamber != null && currentChamber.Neighbors.Contains(null))
            {
                stepPath = random.Next(pathCount);
                nextChamber = currentChamber.Neighbors[stepPath];
            }

            if (nextChamber != null && nextChamber.TreasureCount == 0)
            {
                int beam = BeamSearch(currentChamber);
                if (beam != -1)
                {
                    stepPath = beam;
                    nextChamber = currentChamber.Neighbors[stepPath];
                }
            }

            if (Score > 0 && treasureCount == 0 && stepsToCompensateEmpty * stepReality.AllChambers.Count(c => c.TreasureCount == 0) > stepReality.AllChambers.Count)
                return (-1, 0); //stepPath = -1;
            if (stepPath == -1) stepPath = pathCount;
            next[stepPath]++;

            int maxTake = take;
            while (take > (maxTake + 1) / 2 && stepReality.AllChambers.Any(c => c.Neighbors.Length == pathCount && c.TreasureCount == treasureCount - take)) take--;
            if (stepReality.AllChambers.Any(c => c.Neighbors.Length == pathCount && c.TreasureCount == treasureCount - take)) take = Math.Min(treasureCount, this.MaxTreasure);

            //Console.Write(nextChamber == null ? "?-" : ".-");
            break;
        }

        int path = 0;
        for (int i = 1; i < next.Length; i++)
        {
            if (next[i] > next[path]) path = i;
        }
        if (path == pathCount) return (-1, 0);

        foreach (Reality reality in Realities.Distinct())
        {
            reality.LastAction = path;
            reality.CurrentChamber.TreasureCount -= take;
            reality.LastChamber = reality.CurrentChamber;
            reality.NextChamber = reality.CurrentChamber.Neighbors[path];
        }
        AllChambers = Realities[0].AllChambers;

        plannedId = Realities[0].NextChamber?.ID ?? -1;
        int nextTreasures = Realities[0].NextChamber?.TreasureCount ?? -1;
        // Console.Error.WriteLine(Realities[0].CurrentChamber.ID + " -> " + plannedId + " = " + nextTreasures + "   @" + time);

        Score += take * TreasureValue - StepCost;
        //Console.Error.WriteLine(Score + "  @" + time);
        return (take, path);
    }

    const int BEAM_DEPTH = 6;
    const int BEAM_WIDTH = 100;
    private int BeamSearch(Chamber chamber)
    {
        //int reachable = ReachableTreasures(chamber);
        HashSet<BeamNode> beam = new HashSet<BeamNode> { new BeamNode(chamber) };
        for (int depth = 0; depth < BEAM_DEPTH; depth++)
        {
            beam = new HashSet<BeamNode>(beam.SelectMany(b => b.Expand(this)));
            if (beam.Count > BEAM_WIDTH) beam = new HashSet<BeamNode>(beam.OrderByDescending(b => b.Score).Take(BEAM_WIDTH));
        }
        BeamNode result = beam.OrderByDescending(b => b.Score).First();
        if (result.CollectedScore == 0) return -1;
        while (result.Parent.Parent != null) result = result.Parent;
        return result.Action;
    }

    private int ReachableTreasures(Chamber chamber)
    {
        HashSet<Chamber> visited = new HashSet<Chamber> { chamber };
        Queue<Chamber> queue = new Queue<Chamber>();
        queue.Enqueue(chamber);
        int result = 0;
        while (queue.Count > 0)
        {
            Chamber c = queue.Dequeue();
            result += c.TreasureCount;
            foreach (Chamber n in c.Neighbors)
            {
                if (n == null || visited.Contains(n)) continue;
                visited.Add(n);
                queue.Enqueue(n);
            }
        }
        return result;
    }
}
