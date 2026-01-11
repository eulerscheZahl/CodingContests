using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class LocalEngine : Engine
{
    int numChambers = 100;
    int maxChambers;
    int numSteps = 0;
    int numTreasures = 0;
    bool[,] adj;
    bool[] gen;
    int[] tCount;
    int[][] paths;
    int[] nextChamber = new int[500];
    int curChamber = 0;
    int lastChamber = 1;
    int[] visitCount;
    private Random random;
    private int seed;
    public int maxPossibleScore;
    private int highest = 0;

    public LocalEngine(int seed)
    {
        this.seed = seed;
        random = new Random(seed);
        TreasureValue = random.Next(10, 101);
        StepCost = random.Next(1, TreasureValue + 1);
        maxChambers = random.Next(10, 101);
        MaxTreasurePickup = random.Next(1, 11);
        curChamber = 0;
        lastChamber = 1;
        nextChamber[0] = 0;
        adj = new bool[numChambers, numChambers];
        gen = new bool[numChambers];
        tCount = new int[numChambers];
        paths = new int[numChambers][];
        while (curChamber < lastChamber)
        {
            generateChamber(nextChamber[curChamber]);
            curChamber++;
        }
        visitCount = new int[numChambers];
        for (int i = 0; i < numChambers; i++)
        {
            List<int> p = new List<int>();
            for (int j = 0; j < numChambers; j++)
                if (i != j && adj[i, j])
                    p.Add(j);
            paths[i] = new int[p.Count];
            for (int j = 0; j < p.Count; j++)
                paths[i][j] = p[j];
        }

        for (int i = 0; i < numChambers; i++)
        {
            if (paths[i].Length == 0) continue;
            maxPossibleScore += TreasureValue * tCount[i];
            maxPossibleScore -= StepCost * (1 + (tCount[i] - 1) / MaxTreasurePickup);
        }
    }

    public override void Plot()
    {
        List<string> lines = new List<string> { "graph {" };
        for (int i = 0; i < numChambers; i++)
        {
            foreach (int j in paths[i])
            {
                if (j > i) lines.Add("   " + i + " -- " + j);
            }
        }
        lines.Add("}");
        File.WriteAllLines("graphs/" + seed + ".dot", lines.ToArray());
    }

    private void generateChamber(int c)
    {
        if (gen[c]) return;
        tCount[c] = random.Next(0, 51);
        int pathCount = random.Next(2, 5);
        int i = 0;
        int tries = 0;
        while (i < pathCount)
        {
            int n = random.Next(0, maxChambers);
            if (n == c || adj[c, n])
            {
                tries++;
                if (tries > maxChambers * 2) break;
                continue;
            }
            tries = 0;
            adj[c, n] = adj[n, c] = true;
            nextChamber[lastChamber] = n;
            lastChamber++;
            i++;
        }
        ChamberCount++;
        gen[c] = true;
    }

    private void moveNextChamber(int next)
    {
        if (next == -1)
        {
            if (lastChamber == -1) throw new Exception("no previous chamber");
            int temp = lastChamber;
            lastChamber = curChamber;
            curChamber = temp;
        }
        else
        {
            lastChamber = curChamber;
            curChamber = paths[curChamber][next];
        }
    }

    private long solverTime;
    private List<int> taken = new List<int>();
    public override int Play(Strategy strategy)
    {
        Stopwatch sw = Stopwatch.StartNew();
        curChamber = 0;
        visitCount[curChamber]++;
        lastChamber = -1;
        strategy.Init(TreasureValue, StepCost, ChamberCount, MaxTreasurePickup);
        var plan = strategy.Turn(tCount[curChamber], paths[curChamber].Length, numSteps, curChamber);
        while (plan.take != -1 && numSteps < 1000)
        {
            tCount[curChamber] -= plan.take;
            numTreasures += plan.take;
            taken.Add(plan.take);
            moveNextChamber(plan.next);
            numSteps++;
            visitCount[curChamber]++;
            highest = Math.Max(highest, numTreasures * TreasureValue - numSteps * StepCost);
            plan = strategy.Turn(tCount[curChamber], paths[curChamber].Length, numSteps, curChamber);
        }
        solverTime = sw.ElapsedMilliseconds;
        return Math.Max(0, numTreasures * TreasureValue - numSteps * StepCost);
    }

    public override void PrintStats(Strategy strategy)
    {
        int actualScore = (numTreasures * TreasureValue - numSteps * StepCost);
        int remainingScore = tCount.Sum() * TreasureValue - StepCost * tCount.Sum(t => (t + MaxTreasurePickup - 1) / MaxTreasurePickup);
        maxPossibleScore = Math.Max(maxPossibleScore, 1);
        Console.WriteLine($"\nseed={seed}   chambers={ChamberCount}    maxPickup={MaxTreasurePickup}   cost={StepCost}   value={TreasureValue}\n" +
            "Max possible score: " + maxPossibleScore + "\n" +
            $"achieved score:     {actualScore}  ({100 * actualScore / maxPossibleScore}%)\n" +
            $"highest score:      {highest}  ({100 * highest / maxPossibleScore}%)\n" +
            $"missing score:     {remainingScore} =>{100 * (actualScore + remainingScore) / maxPossibleScore}% \n" +
            "chambers found: " + strategy.AllChambers.Count + "    " + strategy.Realities.Count + " realities  " + (strategy.Realities[0].Failed ? "failed" : "fine") + "\n" +
            $"remaining treasures: {tCount.Sum()}   (expected: {strategy.AllChambers.Sum(c => c.TreasureCount)})\n" +
            "turns: " + numSteps + "   => average: " + (double)numTreasures / numSteps + "\n" +
            string.Join("-", taken) + "\n" +
            "time: " + solverTime + "ms\n"
        );
    }
}
