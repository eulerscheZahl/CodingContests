using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class ArenaEngine : Engine
{
    private int turn;
    private List<string> input = new List<string>();
    public override int Play(Strategy strategy)
    {
        this.N = int.Parse(Console.ReadLine());
        this.C = int.Parse(Console.ReadLine());

        strategy.Init(N, C);

        for (turn = 0; turn < 1000; turn++)
        {
            int[] nextBalls = new int[3];
            int[] grid = new int[N * N];
#if DEBUG
            input.Clear();
            input.Add(N.ToString());
            input.Add(C.ToString());
#endif
            //read grid
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    grid[x * N + y] = int.Parse(Console.ReadLine());
#if DEBUG
                    input.Add(grid[x * N + y].ToString());
#endif
                }
            }

            //read next balls
            for (int k = 0; k < 3; k++)
            {
                nextBalls[k] = int.Parse(Console.ReadLine());
#if DEBUG
                input.Add(nextBalls[k].ToString());
#endif
            }

            //read elapsed time
            int elapsedTime = int.Parse(Console.ReadLine());
#if DEBUG
            input.Add(elapsedTime.ToString());
            //PrintStats(strategy);
#endif

            //while (true) strategy.Turn(grid, nextBalls, elapsedTime);
            var plan = strategy.Turn(grid, nextBalls, elapsedTime);

#if DEBUG
            //Plot(grid, plan.p1, plan.p2, turn);
#endif
            Console.WriteLine(plan.p1.Y + " " + plan.p1.X + " " + plan.p2.Y + " " + plan.p2.X);
        }

        return 0;
    }

    public override void PrintStats(Strategy strategy)
    {
        File.WriteAllLines("plot/" + turn.ToString("000") + ".txt", input);
    }
}