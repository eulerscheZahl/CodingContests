using System;
using System.Collections.Generic;
using System.Linq;

public static class AiTracker
{
    public static List<int> EnemyAis = new List<int> { 1, 2, 3, 4, 5 };
    private static List<Cell> previousBoxes = new List<Cell>();
    public static int[,] PastBoxSpawns;
    public static List<Cell> Blockworthy;
    public static bool[,] ProbablyBlocked;
    public static bool[,] BlockworthyGrid;

    public static void Track()
    {
        List<Cell> boxes = Board.Cells.Where(c => c.HasBox).ToList();
        List<Cell> newBoxes = boxes.Except(previousBoxes).ToList();
        foreach (Cell cell in newBoxes) PastBoxSpawns[cell.X, cell.Y] = Board.Turn;
        ProbablyBlocked = new bool[Board.Size, Board.Size];

        CheckAi1(newBoxes);
        CheckAi2(newBoxes);
        CheckAi3(newBoxes);
        CheckAi4(newBoxes);
        CheckAi5(newBoxes);

        previousBoxes = boxes;
        Blockworthy = new List<Cell>();
        BlockworthyGrid = new bool[Board.Size, Board.Size];
        if (Board.Cells.Any(c => c.HasPresent))
        {
            foreach (Cell cell in Board.Cells)
            {
                ProbablyBlocked[cell.X, cell.Y] = PastBoxSpawns[cell.X, cell.Y] >= 0;
                BlockworthyGrid[cell.X, cell.Y] = !cell.HasBox && ProbablyBlocked[cell.X, cell.Y] && cell.Neighbors.Count(n => n.IsNotBlocked) >= 2;
                if (BlockworthyGrid[cell.X, cell.Y]) Blockworthy.Add(cell);
            }
        }
    }

    public static int AiId()
    {
        if (EnemyAis.Count == 1) return EnemyAis[0];
        return 0;
    }

    // AI 1: can place multiple boxes
    private static void CheckAi1(List<Cell> newBoxes)
    {
        if (!EnemyAis.Contains(1)) return;
        if (newBoxes.Count > 1) EnemyAis = new List<int> { 1 };
        if (Board.Turn == 0 && newBoxes.Count < 2 && Board.Money >= Board.BoxCost) EnemyAis.Remove(1);
        if (Board.Turn < 2 && Board.Cells.Any(c => c.HasPresent) && !Board.Cells.Any(c => c.HasBox) && Board.Money >= Board.BoxCost) EnemyAis.Remove(1);

        if (AiId() == 1)
        {
            List<Cell> presents = Board.Cells.Where(c => c.HasPresent).ToList();
            bool[,] blocked = new bool[Board.Size, Board.Size];
            foreach (Cell cell in Board.Cells)
            {
                if (cell.HasTree || cell.HasBox || cell.Elf[0] != null) blocked[cell.X, cell.Y] = true;
            }
            int[,] dist = Board.BFS(blocked, presents);
            foreach (Cell cell in Board.Cells)
            {
                if (dist[cell.X, cell.Y] == 1000 && cell.Neighbors.All(n => dist[n.X, n.Y] == 1000)) PastBoxSpawns[cell.X, cell.Y] = -1000;
            }
        }
    }

    // AI 2: decrease number of reachable
    private static void CheckAi2(List<Cell> newBoxes)
    {
        if (!EnemyAis.Contains(2)) return;
        if (Board.Turn < 2 && Board.Cells.Any(c => c.HasPresent) && !Board.Cells.Any(c => c.HasBox) && Board.Money >= Board.BoxCost) EnemyAis.Remove(2);
        if (newBoxes.Count == 0) return;
        foreach (Cell cell in newBoxes) cell.CellType = CellType.EMPTY;
        int initialReachable = AI2CountOuterReachablePresentEdges(AI2FindOuterReachable());
        foreach (Cell cell in newBoxes) cell.CellType = CellType.BOX;
        int postReachable = AI2CountOuterReachablePresentEdges(AI2FindOuterReachable());
        if (postReachable >= initialReachable) EnemyAis.Remove(2);
    }

    private static void CheckAi3(List<Cell> nextBoxes)
    {
        if (!EnemyAis.Contains(3)) return;
        if (Board.Turn < 2 && Board.Cells.Any(c => c.HasPresent) && !Board.Cells.Any(c => c.HasBox) && Board.Money >= Board.BoxCost) EnemyAis.Remove(3);
    }

    // AI 4: only places a box, when an elf is next to it
    private static void CheckAi4(List<Cell> newBoxes)
    {
        if (!EnemyAis.Contains(4)) return;
        foreach (Cell box in newBoxes)
        {
            if (box.Neighbors.Any(n => n.HasPresent) && box.Neighbors.Any(n => n.CellType == CellType.ELF)) continue;
            if (AI4Wings(box) == 2 && AI4Doomed(box) && AI4Exposed(box.X, box.Y) > 1) continue;
            EnemyAis.Remove(4);
        }
        if (AiId() == 4)
        {
            foreach (Cell cell in Board.Cells)
            {
                if (cell.Neighbors.Any(n => n.HasPresent)) PastBoxSpawns[cell.X, cell.Y] = Board.Turn;
                else PastBoxSpawns[cell.X, cell.Y] = -1000;
            }
        }
    }

    // AI 5: just random
    private static void CheckAi5(List<Cell> newBoxes)
    {
        if (!EnemyAis.Contains(5)) return;
        int x = 1 + ((Board.Turn * (7919)) % (Board.Size - 2));
        int y = 1 + ((Board.Turn * (50091)) % (Board.Size - 2));
        if (newBoxes.Any(b => b.X != x || b.Y != y)) EnemyAis.Remove(5);
    }


    private static int[] AI2DR = { 0, -1, 0, 1 };
    private static int[] AI2DC = { 1, 0, -1, 0 };
    private static void AI2Reachable(bool[,] visited, int r, int c)
    {
        if (Board.Grid[c, r].IsSolid || visited[r, c]) return;
        int N = Board.Size;
        visited[r, c] = true;
        for (int i = 0; i < 4; i++)
        {
            int nr = r + AI2DR[i];
            int nc = c + AI2DC[i];
            if (AI2Invalid(nr, nc, N)) continue;
            AI2Reachable(visited, nr, nc);
        }
    }

    static bool AI2Invalid(int r, int c, int N) => r < 0 || N <= r || c < 0 || N <= c;

    private static bool[,] AI2FindOuterReachable()
    {
        int N = Board.Size;
        bool[,] res = new bool[N, N];
        for (int i = 0; i < N; i++)
        {
            AI2Reachable(res, 0, i);
            AI2Reachable(res, N - 1, i);
            AI2Reachable(res, i, 0);
            AI2Reachable(res, i, N - 1);
        }
        return res;
    }

    private static int AI2CountOuterReachablePresentEdges(bool[,] reachable)
    {
        int N = Board.Size;
        int outerReachablePresentEdges = 0;
        for (int r = 0; r < N; r++)
        {
            for (int c = 0; c < N; c++)
            {
                if (!Board.Grid[c, r].HasPresent) continue;
                for (int i = 0; i < 4; i++)
                {
                    int nr = r + AI2DR[i];
                    int nc = c + AI2DC[i];
                    if (AI2Invalid(nr, nc, N) || !reachable[nr, nc]) continue;
                    outerReachablePresentEdges++;
                }
            }
        }
        return outerReachablePresentEdges;
    }


    private static bool AI4Doomed(Cell cell) => cell.Neighbors.Any(n => n.CellType == CellType.ELF);

    private static int AI4Wings(Cell cell)
    {
        return ((Board.Grid[cell.X - 1, cell.Y - 1].HasPresent) ? 1 : 0) +
               ((Board.Grid[cell.X + 1, cell.Y + 1].HasPresent) ? 1 : 0) +
               ((Board.Grid[cell.X - 1, cell.Y - 1].HasPresent) ? 1 : 0) +
               ((Board.Grid[cell.X + 1, cell.Y + 1].HasPresent) ? 1 : 0);
    }

    private static int AI4Exposed(int a, int b)
    {
        if (Board.Grid[a - 1, b - 1].HasPresent && Board.Grid[a - 1, b + 1].HasPresent)
            return ((Board.Grid[a - 1, b].IsEmpty) ? 1 : 0) + ((Board.Grid[a, b - 1].IsEmpty) ? 1 : 0) + ((Board.Grid[a, b + 1].IsEmpty) ? 1 : 0);
        if (Board.Grid[a - 1, b - 1].HasPresent && Board.Grid[a + 1, b - 1].HasPresent)
            return ((Board.Grid[a, b - 1].IsEmpty) ? 1 : 0) + ((Board.Grid[a - 1, b].IsEmpty) ? 1 : 0) + ((Board.Grid[a + 1, b].IsEmpty) ? 1 : 0);
        if (Board.Grid[a - 1, b + 1].HasPresent && Board.Grid[a + 1, b + 1].HasPresent)
            return ((Board.Grid[a, b + 1].IsEmpty) ? 1 : 0) + ((Board.Grid[a - 1, b].IsEmpty) ? 1 : 0) + ((Board.Grid[a + 1, b].IsEmpty) ? 1 : 0);
        if (Board.Grid[a + 1, b - 1].HasPresent && Board.Grid[a + 1, b + 1].HasPresent)
            return ((Board.Grid[a + 1, b].IsEmpty) ? 1 : 0) + ((Board.Grid[a, b - 1].IsEmpty) ? 1 : 0) + ((Board.Grid[a, b + 1].IsEmpty) ? 1 : 0);
        return 0; // probable coding error if this happens
    }
}