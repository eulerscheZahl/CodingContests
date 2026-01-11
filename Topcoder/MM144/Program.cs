using System;
using System.Collections.Generic;

public class Solution
{
    public static void Main()
    {
        Board board = new Board();
        board.ReadInitial();

        List<List<Action>> plan = board.Solve();
        Console.WriteLine(plan.Count);
        for (int t = 0; t < plan.Count; t++)
        {
            Console.WriteLine(plan[t].Count);
            foreach (Action act in plan[t]) Console.WriteLine(act);
        }
    }
}