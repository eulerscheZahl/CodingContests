using System;
using System.Collections.Generic;

public class Solution
{
    public static void Main(string[] args)
    {
        Board board = new Board();
        board.ReadInitial();
        board.BuildTrees();
        //board = board.MoveHeuristic();
        //board.PrintActions();
        List<string> actions = board.Solve();
        Console.WriteLine(actions.Count);
        foreach (string a in actions) Console.WriteLine(a);
    }
}
