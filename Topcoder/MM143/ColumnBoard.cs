using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class ColumnBoard : IEquatable<ColumnBoard>
{
    public ColumnBoard Parent;
    public Column[] Columns;
    public int HandTile;
    public int SolvedCount;
    public int Hash;
    public int Depth;
    public int Action;
    public ColumnBoard(Board board)
    {
        this.Columns = new Column[Board.Size];
        for (int x = 0; x < Columns.Length; x++) Columns[x] = new Column(board, x);
        SolvedCount = Columns.Sum(c => c.Length);
        HandTile = board.Tiles[Board.Size];
    }

    public ColumnBoard(ColumnBoard board)
    {
        this.Parent = board;
        this.Columns = board.Columns.ToArray();
        this.HandTile = board.HandTile;
        this.SolvedCount = board.SolvedCount;
        this.Depth = board.Depth;
    }

    public ColumnBoard ApplyNew(int col, int type, int count)
    {
        ColumnBoard board = new ColumnBoard(this);
        board.Apply(col, type, count);
        return board;
    }

    public void Apply(int col, int type, int count)
    {
        Hash ^= Columns[col].GetHashCode();
        Action = (count << 8) + (col << 2) + type;
        Columns[col] = new Column(Columns[col]);
        Depth += count;

        Column column = Columns[col];
        for (int i = 0; i < count; i++)
        {
            if (type == Board.UP)
            {
                int free = column.Current[0];
                for (int y = 0; y < Board.Size - 1; y++) column.Current[y] = column.Current[y + 1];
                column.Current[Board.Size - 1] = HandTile;
                HandTile = free;
                if (column.Length > 0) column.ColTopDist--;
            }
            if (type == Board.DOWN)
            {
                int free = column.Current[Board.Size - 1];
                for (int y = Board.Size - 1; y > 0; y--) column.Current[y] = column.Current[y - 1];
                column.Current[0] = HandTile;
                HandTile = free;
                if (column.Length > 0) column.ColTopDist++;
            }
        }
        Hash ^= Columns[col].GetHashCode();
    }

    private ColumnBoard ExtendTop(int x)
    {
        ColumnBoard board = new ColumnBoard(this);
        board.Apply(x, Board.DOWN, 1);
        board.Columns[x].ExtendTop();
        board.SolvedCount++;
        return board;
    }

    private void StartTop(int x)
    {
        Columns[x].ExtendTop();
        SolvedCount++;
    }

    private IEnumerable<ColumnBoard> Expand()
    {
        for (int x = 0; x < Columns.Length - 1; x++)
        {
            if (Columns[x].Length == Board.Size) continue;
            // TODO also extend bottom
            int target = Columns[x].TargetTop;
            if (HandTile == target)
            {
                if (Columns[x].ColTopDist == 0) // can place tile in hand directly
                {
                    yield return ExtendTop(x);
                    continue;
                }
                // store hand in last column, shift X, restore hand, place at X
                ColumnBoard board = this.ApplyNew(Board.Size - 1, Board.UP, 1);
                board = board.ApplyNew(x, Board.UP, Columns[x].ColTopDist);
                board = board.ApplyNew(Board.Size - 1, Board.DOWN, 1);
                yield return board.ExtendTop(x);
                continue;
            }

            int xFrom = -1, yFrom = -1;
            int moves = 1000;
            for (int x2 = 0; x2 < Columns.Length; x2++)
            {
                if (x == x2)
                {
                    for (int y = 0; y < Board.Size; y++)
                    {
                        if (Columns[x2].Current[y] != target || !Columns[x2].IsFree(y)) continue;
                        int tmpMoves = Columns[x].ColTopDist;
                        if (y < Columns[x].ColTopDist) tmpMoves += 2;
                        else tmpMoves += 2 * (Board.Size - y);
                        if (tmpMoves < moves)
                        {
                            moves = tmpMoves;
                            xFrom = x2;
                            yFrom = y;
                        }
                    }
                    continue;
                }
                for (int y = 0; y < Board.Size; y++)
                {
                    if (Columns[x2].Current[y] != target || !Columns[x2].IsFree(y)) continue;
                    int tmpMoves = Columns[x].ColTopDist;
                    if (Columns[x2].Length == 0) tmpMoves += Math.Min(y + 1, Board.Size - y);
                    else if (y < Columns[x2].ColTopDist) tmpMoves += y + 1;
                    else tmpMoves += Board.Size - y;
                    if (tmpMoves < moves)
                    {
                        moves = tmpMoves;
                        xFrom = x2;
                        yFrom = y;
                    }
                }
            }
            // TODO check last column: sideways

            // take from own column
            if (x == xFrom)
            {
                if (yFrom < Columns[x].ColTopDist)
                {
                    if (Columns[x].Length == 0)
                    {
                        ColumnBoard board_ = this.ApplyNew(x, Board.UP, yFrom);
                        board_.StartTop(x);
                        yield return board_;
                        continue;
                    }
                    ColumnBoard board = this.ApplyNew(x, Board.UP, yFrom + 1);
                    board = board.ApplyNew(Board.Size - 1, Board.UP, 1);
                    if (board.Columns[x].ColTopDist > 0) board = board.ApplyNew(x, Board.UP, board.Columns[x].ColTopDist);
                    board = board.ApplyNew(Board.Size - 1, Board.DOWN, 1);
                    yield return board.ExtendTop(x);
                    continue;
                }
                ColumnBoard b = this.ApplyNew(x, Board.DOWN, Board.Size - yFrom);
                b = b.ApplyNew(Board.Size - 1, Board.UP, 1);
                b = b.ApplyNew(x, Board.UP, b.Columns[x].ColTopDist);
                b = b.ApplyNew(Board.Size - 1, Board.DOWN, 1);
                yield return b.ExtendTop(x);
                continue;
            }

            // from other column
            ColumnBoard colXTop0 = this;
            if (Columns[x].ColTopDist != 0) colXTop0 = this.ApplyNew(x, Board.UP, Columns[x].ColTopDist);
            if (Columns[xFrom].Length == 0)
            {
                if (2 * yFrom < Board.Size) colXTop0 = colXTop0.ApplyNew(xFrom, Board.UP, yFrom + 1);
                else colXTop0 = colXTop0.ApplyNew(xFrom, Board.DOWN, Board.Size - yFrom);
            }
            else if (Columns[xFrom].ColTopDist > yFrom) colXTop0 = colXTop0.ApplyNew(xFrom, Board.UP, yFrom + 1);
            else colXTop0 = colXTop0.ApplyNew(xFrom, Board.DOWN, Board.Size - yFrom);
            yield return colXTop0.ExtendTop(x);
        }
    }

    public IEnumerable<ColumnBoard> Solve(Stopwatch sw)
    {
        List<HashSet<ColumnBoard>> boards = new List<HashSet<ColumnBoard>>();
        boards.Add(new HashSet<ColumnBoard> { this });

        int beamWidth = 2 * 30 * 30 * 30 / (Board.Area * Board.Size);
        for (int depth = 0; depth < boards.Count; depth++)
        {
            if (sw.ElapsedMilliseconds > 9500) yield break;
            foreach (ColumnBoard board in boards[depth].OrderByDescending(b => b.SolvedCount).Take(beamWidth))
            {
                if (board.SolvedCount == Board.Size * (Board.Size - 1))
                {
                    yield return board;
                    continue;
                }
                foreach (ColumnBoard b2 in board.Expand())
                {
                    while (boards.Count <= b2.Depth) boards.Add(new HashSet<ColumnBoard>());
                    boards[b2.Depth].Add(b2);
                }
            }
        }
    }

    public Board ToBoard()
    {
        Board board = new Board();
        board.Tiles = new int[Board.Size * (Board.Size + 1)];
        for (int y = 0; y < Board.Size; y++)
        {
            for (int x = 0; x < Board.Size; x++)
            {
                board.Tiles[(Board.Size + 1) * x + y] = Columns[x].Current[y];
            }
        }
        board.Tiles[Board.Size] = HandTile;
        return board;
    }

    public List<string> GetActions()
    {
        List<string> actions = new List<string>();
        ColumnBoard b = this;
        while (b.Parent != null)
        {
            int count = b.Action >> 8;
            int col = (b.Action >> 2) & 0x3f;
            int type = b.Action & 3;
            for (int i = 0; i < count; i++) actions.Add(Board.dirs[type] + " " + col);
            b = b.Parent;
        }
        actions.Reverse();
        return actions;
    }

    public override int GetHashCode() => Hash;

    public bool Equals(ColumnBoard board)
    {
        for (int x = 0; x < Columns.Length; x++)
        {
            if (!this.Columns[x].Equals(board.Columns[x])) return false;
        }
        return true;
    }
}