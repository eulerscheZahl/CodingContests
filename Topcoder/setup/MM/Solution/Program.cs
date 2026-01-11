using System;
using System.Collections.Generic;

public class Solution
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
        Board board = new Board();
        board.ReadInitial();
    }
}
