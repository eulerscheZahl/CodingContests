#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;

public class Board
{
    public static int Size;
    public int Turn;
    public static int BoxCost;
    public static double ElfProp;
    public Cell[,] Grid = new Cell[Size, Size];
    public int Money;
    public static int Turns => Size * Size;
    private int presentCount;
    private int currentElfs;
    private List<Cell> edges = new List<Cell>();

    public void Read(bool firstTurn)
    {
        Money = int.Parse(Console.ReadLine());
#if DEBUG
        if (firstTurn)
        {
            Console.Error.WriteLine(Size);
            Console.Error.WriteLine(BoxCost);
            Console.Error.WriteLine(ElfProp);
            Console.Error.WriteLine(Money);
        }
#endif
        int x = 0, y = 0;
        while (y < Size)
        {
            string line = Console.ReadLine();
            foreach (char c in line)
            {
                Grid[x, y] = new Cell(x, y, c);
                x++;
                if (x == Size)
                {
                    x = 0;
                    y++;
                }
            }
        }

        presentCount = 0;
        currentElfs = 0;
        edges.Clear();
        foreach (Cell cell in Grid)
        {
            cell.InitNeighbors(this);
            if (cell.Tile == Tile.PRESENT) presentCount++;
            if (cell.Tile == Tile.ELF) currentElfs++;
            if (cell.Edge && cell.Tile != Tile.TREE) edges.Add(cell);
        }
    }

    private Cell[,] correspondingArticulationPoint;
    public void FindArticulationPoints()
    {
        List<Cell> presents = new List<Cell>();
        foreach (Cell cell in Grid)
        {
            if (cell.Tile == Tile.PRESENT) presents.Add(cell);
        }

        correspondingArticulationPoint = new Cell[Board.Size, Board.Size];
        foreach (Cell edge in edges)
        {
            if (correspondingArticulationPoint[edge.X, edge.Y] != null) continue;
            Ring ring = new Ring { Presents = new HashSet<Cell>(presents) };
            MaxFlow flow = new MaxFlow(this, ring, new List<Cell> { edge });
            flow.InvertDirection();
            HashSet<Cell> border = flow.Border(this, ring);
            if (border.Count == 1 && !border.First().Edge)
            {
                Queue<Cell> queue = new Queue<Cell>();
                queue.Enqueue(edge);
                correspondingArticulationPoint[edge.X, edge.Y] = border.First();
                while (queue.Count > 0)
                {
                    Cell c = queue.Dequeue();
                    foreach (Cell n in c.Neighbors)
                    {
                        if (n.Tile == Tile.TREE || correspondingArticulationPoint[n.X, n.Y] != null || border.Contains(n)) continue;
                        correspondingArticulationPoint[n.X, n.Y] = border.First();
                        queue.Enqueue(n);
                    }
                }
            }
        }
    }

    public Ring TargetRing;
    private Dictionary<Cell, List<Cell>> groups = new Dictionary<Cell, List<Cell>>();
    public void FindBestRing(int time)
    {
        groups.Clear();
        List<Ring> cands = Ring.GenerateRings(this, time);
        foreach (Ring r in cands)
        {
            //r.PrintRing(this);
            if (!CanDefend(r))
            {
                TargetRing = r;
                //TargetRing.Smoothen(this, edges);

                int presentLoss = presentCount - r.Presents.Count;
                int remainingTurns = Board.Turns - Turn;
                int boxBuy = (remainingTurns + Money) / BoxCost + r.Border.Count(b => Grid[b.X, b.Y].Tile == Tile.BOX);
                int elfSpawn = (int)(ElfProp * remainingTurns);
                int missing = elfSpawn - presentLoss - (boxBuy - r.Border.Count);
                //if (missing > 2 && cands.Count > 1 && r == cands[1]) TargetRing = cands[0];
                return;
            }
        }
        TargetRing = cands.Last();
        //targetRing.PrintRing(this);
    }

    public bool CanDefend(Ring ring)
    {
        int presentLoss = presentCount - ring.Presents.Count;
        int remainingTurns = Board.Turns - Turn;
        for (int t = remainingTurns / 2; t <= remainingTurns; t++)
        {
            int boxBuy = (t + Money) / BoxCost + ring.Border.Count(b => Grid[b.X, b.Y].Tile == Tile.BOX);
            double elfSpawn = (ElfProp - 0.005) * t + currentElfs;
            double missing = elfSpawn - presentLoss - (boxBuy - ring.Border.Count);
            //Console.Error.WriteLine($"{Turn}: presents={presentCount} ring-presents={ring.Presents.Count}  ring-boxes={string.Join(",", ring.Border)}  boxes={boxBuy}  elfs={elfSpawn}  missing={missing}");
            if (missing > 1) return false;
        }
        return true;
    }

    public List<Cell> Play(int remainingTime)
    {
        //Console.Error.WriteLine($"Turn: {Turn}  Money:{Money}  Presents:{presentCount} ({TargetRing.Presents.Count} inner)  Ring boxes:{TargetRing.Border.Count} ({TargetRing.Border.Count(b => Grid[b.X, b.Y].Tile == Tile.BOX)} active)");
        int remainingTurns = Board.Turns - Turn;
        if (presentCount == 0) return new List<Cell>();

        int oldPresentCount = TargetRing.Presents.Count;
        TargetRing.Presents = new HashSet<Cell>(TargetRing.Presents.Where(p => Grid[p.X, p.Y].Tile == Tile.PRESENT && p.DistToEdge() < remainingTurns));
        if (oldPresentCount != TargetRing.Presents.Count)
        {
            TargetRing.Deflate(this, edges);
            //TargetRing.Smoothen(this, edges);
            groups.Clear();
        }

        List<Cell> needsBox = TargetRing.Border.Select(c => Grid[c.X, c.Y]).Where(w => w.Tile == Tile.EMPTY).ToList();
        List<Cell> needsBoxesNow = needsBox.Where(w => w.Neighbors.Any(n => n.Tile == Tile.ELF)).ToList();
        if (remainingTime > 2500 && BoxCost * ElfProp < 1 && presentCount > TargetRing.Presents.Count && needsBox.Count * BoxCost * 0.8 < Money)
        {
            Ring.UpdateInner(this, edges);
            FindBestRing(1000);
            needsBox = TargetRing.Border.Select(c => Grid[c.X, c.Y]).Where(w => w.Tile == Tile.EMPTY).ToList();
            needsBoxesNow = needsBox.Where(w => w.Neighbors.Any(n => n.Tile == Tile.ELF)).ToList();
        }

        // trapping
        if (false && ElfProp * BoxCost < 0.9 && 2 * Turn < Turns)
        {
            foreach (Cell box in needsBoxesNow.ToList())
            {
                Cell elf = box.Neighbors.First(n => n.Tile == Tile.ELF);
                if (elf.Neighbors.Any(n => n.Tile == Tile.PRESENT)) continue;
                List<Cell> elfEscape = elf.Neighbors.Where(n => n.Tile == Tile.EMPTY && !needsBoxesNow.Contains(n)).ToList();
                if (!elfEscape.Any(e => e.Edge) && elfEscape.Count == 1) needsBoxesNow.Add(elfEscape[0]);
            }
        }

        List<Cell> defendable = needsBoxesNow.Where(c => CanDefendGroup(c)).ToList();
        needsBoxesNow = defendable.Concat(needsBoxesNow.Except(defendable)).ToList();

        if (ElfProp * BoxCost < 0.9 && 2 * Turn < Turns)
        {
            foreach (Cell cell in Grid)
            {
                Cell art = correspondingArticulationPoint[cell.X, cell.Y];
                if (cell.Tile == Tile.ELF && cell.Neighbors.Contains(art))
                {
                    art = Grid[art.X, art.Y];
                    if (art.Tile == Tile.EMPTY) needsBoxesNow.Add(correspondingArticulationPoint[cell.X, cell.Y]);
                }
            }
        }

        //foreach (Cell cell in needsBoxesNow.ToList()) // TargetRing.Border.Select(b => Grid[b.X, b.Y]).Where(b => b.Neighbors.Any(n => n.Tile == Tile.ELF)))
        //{
        //    List<Cell> nextElfs = cell.Neighbors.Where(n => n.Tile == Tile.EMPTY).SelectMany(n => n.Neighbors).Where(n2 => n2.Tile == Tile.ELF).Distinct().ToList();
        //    if (nextElfs.Count == 1 && !nextElfs[0].Neighbors.Any(n => n.Tile == Tile.BOX))
        //    {
        //        Cell toBox = nextElfs[0].Neighbors.OrderBy(n => n.Dist(cell)).FirstOrDefault(n => n.Tile == Tile.EMPTY && !n.Edge);
        //        if (toBox != null && !needsBoxesNow.Contains(toBox)) needsBoxesNow.Add(toBox);
        //    }
        //}

        List<Cell> deduplicated = new List<Cell>();
        foreach (Cell cell in needsBoxesNow)
        {
            if (!deduplicated.Contains(cell)) deduplicated.Add(cell);
            if (cell.Tile != Tile.EMPTY) throw new Exception($"{cell}: {cell.Tile}");
        }
        needsBoxesNow = deduplicated;

        if (needsBoxesNow.Count * BoxCost > Money) return needsBoxesNow.Take(Money / BoxCost).ToList();
        return needsBoxesNow;
    }

    private bool CanDefendGroup(Cell c)
    {
        if (!groups.ContainsKey(c))
        {
            List<Cell> group = new List<Cell> { c };
            int lastCount = 0;
            while (lastCount != group.Count)
            {
                lastCount = group.Count;
                group = TargetRing.Border.Where(b => group.Any(g => g.Dist(b) < 1.5)).ToList();
            }
            groups[c] = group;
        }
        int defendCount = groups[c].Count(g => Grid[g.X, g.Y].Tile == Tile.EMPTY);
        return defendCount * (BoxCost - 1) + 1 <= Money;
    }
}
