using System;
using System.Collections.Generic;
using System.Linq;

public class Chamber
{
    public int ID;
    public int RealId;
    public int TreasureCount;
    public Chamber[] Neighbors;
    public List<Chamber> NeighborsAtUnknownIndex = new List<Chamber>();
    public int KnownNeighbors;

    public Chamber(Chamber chamber)
    {
        this.ID = chamber.ID;
        this.RealId = chamber.RealId;
        this.KnownNeighbors = chamber.KnownNeighbors;
        this.TreasureCount = chamber.TreasureCount;
        this.Neighbors = new Chamber[chamber.Neighbors.Length];
    }

    public Chamber(int treasureCount, int neighborCount)
    {
        this.TreasureCount = treasureCount;
        this.Neighbors = new Chamber[neighborCount];
    }

    public bool MaybeEqual(Chamber c) => this.TreasureCount == c.TreasureCount && this.Neighbors.Length == c.Neighbors.Length;

    internal void AddUnknownNeighbor(Chamber chamber)
    {
        if (NeighborsAtUnknownIndex.Contains(chamber)) return;
        foreach (Chamber c in Neighbors)
        {
            if (c == chamber) return;
        }
        KnownNeighbors++;
        NeighborsAtUnknownIndex.Add(chamber);
    }

    internal bool AddLink(Chamber next, int index)
    {
        if (Neighbors[index] == null) KnownNeighbors++;
        bool replaced = Neighbors[index] != null && Neighbors[index] != next;
        Neighbors[index] = next;
        if (NeighborsAtUnknownIndex.Remove(next))
            KnownNeighbors--;
        next.AddUnknownNeighbor(this);

        return replaced || KnownNeighbors > Neighbors.Length || next.KnownNeighbors > next.Neighbors.Length;
    }
}
