using System;
using System.Threading.Tasks;

public class LostTreasureHunter
{
    private static void Batch(int count, bool parallel)
    {
        double totalScore = 0;
        if (parallel)
        {
            Parallel.For(0, count, seed =>
            {
                double score = SingleRun(seed, new MemoryStrategy(EMPTY_LIMIT));
                totalScore += score;
            });
        }
        else
        {
            for (int seed = 0; seed < count; seed++)
            {
                double score = SingleRun(seed, new MemoryStrategy(EMPTY_LIMIT));
                totalScore += score;
                Console.WriteLine("intermediate score: " + totalScore * 100 / (1 + seed));
            }
        }
        Console.WriteLine("batch score: " + totalScore * 100 / count);
    }

    private static double SingleRun(int seed, Strategy s)
    {
        LocalEngine engine = new LocalEngine(seed);
        double score = engine.Play(s);
        double max = engine.maxPossibleScore;
        engine.PrintStats(s);
        if (max == 0) return 1;
        return score / max;
    }

    private static void PrintStats(int seed, Strategy strategy)
    {
        Engine engine = new LocalEngine(seed);
        engine.Play(strategy);
        engine.PrintStats(strategy);
    }

    private static void Submit()
    {
        Engine engine = new ArenaEngine();
        Strategy strategy = new MemoryStrategy(EMPTY_LIMIT);
        engine.Play(strategy);
    }

    private static void Plot()
    {
        Engine engine = new LocalEngine(0);
        engine.Plot();
    }

    const int EMPTY_LIMIT = 25;
    static void Main(string[] args)
    {
#if DEBUG
        //PrintStats(95, new MemoryStrategy(25));
        // PrintStats(9, new MemoryStrategy(25));
        // PrintStats(12, new MemoryStrategy(25));
        // PrintStats(15, new MemoryStrategy(25));
        // PrintStats(5, new MemoryStrategy(25));
        // PrintStats(8, new MemoryStrategy(25));

        // for (int seed = 0; seed < 20; seed++)
        //     PrintStats(seed, new MemoryStrategy(25));
        //return;
        Batch(1000, false);
#else
        Submit();
#endif
    }
}
