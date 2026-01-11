using System;

public class ArenaEngine : Engine
{
    public override int Play(Strategy strategy)
    {
        this.TreasureValue = int.Parse(Console.ReadLine());
        this.StepCost = int.Parse(Console.ReadLine());
        this.ChamberCount = int.Parse(Console.ReadLine());
        this.MaxTreasurePickup = int.Parse(Console.ReadLine());

        strategy.Init(TreasureValue, StepCost, ChamberCount, MaxTreasurePickup);

        for (; ; )
        {
            int treasureCount = int.Parse(Console.ReadLine());
            int pathCount = int.Parse(Console.ReadLine());
            int time = int.Parse(Console.ReadLine());

            var plan = strategy.Turn(treasureCount, pathCount, time);

            Console.WriteLine(plan.take);
            if (plan.take > -1) Console.WriteLine(plan.next);
            Console.Out.Flush();
        }
    }

    public override void Plot()
    {
        throw new NotImplementedException();
    }

    public override void PrintStats(Strategy strategy)
    {
        throw new NotImplementedException();
    }
}