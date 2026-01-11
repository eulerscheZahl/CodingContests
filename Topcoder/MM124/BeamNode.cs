using System;
using System.Collections.Generic;
using System.Linq;

class BeamNode : IEquatable<BeamNode>
{
    public int Action;
    public double Score => CollectedScore + RandomScore;
    public double CollectedScore;
    public double RandomScore;
    public Chamber CurrentChamber;
    public List<Chamber> VisitedChambers = new List<Chamber>();
    public BeamNode Parent;

    public BeamNode(Chamber currentChamber)
    {
        this.CurrentChamber = currentChamber;
    }

    public BeamNode(BeamNode parent, Chamber currentChamber, Strategy strategy, int action)
    {
        this.Action = action;
        this.Parent = parent;
        this.CurrentChamber = currentChamber;
        this.VisitedChambers = parent.VisitedChambers.ToList();
        this.CollectedScore = parent.CollectedScore * 1.1;
        this.RandomScore = strategy.random.NextDouble() * 1e-3;
        if (action == -1) return; // no extra score, end game
        if (parent.CurrentChamber != null)
        {
            this.VisitedChambers.Add(parent.CurrentChamber);
            // TODO subtracting too much at path, skip last?
            int treasures = parent.CurrentChamber.TreasureCount - strategy.MaxTreasure * VisitedChambers.Count(c => c == parent.CurrentChamber);
            treasures = Math.Max(treasures, 0);
            this.CollectedScore += Math.Min(strategy.MaxTreasure, treasures) * strategy.TreasureValue - strategy.StepCost;
            this.CollectedScore += CurrentChamber.TreasureCount;
        }
        else
        {
            this.CollectedScore += strategy.MaxTreasure * strategy.TreasureValue - strategy.StepCost + 0.1; // small exploration bonus
        }
    }

    public bool Equals(BeamNode other)
    {
        return this.CurrentChamber == other.CurrentChamber && this.CollectedScore == other.CollectedScore;
    }

    public override int GetHashCode()
    {
        return (this.CurrentChamber?.ID ?? 0) * 10000 + (int)this.CollectedScore;
    }

    public IEnumerable<BeamNode> Expand(Strategy strategy)
    {
        yield return new BeamNode(this, null, strategy, -1); // exit search right there
        if (Action == -1) yield break;
        if (CurrentChamber == null)
        {
            yield return new BeamNode(this, null, strategy, -2);
            yield break;
        }
        int index = 0;
        bool nullExpanded = true;
        foreach (Chamber next in CurrentChamber.Neighbors)
        {
            if (next == null)
            {
                if (nullExpanded) continue;
                nullExpanded = true;
            }
            BeamNode node = new BeamNode(this, next, strategy, index++);
            yield return node;
        }
    }
}
