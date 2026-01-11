using System;

public class ShearTheSheep
{
    static void Main(string[] args)
    {
        Board.Size = int.Parse(Console.ReadLine());
        Farmer.Capacity = int.Parse(Console.ReadLine());

        int elapsedTime = 0;
        for (int turn = 0; turn < 1000; turn++)
        {
            int remainingTurns = 1000 - turn;
            int remainingTime = 10000 - elapsedTime;
            Board board = new Board();
#if DEBUG
            remainingTime = 10000;
#endif
            if (remainingTime > 100)
            {
                Console.WriteLine(new Plan(board, remainingTurns));
            }
            else Console.WriteLine(board.Farmers[0].Cell.Y + " " + board.Farmers[0].Cell.X + " N");
            Debug(turn + 1);
            elapsedTime = int.Parse(Console.ReadLine());
        }
    }

    public static void Debug(object s)
    {
#if DEBUG
        //Console.Error.WriteLine(s);
#endif
    }
}
