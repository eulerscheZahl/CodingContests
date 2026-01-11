using System;
using System.Collections.Generic;

public abstract class Strategy
{
    public int TreasureValue;
    public int StepCost;
    public int NumChambers;
    public int MaxTreasure;

    public Random random = new Random(0);
    public List<Chamber> AllChambers = new List<Chamber>();
    public List<Reality> Realities = new List<Reality> { new Reality() };

    public void Init(int treasureValue, int stepCost, int numChambers, int maxTreasure)
    {
        this.TreasureValue = treasureValue;
        this.StepCost = stepCost;
        this.NumChambers = numChambers;
        this.MaxTreasure = maxTreasure;
    }

    public abstract (int take, int next) Turn(int treasureCount, int pathCount, int time, int chamberId = -1);
}