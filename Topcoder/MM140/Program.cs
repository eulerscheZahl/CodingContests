using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

class Solution
{
    static void Main(string[] args)
    {
        Board board = new Board();
        board.Read();

        Stopwatch sw = Stopwatch.StartNew();

        bool multipleEnds = true;
        bool allowEmpty = Board.JumpCost > 2;
        List<Board> beamStarters = new List<Board>();
        if (!board.Location.Target)
        {
            beamStarters.Clear();
            for (int x = 0; x < Board.Size; x++)
            {
                if (Board.Grid[x, 0].Target)
                {
                    beamStarters.Add(new Board(board, new MoveAction(x, true)));
                }
            }
        }
        if (beamStarters.Count == 0) beamStarters.Add(board);
        List<Board> starterCopy = beamStarters.Select(s => new Board(s)).ToList();

        bool allowJump = false;
        int iter = 0;
        while (sw.ElapsedMilliseconds < 9500 && (board.Filled < Board.ToFill || iter < 2))
        {
            iter++;
            int initialFilled = board.Filled;
            board = Beamsearch(beamStarters, sw, 9500, multipleEnds, allowEmpty, allowJump);
            Console.Error.WriteLine("after filling: " + sw.ElapsedMilliseconds);
            board = Hillclimb(board, sw, allowJump ? 6500 : 3500, multipleEnds, allowEmpty, allowJump);
            Console.Error.WriteLine("after climbing: " + sw.ElapsedMilliseconds);
            beamStarters = new List<Board> { board };
            allowJump = 2 * board.Filled > Board.ToFill;
            if (initialFilled == board.Filled && !allowJump) beamStarters = board.Expand(true, true, true).ToList();
        }

        Board withJump = Beamsearch(starterCopy, sw, 9500, multipleEnds, allowEmpty, true);
        Console.Error.WriteLine("after jumping: " + sw.ElapsedMilliseconds);
        if (withJump.Score > board.Score)
            board = withJump;

        board.BuildIslands();

        allowJump = false;
        iter = 0;
        beamStarters = starterCopy.ToList();
        Board board2 = beamStarters[0];
        while (sw.ElapsedMilliseconds < 9500 && (board2.Filled < Board.ToFill || iter < 2))
        {
            iter++;
            int initialFilled = board2.Filled;
            board2 = Beamsearch(beamStarters, sw, 9500, multipleEnds, allowEmpty, allowJump);
            Console.Error.WriteLine("after filling: " + sw.ElapsedMilliseconds);
            board2 = Hillclimb(board2, sw, allowJump ? 9000 : 6000, multipleEnds, allowEmpty, allowJump);
            Console.Error.WriteLine("after climbing: " + sw.ElapsedMilliseconds);
            beamStarters = new List<Board> { board2 };
            allowJump = 2 * board2.Filled > Board.ToFill;
            if (initialFilled == board2.Filled && !allowJump) beamStarters = board2.Expand(true, true, true).ToList();
        }
        if (board2.Score > board.Score) board = board2;
        while (board.Filled < Board.ToFill)
        {
            board = board.Expand(true, true, true).OrderByDescending(b => b.Filled).First();
        }

        var path = board.GetPath();

        List<string> program = string.Join("\n", board.PrintActions()).Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
        program = AddLoops(program, program.Count < 150);
        Console.Error.WriteLine(sw.ElapsedMilliseconds + " ms   " + board.Cost);
        Console.WriteLine(program.Count);
        foreach (string p in program) Console.WriteLine(p);
    }

    private static Board Hillclimb(Board board, Stopwatch sw, int maxTime, bool multipleEnds, bool allowEmpty, bool allowJump)
    {
        Console.Error.WriteLine();
        Random random = new Random(0);
        for (int iter = 0; sw.ElapsedMilliseconds < maxTime; iter++)
        {
            int optimLength = 15;
            List<Board> path = board.GetPath();
            if (path.Count <= optimLength + 5) return board; // no point in improving such a short solution
            int startIndex = random.Next(5, path.Count - optimLength);
            if (iter == 0 && allowJump)
                startIndex = path.Count - optimLength - 1; // try to improve jumps first, those have not been tested before
            int endIndex = startIndex + optimLength;
            Board startBoard = new Board(path[startIndex]);
            Board withoutMiddle = new Board(startBoard);
            withoutMiddle.Location = path[endIndex].Location;
            withoutMiddle.Direction = path[endIndex].Direction;
            if (endIndex != path.Count - 1) withoutMiddle.FillCell();
            for (int i = endIndex + 1; i < path.Count; i++)
            {
                withoutMiddle = new Board(withoutMiddle, path[i].Action);
            }
            Board alternativeStart = new Board(startBoard);
            alternativeStart.Visited = withoutMiddle.Visited.ToArray();
            alternativeStart.Filled = alternativeStart.CountVisited();
            Board alternativeEnd = Beamsearch(new List<Board> { alternativeStart }, sw, maxTime, multipleEnds, true, allowJump, endIndex == path.Count - 1 ? null : path[endIndex]);
            bool reverse = alternativeEnd.Direction != path[endIndex].Direction;
            if (alternativeStart == alternativeEnd) continue;
            for (int i = endIndex + 1; i < path.Count; i++)
            {
                Action action = path[i].Action;
                if (reverse && action is MoveAction) action = new MoveAction((action as MoveAction).Length, !(action as MoveAction).Forward);
                alternativeEnd = new Board(alternativeEnd, action);
            }
            // TODO small bonus for visiting cells twice so i can undo the other visit
            // reduce double visit bonus over time?

            List<Action> actions = alternativeEnd.GetActions();
            alternativeEnd = new Board { Location = Board.Grid[0, 0], Visited = new int[Board.Size] };
            foreach (Action a in actions) alternativeEnd = new Board(alternativeEnd, a);

            if (alternativeEnd.Score >= path.Last().Score)
            {
                //Console.Error.WriteLine(board.Score + " => " + alternativeEnd.Score);
                board = alternativeEnd;
            }
            //else Console.Error.Write(".");
        }

        return board;
    }

    private static Board Beamsearch(List<Board> boards, Stopwatch sw, int maxTime, bool multipleEnds, bool allowEmpty, bool allowJump, Board final = null)
    {
        int BEAM_WIDTH = 300 * 900 / Board.Area;
        List<List<Board>> beam = new List<List<Board>>();
        List<double> minScore = new List<double> { 0 };
        beam.Add(boards);
        HashSet<Board> visited = new HashSet<Board>(boards);
        Board best = boards.OrderByDescending(b => b.Score).First();
        Queue<Board> lowCostQueue = new Queue<Board>();
        for (int cost = 0; beam.Count > cost && sw.ElapsedMilliseconds < maxTime; cost++)
        {
            beam[cost] = beam[cost].OrderByDescending(b => b.Score).ToList();
            if (beam[cost].Count > BEAM_WIDTH) beam[cost] = beam[cost].Take(BEAM_WIDTH).ToList();
            foreach (Board b in beam[cost])
            {
                foreach (Board next in b.Expand(multipleEnds, allowEmpty, allowJump))
                {
                    if (visited.Contains(next)) continue;
                    while (beam.Count <= next.Cost)
                    {
                        beam.Add(new List<Board>());
                        minScore.Add(int.MinValue);
                    }
                    if (minScore[next.Cost] >= next.Score) continue;
                    visited.Add(next);
                    bool isFinal = final == null || final.Location == next.Location && final.Direction % 4 == next.Direction % 4;
                    if (final == null || !isFinal)
                    {
                        if (next.Cost > cost) beam[next.Cost].Add(next);
                        else lowCostQueue.Enqueue(next);
                    }
                    if (isFinal && next.Score > best.Score)
                        best = next;
                }
            }
            // needs refactoring, copy-paste
            while (lowCostQueue.Count > 0)
            {
                Board b = lowCostQueue.Dequeue();
                foreach (Board next in b.Expand(multipleEnds, allowEmpty, allowJump))
                {
                    if (visited.Contains(next)) continue;
                    while (beam.Count <= next.Cost)
                    {
                        beam.Add(new List<Board>());
                        minScore.Add(int.MinValue);
                    }
                    if (minScore[next.Cost] >= next.Score) continue;
                    visited.Add(next);
                    bool isFinal = final == null || final.Location == next.Location && final.Direction % 4 == next.Direction % 4;
                    if (final == null || !isFinal)
                    {
                        if (next.Cost > cost) beam[next.Cost].Add(next);
                        else lowCostQueue.Enqueue(next);
                    }
                    if (isFinal && next.Score > best.Score)
                        best = next;
                }
            }
            for (int c = cost + 1; c < beam.Count; c++)
            {
                if (beam[c].Count > BEAM_WIDTH)
                {
                    beam[c] = beam[c].OrderByDescending(b => b.Score).Take(BEAM_WIDTH).ToList();
                    minScore[c] = beam[c].Last().Score;
                }
            }
        }

        return best;
    }



    private static int ComputeCost(List<string> program)
    {
        int result = 0;
        foreach (string p in program)
        {
            if (p.StartsWith("F ") || p.StartsWith("B ") || p == "R" || p == "L" || p == "D") result++;
            else if (p.StartsWith("FOR")) result += Board.LoopCost;
            else if (p.StartsWith("J ")) result += Board.JumpCost;
        }
        return result;
    }

    private static Dictionary<string, List<string>> loopCache = new Dictionary<string, List<string>>();
    private static List<string> AddLoops(List<string> program, bool split)
    {
        string key = string.Join(",", program);
        if (loopCache.ContainsKey(key)) return loopCache[key].ToList();

        List<string> result = program.ToList();
        int minCost = ComputeCost(result);

        List<int[]> pairs = new List<int[]>();
        for (int loopCount = 2; loopCount < 10; loopCount++)
        {
            for (int loopLength = 1; loopLength <= program.Count / loopCount && loopLength <= 10; loopLength++)
            {
                pairs.Add(new[] { loopCount, loopLength });
            }
        }
        pairs = pairs.OrderByDescending(p => (p[0] - 1) * p[1]).ToList();
        foreach (int[] pair in pairs)
        {
            bool found = false;
            int loopCount = pair[0];
            int loopLength = pair[1];
            for (int start = 0; !found && start < program.Count; start++)
            {
                bool valid = true;
                int cost = 0;
                for (int i = 0; valid && i < loopLength; i++)
                {
                    if (program[start + i][0] == 'J') cost += Board.JumpCost;
                    else cost++;
                    for (int j = 1; valid && j < loopCount; j++)
                    {
                        int index = start + i + j * loopLength;
                        if (index >= program.Count || program[start + i] != program[index]) valid = false;
                    }
                }
                if (valid && cost * (loopCount - 1) > Board.LoopCost)
                {
                    List<string> current = AddLoops(program.Take(start).ToList(), false);
                    current.Add("FOR " + loopCount);
                    current.AddRange(program.Skip(start).Take(loopLength).ToList());
                    current.Add("END");
                    current.AddRange(AddLoops(program.Skip(start + loopCount * loopLength).ToList(), false));
                    if (ComputeCost(current) < minCost)
                    {
                        result = current;
                        minCost = ComputeCost(result);
                    }
                }
            }
            if (found) break;
        }

        if (split)
        {
            List<string> surrounding = program.Take(Math.Min(10, program.Count)).ToList();
            for (int i = 0; i < program.Count; i++)
            {
                if (i + 10 < program.Count) surrounding.Add(program[i]);
                if (surrounding.Count > 21) surrounding.RemoveAt(0);
                if (program[i].StartsWith("F ") || program[i].StartsWith("B "))
                {
                    int val = int.Parse(program[i].Split().Last());
                    for (int partial = 1; partial < val; partial++)
                    {
                        string s1 = program[i].Substring(0, 2) + partial;
                        string s2 = program[i].Substring(0, 2) + (val - partial);
                        if (!surrounding.Contains(s1) && !surrounding.Contains(s2)) continue;

                        List<string> clone = program.Take(i).ToList();
                        clone.Add(s1);
                        clone.Add(s2);
                        clone.AddRange(program.Skip(i + 1).ToList());
                        List<string> looped = AddLoops(clone, false);
                        if (ComputeCost(looped) < minCost)
                        {
                            result = looped;
                            minCost = ComputeCost(result);
                        }
                    }
                }
            }
        }

        loopCache[key] = result.ToList();
        return result;
    }
}