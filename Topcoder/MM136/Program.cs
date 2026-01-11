using System;
using System.Collections.Generic;

public class HungryKnights
{
    private static Queue<int> queue = new Queue<int>();
    public static int ReadInt()
    {
        while (queue.Count == 0)
        {
            foreach (string s in Console.ReadLine().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) queue.Enqueue(int.Parse(s));
        }
        return queue.Dequeue();
    }

    public static void Main(string[] args)
    {
        Board board = Board.ReadBoard();
        List<Move> result = board.Solve();
        Console.WriteLine(result.Count);
        for (int i = 0; i < result.Count; i++)
        {
            //Console.Error.WriteLine(i + ": " + result[i]);
            Console.WriteLine(result[i].Print());
        }
    }
}
