public abstract class Engine
{
    public int TreasureValue;
    public int StepCost;
    public int ChamberCount;
    public int MaxTreasurePickup;
    public abstract int Play(Strategy strategy);
    public abstract void PrintStats(Strategy strategy);
    public abstract void Plot();
}