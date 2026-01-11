using System;
using System.Collections.Generic;
using System.Diagnostics;

public class TrafficController
{
    static void Main(string[] args)
    {
        Board board = new Board();
        board.ReadInitial();

        Stopwatch sw = Stopwatch.StartNew();

        for (int turn = 0; turn < 1000; turn++)
        {
#if DEBUG
            Console.Error.WriteLine("Turn " + turn);
#endif
            board.ReadCurrent();
            board.PlayTurn(turn);
        }
    }
}