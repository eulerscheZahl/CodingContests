using System;
using System.Collections.Generic;

public class BridgeBuilder
{
    static void Main(string[] args)
    {
        Board board = new Board();
        List<Bridge> bridges = board.Solve();
        Console.WriteLine(bridges.Count);
        foreach (Bridge bridge in bridges) Console.WriteLine(bridge);
    }
}
