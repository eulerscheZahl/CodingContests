using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    public static int Width;
    public static int Height;
    public static int Area => Width * Height;
    public static Cell[,] Grid;

    public static int WaterStartTime;
    public static List<Cell> WaterCells = new List<Cell>();
    public static List<Cell> BuilderCells = new List<Cell>();
    const int MAX_TIME = 9000;

    public void ReadInitial()
    {
        Height = int.Parse(Console.ReadLine().Split().Last());
        Width = int.Parse(Console.ReadLine().Split().Last());
        Console.ReadLine(); // builders
        Console.ReadLine(); // taps
        WaterStartTime = int.Parse(Console.ReadLine().Split().Last());

        Grid = new Cell[Width, Height];
        int x = 0, y = 0;
        while (y < Height)
        {
            string line = Console.ReadLine();
            while (line.StartsWith("Grid") || line.Contains("ratio")) line = Console.ReadLine();
            foreach (char c in line)
            {
                Grid[x, y] = new Cell(x, y, c);
                if (Grid[x, y].Hydrant) WaterCells.Add(Grid[x, y]);
                if (Grid[x, y].Builder) BuilderCells.Add(Grid[x, y]);

                x++;
                if (x == Width)
                {
                    x = 0;
                    y++;
                }
            }
        }
        foreach (Cell cell in Grid) cell.MakeNeighbors();
        foreach (Cell cell in Grid) cell.BFS();
    }

    private static void ShuffleList(List<Cell> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int idx = random.Next(i, list.Count);
            Cell tmp = list[i];
            list[i] = list[idx];
            list[idx] = tmp;
        }
    }

    private static Random random = new Random(0);
    public List<List<Action>> Solve()
    {
        Stopwatch sw = Stopwatch.StartNew();
        int[] waterTime = new int[Area];
        int[] builderDist = new int[Area];
        for (int i = 0; i < Area; i++)
        {
            waterTime[i] = 1000;
            builderDist[i] = 1000;
        }
        List<Cell> water = WaterCells.ToList();
        water.ForEach(c => waterTime[c.ID] = 0);
        int t = WaterStartTime;
        while (water.Count > 0)
        {
            water = water.SelectMany(w => w.Neighbors).Where(n => waterTime[n.ID] == 1000).Distinct().ToList();
            water.ForEach(c => waterTime[c.ID] = t);
            t++;
        }

        List<Cell> builders = BuilderCells.ToList();
        builders.ForEach(c => builderDist[c.ID] = 0);
        t = 1;
        while (builders.Count > 0)
        {
            builders = builders.SelectMany(w => w.Neighbors).Where(n => !Grid[n.X, n.Y].Hydrant && builderDist[n.ID] == 1000).Distinct().ToList();
            builders.ForEach(c => builderDist[c.ID] = t);
            t++;
        }

        List<Cell> toSave = BuilderCells.Where(c => !c.NearHydrant).ToList();
        int bestScore = 0;
        bool[] toSaveLookup = new bool[Area];
        foreach (Cell s in toSave) toSaveLookup[s.ID] = true;
        List<Cell> frame = GetFrame(toSave, builderDist, toSaveLookup);
        List<List<Action>> plan = MakePlan(toSave, toSaveLookup, frame.ToList(), waterTime, out bestScore);
        if (plan == null) toSave = new List<Cell>();
        List<Cell> saveBest = toSave.ToList();
        List<Cell> saveStart = toSave.ToList();
        int run;
        HashSet<PartialSolution> partials = new HashSet<PartialSolution>();
        for (run = 0; sw.ElapsedMilliseconds < MAX_TIME; run++)
        {
            frame.Clear();
            ShuffleList(BuilderCells);
            ShuffleList(WaterCells);
            toSave = saveStart.ToList();
            if (toSave.Count > 0 && random.Next(5) == 0) toSave.RemoveAt(random.Next(toSave.Count));
            if (run > 100 && random.Next(4) == 0) toSave.Clear();
            if (run < 1 << WaterCells.Count)
            {
                toSave.Clear();
                for (int i = 0; i < WaterCells.Count; i++)
                {
                    if ((run & (1 << i)) == 0) continue;
                    List<Cell> save = WaterCells.OrderBy(c => c.ID).First().Neighbors.SelectMany(n => n.Neighbors).Distinct().Where(c => !c.Hydrant && !toSaveLookup[c.ID]).ToList();
                    toSave.AddRange(save.Where(s => BuilderCells.Any(b => b.Dist[s.ID] < 1000))); // remove unreachable cells
                }
                toSave = toSave.Distinct().ToList();
            }
            toSaveLookup = new bool[Area];
            foreach (Cell s in toSave) toSaveLookup[s.ID] = true;
            int stuck = 0;
            List<Cell> extend = null;
            List<List<Action>> runPlan = null;
            int[] lastFailed = new int[Area];
            for (int step = 0; sw.ElapsedMilliseconds < MAX_TIME && stuck < 20; step++)
            {
                while (toSave.Count == 0)
                {
                    Cell builder = BuilderCells[random.Next(BuilderCells.Count)];
                    for (int i = 0; i < 5; i++) builder = builder.Neighbors[random.Next(builder.Neighbors.Length)];
                    if (!builder.NearHydrant && builderDist[builder.ID] < 1000)
                    {
                        toSave.Add(builder);
                        toSaveLookup[toSave.Last().ID] = true;
                    }
                }
                if (extend == null) extend = toSave.SelectMany(s => s.Neighbors).Distinct().Where(n => !n.NearHydrant && !toSaveLookup[n.ID]).OrderByDescending(c => lastFailed[c.ID]).ToList();
                if (extend.Count == 0) break;
                bool added = false;
                if (step == 0 && random.Next(10) == 0)
                {
                    foreach (Cell hydrant in WaterCells)
                    {
                        List<Cell> save = hydrant.Neighbors.SelectMany(n => n.Neighbors).Distinct().Where(c => !c.Hydrant && !toSaveLookup[c.ID]).ToList();
                        save = save.Where(s => BuilderCells.Any(b => b.Dist[s.ID] < 1000)).ToList(); // remove unreachable cells
                        if (save.Count > 0)
                        {
                            added = true;
                            toSave.AddRange(save);
                            foreach (Cell s in save) toSaveLookup[s.ID] = true;
                            break;
                        }
                    }
                }
                if (!added && frame.Count > 0 && random.Next(5) == 0)
                {
                    Cell f = frame[random.Next(frame.Count)];
                    f = f.Neighbors[random.Next(f.Neighbors.Length)];
                    if (random.Next(2) == 0) f = f.Neighbors[random.Next(f.Neighbors.Length)];
                    if (!toSaveLookup[f.ID] && !f.NearHydrant && BuilderCells.Any(b => b.Dist[f.ID] < 1000))
                    {
                        toSave.Add(f);
                        toSaveLookup[f.ID] = true;
                        extend.Remove(f);
                        added = true;
                    }
                }
                if (!added)
                {
                    toSave.Add(extend[(int)Math.Sqrt(random.Next(extend.Count * extend.Count))]);
                    extend.Remove(toSave.Last());
                    toSaveLookup[toSave.Last().ID] = true;
                }
                int saveCount = toSave.Count - 1;
                frame = GetFrame(toSave, builderDist, toSaveLookup);
                int tmpScore = 0;
                List<List<Action>> plan2 = MakePlan(toSave, toSaveLookup, frame.ToList(), waterTime, out tmpScore);
                if (plan2 == null)
                {
                    stuck++;
                    while (toSave.Count > saveCount)
                    {
                        toSaveLookup[toSave.Last().ID] = false;
                        lastFailed[toSave.Last().ID] = step;
                        toSave.RemoveAt(toSave.Count - 1);
                    }
                    continue;
                }
                runPlan = plan2;
                stuck = 0;
                extend = null;
                if (tmpScore > bestScore)
                {
                    bestScore = tmpScore;
                    plan = plan2;
                    saveBest = toSave.ToList();
                    Console.Error.WriteLine("saving: " + bestScore + "/" + saveBest.Count + " @" + run);
                }
            }
            if (runPlan != null) partials.UnionWith(PartialSolution.GetPartials(runPlan, toSaveLookup));
        }
        plan = PartialSolution.Combine(partials, plan, bestScore);
        Console.Error.WriteLine("saving: " + bestScore + "/" + saveBest.Count + " @" + run);

        // saveBest.Add(Grid[11, 2]);
        // toSaveLookup = new bool[Area];
        // foreach (Cell cell in saveBest) toSaveLookup[cell.ID] = true;
        // frame = GetFrame(toSave, builderDist, toSaveLookup);
        // var ppp = MakePlan(saveBest, toSaveLookup, frame.ToList(), waterTime, out int testScore);

        return plan;
    }

    private List<List<Action>> MakePlan(List<Cell> toSave, bool[] toSaveLookup, List<Cell> frame, int[] waterTime, out int score)
    {
        score = 0;
        if (frame.Any(f => f.Hydrant)) return null;
        bool[] water = new bool[Area];
        List<Cell> waterCells = WaterCells.ToList();
        waterCells.ForEach(w => water[w.ID] = true);
        int[] times = BuilderCells.Select(c => 0).ToArray();
        HashSet<Cell> flowers = new HashSet<Cell>(toSave.Where(c => c.Flower));
        List<Cell>[] visitOrder = BuilderCells.Select(b => new List<Cell> { b }).ToArray();

        // define initial tasks
        while (frame.Count > 0)
        {
            int[] frameIds = frame.Select(c => c.ID).ToArray();
            int sourceIndex = 0, targetFrameId = frameIds[0], insertIndex = visitOrder[sourceIndex].Count;
            int time = times[sourceIndex] + visitOrder[sourceIndex][insertIndex - 1].Dist[targetFrameId];
            for (int source = 0; source < BuilderCells.Count; source++)
            {
                int t0 = times[source];
                if (t0 + 1 >= time) continue;
                List<Cell> visitSource = visitOrder[source];
                for (int insert = 1; insert <= visitSource.Count; insert++)
                {
                    int[] prevDist = visitSource[insert - 1].Dist;
                    foreach (int frameId in frameIds)
                    {
                        int t = t0 + prevDist[frameId];
                        //if (insert < visitSource.Count) t += visitSource[insert].Dist[frameId] - visitSource[insert].Dist[prev.ID];
                        if (t < time)
                        {
                            if (insert < visitOrder[source].Count) t += visitSource[insert].Dist[frameId];
                            if (t < time)
                            {
                                time = t;
                                sourceIndex = source;
                                targetFrameId = frameId;
                                insertIndex = insert;
                            }
                        }
                    }
                }
            }
            times[sourceIndex] += visitOrder[sourceIndex][insertIndex - 1].Dist[targetFrameId];
            if (insertIndex < visitOrder[sourceIndex].Count) times[sourceIndex] += visitOrder[sourceIndex][insertIndex].Dist[targetFrameId] - visitOrder[sourceIndex][insertIndex].Dist[visitOrder[sourceIndex][insertIndex - 1].ID];
            visitOrder[sourceIndex].Insert(insertIndex, frame.First(f => f.ID == targetFrameId));
            frame.Remove(visitOrder[sourceIndex][insertIndex]);
        }

        // transfer tasks
        bool transferred = times.Length > 1;
        while (transferred)
        {
            transferred = false;
            int sourceIndex = 0;
            for (int i = 1; i < times.Length; i++)
            {
                if (times[i] > times[sourceIndex]) sourceIndex = i;
            }
            for (int targetIndex = 0; !transferred && targetIndex < times.Length; targetIndex++)
            {
                if (targetIndex == sourceIndex) continue;
                for (int removeIndex = 1; !transferred && removeIndex < visitOrder[sourceIndex].Count; removeIndex++)
                {
                    Cell move = visitOrder[sourceIndex][removeIndex];
                    int removeTime = times[sourceIndex] - visitOrder[sourceIndex][removeIndex - 1].Dist[move.ID];
                    if (removeIndex + 1 < visitOrder[sourceIndex].Count)
                    {
                        Cell next = visitOrder[sourceIndex][removeIndex + 1];
                        removeTime += visitOrder[sourceIndex][removeIndex - 1].Dist[next.ID] - move.Dist[next.ID];
                    }
                    if (removeTime == times[sourceIndex]) continue;
                    for (int insertIndex = 1; insertIndex <= visitOrder[targetIndex].Count; insertIndex++)
                    {
                        int addTime = times[targetIndex] + visitOrder[targetIndex][insertIndex - 1].Dist[move.ID];
                        if (addTime < times[sourceIndex] && insertIndex < visitOrder[targetIndex].Count) addTime += visitOrder[targetIndex][insertIndex].Dist[move.ID];
                        if (addTime < times[sourceIndex])
                        {
                            times[sourceIndex] = removeTime;
                            times[targetIndex] = addTime;
                            visitOrder[targetIndex].Insert(insertIndex, move);
                            visitOrder[sourceIndex].RemoveAt(removeIndex);
                            transferred = true;
                            break;
                        }
                    }
                }
            }
        }

        // generate paths for builders
        List<List<Action>> byBuilder = new List<List<Action>>();
        foreach (List<Cell> walls in visitOrder)
        {
            List<Cell> path = new List<Cell> { walls[0] };
            for (int i = 1; i < walls.Count; i++)
            {
                Cell last = path.Last();
                while (last != walls[i] && !last.Neighbors.Contains(walls[i]))
                {
                    Cell next = null;
                    int nextScore = int.MaxValue;
                    foreach (Cell n in last.Neighbors)
                    {
                        if (n.Dist[walls[i].ID] >= last.Dist[walls[i].ID]) continue;
                        int nScore = 0x10000 * (i + 1 < walls.Count ? n.Dist[walls[i + 1].ID] : 0) + 0x100 * (i + 2 < walls.Count ? n.Dist[walls[i + 2].ID] : 0) + (toSaveLookup[n.ID] ? (n.Flower ? 1 : 0) : 2);
                        if (nScore < nextScore)
                        {
                            nextScore = nScore;
                            next = n;
                        }
                    }
                    last = next;
                    path.Add(last);
                }
            }
            if (walls.Skip(1).Contains(path.Last())) path.Add(path.Last().Neighbors.OrderBy(n => toSaveLookup[n.ID] ? 0 : 1).First(n => !n.Hydrant && !frame.Contains(n)));
            List<Action> actions = new List<Action>();
            for (int i = 1; i < path.Count; i++) actions.Add(new Action(path[i - 1], path[i], false));

            foreach (Cell wall in walls.Skip(1).Reverse<Cell>())
            {
                Action act = actions.LastOrDefault(a => a.Source.Neighbors.Contains(wall));
                if (act != null) actions.Insert(actions.IndexOf(act), new Action(act.Source, wall, true));
                else
                {
                    act = actions.LastOrDefault(a => a.Target.Neighbors.Contains(wall));
                    if (act != null) actions.Insert(actions.IndexOf(act) + 1, new Action(act.Target, wall, true));
                    else actions.Add(new Action(path.Last(), wall, true)); // builder doesn't move at all
                }
            }

            while (actions.Count > 0 && !actions.Last().Build) actions.RemoveAt(actions.Count - 1);
            bool swapped = true;
            while (swapped)
            {
                swapped = false;
                for (int i = 1; i < actions.Count; i++)
                {
                    if (!actions[i].Build || !actions[i - 1].Build) continue;
                    if (waterTime[actions[i].Target.ID] < waterTime[actions[i - 1].Target.ID])
                    {
                        Action tmp = actions[i];
                        actions[i] = actions[i - 1];
                        actions[i - 1] = tmp;
                        swapped = true;
                    }
                }
            }
            byBuilder.Add(actions);
        }

        // aviod builders blocking each other
        List<List<Action>> result = new List<List<Action>>();
        Queue<Action>[] queues = byBuilder.Select(b => new Queue<Action>(b)).ToArray();
        char[] blocked = new char[Area];
        foreach (Cell c in BuilderCells) blocked[c.ID] = 'B';
        int turn = 0;
        while (queues.Any(q => q.Count > 0))
        {
            if (turn >= WaterStartTime)
            {
                List<Cell> newWater = new List<Cell>();
                foreach (Cell w in waterCells)
                {
                    foreach (Cell n in w.Neighbors)
                    {
                        if (water[n.ID] || blocked[n.ID] == '#') continue;
                        water[n.ID] = true;
                        newWater.Add(n);
                    }
                }
                waterCells = newWater;
            }
            bool[] applied = new bool[BuilderCells.Count];
            bool changed = true;
            List<Action> tPlan = new List<Action>();
            result.Add(tPlan);
            while (changed)
            {
                changed = false;
                for (int i = 0; i < BuilderCells.Count; i++)
                {
                    if (applied[i] || queues[i].Count == 0) continue;
                    Action action = queues[i].Peek();
                    if (water[action.Source.ID] || water[action.Target.ID])
                        return null;
                    if (blocked[action.Target.ID] != 0) continue;
                    blocked[action.Target.ID] = action.Build ? '#' : 'B';
                    flowers.Remove(action.Target);
                    if (!action.Build) blocked[action.Source.ID] = '\0';
                    changed = true;
                    applied[i] = true;
                    tPlan.Add(queues[i].Dequeue());
                }
            }
            if (tPlan.Count == 0)
                return null;
            turn++;
        }

        score = toSave.Count + 2 * flowers.Count;
        foreach (List<Action> builderPath in byBuilder)
        {
            Cell lastCell = BuilderCells[byBuilder.IndexOf(builderPath)];
            if (builderPath.Count > 0) lastCell = builderPath.Last().Source;
            if (toSaveLookup[lastCell.ID]) score += 5;
        }
        return result;
    }

    public static int Simulate(List<List<Action>> plan)
    {
        List<Cell> water = WaterCells.ToList();
        char[] grid = new char[Area];
        foreach (Cell cell in Grid) grid[cell.ID] = cell.C;
        int turn = 0;
        while (water.Count > 0)
        {
            if (turn >= WaterStartTime)
            {
                water = water.SelectMany(w => w.Neighbors).Where(n => grid[n.ID] != '~' && grid[n.ID] != '#').Distinct().ToList();
                water.ForEach(c => grid[c.ID] = '~');
            }
            if (turn < plan.Count)
            {
                foreach (Action act in plan[turn])
                {
                    if (grid[act.Source.ID] == '~' || grid[act.Target.ID] == '~' || grid[act.Target.ID] == '#' || grid[act.Target.ID] == 'B')
                        return -1;
                    grid[act.Target.ID] = act.Build ? '#' : 'B';
                    if (!act.Build) grid[act.Source.ID] = '.';
                }
            }
            turn++;

            // Console.Error.WriteLine(" == turn " + turn + " == ");
            // for (int y = 0; y < Board.Height; y++)
            // {
            //     for (int x = 0; x < Board.Width; x++)
            //     {
            //         Console.Error.Write(grid[Grid[x, y].ID]);
            //     }
            //     Console.Error.WriteLine();
            // }
        }

        return grid.Count(c => c == '.') + 3 * grid.Count(c => c == '*') + 6 * grid.Count(c => c == 'B');
    }

    public List<Cell> GetFrame(List<Cell> toSave, int[] builderDist, bool[] toSaveLookup)
    {
        List<Cell> frame = new List<Cell>();
        foreach (Cell c in toSave)
        {
            foreach (Cell n in c.Neighbors)
            {
                if (toSaveLookup[n.ID]) continue;
                toSaveLookup[n.ID] = true;
                frame.Add(n);
            }
        }

        Queue<Cell> water = new Queue<Cell>(WaterCells);
        bool[] reachable = new bool[Area];
        foreach (Cell f in water) reachable[f.ID] = true;
        while (water.Count > 0)
        {
            Cell c = water.Dequeue();
            foreach (Cell n in c.Neighbors)
            {
                if (!reachable[n.ID])
                {
                    reachable[n.ID] = true;
                    if (!toSaveLookup[n.ID]) water.Enqueue(n);
                }
            }
        }

        foreach (Cell c in frame) toSaveLookup[c.ID] = false;
        for (int i = 0; i < Area; i++)
        {
            if (!toSaveLookup[i] && !reachable[i] && builderDist[i] < 1000)
            {
                toSaveLookup[i] = true;
                toSave.Add(Grid[i % Width, i / Width]);
            }
        }

        return frame.Where(f => reachable[f.ID]).ToList();
    }
}
