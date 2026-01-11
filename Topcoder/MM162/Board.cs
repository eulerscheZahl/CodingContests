using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board : IEquatable<Board>
{
    public List<Coin> Coins;
    public List<Guard> Guards;
    public Point Thief;
    private bool thiefCrouch;
    public int Turn;
    public int Score;
    public double BribeReimburse;
    public static int BribeCost;
    private static int guardCount;
    public int Hash;
    private static int crouchHash;
    public static double BribeCoefficient;
    public int BribedBlocking;

    private string ReadLine()
    {
        string line = Console.ReadLine().Trim();
#if DEBUG
        Console.Error.WriteLine(line);
#endif
        return line;
    }

    public Board()
    {
        crouchHash = random.Next();
    }

    public Board Parent;
    public int ID;
    private static int idCounter;
    public Board(Board parent, Point thief, bool crouch, Coin coin = null, Guard guard = null)
    {
        this.ID = idCounter++;
        this.thiefCrouch = crouch;
        this.Parent = parent;
        this.Thief = thief;
        this.Coins = parent.Coins;
        this.Guards = parent.Guards;
        this.Turn = parent.Turn + 1;
        this.Score = parent.Score;
        this.BribeReimburse = parent.BribeReimburse;
        this.BribedBlocking = parent.BribedBlocking;
        this.Hash = parent.Hash;
        if (coin != null)
        {
            Hash ^= coin.GetHashCode();
            Coins = Coins.ToList();
            Coins.Remove(coin);
            Score -= 1000 - Turn;
            int guardIndex = 0;
            for (int guardId = 0; guardId < guardCount; guardId++)
            {
                while (guardIndex + 1 < Guards.Count && Guards[guardIndex].ID < guardId) guardIndex++;
                if (guardIndex >= Guards.Count || Guards[guardIndex].ID != guardId) // guard not found => already bribed
                    BribeReimburse -= bribeMatrix[coin.ID, guardId];
            }

        }
        if (guard != null)
        {
            Hash ^= guard.GetHashCode();
            Guards = Guards.ToList();
            Guards.Remove(guard);
            Score += BribeCost;
            if (guard.IsBlocking) BribedBlocking++;
            BribeReimburse += 0.1;
            foreach (Coin c in Coins) BribeReimburse += bribeMatrix[c.ID, guard.ID];
        }
        if (OutOfBounds()) Score -= 5000;
    }

    public bool Detected()
    {
        if (OutOfBounds()) return false;
        for (int i = 0; i < dangerousGuardsCount; i++)
        {
            if (dangerousGuards[i].CanSee(Thief, thiefCrouch, Turn)) return true;
        }
        return false;
    }

    public void ReadInput()
    {
        int coinCount = int.Parse(ReadLine());
        guardCount = int.Parse(ReadLine());
        BribeCost = 20 * int.Parse(ReadLine());

        Coins = new List<Coin>();
        for (int i = 0; i < coinCount; i++)
        {
            int[] tmp = ReadLine().Split().Select(int.Parse).ToArray();
            Coins.Add(new Coin(i, tmp[0], tmp[1]));
        }
        Guards = new List<Guard>();

        for (int i = 0; i < guardCount; i++)
        {
            int n = int.Parse(ReadLine());
            VisionRange[] visions = new VisionRange[n];
            for (int j = 0; j < n; j++)
            {
                int[] tmp = ReadLine().Split().Select(int.Parse).ToArray();
                Point[] vision = Enumerable.Range(0, 5).Select(k => new Point(tmp[2 * k], tmp[2 * k + 1])).ToArray();
                visions[j] = new VisionRange(vision);
            }
            Guards.Add(new Guard(i, visions));
        }

        int[] thiefInput = ReadLine().Split().Select(int.Parse).ToArray();
        Thief = new Point(thiefInput[0], thiefInput[1]);
        Hash = Coins.Select(c => c.GetHashCode()).Aggregate((a, b) => a ^ b);
        Score = 5000 + 1000 * Coins.Count;
    }

    public static int expandCounter = 0;
    // 8 directions, distance = 198 and 398
    private static readonly int[] dx = { 0, 0, 0, 0, 0, 140, 281, 198, 398, 140, 281, 0, 0, -140, -281, -198, -398, -140, -281 };
    private static readonly int[] dy = { 0, 0, 0, 198, 398, 140, 281, 0, 0, -140, -281, -198, -398, -140, -281, 0, 0, 140, 281 };
    private static readonly bool[] dCrouch = { true, true, true, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false };
    private static Guard[] dangerousGuards = new Guard[100];
    private static int dangerousGuardsCount;
    public void Expand(List<Board> children, HashSet<int> hashes)
    {
        expandCounter++;
        if (OutOfBounds()) return;
        dangerousGuardsCount = 0;
        foreach (Guard guard in Guards)
        {
            if (guard.MightDetect(Thief, Turn + 1)) dangerousGuards[dangerousGuardsCount++] = guard;
        }
        for (int i = 1; i <= 2; i++)
        {
            dx[i] = random.Next(-280, 281);
            dy[i] = random.Next(-280, 281);
            dCrouch[i] = dx[i] * dx[i] + dy[i] * dy[i] <= 200 * 200;
        }
        for (int i = 0; i < dx.Length; i++)
        {
            int x = Thief.X + dx[i];
            int y = Thief.Y + dy[i];
            int newHash = Hash ^ (int)(((x << 16) + y) & 0xff80ff80) ^ (dCrouch[i] ? crouchHash : 0);
            if (x < 0 || x >= 10000 || y < 0 || y >= 10000) newHash = 0;
            if (!hashes.Add(newHash)) continue;
            Board board = new Board(this, new Point(x, y), dCrouch[i]);
            if (!board.Detected())
            {
                children.Add(board);
                hashes.Add(board.GetHashCode() ^ crouchHash); // don't store same pos standing and crouching
            }
            else if (dCrouch[i]) hashes.Add(board.GetHashCode() ^ crouchHash);
        }
        foreach (Coin coin in Coins)
        {
            if (Thief.InRange(coin, 400))
            {
                Board board = new Board(this, coin, Thief.InRange(coin, 200), coin: coin);
                if (hashes.Add(board.GetHashCode()) && !board.Detected())
                {
                    children.Add(board);
                    hashes.Add(board.GetHashCode() ^ crouchHash); // don't store same pos standing and crouching
                }
            }
        }
        foreach (Guard guard in Guards)
        {
            Point pos = guard.GetPos(Turn + 1);
            if (Thief.InRange(pos, 400))
            {
                Board board = new Board(this, pos, false, guard: guard);
                if (hashes.Add(board.GetHashCode()) && !board.Detected()) children.Add(board);
            }
        }
    }

    public bool OutOfBounds()
    {
        return Thief.X < 0 || Thief.X >= 10000 || Thief.Y < 0 || Thief.Y >= 10000;
    }

    public void ComputeBribeBonus(bool forceBribe)
    {
        double matrixAverage = Guards.Where(g => !g.IsBlocking).Average(g => Coins.Sum(c => bribeMatrix[c.ID, g.ID]));
        bribeBonus = Guards.Count / 40.0 + Coins.Count / 80.0 + (100.0 - BribeCost) / 100 - 1;
        bribeBonus = 100 * Math.Max(0, bribeBonus) / matrixAverage;
        if (forceBribe) bribeBonus = Math.Max(bribeBonus, 1.5 * BribeCost / matrixAverage);

        BribeCoefficient = 100 * (40 + Guards.Count) / 40.0 * (80 + Coins.Count) / 80.0;
        if (forceBribe) BribeCoefficient *= 10;

        //var debugInfo = Guards.Select(g => (g.ID, BribeCoefficient * Coins.Sum(c => bribeMatrix[c.ID, g.ID]))).ToList();
    }

    private static Random random = new Random(0);
    private static double bribeBonus;
    public double ComputeScore()
    {
        if (OutOfBounds()) return Score;
        double score = Score + 1000 * Coins.Count - 1500 * BribedBlocking;
        score -= BribeCoefficient * BribeReimburse;
        if (Coins.Count > 0)
        {
            Coin closestCoin = Coins[0];
            double dist2 = closestCoin.Dist2(Thief);
            for (int i = 1; i < Coins.Count; i++)
            {
                double tmp = Coins[i].Dist2(Thief);
                if (tmp < dist2)
                {
                    dist2 = tmp;
                    closestCoin = Coins[i];
                }
            }
            score += 0.1 * closestCoin.Dist(Thief);
        }
        else score += 0.1 * Math.Min(Math.Min(Thief.X, 10000 - Thief.X), Math.Min(Thief.Y, 10000 - Thief.Y));
        return score;
    }

    public List<string> GetPath()
    {
        Board b = this;
        List<string> path = new List<string>();
        while (b.Parent != null)
        {
            path.Add(b.Thief.X + " " + b.Thief.Y + " id=" + b.ID + "\\nscore=" + b.Score + "\\nbribeReimburse=" + b.BribeReimburse.ToString("0.######"));
            b = b.Parent;
        }
        path.Reverse();
        return path;
    }

    public override int GetHashCode()
    {
        if (OutOfBounds()) return 0;
        return Hash ^ (int)(Thief.GetHashCode() & 0xff80ff80) ^ (thiefCrouch ? crouchHash : 0);
    }

    public bool Equals(Board board)
    {
        return GetHashCode() == board.GetHashCode();
    }

    private static double[,] bribeMatrix;
    public void FindBlockingGuards()
    {
        bribeMatrix = new double[Coins.Count, Guards.Count];
        foreach (Guard guard in Guards)
        {
            foreach (Coin coin in Coins)
            {
                List<int> visionTurns = Enumerable.Range(0, guard.Path.Length).Where(t => guard.CanSee(coin, true, t)).ToList();
                if (visionTurns.Count == 0) continue;
                int pickupTimeframe = 0;
                for (int t = 0; t < guard.Path.Length; t++)
                {
                    if (visionTurns.Contains(t)) continue;
                    Board board = new Board(this, coin, true) { Turn = t, Guards = new List<Guard> { guard }, Coins = new List<Coin> { } };
                    List<Board> expand = new List<Board> { board };
                    bool canReach = true;
                    for (int i = 0; i < 3; i++)
                    {
                        HashSet<int> hashes = new HashSet<int>();
                        List<Board> next = new List<Board>();
                        foreach (Board e in expand) e.Expand(next, hashes);
                        expand = next;
                        canReach &= next.Any(n => !guard.CanSee(n.Thief, true, t - i - 1 + 3 * guard.Path.Length));
                    }
                    bool canEscape = expand.Count > 0;
                    if (canEscape && canReach) pickupTimeframe++;
                }
                if (pickupTimeframe == 0)
                {
                    guard.IsBlocking = true;
                    Console.Error.WriteLine("guard " + guard.ID + " blocks coin " + coin.ID);
                    break;
                }
                else bribeMatrix[coin.ID, guard.ID] = Math.Pow((guard.Path.Length - pickupTimeframe) / (double)guard.Path.Length, 2);
            }
        }
    }

    public int PredictMinScore()
        => Score - 5000 - (999 - Turn) * Coins.Count + Coins.Count * (Coins.Count - 1) / 2;
}
