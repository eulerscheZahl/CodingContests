using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
#if LOCAL_ENV
    public const int MAX_TIME = 4000;
#else
    public const int MAX_TIME = 9800;
#endif

    static void Main(string[] args)
    {
        Board board = new Board();
        board.ReadInitial();
        List<string> output = board.Solve();
        Console.WriteLine(output.Count);
        foreach (string s in output) Console.WriteLine(s);
    }
}