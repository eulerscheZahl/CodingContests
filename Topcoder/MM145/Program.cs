using System;
using System.Collections.Generic;

public class Solution
{
    public static string ReadLine()
    {
        string line = Console.ReadLine();
        return line;
    }

    public static void Main()
    {
        Board board = new Board();
        board.ReadInitial();

        for (int turn = 0; turn < 1000; turn++)
        {
            board.ReadTurn();
            List<string> actions = board.Plan(turn);
            if (turn == 999) Console.Error.WriteLine("TESTCASE_INFO {\"Rollouts\": " + board.totalRuns + "}");
            foreach (string action in actions) Console.WriteLine(action);
        }
    }
}