using System;
using System.Collections.Generic;

public class HelpTheElves
{
    static void Main(string[] args)
    {
        Board.ReadInitial();
        int firstTurn = Board.Turn;
        for (; Board.Turn < Board.Area; Board.Turn++)
        {
            Board board = new Board();
            board.ReadInput();
#if DEBUG
            board.PrintDebug();
#endif
            List<string> moves = board.Plan();
            if (moves.Count == 0) Console.WriteLine(-1);
            else Console.WriteLine(string.Join(" ", moves));
            if (firstTurn != 0) break;
        }
    }
}
