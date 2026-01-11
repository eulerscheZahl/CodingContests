using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    public static int Size;
    public static int TileCount;
    public static int FruitCount;
    public static int FruitCountPlus;
    public static double GridRatio;
    public static double TileRatio;
    public static int Area => Size * Size;
    public int[] Grid;
    public int[] RowCounts;
    public int[] ColCounts;
    public Tile[] Tiles;
    public Board Parent;
    public Tile ActionTile;
    public int ActionX;
    public int ActionY;
    public int Score;
    public int DropIndex;

    public Board() { }

    public Board(Board board, Tile tile, int x, int y)
    {
        this.Grid = (int[])board.Grid.Clone();
        this.Tiles = (Tile[])board.Tiles.Clone();
        this.RowCounts = (int[])board.RowCounts.Clone();
        this.ColCounts = (int[])board.ColCounts.Clone();
        this.Score = board.Score;
        this.Parent = board;
        this.ActionTile = tile;
        this.ActionX = x;
        this.ActionY = y;
        tile.Place(this, x, y);
        RemoveRows();

        if (tile.Index != -1) Tiles[tile.Index] = null;
    }

    private void RemoveRows()
    {
        int linesCleared = 0;
        int localScore = 0;
        int filled = 0;
        for (int i = 0; i < Size; i++) filled += RowCounts[i * FruitCountPlus];
        List<int> rowClear = new List<int>();
        for (int y = 0; y < Size; y++)
        {
            if (RowCounts[y * FruitCountPlus] < Size) continue;
            rowClear.Add(y);
            int max = 0;
            for (int i = 1; i <= FruitCount; i++) max = Math.Max(max, RowCounts[y * FruitCountPlus + i]);
            localScore += max * max;
            linesCleared++;
        }
        List<int> colClear = new List<int>();
        for (int x = 0; x < Size; x++)
        {
            if (ColCounts[x * FruitCountPlus] < Size) continue;
            colClear.Add(x);
            int max = 0;
            for (int i = 1; i <= FruitCount; i++) max = Math.Max(max, ColCounts[x * FruitCountPlus + i]);
            localScore += max * max;
            linesCleared++;
        }

        bool canPlaceFull = false;
        if (linesCleared == 1 && TileCount == 1)
        {
            for (int x = 0; x < Size - 2; x++)
            {
                for (int y = 0; !canPlaceFull && y < Size - 2; y++)
                {
                    int empty = 0;
                    for (int dx = x; dx < x + 3; dx++)
                    {
                        for (int dy = y; dy < y + 3; dy++)
                        {
                            if (Grid[x * Size + y] == 0) empty++;
                        }
                    }
                    if (empty >= 7) canPlaceFull = true;
                }
            }
        }

        foreach (int y in rowClear)
        {
            for (int x = 0; x < Size; x++)
            {
                int fruit = Grid[x * Size + y];
                if (fruit == 0) continue;
                Grid[x * Size + y] = 0;
                ColCounts[x * FruitCountPlus + fruit]--;
                ColCounts[x * FruitCountPlus]--;
            }
            for (int f = 0; f <= FruitCount; f++) RowCounts[y * FruitCountPlus + f] = 0;
        }
        foreach (int x in colClear)
        {
            for (int y = 0; y < Size; y++)
            {
                int fruit = Grid[x * Size + y];
                if (fruit == 0) continue;
                Grid[x * Size + y] = 0;
                RowCounts[y * FruitCountPlus + fruit]--;
                RowCounts[y * FruitCountPlus]--;
            }
            for (int f = 0; f <= FruitCount; f++) ColCounts[x * FruitCountPlus + f] = 0;
        }

        if (linesCleared == 1 && (canPlaceFull || FruitCount < 3 && filled * 3 / 2 + 30 < Area))
        {
            linesCleared = 0;
            Score++;
        }
        Score += linesCleared * localScore;
    }

    private static double[] pows = new double[10];
    public void ReadInitial()
    {
        Size = int.Parse(Console.ReadLine().Split().Last());
        TileCount = int.Parse(Console.ReadLine().Split().Last());
        FruitCount = int.Parse(Console.ReadLine().Split().Last());
        FruitCountPlus = FruitCount + 1;
        GridRatio = double.Parse(Console.ReadLine().Split().Last());
        TileRatio = double.Parse(Console.ReadLine().Split().Last());
        for (int i = 0; i < 10; i++) pows[i] = Math.Pow(1 - TileRatio, 9 - i);

        Grid = new int[Area];
        RowCounts = new int[Size * FruitCountPlus];
        ColCounts = new int[Size * FruitCountPlus];
        int x = 0, y = 0;
        while (y < Size)
        {
            string line = Console.ReadLine();
            if (line.StartsWith("Grid")) line = Console.ReadLine();
            foreach (char c in line)
            {
                Grid[x * Size + y] = c - '0';
                if (Grid[x * Size + y] != 0)
                {
                    RowCounts[y * FruitCountPlus]++;
                    RowCounts[y * FruitCountPlus + Grid[x * Size + y]]++;
                    ColCounts[x * FruitCountPlus]++;
                    ColCounts[x * FruitCountPlus + Grid[x * Size + y]]++;
                }

                x++;
                if (x == Size)
                {
                    x = 0;
                    y++;
                }
            }
        }

        Tiles = new Tile[TileCount];
        for (int i = 0; i < TileCount; i++)
        {
            string line = Console.ReadLine();
            if (line.StartsWith("Starting")) line = Console.ReadLine();
            Tiles[i] = new Tile(line.Split().Last()) { Index = i };
        }
    }

    private static int remainingTime;
    private static int turn;
    public Board Plan(int remainingTime, int turn)
    {
        Board.remainingTime = remainingTime;
        Board.turn = turn;
        int depth = Math.Min(TileCount, 1);
        List<Board> beam = new List<Board> { this };
        for (int i = 0; i < depth; i++)
        {
            List<Board> next = beam.SelectMany(b => b.Expand()).ToList();
            next = next.OrderByDescending(b => b.HeuristicScore(true)).ToList();
            if (next.Count > 0 || i == 0) beam = next;
        }
        if (beam.Count > 0 && beam.Count < 100 && (10000 - remainingTime) * 1000 / (1 + turn) < 9000 && beam.Any(b => b.Score == this.Score))
        {
            List<Board> next = beam.Where(b => b.Score == this.Score).SelectMany(b => b.Expand()).ToList();
            if (next.Count > 0) next.Concat(beam).OrderByDescending(n => n.HeuristicScore()).First();
        }
        Board best = beam.OrderByDescending(b => b.HeuristicScore(true)).FirstOrDefault();
        if (best == null) return null;

        if (best.Score > this.Score && turn + Size / 2 < 1000 && remainingTime > 1000)
        {
            // try to hold back the action and complete more rows at once later
            Tile tile = best.ActionTile;
            int tileX = best.ActionX;
            int tileY = best.ActionY;
            Tiles[tile.Index] = null;
            beam = new List<Board> { this };
            List<Board> next = beam.SelectMany(b => b.Expand()).ToList();
            Tiles[tile.Index] = tile;
            next = next.Where(n => n.Score == this.Score && tile.CanPlace(n, tileX, tileY)).ToList();
            bool noOtherMove = next.Count == 0;
            if (next.Count > 0)
            {
                // TODO bonus if in same row or column as the delayed tile
                next = next.OrderByDescending(b => new Board(b, tile, tileX, tileY).HeuristicScore(false, tile, tileX, tileY, b.ActionTile, b.ActionX, b.ActionY)).ToList();
                best = next.First();
                best.Tiles[tile.Index] = tile;
            }
            else if (noOtherMove && TileCount > 1)
            {
                // discard and wait for tile with single fruit
                double p0 = Math.Pow(1 - TileRatio, 9);
                double p1 = Math.Pow(1 - TileRatio, 8) * TileRatio * 9;
                double pSingle = p1 * (1 - p0);
                List<Board> withSingle = this.Expand(new[] { Tile.Single }).ToList();
                withSingle = withSingle.Where(w => w.Score == this.Score && tile.CanPlace(w, tileX, tileY)).ToList();
                Board bestWait = withSingle.Select(b => new Board(b, tile, tileX, tileY)).OrderByDescending(b => b.HeuristicScore()).FirstOrDefault();
                if (bestWait == null) return best;
                double scoreGain = bestWait.Score - best.Score;
                double averageScorePerTurn = Math.Pow(Size * (9 / 8.0 - FruitCount / 8.0), 2) / Size * (9 * TileRatio);
                if (scoreGain * pSingle > averageScorePerTurn)
                {
                    return new Board() { DropIndex = Tiles.OrderBy(t => t.FruitsSet).First(t => t != tile).Index };
                }
            }
        }
        return best;
    }

    private double? heuristic = null;
    public double HeuristicScore(bool rewardBigTiles = false, Tile delayedTile = null, int delayedX = 0, int delayedY = 0, Tile insertedTile = null, int insertedX = 0, int insertedY = 0)
    {
        if (heuristic != null) return heuristic.Value;
        double result = Score;

        // reward same fruit type
        double factor = 1;
        foreach (int[] counts in new[] { RowCounts, ColCounts })
        {
            for (int i = 0; i < Size; i++)
            {
                double max = 0;
                for (int j = 1; j <= FruitCount; j++) max = Math.Max(max, counts[i * FruitCountPlus + j]);
                if (1.5 * turn + 20 < Area / (9 * TileRatio)) max = 1.1 * max - 0.1 * counts[i * FruitCountPlus];
                else max = 0.9 * max + 0.1 * counts[i * FruitCountPlus];
                result += (double)max * max / Size * factor;
            }
            factor = 2;
        }

        // reward no gaps
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                if (Grid[x * Size + y] != 0) continue;
                int diagonalEmpty = 0, straightEmpty = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (x + dx < 0 || x + dx >= Size || y + dy < 0 || y + dy >= Size || Grid[(x + dx) * Size + y + dy] == 0) continue;
                        if (dx * dy == 0) straightEmpty++;
                        else diagonalEmpty++;
                    }
                }
                result -= 3e-2 * straightEmpty;
                result -= 1e-2 * diagonalEmpty;
            }
        }

        // reward for possibility to place existing tiles
        foreach (Tile t in Tiles)
        {
            if (t == null) continue;
            int spots = 0;
            for (int y = -2; y < Size; y++)
            {
                for (int x = -2; x < Size; x++)
                {
                    if (t.CanPlace(this, x, y)) spots++;
                    if (spots >= 10) break;
                }
            }
            result += Math.Sqrt(spots) + 15 * Math.Sign(spots);
        }

        // reward for possibility to place next spawning tile
        if (remainingTime > 6000)
        {
            for (int y = -2; y < Size; y++)
            {
                for (int x = -2; x < Size; x++)
                {
                    int canPlace = 0;
                    for (int dx = 0; dx < 3; dx++)
                    {
                        for (int dy = 0; dy < 3; dy++)
                        {
                            if (x + dx >= 0 && y + dy >= 0 && x + dx < Size && y + dy < Size && Grid[(x + dx) * Size + y + dy] == 0) canPlace++;
                        }
                    }
                    result += pows[canPlace];
                }
            }
        }

        if (rewardBigTiles) result += 1 << ActionTile.MaxRows;

        if (delayedTile != null)
        {
            for (int x = 0; x < 3; x++)
            {
                if (!delayedTile.XUsed[x]) continue;
                int otherX = x + delayedX - insertedX;
                if (otherX >= 0 && otherX < 3 && insertedTile.XUsed[otherX]) result += 1;
            }
            for (int y = 0; y < 3; y++)
            {
                if (!delayedTile.YUsed[y]) continue;
                int otherY = y + delayedY - insertedY;
                if (otherY >= 0 && otherY < 3 && insertedTile.YUsed[otherY]) result += 1;
            }
        }

        heuristic = result;
        return result;
    }

    public IEnumerable<Board> Expand(Tile[] tiles = null)
    {
        if (tiles == null) tiles = this.Tiles;
        foreach (Tile tile in tiles)
        {
            if (tile == null) continue;
            for (int x = -2; x < Size; x++)
            {
                for (int y = -2; y < Size; y++)
                {
                    if (!tile.CanPlace(this, x, y)) continue;
                    yield return new Board(this, tile, x, y);
                }
            }
        }
    }

    public void ClearParent()
    {
        Parent = null;
        ActionTile = null;
        ActionX = 0;
        ActionY = 0;
    }

    public string GetInputs()
    {
        string result = Size + "\n" + TileCount + "\n" + FruitCount + "\n" + GridRatio + "\n" + TileRatio + "\nGrid";
        for (int y = 0; y < Size; y++)
        {
            result += "\n";
            for (int x = 0; x < Size; x++)
            {
                result += Grid[x * Size + y];
            }
        }
        result += "\nStarting tiles";
        foreach (Tile tile in Tiles) result += "\n" + tile.GetInputs();
        return result;
    }

    public string PrintAction() => $"{ActionTile.Index} {ActionY} {ActionX}";

    public override string ToString() => PrintAction() + " S:" + Score + "  H:" + HeuristicScore();
}