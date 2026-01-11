using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    public const int SEARCH_DEPTH = 2;
    public static int Size;
    public static int BoxCost;
    public static double ElfProbability;
    public static int Money;
    public static int ElapsedTime;
    public static int ToalSims;
    public static int Turn;
    public static int Area => Size * Size;
    public static Cell[,] Grid;
    public static List<Cell> Cells = new List<Cell>();

    public Board() { }

    public static void ReadInitial()
    {
        Turn = 0;
        Size = int.Parse(Console.ReadLine().Split().Last());
        AiTracker.PastBoxSpawns = new int[Size, Size];
        for (int x = 0; x < Board.Size; x++)
        {
            for (int y = 0; y < Board.Size; y++) AiTracker.PastBoxSpawns[x, y] = -1000;
        }

        string line = Console.ReadLine();
        if (line.StartsWith("Turn: "))
        {
            Turn = int.Parse(line.Split().Last());
            AiTracker.EnemyAis = Console.ReadLine().Split().Select(int.Parse).ToList();
            for (int y_ = 0; y_ < Size; y_++)
            {
                int[] spawns = Console.ReadLine().Split().Select(int.Parse).ToArray();
                for (int x_ = 0; x_ < Size; x_++) AiTracker.PastBoxSpawns[x_, y_] = spawns[x_];
            }
            BoxCost = int.Parse(Console.ReadLine().Split().Last());
        }
        else BoxCost = int.Parse(line.Split().Last());
        ElfProbability = double.Parse(Console.ReadLine().Split().Last());
        Money = int.Parse(Console.ReadLine().Split().Last());

        Grid = new Cell[Size, Size];
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                Grid[x, y] = new Cell(x, y);
                Cells.Add(Grid[x, y]);
            }
        }
        foreach (Cell cell in Cells) cell.MakeNeighbors();
    }

    public void ReadInput()
    {
        ElapsedTime = int.Parse(Console.ReadLine().Split().Last());
        Money = int.Parse(Console.ReadLine().Split().Last());

        int x = 0, y = 0;
        while (y < Size)
        {
            string line = Console.ReadLine().Replace(" ", "");
            if (line.StartsWith("Grid")) line = Console.ReadLine().Replace(" ", "");
            foreach (char c in line)
            {
                Cell cell = Grid[x, y];
                cell.SetType(c);
                if (cell.CellType == CellType.ELF || cell.CellType == CellType.ELF_WITH_BOX || cell.CellType == CellType.ELF_WITH_PRESENT) new Elf(cell);
                else cell.Elf = new Elf[cell.Elf.Length];
                x++;
                if (x == Size)
                {
                    x = 0;
                    y++;
                }
            }
        }

        AiTracker.Track();
    }

    public List<string> Plan()
    {
        bool[,] blockedForEntrance = new bool[Size, Size];
        foreach (Cell cell in Cells.Where(c => c.HasTree)) blockedForEntrance[cell.X, cell.Y] = true;
        bool[,] blockedForExit = new bool[Size, Size];
        foreach (Cell cell in Cells.Where(c => c.IsSolid)) blockedForExit[cell.X, cell.Y] = true;
        List<string> actions = new List<string>();

        List<Elf> elves = Cells.Select(c => c.Elf[0]).Where(e => e != null).ToList();
        Elf lastElf = null;
        if (Cells.Count(c => c.HasPresent) == 0) lastElf = elves.FirstOrDefault(e => e.HasPresent);
        foreach (Elf elf in elves.Where(e => !e.IsFree && e.Plan[0].IsEdge).ToList())
        //foreach (Elf elf in elves.Where(e => e.Plan[0].IsEdge && (e.HasPresent || e.HasBox && !e.Plan[0].Neighbors.Any(n => n != null && AiTracker.BlockworthyGrid[n.X, n.Y] && (n.Elf[0] == null || !n.Elf[0].HasBox)))).ToList())
        {
            if (Turn + 1 < Area && elf == lastElf) continue;
            elf.Escape(1);
            elves.Remove(elf);
            actions.Add(elf.PrintAction());
        }

        int[,] distToEdge = BFS(blockedForExit, Cells.Where(c => c.IsEdge).ToList());
        List<Cell> presents = Cells.Where(c => c.HasPresent || c.Elf[0] != null && c.Elf[0].HasPresent && distToEdge[c.X, c.Y] == 1000).ToList();
        if (presents.Count == 0) presents = Cells.Where(c => c.HasBox).ToList();
        int[,] distToPresents = BFS(blockedForEntrance, presents);
        int[,] distToPresentsExtended = BFS(blockedForEntrance, Cells.Where(c => c.HasPresent || c.Elf[0] != null && c.Elf[0].HasPresent).ToList());
        elves = elves.Where(e => distToPresents[e.Plan[0].X, e.Plan[0].Y] < 1000 || distToPresentsExtended[e.Plan[0].X, e.Plan[0].Y] < 1000).ToList(); // don't move those stuck at edge
        foreach (Cell cell in Cells)
        {
            if (AiTracker.ProbablyBlocked[cell.X, cell.Y] && cell.Neighbors.Count(n => n.HasTree || n.HasBox) >= 2)
            {
                blockedForExit[cell.X, cell.Y] = true;
                blockedForEntrance[cell.X, cell.Y] = true;
            }
        }
        distToEdge = BFS(blockedForExit, Cells.Where(c => c.IsEdge).ToList());
        int[,] newDistToPresents = BFS(blockedForEntrance, presents);
        if (Cells.Where(c => c.IsEdge).Any(p => newDistToPresents[p.X, p.Y] < 1000)) distToPresents = newDistToPresents;


        double penalty = ComputePenalty(elves, distToEdge, distToPresents, lastElf != null);
        Random random = new Random(0);
        int turnSims = 700 * 900 / Area;
        if (ElapsedTime > 200)
        {
            if (ElapsedTime >= 9000) turnSims = 200;
            else
            {
                int remainingTurns = Area - Turn;
                int remainingTime = 9000 - ElapsedTime;
                double simsPerMs = (double)ToalSims / ElapsedTime;
                turnSims = (int)(simsPerMs * remainingTime / remainingTurns);
                turnSims = Math.Max(200, turnSims);
            }
        }
#if DEBUG
        turnSims = 200;
#endif
        //foreach (Elf skipper in elves)
        //{
        //    if (skipper.Plan[0] != skipper.Plan[1] || skipper.Plan[0] != skipper.Plan[2]) continue;
        //    foreach (Cell intermediate in skipper.Plan[0].Neighbors)
        //    {
        //        if (!AiTracker.BlockworthyGrid[intermediate.X, intermediate.Y] || intermediate.Elf[0] == null) continue;
        //        if (intermediate.Elf[0] != intermediate.Elf[1] || intermediate.Elf[0] != intermediate.Elf[2]) continue;
        //        foreach (Cell final in intermediate.Neighbors)
        //        {
        //            if (!final.IsEmpty || final == skipper.Plan[0] ||
        //                final.Elf[0] != null ||
        //                (final.Elf[1] != null && final.Elf[1] != intermediate.Elf[0]) ||
        //                final.Elf[2] != null) continue;
        //            Cell avoid = intermediate.Neighbors.FirstOrDefault(a => a != final && a.IsEmpty);
        //            if (avoid == null || ((avoid.Elf[1] != null && avoid.Elf[1] != intermediate.Elf[0])) || (avoid.Elf[2] != null && avoid.Elf[2] != skipper)) continue;
        //
        //            skipper.Plan[0].Elf[1] = null;
        //            skipper.Plan[0].Elf[2] = null;
        //            skipper.Plan[1] = intermediate;
        //            skipper.Plan[2] = final;
        //            intermediate.Elf[1] = skipper;
        //            final.Elf[2] = skipper;
        //            intermediate.Elf[0].Plan[1] = avoid;
        //            avoid.Elf[1] = intermediate.Elf[0];
        //
        //            double newPenalty = ComputePenalty(elves, distToEdge, distToPresents, lastElf != null);
        //            if (newPenalty <= penalty)
        //            {
        //                penalty = newPenalty;
        //                skipper.Backup();
        //                intermediate.Elf[0].Backup();
        //            }
        //            else
        //            {
        //                skipper.Restore();
        //                intermediate.Elf[0].Restore();
        //            }
        //        }
        //    }
        //}

        for (int i = 0; elves.Count > 0 && i < turnSims; i++)
        {
            ToalSims++;
            List<Elf> moving = new List<Elf> { elves[random.Next(elves.Count)] };
            bool loop = false;
            for (int depth = 1; !loop && depth <= SEARCH_DEPTH; depth++)
            {
                List<Elf> nextMoving = new List<Elf>();
                while (moving.Count > 0)
                {
                    Elf elf = moving[random.Next(moving.Count)];
                    Cell prev = null;
                    while (elf != null)
                    {
                        if (nextMoving.Contains(elf)) { loop = true; break; }
                        nextMoving.Add(elf);
                        moving.Remove(elf);
                        if (elf.Plan[depth].Elf[depth] == elf) elf.Plan[depth].Elf[depth] = null;
                        elf.Plan[depth] = elf.RandomTarget(random, depth - 1, elf.IsFree ? distToPresents : distToEdge, prev);
                        if (elf.Plan[depth] == null) { loop = true; break; }
                        prev = elf.Plan[depth - 1];
                        Elf nextElf = elf.Plan[depth].Elf[depth];
                        elf.Plan[depth].Elf[depth] = elf;
                        if (elf == nextElf) break;
                        elf = nextElf;
                    }
                }
                if (!loop && GetMoveOrdering(elves, depth) == null) loop = true;
                moving = nextMoving;
                nextMoving = new List<Elf>();
            }

            double newPenalty = loop ? int.MaxValue : ComputePenalty(elves, distToEdge, distToPresents, lastElf != null);
            if (newPenalty <= penalty && !loop)
            {
                penalty = newPenalty;
                foreach (Elf e in moving) e.Backup();
            }
            else
            {
                foreach (Elf e in moving) e.Restore();
            }
            //Validate(elves);
        }

        elves = GetMoveOrdering(elves, 1);
        actions.AddRange(elves.Select(e => e.PrintAction()));
#if DEBUG
        Console.Error.WriteLine("penalty: " + ComputePenalty(elves, distToEdge, distToPresents, lastElf != null));
#endif
        return actions.Where(a => a != null).ToList();
    }

    private void Validate(List<Elf> elves)
    {
        for (int d = 0; d <= SEARCH_DEPTH; d++)
        {
            foreach (Cell cell in Cells)
            {
                Elf elf = cell.Elf[d];
                if (elf != null && elf.Plan[d] != cell) throw new Exception();
            }
            foreach (Elf elf in elves)
            {
                Cell cell = elf.Plan[d];
                if (cell.Elf[d] != elf) throw new Exception();
            }
        }
    }

    private static bool[,] taken;
    private List<Elf> GetMoveOrdering(List<Elf> elves, int depth)
    {
        if (taken == null) taken = new bool[Size, Size];
        List<Elf> result = new List<Elf>();
        List<Elf> unresolved = elves.ToList();
        foreach (Elf elf in unresolved.ToList())
        {
            if (elf.Plan[depth] == elf.Plan[depth - 1])
            {
                unresolved.Remove(elf);
                result.Add(elf);
            }
            else taken[elf.Plan[depth - 1].X, elf.Plan[depth - 1].Y] = true;
        }

        int lastCount = -1;
        while (result.Count != lastCount)
        {
            lastCount = result.Count;
            foreach (Elf elf in unresolved.ToList())
            {
                if (!taken[elf.Plan[depth].X, elf.Plan[depth].Y])
                {
                    taken[elf.Plan[depth - 1].X, elf.Plan[depth - 1].Y] = false;
                    taken[elf.Plan[depth].X, elf.Plan[depth].Y] = true;
                    unresolved.Remove(elf);
                    result.Add(elf);
                }
            }
        }

        foreach (Elf elf in unresolved.Concat(result))
        {
            for (int d = depth - 1; d <= SEARCH_DEPTH; d++)
            {
                taken[elf.Plan[d].X, elf.Plan[d].Y] = false;
                taken[elf.Plan[d].X, elf.Plan[d].Y] = false;
            }
        }
        if (unresolved.Count > 0) return null;
        return result;
    }

    private double ComputePenalty(List<Elf> elves, int[,] distToEdge, int[,] distToPresents, bool finalizing)
    {
        double penalty = 0;
        foreach (Cell box in AiTracker.Blockworthy)
        {
            if (box.Elf.Skip(1).Any(e => e == null))
            {
                //double factor = box.Elf[SEARCH_DEPTH] == null ? 1 : 0.7;
                //penalty += factor * Math.Max(20, 60 - 3 * distToPresents[box.X, box.Y]);
                penalty += Math.Max(20, 60 - 3 * distToPresents[box.X, box.Y]);
            }
            // else if (!box.Elf[SEARCH_DEPTH].HasBox) penalty += 5;
        }

        int elfBoxIndex = finalizing ? 0 : 1;
        foreach (Elf elf in elves)
        {
            if (elf.IsFree)
            {
                penalty += elf.Dist(distToPresents);
                penalty += 0.01 * elf.Dist(distToPresents, 1);
                if (!elf.Plan[elfBoxIndex].HasBox && elf.Dist(distToPresents, 1) == 1) penalty -= 70;
                if (!elf.Plan[elfBoxIndex].HasBox && elf.Dist(distToPresents, 1) == 0) penalty -= 140; // pickup bonus
            }
            if (elf.HasPresent)
            {
                penalty += 2.1 * elf.Dist(distToEdge);
                penalty += 0.021 * elf.Dist(distToEdge, 1);
                if (elf.Dist(distToEdge) >= Area - Turn) penalty += 1000;
                if (elf.Dist(distToEdge) + 1 >= Area - Turn) penalty += 1000;
            }
            if (elf.HasBox)
            {
                penalty += 0.2 * elf.Dist(distToEdge);
                penalty += 0.002 * elf.Dist(distToEdge, 1);
            }
        }
        return penalty;
    }

    public static int[,] BFS(bool[,] blocked, List<Cell> starting)
    {
        int[,] dist = new int[Size, Size];
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++) dist[x, y] = 1000;
        }
        foreach (Cell s in starting) dist[s.X, s.Y] = 0;

        List<List<Cell>> queue = new List<List<Cell>>();
        queue.Add(starting);
        for (int d = 0; d < queue.Count; d++)
        {
            foreach (Cell q in queue[d])
            {
                foreach (Cell n in q.Neighbors)
                {
                    if (blocked[n.X, n.Y]) continue;
                    int newDist = dist[q.X, q.Y] + (n.HasBox || AiTracker.ProbablyBlocked[n.X, n.Y] ? (n.HasBox ? 21 : 15) - 3 * n.Neighbors.Count(m => m.IsNotBlocked) : 1);
                    if (newDist >= dist[n.X, n.Y]) continue;
                    dist[n.X, n.Y] = newDist;
                    while (queue.Count <= newDist) queue.Add(new List<Cell>());
                    queue[newDist].Add(n);
                }
            }
        }
        return dist;
    }

    public void PrintDebug()
    {
        Console.Error.WriteLine(Size);
        Console.Error.WriteLine("Turn: " + Turn);
        Console.Error.WriteLine(string.Join(" ", AiTracker.EnemyAis));
        for (int y = 0; y < Size; y++)
        {
            string line = "";
            for (int x = 0; x < Size; x++) line += AiTracker.PastBoxSpawns[x, y] + " ";
            Console.Error.WriteLine(line.Trim());
        }
        Console.Error.WriteLine(BoxCost);
        Console.Error.WriteLine(ElfProbability);
        Console.Error.WriteLine(Money);

        Console.Error.WriteLine(0);
        Console.Error.WriteLine(Money);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++) Console.Error.Write((char)Grid[x, y].CellType);
            Console.Error.WriteLine();
        }
    }
}