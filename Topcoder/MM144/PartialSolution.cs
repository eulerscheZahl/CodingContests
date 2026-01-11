using System;
using System.Collections.Generic;
using System.Linq;

public class PartialSolution : IEquatable<PartialSolution>
{
    public long[] Cells;
    public int Builders;
    public int Score;
    public List<List<Action>> Actions;

    private static int[] zobrist;
    static PartialSolution()
    {
        zobrist = new int[Board.Area];
        Random random = new Random(0);
        for (int i = 0; i < zobrist.Length; i++) zobrist[i] = random.Next();
    }

    private int hash;
    public PartialSolution(List<List<Action>> actions, int builders, int score, long[] cells, HashSet<Cell> affected)
    {
        Actions = actions;
        Builders = builders;
        Score = score;
        Cells = cells;
        hash = 0x100 * score + builders;
        foreach (Cell c in affected) hash ^= zobrist[c.ID];
    }

    public override int GetHashCode() => hash;

    public bool Equals(PartialSolution sol)
    {
        if (this.Builders != sol.Builders) return false;
        if (this.Score != sol.Score) return false;
        for (int i = 0; i < Cells.Length; i++)
        {
            if (this.Cells[i] != sol.Cells[i]) return false;
        }
        return true;
    }

    public static IEnumerable<PartialSolution> GetPartials(List<List<Action>> plan, bool[] savedLookup)
    {
        bool[] walked = new bool[Board.Area];
        List<Cell> currents = Board.BuilderCells.OrderBy(c => c.ID).ToList();
        List<HashSet<Cell>> affecting = currents.Select(c => new HashSet<Cell> { c }).ToList();
        List<List<Action>> actions = currents.Select(c => new List<Action>()).ToList();
        foreach (List<Action> turn in plan)
        {
            foreach (Action act in turn)
            {
                int idx = currents.IndexOf(act.Source);
                if (!act.Build) currents[idx] = act.Target;
                if(act.Build) affecting[idx].Add(act.Target);
                actions[idx].Add(act);
                walked[act.Source.ID] = true;
            }
        }

        for (int i = 0; i < affecting.Count; i++)
        {
            bool[] visited = new bool[Board.Area];
            foreach (Cell c in affecting[i]) visited[c.ID] = true;
            Queue<Cell> queue = new Queue<Cell>(affecting[i]);
            while (queue.Count > 0)
            {
                Cell c = queue.Dequeue();
                foreach (Cell n in c.Neighbors)
                {
                    if (visited[n.ID] || !savedLookup[n.ID]) continue;
                    visited[n.ID] = true;
                    affecting[i].Add(n);
                    queue.Enqueue(n);
                }
            }

            // Console.Error.WriteLine(" == affecting " + i + " == ");
            // for (int y = 0; y < Board.Height; y++)
            // {
            //     for (int x = 0; x < Board.Width; x++)
            //     {
            //         Console.Error.Write(affecting[i].Contains(Board.Grid[x, y]) ? 'a' : Board.Grid[x, y].C);
            //     }
            //     Console.Error.WriteLine();
            // }
        }

        for (int i = 0; i < affecting.Count; i++)
        {
            if (affecting[i] == null) continue;
            int builders = 1 << i;
            HashSet<Action> acts = new HashSet<Action>(actions[i]);
            for (int j = i + 1; j < affecting.Count; j++)
            {
                if (affecting[j] == null) continue;
                if (affecting[i].Any(a => affecting[j].Contains(a)))
                {
                    builders |= 1 << j;
                    acts.UnionWith(actions[j]);
                    affecting[i].UnionWith(affecting[j]);
                    affecting[j] = null;
                }
            }
            if (builders + 1 == (1 << Board.BuilderCells.Count)) yield break; // makes no sense to store a solution with all builders involved

            long[] cells = new long[(Board.Area + 63) / 64];
            int score = 0;
            foreach (Cell cell in affecting[i])
            {
                cells[cell.ID / 64] |= 1L << (cell.ID % 64);
                if (!savedLookup[cell.ID]) continue;
                score++;
                if (cell.Flower && !walked[cell.ID]) score += 2;
            }
            for (int b = 0; b < Board.BuilderCells.Count; b++)
            {
                if ((builders & (1 << b)) != 0 && savedLookup[currents[b].ID]) score += 5;
            }
            yield return new PartialSolution(plan.Select(ac => ac.Where(a => acts.Contains(a)).ToList()).ToList(), builders, score, cells, affecting[i]);
        }
    }

    public static List<List<Action>> Combine(IEnumerable<PartialSolution> partials, List<List<Action>> bestPlan, int bestScore)
    {
        if (!partials.Any()) return bestPlan;
        PartialSolution.bestPlan = bestPlan;
        PartialSolution.bestScore = bestScore;
        Dictionary<int, List<PartialSolution>> dict = new Dictionary<int, List<PartialSolution>>();
        foreach (PartialSolution partial in partials.OrderByDescending(p => p.Score))
        {
            if (dict.ContainsKey(partial.Builders)) dict[partial.Builders].Add(partial);
            else dict[partial.Builders] = new List<PartialSolution> { partial };
        }

        Combine(dict.Values.ToArray(), new PartialSolution[Board.BuilderCells.Count], 0, 0, 0, new long[partials.First().Cells.Length]);
        return PartialSolution.bestPlan;
    }

    private static int bestScore;
    private static List<List<Action>> bestPlan;

    private static void Combine(List<PartialSolution>[] partials, PartialSolution[] partialPath, int pathsCount, int nextIndex, int usedBuilders, long[] usedCells)
    {
        if (nextIndex == partials.Length)
        {
            if (bestScore >= partialPath.Take(pathsCount).Sum(p => p.Score)) return;
            List<List<Action>> plan = new List<List<Action>>();
            for (int t = 0; ; t++)
            {
                List<Action> acts = new List<Action>();
                foreach (PartialSolution part in partialPath.Take(pathsCount))
                {
                    if (part.Actions.Count > t) acts.AddRange(part.Actions[t]);
                }
                if (acts.Count == 0) break;
                plan.Add(acts);
            }
            int score = Board.Simulate(plan);
            if (score > bestScore)
            {
                bestScore = score;
                bestPlan = plan;
            }
            return;
        }
        Combine(partials, partialPath, pathsCount, nextIndex + 1, usedBuilders, usedCells);
        for (int i = 0; i < 3 && i < partials[nextIndex].Count; i++)
        {
            if ((partials[nextIndex][i].Builders & usedBuilders) != 0) continue;
            bool collision = false;
            for (int k = 0; k < usedCells.Length; k++) collision |= (usedCells[k] & partials[nextIndex][i].Cells[k]) != 0;
            if (collision) continue;
            for (int k = 0; k < usedCells.Length; k++) usedCells[k] ^= partials[nextIndex][i].Cells[k];
            partialPath[pathsCount] = partials[nextIndex][i];
            Combine(partials, partialPath, pathsCount + 1, nextIndex + 1, usedBuilders | partials[nextIndex][i].Builders, usedCells);
            for (int k = 0; k < usedCells.Length; k++) usedCells[k] ^= partials[nextIndex][i].Cells[k];
        }
    }
}