using System;
using System.Collections.Generic;
using System.Linq;

public class Board : IEquatable<Board>
{
    public static int Size;
    public static int Area => Size * Size;
    public static int JumpCost;
    public static int LoopCost;
    public static Cell[,] Grid;
    public static List<Cell> TargetCells = new List<Cell>();
    public static int ToFill;
    private static int idCounter;
    private static Random random = new Random(0);

    public int[] Visited;
    public Cell Location;
    public int Direction;
    public bool Writing;
    public Board Parent;
    public Action Action;
    public int Filled;
    public int Cost;
    public int ID;
    public double Score => Filled * (JumpCost + 0.5) - Cost;

    public Board() { }

    public Board(Board board)
    {
        ID = idCounter++;
        if (board == null)
        {
            Visited = new int[Size];
            return;
        }
        this.Visited = (int[])board.Visited.Clone();
        this.Location = board.Location;
        this.Direction = board.Direction;
        this.Writing = board.Writing;
        this.Filled = board.Filled;
        this.Cost = board.Cost;
        this.Parent = board.Parent;
        this.Action = board.Action;
        this.Hash = board.Hash;
    }

    Action[] pastActions = new Action[10];
    public Board(Board parent, Action action) : this(parent)
    {
        this.Parent = parent;
        this.Action = action;
        this.Cost += action.Cost(this);
        action.Apply(this);

        //if (PrintActions().SequenceEqual(new[] { "D", "R\nR", "F 2", "L", "F 3", "L", "F 3", "L", "F 3", "L", "F 3" }))
        //    System.Diagnostics.Debugger.Break();

        // does not detect ABAC|ABAC|ABAC...

        pastActions[0] = action;
        int pastActionsCount = 1;
        Board pastBoard = Parent;
        int loopLength = 100;
        int loopInnerCost = 0;
        Board loopBoard = null;
        for (int i = 0; pastBoard.Action != null && i < 7; i++)
        {
            pastActions[pastActionsCount++] = pastBoard.Action;
            loopInnerCost += pastBoard.Action.Cost(this);
            if (pastActions[pastActionsCount - 1].Equals(pastActions[0]))
            {
                loopLength = i + 1;
                loopBoard = pastBoard;
                break;
            }
            pastBoard = pastBoard.Parent;
        }
        if (loopBoard == null || loopBoard.Filled == this.Filled) return;
        int loopCount = 1;
        while (pastBoard.Action != null)
        {
            bool valid = true;
            for (int i = 0; valid && i < loopLength; i++)
            {
                valid &= pastActions[i].Equals(pastBoard.Action);
                pastBoard = pastBoard.Parent;
                if (valid && i + 1 == loopLength) loopBoard = pastBoard;
            }
            if (!valid) break;
            loopCount++;
        }

        if (loopCount > 1 && !this.Action.Equals(loopBoard.Action))
        {
            int oldDiscount = Math.Max(0, (loopCount - 1) * loopInnerCost - (loopInnerCost + LoopCost));
            int newDiscount = Math.Max(0, loopCount * loopInnerCost - (loopInnerCost + LoopCost));
            Cost -= newDiscount - oldDiscount;
        }
    }

    public void BuildIslands()
    {
        List<Board> path = GetPath();
        List<Cell> islands = new List<Cell>();
        for (int i = 1; i < path.Count; i++)
        {
            if (path[i].Action.Type() == ActionType.JUMP && (i + 1 == path.Count || path[i + 1].Action.Type() == ActionType.JUMP))
                islands.Add(path[i].Location);
        }

        for (int i = 0; i < islands.Count; i++)
        {
            for (int j = i + 1; j < islands.Count; j++)
            {
                Cell c1 = islands[i];
                Cell c2 = islands[j];
                int[] offset = c1.Offset(c2);
                Cell prev = Grid[(c1.X - offset[0] + Board.Size) % Board.Size, (c1.Y - offset[1] + Board.Size) % Board.Size];
                if (!prev.Target || islands.Contains(prev)) continue;
                List<Cell> chain = new List<Cell> { prev, c1, c2 };
                while (islands.Contains(c2) && c2.Target)
                {
                    c2 = Grid[(c2.X + offset[0] + Board.Size) % Board.Size, (c2.Y + offset[1] + Board.Size) % Board.Size];
                    chain.Add(c2);
                }
                if (c2.Target)
                {
                    c2.Chains.Add(chain.Reverse<Cell>().Skip(1).ToList());
                    prev.Chains.Add(chain.Skip(1).ToList());
                }
            }
        }
    }

    public bool GetVisited(Cell cell) => ((Visited[cell.Y] >> cell.X) & 1) == 1;
    public void SetVisited(Cell cell) => Visited[cell.Y] |= 1 << cell.X;

    public int CountVisited()
    {
        int result = 0;
        foreach (int v in Visited)
        {
            int tmp = v;
            while (tmp > 0)
            {
                result++;
                tmp ^= tmp & -tmp;
            }
        }
        return result;
    }

    public void Read()
    {
        Size = int.Parse(Console.ReadLine().Split().Last());
        JumpCost = int.Parse(Console.ReadLine().Split().Last());
        LoopCost = int.Parse(Console.ReadLine().Split().Last());

        Grid = new Cell[Size, Size];
        Visited = new int[Size];
        int x = 0, y = 0;
        while (y < Size)
        {
            string line = Console.ReadLine();
            if (line.StartsWith("Grid")) line = Console.ReadLine();
            foreach (char c in line)
            {
                Grid[x, y] = new Cell(x, y, c, random);
                if (c == '+')
                {
                    SetVisited(Grid[x, y]);
                    Filled++;
                }
                if (c >= '0' && c <= '7')
                {
                    Location = Grid[x, y];
                    SetVisited(Grid[x, y]);
                    Filled++;
                    Direction = c - '0';
                    Writing = true;
                }
                x++;
                if (x == Size)
                {
                    x = 0;
                    y++;
                }
            }
        }
        foreach (Cell cell in Grid) cell.MakeNeighbors();
        foreach (Cell cell in Grid) cell.GenerateStatistics();
        foreach (Cell cell in Grid)
        {
            if (cell.Target)
            {
                ToFill++;
                TargetCells.Add(cell);
            }
        }
        if (Location == null) Location = Grid[0, 0];

        for (int dir = 0; dir < 4; dir++)
        {
            DirectionHash[dir] = random.Next();
            DirectionHash[dir + 4] = DirectionHash[dir];
        }
    }

    public IEnumerable<Board> Expand(bool multipleEnds, bool allowEmpty, bool allowJump)
    {
        if (allowEmpty && Parent != null && Parent.Parent != null && Parent.Parent.Filled == Filled)
            allowEmpty = false;

        if (Location.Target && !Writing)// && !GetVisited(Location))
        {
            yield return new Board(this, new WriteAction(true));
            yield break;
        }

        // turn
        if (Parent == null || Parent.Direction == this.Direction)
        {
            for (int deltaDir = -1; deltaDir <= 2; deltaDir++)
            {
                if (deltaDir == 0) continue;
                if (!Location.ValidTurnDirection[(Direction + deltaDir + 8) % 8]) continue;
                yield return new Board(this, new TurnAction(Direction, (Direction + deltaDir + 8) % 8));
            }
        }

        // move 
        bool hasMoved = false;
        foreach (int dir in new[] { Direction, (Direction + 4) % 8 })
        {
            int length = -1;
            int filling = 0;
            Cell pos = Location;
            while (length + 1 < Size && pos.Target)
            {
                pos = pos.Neighbors[dir];
                if (pos.Target && !GetVisited(pos)) filling++;
                length++;
            }
            if (filling > 0 || length >= 1 && allowEmpty)
            {
                if (filling > 0) hasMoved = true;
                yield return new Board(this, new MoveAction(length, dir == Direction));
                if (multipleEnds)
                {
                    while (length > 1)
                    {
                        pos = pos.Neighbors[(dir + 4) % 8];
                        if (!pos.Target || !GetVisited(pos)) break;
                        length--;
                        yield return new Board(this, new MoveAction(length, dir == Direction));
                    }
                }
            }
        }

        // chain jumps
        foreach (List<Cell> chain in Location.Chains)
        {
            Board final = this;
            foreach (Cell cell in chain) final = new Board(final, new JumpAction(final.Location, cell));
            yield return final;
        }

        //if (!hasMoved && Writing) yield return new Board(this, new WriteAction(false));

        // jump
        if (!hasMoved && allowJump)
        {
            List<Cell> jumpCands = new List<Cell>();
            int[] tested = new int[Size];
            foreach (Cell cell in TargetCells)
            {
                if (GetVisited(cell) || ((tested[cell.Y] >> cell.X) & 1) == 1) continue;
                for (int dir = 0; dir < 4; dir++)
                {
                    List<Cell> path = new List<Cell> { cell };
                    Cell pos = cell;
                    while (pos.Neighbors[dir].Target)
                    {
                        pos = pos.Neighbors[dir];
                        if (!GetVisited(pos)) path.Add(pos);
                        if (pos == cell) break;
                    }
                    pos = cell;
                    while (pos.Neighbors[(dir + 4) % 8].Target)
                    {
                        pos = pos.Neighbors[(dir + 4) % 8];
                        if (!GetVisited(pos)) path.Insert(0, pos);
                        if (pos == cell) break;
                    }
                    if (((tested[path[0].Y] >> path[0].X) & 1) == 0) jumpCands.Add(path[0]);
                    if (path.Count > 1 && ((tested[path[path.Count - 1].Y] >> path[path.Count - 1].X) & 1) == 0) jumpCands.Add(path.Last());
                    foreach (Cell p in path) tested[p.Y] |= 1 << p.X;
                }
            }

            int jumpTargets = Math.Min(jumpCands.Count, 100 / Math.Max(1, jumpCands.Count));
            if (jumpTargets == 0 && jumpCands.Count > 0) jumpTargets = 1;
            for (int i = 1; i <= jumpTargets; i++)
            {
                int idx = random.Next(jumpTargets - i);
                Cell tmp = jumpCands[jumpTargets - i];
                jumpCands[jumpTargets - i] = jumpCands[idx];
                jumpCands[idx] = tmp;
                yield return new Board(this, new JumpAction(Location, jumpCands[jumpTargets - i]));
            }
        }
    }

    public void FillCell()
    {
        if (Writing && !GetVisited(Location))
        {
            SetVisited(Location);
            Filled++;
            Hash ^= Location.WritingHash;
        }
    }

    public List<Board> GetPath()
    {
        List<Board> path = new List<Board> { this };
        Board current = this;
        while (current.Parent != null)
        {
            current = current.Parent;
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    public List<Action> GetActions() => GetPath().Where(p => p.Action != null).Select(p => p.Action).ToList();

    public List<string> PrintActions()
    {
        List<string> result = GetActions().Select(a => a.ToString()).ToList();
        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].StartsWith("LOOP"))
            {
                int loopCount = int.Parse(result[i].Split()[1]);
                int loopLength = int.Parse(result[i].Split()[2]);
                result.Insert(i - loopLength, "FOR " + loopCount);
                result[i + 1] = "END";
            }
        }
        return result;
    }

    public string PrintCurrent()
    {
        string result = "";
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (Grid[x, y] == Location) result += Direction;
                else if (Grid[x, y].Target) result += GetVisited(Grid[x, y]) ? '+' : '#';
                else result += '.';
            }
            result += "\n";
        }
        return result;
    }

    public static int[] DirectionHash = new int[8];
    public int Hash;
    public override int GetHashCode() => Hash;

    public bool Equals(Board other)
    {
        if (this.Location != other.Location) return false;
        if (this.Direction % 4 != other.Direction % 4) return false;
        for (int i = 0; i < Size; i++)
        {
            if (this.Visited[i] != other.Visited[i]) return false;
        }
        return true;
    }

    public override string ToString() => Location + "@" + Direction + " C" + Cost + " F" + Filled + " :" + Action;
}