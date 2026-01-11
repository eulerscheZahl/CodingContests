using System;
using System.Diagnostics;
using System.Linq;

public class Column : IEquatable<Column>
{
    public int X;
    public int[] Target;
    public int[] Current;
    public int ColTopDist;
    public int TargetTopDist;
    public int Length;

    public int ColBottomDist => Board.Size - ColTopDist - Length;
    public int TargetBottomDist => Board.Size - TargetTopDist - Length;
    public bool IsFree(int y) => y < ColTopDist || y >= ColTopDist + Length;
    public int TargetTop => Target[TargetTopDist - 1];

    public Column(Board board, int x)
    {
        this.X = x;
        TargetTopDist = Board.Size;
        Current = new int[Board.Size];
        Target = new int[Board.Size];
        for (int y = Board.Size - 1; y >= 0; y--)
        {
            Current[y] = board.Tiles[(Board.Size + 1) * x + y];
            Target[y] = Board.targetTiles[(Board.Size + 1) * ((x + 1) % Board.Size) + y];
        }

        if (X == Board.Size - 1) return;
        for (int length = Board.Size; length > 0; length--)
        {
            bool valid = true;
            for (int y = 0; y < length; y++)
            {
                valid &= Current[y] == Target[Board.Size - length + y];
            }
            if (valid)
            {
                Length = length;
                TargetTopDist -= length;
                break;
            }
        }
    }

    public Column(Column column)
    {
        this.X = column.X;
        this.Target = column.Target;
        this.Current = column.Current.ToArray();
        this.ColTopDist = column.ColTopDist;
        this.TargetTopDist = column.TargetTopDist;
        this.Length = column.Length;
    }

    public void ExtendTop()
    {
        Length++;
        TargetTopDist--;
        ColTopDist = 0;
    }

    public override int GetHashCode()
    {
        int hash = 0;
        for (int y = 0; y < Current.Length; y++) hash ^= Board.zobrist[X, y, Current[y]];
        return hash;
    }

    public bool Equals(Column col)
    {
        for (int y = 0; y < Current.Length; y++)
        {
            if (this.Current[y] != col.Current[y]) return false;
        }
        return true;
    }
}