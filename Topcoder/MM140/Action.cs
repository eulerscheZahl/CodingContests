using System;
using System.Collections.Generic;

public enum ActionType
{
    TURN,
    MOVE,
    JUMP,
    WRITE
}

public abstract class Action : IEquatable<Action>
{
    protected int Hash;
    public abstract void Apply(Board board);
    public abstract int Cost(Board board);

    public abstract ActionType Type();
    public bool Equals(Action other)
    {
        if (other == null) return false;
        return this.Hash == other.Hash;
    }
}

public class TurnAction : Action
{
    public int Target;
    private int initialDirection;

    public TurnAction(int initial, int Target)
    {
        this.initialDirection = initial;
        this.Target = Target;
        this.Hash = (this.Target - initialDirection + 8) % 8;
    }

    public override void Apply(Board board)
    {
        board.Hash ^= Board.DirectionHash[board.Direction];
        board.Direction = (board.Direction + this.Target - initialDirection + 8) % 8;
        board.Hash ^= Board.DirectionHash[board.Direction];
    }

    public override int Cost(Board board)
    {
        int cost = Math.Abs(this.Target - initialDirection);
        if (cost > 4) cost = 8 - cost;
        return cost;
    }

    public override ActionType Type() => ActionType.TURN;

    public override string ToString()
    {
        if ((initialDirection + 1) % 8 == Target) return "R";
        if ((initialDirection + 2) % 8 == Target) return "R\nR";
        if ((initialDirection + 3) % 8 == Target) return "R\nR\nR";
        if ((initialDirection + 7) % 8 == Target) return "L";
        if ((initialDirection + 6) % 8 == Target) return "L\nL";
        if ((initialDirection + 5) % 8 == Target) return "L\nL\nL";
        if ((initialDirection + 4) % 8 == Target) return "L\nL\nL\nL";
        return "";
    }
}

public class MoveAction : Action
{
    public int Length;
    public bool Forward;

    public MoveAction(int length, bool forward)
    {
        Length = length;
        Forward = forward;
        this.Hash = (int)1e6 + (Forward ? 100 : 0) + Length;
    }

    public override void Apply(Board board)
    {
        board.Hash ^= board.Location.PositionHash;
        int dir = board.Direction;
        if (!Forward) dir = (dir + 4) % 8;
        for (int i = 0; i < Length; i++)
        {
            board.Location = board.Location.Neighbors[dir];
            board.FillCell();
        }
        board.Hash ^= board.Location.PositionHash;
    }

    public override int Cost(Board board) => 1;

    public override ActionType Type() => ActionType.MOVE;

    public override string ToString()
    {
        if (Forward) return "F " + Length;
        return "B " + Length;
    }
}

public class JumpAction : Action
{
    private Cell initial;
    public Cell Target;

    public JumpAction(Cell initial, Cell target)
    {
        this.initial = initial;
        this.Target = target;
        this.Hash = (int)2e6 + (Target.X - initial.X + Board.Size) % Board.Size * 100 + (Target.Y - initial.Y + Board.Size) % Board.Size;
    }
    public override void Apply(Board board)
    {
        board.Hash ^= board.Location.PositionHash;
        int dx = Target.X - initial.X;
        int dy = Target.Y - initial.Y;
        board.Location = Board.Grid[(board.Location.X + dx + Board.Size) % Board.Size, (board.Location.Y + dy + Board.Size) % Board.Size];
        board.FillCell();
        board.Hash ^= board.Location.PositionHash;
    }

    public override int Cost(Board board) => Board.JumpCost;

    public override ActionType Type() => ActionType.JUMP;

    public override string ToString()
    {
        int[] offset = initial.Offset(Target);
        return "J " + offset[1] + " " + offset[0];
    }
}

public class WriteAction : Action
{
    private bool writing;
    public WriteAction(bool writing)
    {
        this.writing = writing;
        this.Hash = (int)3e6 + (writing ? 1 : 0);
    }

    public override void Apply(Board board)
    {
        board.Writing = writing;
        board.FillCell();
    }

    public override int Cost(Board board) => 1;

    public override ActionType Type() => ActionType.JUMP;

    public override string ToString()
    {
        if (writing) return "D";
        return "U";
    }
}