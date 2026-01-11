using System;

public class RandomStrategy : Strategy
{
    private int empty = 0;
    private int stop = 0;
    public RandomStrategy(int stop)
    {
        this.stop = stop;
    }
    
    public override (int take, int next) Turn(int treasureCount, int pathCount, int time, int chamberId = -1)
    {
        if (treasureCount == 0) empty++;
        else empty = 0;

        if (empty > stop) return (-1, 0);
        return (Math.Min(treasureCount, this.MaxTreasure), random.Next(pathCount));
    }
}