using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    public static int FloorCount;
    public static int LiftCount;
    public static double SpawnProbability;
#if LOCAL_ENV || DEBUG
    const int MaxTime = 4000;
#else
    const int MaxTime = 9000;
#endif
    public List<Lift> Lifts = new List<Lift>();
    public List<FloorQueue> FloorQueues = new List<FloorQueue>();

    public void ReadInitial()
    {
        FloorCount = int.Parse(Solution.ReadLine());
        LiftCount = int.Parse(Solution.ReadLine());
        SpawnProbability = double.Parse(Solution.ReadLine());

        for (int i = 0; i < LiftCount; i++) Lifts.Add(new Lift { Floor = int.Parse(Solution.ReadLine()) - 1, ID = i });
        for (int i = 0; i < FloorCount; i++) FloorQueues.Add(new FloorQueue { Floor = i });
    }

    private static Random random = new Random(0);
    public void ReadTurn()
    {
        string line = Solution.ReadLine();
        if (line.Contains(" "))
        {
            Lifts[0].Parse(line);
            for (int i = 1; i < LiftCount; i++) Lifts[i].Parse(Solution.ReadLine());
            for (int i = 0; i < FloorCount; i++) FloorQueues[i].Parse(Solution.ReadLine());
        }
        else
        {
            int newPeople = int.Parse(line);
            Stack<string> people = new Stack<string>();
            for (int i = 0; i < newPeople; i++) people.Push(Solution.ReadLine());
            while (people.Count > 0)
            {
                string[] temp = people.Pop().Split();
                int floor = int.Parse(temp[0]) - 1;
                FloorQueues[floor].SpawnCount++;
                char direction = temp[1].ToUpper()[0];
                if (direction == 'U') FloorQueues[floor].Queue.Add(random.Next(floor + 1, FloorCount));
                else FloorQueues[floor].Queue.Add(random.Next(floor));
            }

            int enteredPeople = int.Parse(Solution.ReadLine());
            Stack<int[]> input = new Stack<int[]>();
            for (int i = 0; i < enteredPeople; i++) input.Push(Solution.ReadLine().Split().Select(int.Parse).ToArray());
            while (input.Count > 0)
            {
                int[] nums = input.Pop();
                int lift = nums[0];
                int targetFloor = nums[1] - 1;
                int source = Lifts[lift].Floor;
                FloorQueues[source].Queue.RemoveAt(0);
                Lifts[lift].Targets[targetFloor]++;
                Lifts[lift].TargetCount++;
            }
        }
    }

    public int totalRuns = 0;
    public List<string> Plan(int turn)
    {
        int elapsedTime = int.Parse(Solution.ReadLine());
#if DEBUG
        Console.Error.WriteLine("turn " + turn + " @" + elapsedTime + " ms");
        Console.Error.WriteLine(FloorCount + "\n" + LiftCount + "\n" + SpawnProbability);
        foreach (Lift lift in Lifts) Console.Error.WriteLine(0);
        foreach (Lift lift in Lifts) Console.Error.WriteLine(lift.ID + " " + lift.Floor + " " + lift.Open + " " + string.Join(" ", lift.Targets));
        foreach (FloorQueue queue in FloorQueues) Console.Error.WriteLine(queue.SpawnCount + " " + queue.Floor + " " + string.Join(" ", queue.Queue));
        Console.Error.WriteLine(elapsedTime);
#endif

        Random random = new Random(0);
        List<int> down = new List<int>();
        List<int> up = new List<int>();
        int targetRuns = 100;
        int subCount = 20;
#if !DEBUG
        if (turn > 10)
        {
            double runsPerMs = (double)totalRuns / Math.Max(elapsedTime, turn);
            double timePerTurn = (MaxTime - elapsedTime) / (1000.0 - turn);
            targetRuns = (int)(timePerTurn * runsPerMs);
            if (targetRuns < 10) targetRuns = 10;
        }
#endif

        Dictionary<string, List<double>> scores = new Dictionary<string, List<double>>();
        for (int run = 0; run < targetRuns; run += subCount)
        {
            RandomizeQueues(random);
            Dictionary<string, double> runScores = new Dictionary<string, double>();
            for (int subRun = 0; subRun < subCount; subRun++)
            {
                totalRuns++;
                double randomFactor = subRun == 0 ? 0 : 5;
                string[] tmpResult = Enumerable.Range(0, LiftCount).Select(i => "stay").ToArray();
                double tmpScore = 0;
                List<Lift> lifts = Lifts.Select(l => new Lift(l)).ToList();
                List<int>[] queues = FloorQueues.Select(f => f.Queue.ToList()).ToArray();
                int[] times = new int[LiftCount];
                foreach (Lift lift in lifts.Where(l => l.Open))
                {
                    times[lift.ID]++;
                    lift.Open = false;
                    tmpResult[lift.ID] = "close";
                }
                // transport people
                while (true)
                {
                    // for each lift: generate and score possible actions - include a bit of random in score for sorting
                    Action bestAction = null;
                    foreach (Lift lift in lifts)
                    {
                        down.Clear();
                        for (int i = lift.Floor - 1; i >= 0; i--) { if (lift.Targets[i] > 0) down.Add(i); }
                        if (down.Count > 0)
                        {
                            for (int i = down[0] + 1; !lift.Full && i < lift.Floor; i++) { if (queues[i].Count > 0) down.Add(i); }
                            foreach (int d in down)
                            {
                                if (turn + times[lift.ID] + Math.Abs(lift.Floor - d) + 1 > 1000) continue;
                                Action act = new Action(lift, d, times[lift.ID] + Math.Abs(lift.Floor - d) + (lift.Targets[d] > 0 ? 0 : 1) - 2 * lift.Targets[d] - Math.Min(4 - lift.TargetCount - lift.Targets[d], queues[d].Count) + randomFactor * random.NextDouble());
                                if (bestAction == null || act.Score < bestAction.Score) bestAction = act;
                            }
                        }
                        up.Clear();
                        for (int i = lift.Floor + 1; i < FloorCount; i++) { if (lift.Targets[i] > 0) up.Add(i); }
                        if (up.Count > 0)
                        {
                            for (int i = up[0] - 1; !lift.Full && i > lift.Floor; i--) { if (queues[i].Count > 0) up.Add(i); }
                            foreach (int u in up)
                            {
                                if (turn + times[lift.ID] + Math.Abs(lift.Floor - u) + 1 > 1000) continue;
                                Action act = new Action(lift, u, times[lift.ID] + Math.Abs(lift.Floor - u) + (lift.Targets[u] > 0 ? 0 : 1) - 2 * lift.Targets[u] - Math.Min(4 - lift.TargetCount - lift.Targets[u], queues[u].Count) + randomFactor * random.NextDouble());
                                if (bestAction == null || act.Score < bestAction.Score) bestAction = act;
                            }
                        }
                        if (lift.Targets[lift.Floor] > 0 || !lift.Full && queues[lift.Floor].Count > 0)
                        {
                            if (turn + times[lift.ID] + 1 > 1000) continue;
                            Action act = new Action(lift, lift.Floor, times[lift.ID] + (lift.Targets[lift.Floor] > 0 ? 0 : 1) - 2 * lift.Targets[lift.Floor] - Math.Min(4 - lift.TargetCount - lift.Targets[lift.Floor], queues[lift.Floor].Count) + randomFactor * random.NextDouble());
                            if (bestAction == null || act.Score < bestAction.Score) bestAction = act;
                        }
                        if (!lift.Full)
                        {
                            for (int i = 0; i < FloorCount; i++)
                            {
                                if (i == lift.Floor) continue;
                                if (lift.TargetCount > 0 && Math.Abs(i - lift.Floor) > 1) continue;
                                if (queues[i].Count > 0)
                                {
                                    if (turn + times[lift.ID] + Math.Abs(lift.Floor - i) + 1 > 1000) continue;
                                    Action act = new Action(lift, i, times[lift.ID] + Math.Abs(lift.Floor - i) + 1 - 2 * lift.Targets[i] - Math.Min(4 - lift.TargetCount - lift.Targets[i], queues[i].Count) + randomFactor * random.NextDouble());
                                    if (bestAction == null || act.Score < bestAction.Score) bestAction = act;
                                }
                            }
                        }
                    }
                    if (bestAction == null) break;
                    Lift l = bestAction.Lift;
                    int initialFloor = l.Floor;
                    if (l.Floor != bestAction.Target)
                    {
                        times[l.ID] += Math.Abs(l.Floor - bestAction.Target);
                        times[l.ID] += l.Open ? 2 : 1;
                    }
                    else times[l.ID] += l.Open ? 0 : 1;
                    l.Floor = bestAction.Target;
                    l.Open = true;
                    tmpScore += times[l.ID] * l.Targets[l.Floor];
                    l.TargetCount -= l.Targets[l.Floor];
                    l.Targets[l.Floor] = 0;
                    while (!l.Full && queues[l.Floor].Count > 0 && times[l.ID] + turn < 1000)
                    {
                        l.Targets[queues[l.Floor][0]]++;
                        l.TargetCount++;
                        queues[l.Floor].RemoveAt(0);
                    }
                    if (tmpResult[l.ID] == "stay")
                    {
                        if (l.Floor == initialFloor) tmpResult[l.ID] = "open";
                        else if (l.Floor < initialFloor) tmpResult[l.ID] = "down";
                        else if (l.Floor > initialFloor) tmpResult[l.ID] = "up";
                    }
                }

                // distribute evenly
                List<int> floorPriority = FloorQueues.OrderByDescending(f => f.SpawnCount).Select(f => f.Floor).ToList();
                floorPriority = floorPriority.Concat(floorPriority).ToList();
                tmpScore += (1000 - turn) * (lifts.Sum(l => l.TargetCount) + queues.Sum(q => q.Count));
                if (turn > 990) tmpScore += 10 * FloorQueues.Where(f => f.Full).Sum(f => queues[f.Floor].Count);
                tmpScore *= 100;
                List<Lift> available = lifts.ToList();
                foreach (int floor in floorPriority)
                {
                    if (available.Count == 0) break;
                    List<Action> actions = new List<Action>();
                    foreach (Lift lift in available)
                    {
                        if (lift.Floor == floor && times[lift.ID] == 1 && tmpResult[lift.ID] == "close") actions.Add(new Action(lift, floor, 0));
                        else if (lift.Floor == floor) actions.Add(new Action(lift, floor, times[lift.ID] + (lift.Open ? 0 : 1)));
                        else actions.Add(new Action(lift, floor, times[lift.ID] + Math.Abs(lift.Floor - floor) + (lift.Open ? 2 : 1)));
                    }
                    Action action = actions.OrderBy(a => a.Score).First();
                    Lift l = action.Lift;
                    int initialFloor = l.Floor;
                    if (l.Floor != action.Target) times[l.ID] += 2 + Math.Abs(l.Floor - action.Target);
                    l.Floor = action.Target;
                    if (l.Floor == action.Target) tmpScore += l.Open ? 0 : 1;
                    else tmpScore += Math.Abs(l.Floor - action.Target) + (l.Open ? 2 : 1);
                    available.Remove(l);
                    if (tmpResult[l.ID] == "stay")
                    {
                        if (l.Floor < initialFloor) tmpResult[l.ID] = "down";
                        else if (l.Floor > initialFloor) tmpResult[l.ID] = "up";
                    }
                }

                for (int i = 0; i < LiftCount; i++)
                {
                    if (tmpResult[i] == "close" && times[i] == 1) tmpResult[i] = "stay";
                    else if (tmpResult[i] == "stay" && lifts[i].Closed) tmpResult[i] = "open";
                }

                string key = string.Join(" ", tmpResult);
                if (runScores.ContainsKey(key)) runScores[key] = Math.Min(runScores[key], tmpScore);
                else runScores[key] = tmpScore;
            }

            foreach (var pair in runScores)
            {
                if (scores.ContainsKey(pair.Key)) scores[pair.Key].Add(pair.Value);
                else scores[pair.Key] = new List<double> { pair.Value };
            }
        }

        string best = scores.Keys.OrderBy(k => scores[k].Average()).First();
        List<string> result = best.Split().ToList();
        for (int i = 0; i < LiftCount; i++)
        {
            if (result[i] == "open")
            {
                Lifts[i].Open = true;
                Lifts[i].TargetCount -= Lifts[i].Targets[Lifts[i].Floor];
                Lifts[i].Targets[Lifts[i].Floor] = 0;
            }
            else if (result[i] == "close") Lifts[i].Open = false;
            else if (result[i] == "up") Lifts[i].Floor++;
            else if (result[i] == "down") Lifts[i].Floor--;
        }
        return result;
    }

    private void RandomizeQueues(Random random)
    {
        foreach (FloorQueue queue in FloorQueues)
        {
            for (int i = 0; i < queue.Queue.Count; i++)
            {
                if (queue.Queue[i] < queue.Floor) queue.Queue[i] = random.Next(queue.Floor);
                else queue.Queue[i] = random.Next(queue.Floor + 1, FloorCount);
            }
        }
    }
}