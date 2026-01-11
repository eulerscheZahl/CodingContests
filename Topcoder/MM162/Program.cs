using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
#if LOCAL_ENV
    const int TIME_LIMIT_FINAL = 9700 * 2/5;
    const int TIME_LIMIT_1 = 9600 * 2/5;
    const int TIME_LIMIT_2 = 9000 * 2/5;
    const int TIME_LIMIT_3 = 8000 * 2/5;
#elif DEBUG
    const int TIME_LIMIT_FINAL = 9700000;
    const int TIME_LIMIT_1 = 9600000;
    const int TIME_LIMIT_2 = 9000000;
    const int TIME_LIMIT_3 = 8000000;
#else
    const int TIME_LIMIT_FINAL = 9700;
    const int TIME_LIMIT_1 = 9600;
    const int TIME_LIMIT_2 = 9000;
    const int TIME_LIMIT_3 = 8000;
#endif
    static void Main(string[] args)
    {
        Board board = new Board();
        board.ReadInput();
        board.FindBlockingGuards();
        Stopwatch sw = Stopwatch.StartNew();

        Board final = new Board(board, board.Thief, true);
        int runs = 0;
        int bestScore = int.MaxValue;
        for (; sw.ElapsedMilliseconds < TIME_LIMIT_2; runs++)
        {
            Board tmp = Beamsearch(board, sw, bestScore);
            if (tmp.Score < final.Score) final = tmp;
            bestScore = final.Score;
            Console.Error.WriteLine("finished run " + (runs + 1) + " at " + sw.ElapsedMilliseconds + " ms with score " + tmp.Score);
#if DEBUG
            break;
#endif
        }
        List<string> output = final.GetPath();
        Console.Error.WriteLine("TESTCASE_INFO {\"CoinsRemaining\": " + final.Coins.Count + ", \"GuardsRemaining\": " + final.Guards.Count + ", \"Turns\": " + output.Count + ", \"Escaped\": " + (final.OutOfBounds() ? 1 : 0) + ", \"Runs\": " + runs + "}");
        Console.Error.WriteLine(Board.expandCounter);
        Console.WriteLine(output.Count);
        foreach (string s in output) Console.WriteLine(s);
    }

    private static Board Beamsearch(Board board, Stopwatch sw, int bestScore)
    {
        Board final = board;
        int baseBeamWidth = (int)(40000 / Math.Pow(board.Guards.Count * board.Coins.Count, 0.4));
        int beamWidth = baseBeamWidth;
        Console.Error.WriteLine("beam width: " + beamWidth);
        int maxRuns = 2;
        for (int run = 0; run < maxRuns; run++)
        {
            List<Board> states = new List<Board> { board };
            board.ComputeBribeBonus(run == 2);
            Board currentFinal = board;
            for (int turn = 1 + board.Turn; turn <= 1000; turn++)
            {
                int time = (int)sw.ElapsedMilliseconds;
                if (time > TIME_LIMIT_FINAL) break;
                if (time > TIME_LIMIT_1) beamWidth = 1;
                if (time > TIME_LIMIT_2) beamWidth = 10;
                if (time > TIME_LIMIT_3) beamWidth = baseBeamWidth / 10;
                HashSet<int> hashes = new HashSet<int>();
                List<Board> newStates = new List<Board>();
                foreach (Board state in states.OrderBy(s => s.Score)) state.Expand(newStates, hashes);
                if (newStates.Count == 0) break;
                Board bestNew = newStates.OrderBy(s => s.Score).FirstOrDefault();
                if (bestNew.Score < currentFinal.Score) currentFinal = bestNew;
                states = newStates.Where(s => s.PredictMinScore() < bestScore).OrderBy(s => s.ComputeScore()).Take(beamWidth).ToList();
#if DEBUG
                //Console.Error.WriteLine("turn: " + i + "  min score: " + states.Where(s => !s.OutOfBounds()).Min(s => s.Score) + "  max score: " + states.Max(s => s.Score));
#endif
                bestScore = Math.Min(bestScore, currentFinal.Score);
            }
            if (currentFinal.Score < final.Score) final = currentFinal;
            board = currentFinal;
            for (int i = 0; i < 100; i++)
            {
                if (board.Parent != null) board = board.Parent;
            }
            if (run == 1 && final.Coins.Count > 0) maxRuns = 3;
        }
        return final;
    }
}