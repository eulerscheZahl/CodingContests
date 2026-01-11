using System;
using System.Collections.Generic;
using System.Linq;

public class StopTheElves
{
    static void Main(string[] args)
    {
        Board.Size = int.Parse(Console.ReadLine());
        Board.BoxCost = int.Parse(Console.ReadLine());
        Board.ElfProp = double.Parse(Console.ReadLine());

        Board board = new Board();
        board.Read(true);
        board.FindArticulationPoints();
        board.FindBestRing(6000);

        int remainingTime = 0;
        for (int turn = 0; turn < Board.Turns; turn++)
        {
            board.Turn = turn;
            List<Cell> toBox = board.Play(remainingTime);
            if (toBox.Count == 0) Console.WriteLine(-1);
            else Console.WriteLine(string.Join(" ", toBox));

            remainingTime = 10000 - int.Parse(Console.ReadLine());
            board.Read(false);
        }

    }
}