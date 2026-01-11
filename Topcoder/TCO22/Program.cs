using System;
using System.Collections.Generic;
using System.Linq;

public class Solution
{
    public static void Main(string[] args)
    {
        Board board = new Board();
        board.ReadInitial();
        int remainingTime = 10000;

        for (int turn = 0; turn < 1000; turn++)
        {
#if DEBUG
            Console.Error.WriteLine("\nTurn: " + turn);
            Console.Error.WriteLine(board.GetInputs());
#endif
            Board action = null;
            if (remainingTime > 300) action = board.Plan(remainingTime, turn);
            int actionIndex = board.Tiles.OrderByDescending(t => t.FruitsSet).First().Index;
            if (action == null || action.ActionTile == null)
            {
                if (action != null) actionIndex = action.DropIndex;
                Console.WriteLine(actionIndex);
            }
            else
            {
                while (action.Parent.Parent != null) action = action.Parent;
#if DEBUG
                Console.Error.WriteLine("action: " + action.PrintAction());
#endif
                Console.WriteLine(action.PrintAction());
                actionIndex = action.ActionTile.Index;
                board = action;
            }
            string tileText = Console.ReadLine();
            Tile newTile = new Tile(tileText) { Index = actionIndex };
            board.Tiles[newTile.Index] = newTile;
            board.ClearParent();
            int elapsedTime = int.Parse(Console.ReadLine());
            remainingTime = 10000 - elapsedTime;
        }
    }
}
