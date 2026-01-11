using System.Collections.Generic;
using System;
using System.Linq;

public class PipeConnector
{
    private static Queue<int> queue = new Queue<int>();
    public static int ReadInt()
    {
        while (queue.Count == 0)
        {
            string line = Console.ReadLine();
            if (debug) Console.Error.WriteLine(line);
            foreach (string s in line.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) queue.Enqueue(int.Parse(s));
        }
        return queue.Dequeue();
    }

private static bool debug;
    static void Main(string[] args)
    {
        debug = args.ToList().Contains("debug");
        Board board = new Board();
        board.ReadInput();

        List<Path> solution = board.Solve();
        Console.WriteLine(solution.Count);
        foreach (Path path in solution) Console.WriteLine(path.Print());
    }
}
