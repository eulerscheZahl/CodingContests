using System;
using System.Collections.Generic;

public class Cell
{
    public bool Target;
    public int X;
    public int Y;
    public int ID;
    public int PositionHash;
    public int WritingHash;
    public Cell[] Neighbors = new Cell[8];
    public bool[] ValidTurnDirection = new bool[8];
    public List<List<Cell>> Chains = new List<List<Cell>>();

    public Cell(int x, int y, char c, Random random)
    {
        this.X = x;
        this.Y = y;
        this.ID = x + y * Board.Size;
        Target = c != '.';

        PositionHash = random.Next();
        WritingHash = random.Next();
    }

    public int[] Offset(Cell cell)
    {
        return new[]
        {
            (cell.X - this.X + Board.Size) % Board.Size,
            (cell.Y - this.Y + Board.Size) % Board.Size
        };
    }

    public bool NeedsVisit(Board board)
    {
        return Target && !board.GetVisited(this);
    }

    private static int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
    public void MakeNeighbors()
    {
        for (int dir = 0; dir < 8; dir++)
        {
            int x = (X + Board.Size + dx[dir]) % Board.Size;
            int y = (Y + Board.Size + dy[dir]) % Board.Size;
            Neighbors[dir] = Board.Grid[x, y];
        }
    }

    public override string ToString() => X + "/" + Y;

    int[] maxLength = new int[8];
    public void GenerateStatistics()
    {
        for (int dir = 0; dir < 8; dir++)
        {
            Cell current = this;
            int length = 0;
            while (current.Target && length < Board.Size)
            {
                current = current.Neighbors[dir];
                length++;
            }
            maxLength[dir] = length;

            ValidTurnDirection[dir] = Neighbors[dir].Target || Neighbors[(dir + 4) % 8].Target;
        }
    }
}