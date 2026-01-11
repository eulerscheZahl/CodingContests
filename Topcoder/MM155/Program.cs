using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Program
{
#if LOCAL_ENV
    public static int timeLimit = 4000;
#else
    public static int timeLimit = 9500;
#endif
    public static void Main(string[] args)
    {
        Board board = new Board();
        board.ReadInitial(args.Length > 0 && args[0] == "debug");

        Stopwatch sw = Stopwatch.StartNew();
        List<Cell> candidates = new List<Cell>();
        foreach (var cell in Board.Grid) candidates.Add(cell);
        candidates = candidates.OrderByDescending(c => c.Mult).Take(50).ToList();
        List<BeamNode> beam = candidates.Select(c => new BeamNode(c)).ToList();
        int beamWidth = 2700000 / Board.Area / Board.Size;
        int solutionsCount = 20;
        Console.Error.WriteLine("beam width: " + beamWidth);
        List<BeamNode> bestNodes = new List<BeamNode>();
        Random random = new Random(0);
        while (beam.Count > 0)
        {
            beam = beam.SelectMany(b => b.ExpandBefore()).OrderByDescending(b => b.Score + random.NextDouble()).ToList();
            bestNodes.AddRange(beam.Take(solutionsCount));
            bestNodes = bestNodes.Distinct().OrderByDescending(b => b.Score).Take(solutionsCount).ToList();
            HashSet<BeamNode> distinct = new HashSet<BeamNode>();
            foreach (BeamNode p in beam)
            {
                distinct.Add(p);
                if (distinct.Count >= beamWidth) break;
            }
            beam = distinct.ToList();
        }

        List<Plan> searches = bestNodes.Select(b => b.GetPlan()).ToList();

        beamWidth = 20;
        int resets = 0;
        List<Plan> stucks = new List<Plan> { searches[0] };
        Plan current = searches[0];
        Plan currentBest = searches[0];
        List<Cell> path = null;
        bool allEdges = false;
        int mutate = 0;
        int lastImprovement = 0;
        int resetSteps = Board.Size * Board.Area;
        for (; sw.ElapsedMilliseconds < timeLimit; mutate++)
        {
            if (!allEdges && lastImprovement + resetSteps * 4 / 5 < mutate)
            {
                allEdges = true;
                Board.ExtendEdges();
            }
            path = current.Path.ToList();
            int skipCount = 10 + random.Next(1 + 30 / Board.Size);
            int startIndex = random.Next(-5, path.Count - skipCount);
            BeamNode intermediate = new BeamNode
            {
                Cell = path[startIndex + skipCount],
                Score = current.PartialScore(startIndex + skipCount),
                Mult = current.Path.Skip(startIndex + skipCount - 1).Sum(c => c.Mult),
                PathLength = current.Path.Count - startIndex - skipCount + 1,
                Visited = current.Visited.ToArray()
            };
            for (int i = Math.Max(0, startIndex); i < startIndex + skipCount; i++)
            {
                intermediate.Visited[path[i].Y] ^= 1 << path[i].X;
            }
            int[] visitedBackup = intermediate.Visited.ToArray();
            bool[] rearrange = new bool[Board.Area];
            for (int i = 1; i + 2 < startIndex; i++)
            {
                if (Board.NextLookup[path[i - 1].ID, path[i + 1].ID])
                {
                    intermediate.Visited[path[i].Y] ^= 1 << path[i].X;
                    rearrange[path[i].ID] = true;
                }
            }
            for (int i = startIndex + skipCount + 1; i + 1 < path.Count; i++)
            {
                if (path[i].Mult == 1 && Board.NextLookup[path[i - 1].ID, path[i + 1].ID])
                {
                    intermediate.Visited[path[i].Y] ^= 1 << path[i].X;
                    rearrange[path[i].ID] = true;
                }
            }

            beam = new List<BeamNode> { intermediate };
            if (random.Next(Board.Size / 2) == 0) // block random cell to avoid same result
            {
                Cell c = startIndex >= 0 ? path[startIndex + 1 + random.Next(skipCount - 2)] : path[random.Next(startIndex + skipCount)];
                intermediate.Visited[c.Y] ^= 1 << c.X;
            }
            Cell target = startIndex >= 0 ? path[startIndex] : null;
            BeamNode final = null;
            while (beam.Count > 0)
            {
                List<BeamNode> next = new List<BeamNode>();
                HashSet<BeamNode> distinct = new HashSet<BeamNode>();
                int minScore = 0;
                foreach (BeamNode node in beam)
                {
                    foreach (BeamNode n in node.ExpandBefore(false))
                    {
                        if (rearrange[n.Cell.ID])
                        {
                            n.Score -= n.Mult;
                            n.PathLength--;
                            n.Mult -= n.Cell.Mult;
                        }
                        n.SortScore = n.EstimateScore(startIndex) + random.NextDouble();
                        if (n.SortScore < minScore) continue;
                        n.Visited = node.Visited.ToArray();
                        n.Visited[n.Cell.Y] |= 1 << n.Cell.X;
                        if ((n.Cell == target || target == null) && (final == null || n.EstimateScore(startIndex) > final.EstimateScore(startIndex))) final = n;
                        if (next.Count < beamWidth || distinct.Add(n)) next.Add(n);
                        if (next.Count > 2 * beamWidth)
                        {
                            next = next.OrderByDescending(ne => ne.SortScore).Take(beamWidth).ToList();
                            minScore = (int)next.Last().SortScore;
                        }
                    }
                }
                beam = next.OrderByDescending(n => n.SortScore).Take(beamWidth).ToList();
            }
            if (final == null) continue;

            List<Cell> intermediatePath = new List<Cell>();
            int[] finalVisited = final.Visited;
            while (final != null)
            {
                intermediatePath.Add(final.Cell);
                final = final.Parent;
            }
            if (startIndex >= 0) path.RemoveRange(startIndex + 1, skipCount);
            else path.RemoveRange(0, skipCount + startIndex);
            bool[] newSet = new bool[Board.Area];
            for (int i = startIndex < 0 ? 0 : 1; i < intermediatePath.Count; i++) newSet[intermediatePath[i].ID] = true;
            path = path.Where(p => !newSet[p.ID]).ToList();
            if (startIndex >= 0) path.InsertRange(path.IndexOf(intermediatePath[0]) + 1, intermediatePath.Skip(1));
            else path.InsertRange(0, intermediatePath);
            bool valid = true;
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (!Board.NextLookup[path[i].ID, path[i + 1].ID]) valid = false;
            }
            if (!valid) continue;

            foreach (Cell c in intermediatePath) visitedBackup[c.Y] |= 1 << c.X;
            Plan newPlan = new Plan(path, new int[Board.Size]) { Visited = visitedBackup };
            current = newPlan;
#if DEBUG
            //if (path.Count != path.Distinct().Count()) Debugger.Break();
            //int[] tmp = new int[Board.Size];
            //foreach (Cell c in path) tmp[c.Y] |= 1 << c.X;
            //if (!tmp.SequenceEqual(newPlan.Visited)) Debugger.Break();
            if (newPlan.Score > currentBest.Score)
                Console.Error.WriteLine(sw.ElapsedMilliseconds + ": " + newPlan.Path.Count + "," + newPlan.Score);
#endif
            if (newPlan.Score > currentBest.Score) lastImprovement = mutate;
            if (newPlan.Score >= currentBest.Score) currentBest = newPlan;
            if (newPlan.Score < currentBest.Score * 0.99 - 500) current = currentBest;
            if (lastImprovement + resetSteps < mutate)
            {
                allEdges = false;
                Board.ReduceEdges();
                lastImprovement = mutate;
                stucks.Add(currentBest);
                resets++;
                currentBest = searches[resets % searches.Count];
                current = currentBest;
            }
        }

        Board.ExtendEdges();
        stucks.Add(currentBest);
        for (int i = 0; i < stucks.Count; i++)
        {
            Plan stuck = stucks[i];
            beam = new List<BeamNode> { stuck.GetBeamNode() };
            while (beam.Count > 0)
            {
                stuck = beam[0].GetPlan();
                beam = beam.SelectMany(b => b.ExpandBefore()).OrderByDescending(b => b.Score).Take(beamWidth).ToList();
            }

            candidates = new List<Cell>();
            foreach (var cell in Board.Grid)
            {
                if ((stuck.Visited[cell.Y] & (1 << cell.X)) == 0)
                    candidates.Add(cell);
            }
            beam = candidates.Select(c => new BeamNode(c)).ToList();
            foreach (BeamNode b in beam)
            {
                b.Visited = stuck.Visited.ToArray();
                b.Visited[b.Cell.Y] |= 1 << b.Cell.X;
            }
            List<BeamNode> prev = beam.ToList();
            while (beam.Count > 0)
            {
                beam = beam.SelectMany(b => b.ExpandBefore()).OrderByDescending(b => b.Score).Take(beamWidth).ToList();
                prev.AddRange(beam);
            }
            path = stuck.Path;
            prev = prev.Where(c => c.Cell.Prev.Any(p => p == path.Last())).ToList();
            if (prev.Any())
            {
                BeamNode node = prev.OrderByDescending(c => c.PathLength * stuck.Path.Sum(p => p.Mult) + c.Score).First();
                for (int j = path.Count - 1; j >= 0; j--) node = new BeamNode(path[j], node, true);
                stuck = node.GetPlan();
            }
            stucks[i] = stuck;
        }

        Plan best = stucks.OrderBy(b => b.Score).Last();
        Console.Error.WriteLine("score: " + best.Score);
        Console.Error.WriteLine("TESTCASE_INFO {\"visitRatio\": " + (double)best.Path.Count / Board.Area + ", \"mutations\": " + mutate + ", \"resets\": " + resets + "}");
        best.Print();
    }
}