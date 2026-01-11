using System;
using System.Collections.Generic;
using System.Linq;

public class Reality
{
    public List<Chamber> AllChambers;
    public int LastAction = -1;
    public Chamber CurrentChamber;
    public Chamber LastChamber;
    public Chamber NextChamber;
    public bool Failed = false;

    public Reality() { AllChambers = new List<Chamber>(); }

    public Reality(Reality reality)
    {
        this.AllChambers = new List<Chamber>(reality.AllChambers.Count);
        for (int i = 0; i < reality.AllChambers.Count; i++)
            this.AllChambers.Add(new Chamber(reality.AllChambers[i]));

        this.Failed = reality.Failed;
        this.LastAction = reality.LastAction;
        this.CurrentChamber = this.AllChambers[reality.CurrentChamber.ID];
        this.LastChamber = this.AllChambers[reality.LastChamber.ID];
        if (reality.NextChamber != null) this.NextChamber = this.AllChambers[reality.NextChamber.ID];

        for (int i = 0; i < reality.AllChambers.Count; i++)
        {
            Chamber source = reality.AllChambers[i];
            Chamber target = this.AllChambers[i];
            for (int j = 0; j < source.Neighbors.Length; j++)
            {
                if (source.Neighbors[j] == null) continue;
                target.Neighbors[j] = this.AllChambers[source.Neighbors[j].ID];
            }
            target.NeighborsAtUnknownIndex = new List<Chamber>(source.NeighborsAtUnknownIndex.Count);
            for (int j = 0; j < source.NeighborsAtUnknownIndex.Count; j++)
                target.NeighborsAtUnknownIndex.Add(AllChambers[source.NeighborsAtUnknownIndex[j].ID]);
        }
    }

    private bool TopologicSort()
    {
        if (Failed) return false;
        List<int>[] edges = new List<int>[AllChambers.Count];
        for (int i = 0; i < AllChambers.Count; i++) edges[i] = new List<int>();

        for (int i = 0; i < AllChambers.Count; i++)
        {
            Chamber before = AllChambers[i].Neighbors[0];
            for (int j = 1; j < AllChambers[i].Neighbors.Length; j++)
            {
                Chamber after = AllChambers[i].Neighbors[j];
                if (after != null)
                {
                    if (before != null && !edges[after.ID].Contains(before.ID)) edges[after.ID].Add(before.ID);
                    before = after;
                }
            }
        }

        int[] sort = new int[AllChambers.Count];
        Array.Fill(sort, sort.Length);
        sort[0] = 0; // starting chamber is known

        bool change = true;
        for (int order = 1; change; order++)
        {
            change = false;
            for (int i = 0; i < sort.Length; i++)
            {
                if (sort[i] < order) continue;
                sort[i] = order;
                foreach (int before in edges[i])
                {
                    if (sort[before] >= order)
                    {
                        sort[i] = sort.Length;
                        break;
                    }
                }
                change |= sort[i] == order;
            }
        }
        if (sort.Contains(sort.Length))
        {
            Failed = true;
            return false;
        }
        foreach (Chamber chamber in new[] { CurrentChamber, LastChamber })
        {
            if (chamber != null && chamber.KnownNeighbors == chamber.Neighbors.Length && chamber.NeighborsAtUnknownIndex.Count > 0)
            {
                chamber.NeighborsAtUnknownIndex = chamber.NeighborsAtUnknownIndex.OrderBy(c => sort[c.ID]).ToList();
                for (int i = 0; i < chamber.NeighborsAtUnknownIndex.Count; i++)
                {
                    bool noSameBefore = i == 0 || sort[chamber.NeighborsAtUnknownIndex[i - 1].ID] < sort[chamber.NeighborsAtUnknownIndex[i].ID];
                    bool noSameAfter = i == chamber.NeighborsAtUnknownIndex.Count - 1 || sort[chamber.NeighborsAtUnknownIndex[i].ID] < sort[chamber.NeighborsAtUnknownIndex[i + 1].ID];
                    if (noSameBefore && noSameAfter)
                    {
                        int write = Enumerable.Range(0, chamber.Neighbors.Length).Where(i => chamber.Neighbors[i] == null).ElementAt(i);
                        chamber.Neighbors[write] = chamber.NeighborsAtUnknownIndex[i];
                        chamber.NeighborsAtUnknownIndex.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
        return true;
    }

    public IEnumerable<Reality> AddMove(Strategy strategy, int treasureCount, int nextPathsCount, int chamberRealId)
    {
        CurrentChamber = new Chamber(treasureCount, nextPathsCount) { ID = AllChambers.Count, RealId = chamberRealId };
        if (chamberRealId != -1 && AllChambers.Any(r => r.RealId == chamberRealId)) CurrentChamber.RealId += 100;

        // i'm not where i'm supposed to be, something went wrong
        if (NextChamber != null && !NextChamber.MaybeEqual(CurrentChamber))
            Failed = true;
        //    yield break;

        // following the predicted path
        if (NextChamber != null && NextChamber.MaybeEqual(CurrentChamber))
        {
            CurrentChamber = NextChamber;
            Failed |= LastChamber?.AddLink(CurrentChamber, LastAction) ?? false;
            this.TopologicSort();
            yield return this;
            yield break;
        }

        Chamber currChamberBackup = CurrentChamber;
        // find old chamber again
        foreach (Chamber m in AllChambers)
        {
            if (!m.MaybeEqual(currChamberBackup)) continue;
            if (m == LastChamber) continue;
            this.CurrentChamber = m;
            Reality reality = new Reality(this);
            reality.Failed |= reality.LastChamber.AddLink(reality.CurrentChamber, LastAction);
            reality.TopologicSort();
            yield return reality;
        }

        // discover new chamber
        CurrentChamber = currChamberBackup;
        AllChambers.Add(CurrentChamber);
        if (AllChambers.Count > strategy.NumChambers)
        {
            Failed = true;
            LastChamber?.AddLink(CurrentChamber, LastAction);
        }
        else
        {
            Failed |= LastChamber?.AddLink(CurrentChamber, LastAction) ?? false;
            this.TopologicSort();
        }
        yield return this;
    }
}
