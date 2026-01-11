using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class Solution
{
    public static Random random = new Random();
    public static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            Console.Error.WriteLine("seed: " + args[0]);
            random = new Random(int.Parse(args[0]));
        }
        Stopwatch sw = Stopwatch.StartNew();
        int[] tmp = Console.ReadLine().Split().Select(int.Parse).ToArray();
        Network.N = tmp[0]; // groupsCount
        Network.S = tmp[1]; // spinesCount (per group)
        Network.L = tmp[2]; // leavesCount (per group)
        tmp = Console.ReadLine().Split().Select(int.Parse).ToArray();
        Network.M = tmp[0]; //OxcCount
        Network.K = tmp[1]; //OxcSpineLinkCount
        Network.P = tmp[2]; // PlaneCount
        Network.Init();

        List<double> scores = [];
        double upperBar = 0;
        for (int run = 0; run < 5; run++)
        {
            int Q = int.Parse(Console.ReadLine()); // queryCount
            List<Task> tasks = [];
            for (int q = 0; q < Q; q++)
            {
                tmp = Console.ReadLine().Split().Select(int.Parse).ToArray();
                Leaf leafA = Network.Groups[tmp[0]].Leaves[tmp[1]];
                Leaf leafB = Network.Groups[tmp[2]].Leaves[tmp[3]];
                tasks.Add(new Task(leafA, leafB));
            }
            DebugLine("query: " + run + "  tasks: " + tasks.Count);
            List<Container> containers = null;
            int targetParallel = Network.Convergence;
            for (; containers == null; targetParallel++)
            {
                List<Task> reduced = ReduceTasks(tasks, targetParallel);
                if (reduced == null || reduced.Count > Network.M * Network.Oxcs[0].PortLinks.Length / 2) continue;
                containers = BuildPlaneGroups(reduced);
                DebugLine("          try parallel: " + targetParallel + "   tasks: " + reduced.Count + "/" + (Network.M * Network.Oxcs[0].PortLinks.Length / 2) + (containers == null ? "  no groups" : "  continue with OXCs"));
                if (!SolveOxcs(containers, Network.Convergence > 1))
                {
                    containers = null;
                    foreach (Task task in reduced) task.Reset();
                }
                else break;
            }
            RestoreTasks(tasks, targetParallel);
            int parallel = tasks.SelectMany(t => t.Connections).Max(c => c.Tasks.Count);
            if (parallel > targetParallel || run > 0 && sw.ElapsedMilliseconds * 5 / run < 4000)
            {
                double oldScore = 1000.0 / parallel / Network.ConvergenceRatio + 300.0 * (1 - (double)Network.Oxcs.Sum(o => o.CountChanges()) / (Network.M * Network.Oxcs[0].PortLinks.Length));
                List<Connection>[] backupConn = tasks.Select(t => t.Connections).ToArray();
                int[][] backupOxcs = Network.Oxcs.Select(o => o.PortLinks.ToArray()).ToArray();
                Connection[][] backupOxcConns = Network.Oxcs.Select(o => o.ConnectionsCrossByPort.ToArray()).ToArray();
                while (true)
                {
                    foreach (Task task in tasks) task.Reset();
                    List<Task> reduced = ReduceTasks(tasks, targetParallel);
                    if (reduced == null || reduced.Count > Network.M * Network.Oxcs[0].PortLinks.Length / 2) continue;
                    containers = BuildPlaneGroups(reduced);
                    if (!SolveOxcs(containers, targetParallel == parallel && Network.Convergence > 1)) continue;
                    RestoreTasks(tasks, targetParallel);
                    break;
                }
                double newScore = 1000.0 / tasks.SelectMany(t => t.Connections).Max(c => c.Tasks.Count) / Network.ConvergenceRatio + 300.0 * (1 - (double)Network.Oxcs.Sum(o => o.CountChanges()) / (Network.M * Network.Oxcs[0].PortLinks.Length));
                if (newScore < oldScore)
                {
                    foreach (Task task in tasks) task.Reset();
                    for (int i = 0; i < Network.M; i++)
                    {
                        Network.Oxcs[i].PortLinks = backupOxcs[i];
                        Network.Oxcs[i].ConnectionsCrossByPort = backupOxcConns[i];
                    }
                    for (int i = 0; i < backupConn.Length; i++)
                    {
                        Task t = tasks[i];
                        t.Connections = backupConn[i];
                        t.SpineA = t.Connections[1].Spine;
                        t.SpineB = t.Connections[1].Spine2;
                        t.Oxc = t.Connections[1].Oxc;
                        foreach (Connection c in t.Connections) c.Tasks.Add(t);
                    }
                }
            }

            var g2 = GetTaskPreferences(lastTasks);
            lastTasks = [];
            HashSet<Connection> covered = [];
            foreach (Task t in tasks)
            {
                t.Validate();
                if (covered.Add(t.Connections[1])) lastTasks.Add(t);
            }

            int rearrangeCount = Network.Oxcs.Sum(o => o.CountChanges());
            foreach (Oxc oxc in Network.Oxcs)
            {
                foreach (Task dummy in oxc.EndQuery())
                {
                    if (covered.Add(dummy.Connections[1])) lastTasks.Add(dummy);
                }
            }
            upperBar += 1000.0 / targetParallel;
            var g1 = GetTaskPreferences(tasks);
            int rearr = 0;
            for (int i = 0; i < Network.N; i++)
            {
                for (int j = 0; j < Network.N; j++)
                {
                    rearr += Math.Max(g1[i, j].Count - g2[i, j].Count, 0);
                    rearr += Math.Max(g2[i, j].Count - g1[i, j].Count, 0);
                }
            }
            double arrUpper = 300 * (1 - (double)rearr / (Network.M * Network.Oxcs[0].PortLinks.Length));
            upperBar += arrUpper;

            parallel = tasks.SelectMany(t => t.Connections).Max(c => c.Tasks.Count);
            DebugLine("          parallel traffic: " + parallel);
            DebugLine("          rearrangeCount: " + rearrangeCount);
            scores.Add(1000.0 / parallel / Network.ConvergenceRatio);
            scores.Add(300.0 * (1 - (double)rearrangeCount / (Network.M * Network.Oxcs[0].PortLinks.Length)));
            DebugLine("          flow conflict score: " + scores[^2] + "   rearrange score: " + scores[^1] + " / " + arrUpper);
#if !DEBUG
            foreach (Oxc oxc in Network.Oxcs) Console.WriteLine(string.Join(" ", oxc.PortLinks));
            foreach (Task task in tasks) Console.WriteLine(task.PrintPath());
#endif
            foreach (Task t in tasks) t.Reset();
        }

        Console.Error.WriteLine("final score: " + scores.Sum() + "   upper bar: " + upperBar);
        Console.Error.WriteLine("runtime: " + sw.ElapsedMilliseconds + " ms    container fails: " + containerFails);
    }

    public static void DebugLine(object obj)
    {
#if DEBUG
        Console.Error.WriteLine(obj);
#endif
    }

    private static List<Task> lastTasks = [];
    private static bool SolveOxcs(List<Container> planes, bool allowDoubleSpine)
    {
        if (planes == null) return false;
        int groupCap = Network.Oxcs[0].ConnectionsDown.Count(c => c.Spine.Group.ID == 0);
        for (int planeId = 0; planeId < planes.Count; planeId++)
        {
            int oxcBaseId = Network.Planes[planeId].Oxcs[0].ID;
            int spineBaseId = Network.Planes[planeId].Spines[0].ID;
            List<Task> planeTasks = planes[planeId].GetTasks().ToList();
            DebugLine("plane " + planeId + "   fixed: " + planes[planeId].CountFixedPreserved());
            List<Container> containers = null;
            List<(Task task, Spine spineA, Spine spineB)> bestAssigns = [];
            int bestPreserved = -1;
            List<Task> prevTasks = lastTasks.Where(t => t.Oxc.Plane.ID == planeId).ToList();

            for (int attempt = 0; attempt < 10; attempt++)
            {
                if ((prevTasks.Count == 0 || attempt >= 3 || Network.Convergence == 1) && containers != null) break;
                List<Container> currentContainers = Network.Planes[planeId].Oxcs.Select(o => new Container(Network.Planes[planeId].Spines.Count, Network.K, Network.Oxcs[0].PortLinks.Length / 2, groupCap)).ToList();
                List<Spine>[] freeSpineList = Network.Leaves.Select(l => l.Spines.Where(s => s.Plane.ID == planeId).OrderBy(s => random.NextDouble()).ToList()).ToArray();
                List<Task>[,] lookup = GetTaskPreferences(prevTasks);
                List<Task> reuse = [];
                List<Task> backlog = [];
                int[] spineFree = Network.Planes[planeId].Spines.Select(g => Network.K * currentContainers.Count).ToArray();
                List<(Task task, Container container, bool fix)> assigns = [];
                int[][] containerUsed = currentContainers.Select(c => new int[Network.N]).ToArray();
                foreach (Task t in planeTasks.OrderBy(t => random.NextDouble()))
                {
                    List<Task> q = lookup[t.GroupA, t.GroupB];
                    Task idx = q.Where(x => freeSpineList[t.LeafA.LeafId].Contains(x.SpineA) && freeSpineList[t.LeafB.LeafId].Contains(x.SpineB))
                            .OrderBy(x => Math.Max(containerUsed[x.Oxc.ID - oxcBaseId][t.GroupA], containerUsed[x.Oxc.ID - oxcBaseId][t.GroupB]))
                            .ThenBy(x => Math.Min(containerUsed[x.Oxc.ID - oxcBaseId][t.GroupA], containerUsed[x.Oxc.ID - oxcBaseId][t.GroupB]))
                            .ThenBy(x => containerUsed[x.Oxc.ID - oxcBaseId].Sum())
                            .FirstOrDefault();
                    if (idx == null && allowDoubleSpine) idx = q.FirstOrDefault();
                    q.Remove(idx);
                    if (idx != null)
                    {
                        containerUsed[idx.Oxc.ID - oxcBaseId][t.GroupA]++;
                        containerUsed[idx.Oxc.ID - oxcBaseId][t.GroupB]++;
                        t.SpineA = idx.SpineA;
                        t.SpineB = idx.SpineB;
                        freeSpineList[t.LeafA.LeafId].Remove(t.SpineA);
                        freeSpineList[t.LeafB.LeafId].Remove(t.SpineB);
                        spineFree[t.GroupA + Network.N * (t.SpineA.ID - spineBaseId)]--;
                        spineFree[t.GroupB + Network.N * (t.SpineB.ID - spineBaseId)]--;
                        reuse.Add(t);
                        assigns.Add((t, currentContainers[idx.Oxc.ID - oxcBaseId], true));
                    }
                    else backlog.Add(t);
                }
                foreach (Task t in backlog)
                {
                    t.SpineA = freeSpineList[t.LeafA.LeafId][0];
                    freeSpineList[t.LeafA.LeafId].Remove(t.SpineA);
                    spineFree[t.GroupA + Network.N * (t.SpineA.ID - spineBaseId)]--;

                    t.SpineB = freeSpineList[t.LeafB.LeafId][0];
                    freeSpineList[t.LeafB.LeafId].Remove(t.SpineB);
                    spineFree[t.GroupB + Network.N * (t.SpineB.ID - spineBaseId)]--;

                    assigns.Add((t, currentContainers[random.Next(currentContainers.Count)], false));
                }
                foreach (Task t in backlog.Concat(reuse))
                {
                    while (spineFree[t.GroupA + Network.N * (t.SpineA.ID - spineBaseId)] < 0 && freeSpineList[t.LeafA.LeafId].Count > 0)
                    {
                        spineFree[t.GroupA + Network.N * (t.SpineA.ID - spineBaseId)]++;
                        t.SpineA = freeSpineList[t.LeafA.LeafId][0];
                        freeSpineList[t.LeafA.LeafId].Remove(t.SpineA);
                        spineFree[t.GroupA + Network.N * (t.SpineA.ID - spineBaseId)]--;
                    }
                    while (spineFree[t.GroupB + Network.N * (t.SpineB.ID - spineBaseId)] < 0 && freeSpineList[t.LeafB.LeafId].Count > 0)
                    {
                        spineFree[t.GroupB + Network.N * (t.SpineB.ID - spineBaseId)]++;
                        t.SpineB = freeSpineList[t.LeafB.LeafId][0];
                        freeSpineList[t.LeafB.LeafId].Remove(t.SpineB);
                        spineFree[t.GroupB + Network.N * (t.SpineB.ID - spineBaseId)]--;
                    }
                }
                foreach (var ass in assigns.OrderByDescending(a => a.fix))
                {
                    Task t = ass.task;
                    if (ass.fix)
                        ass.container.AddFixed((t, t.GroupA + Network.N * (t.SpineA.ID - spineBaseId), t.GroupB + Network.N * (t.SpineB.ID - spineBaseId)));
                    else
                        ass.container.AddTask((t, t.GroupA + Network.N * (t.SpineA.ID - spineBaseId), t.GroupB + Network.N * (t.SpineB.ID - spineBaseId)));
                }

                foreach (Task t in planeTasks) t.ResetPartners();
                HashSet<Task> unfixed = [.. assigns.Where(a => !a.fix).Select(a => a.task)];
                foreach (var group in planeTasks.GroupBy(t => t.LeafA))
                {
                    List<Task> leafTasks = group.ToList();
                    List<Task> leafUnfixed = leafTasks.Where(t => unfixed.Contains(t)).ToList();
                    if (leafTasks.Count == 1 && leafUnfixed.Count == 1 && freeSpineList[leafUnfixed[0].LeafA.LeafId].Count > 0)
                    {
                        Spine spine = freeSpineList[leafUnfixed[0].LeafA.LeafId][0];
                        leafUnfixed[0].JokerA = (spine, spine.Group.ID + Network.N * (spine.ID - spineBaseId));
                    }
                }
                foreach (var group in planeTasks.GroupBy(t => t.LeafB))
                {
                    List<Task> leafTasks = group.ToList();
                    List<Task> leafUnfixed = leafTasks.Where(t => unfixed.Contains(t)).ToList();
                    if (leafTasks.Count == 1 && leafUnfixed.Count == 1 && freeSpineList[leafUnfixed[0].LeafB.LeafId].Count > 0)
                    {
                        Spine spine = freeSpineList[leafUnfixed[0].LeafB.LeafId][0];
                        leafUnfixed[0].JokerB = (spine, spine.Group.ID + Network.N * (spine.ID - spineBaseId));
                    }
                }

                currentContainers = SolveContainers(currentContainers, (int)2e4, attempt < 5);

                if (currentContainers == null) continue;
                int preserved = currentContainers.Sum(c => c.CountFixedPreserved());
                if (preserved <= bestPreserved) continue;
                bestPreserved = preserved;
                containers = currentContainers;
                bestAssigns = planeTasks.Select(p => (p, p.SpineA, p.SpineB)).ToList();
            }
            if (containers == null) return false;
            foreach (var b in bestAssigns)
            {
                b.task.SpineA = b.spineA;
                b.task.SpineB = b.spineB;
            }
            for (int j = 0; j < containers.Count; j++)
            {
                Oxc oxc = Network.Planes[planeId].Oxcs[j];
                Array.Fill(oxc.ConnectionsCrossByPort, null);
                Array.Fill(oxc.PortLinks, -1);
                List<Task> tasks = containers[j].GetTasks().ToList();
                oxc.ConnectAll(tasks);
            }
        }
        return true;
    }

    private static List<Container> BuildPlaneGroups(List<Task> tasks)
    {
        int targetUsage = Network.Planes[0].Spines.Count(s => s.Group.ID == 0);
        int maxTotal = Network.Planes[0].Oxcs.Count * Network.Oxcs[0].PortLinks.Length / 2;
        int groupCap = Network.Planes[0].Oxcs.Count * Network.Oxcs[0].ConnectionsDown.Count(c => c.Spine.Group.ID == 0);
        List<Container> containers = Enumerable.Range(0, Network.P).Select(i => new Container(Network.Leaves.Count, targetUsage, maxTotal, groupCap)).ToList();
        List<Task>[,] lookup = GetTaskPreferences(lastTasks);
        List<Task> backlog = [];
        foreach (Task task in tasks.OrderBy(t => random.NextDouble()))
        {
            task.ResetPartners();
            List<Task> q = lookup[task.GroupA, task.GroupB];
            Task idx = q.FirstOrDefault(t => containers[t.Oxc.Plane.ID].FreeCapacity[task.LeafA.LeafId] > 0 && containers[t.Oxc.Plane.ID].FreeCapacity[task.LeafB.LeafId] > 0);
            q.Remove(idx);
            if (idx != null) containers[idx.Oxc.Plane.ID].AddFixed((task, task.LeafA.LeafId, task.LeafB.LeafId));
            else backlog.Add(task);
        }
        foreach (Task task in backlog)
            containers[random.Next(containers.Count)].AddTask((task, task.LeafA.LeafId, task.LeafB.LeafId));
        return SolveContainers(containers, (int)4e5, false);
    }

    private static List<Task>[,] GetTaskPreferences(List<Task> tasks)
    {
        List<Task>[,] lookup = new List<Task>[Network.N, Network.N];
        for (int i = 0; i < Network.N; i++)
        {
            for (int j = 0; j < Network.N; j++) lookup[i, j] = [];
        }
        foreach (Task task in tasks)
        {
            lookup[task.GroupA, task.GroupB].Add(task);
        }
        return lookup;
    }

    private static int containerFails = 0;
    private static List<Container> SolveContainers(List<Container> containers, int maxIterations, bool tryPreserve, bool entry = true)
    {
        int mutCount = 0;
        Container[][] ordered = Enumerable.Range(0, 2 * containers.Count).Select(i => containers.OrderBy(c => random.NextDouble()).ToArray()).ToArray();
        List<Container>[] freeCap = Enumerable.Range(0, containers[0].KeysCount).Select(i => new List<Container>()).ToArray();
        foreach (Container c in containers)
        {
            for (int i = 0; i < freeCap.Length; i++)
            {
                if (c.FreeCapacity[i] > 0) freeCap[i].Add(c);
            }
        }
        for (; mutCount < maxIterations; mutCount++)
        {
            if (4 * mutCount > maxIterations && mutCount % 16 == 0) containers[random.Next(containers.Count)].RemoveOneFixed();
            if (mutCount == maxIterations * 3 / 5)
            {
                foreach (Container c in containers) c.RemoveFixed();
            }
            Container[] order = ordered[random.Next(ordered.Length)];
            Container cont = null;
            foreach (Container c in order)
            {
                if (!c.IsSolved())
                {
                    cont = c;
                    break;
                }
            }
            if (cont == null) break;
            if (cont == null) break;
            (Task task, int a, int b) = cont.GetRemovableTask();
            if (tryPreserve && random.Next(2) != 0 && cont.WantedFixed.Contains(task)) continue;
            if (cont.FreeCapacity[a] < 0 && task.JokerA.spine != null && cont.FreeCapacity[task.JokerA.pos] > 0 && random.Next(10) == 0) { cont.MoveJokerA(task, a, b); continue; }
            if (cont.FreeCapacity[b] < 0 && task.JokerB.spine != null && cont.FreeCapacity[task.JokerB.pos] > 0 && random.Next(10) == 0) { cont.MoveJokerB(task, a, b); continue; }
            bool goodMatch = freeCap[a].Any(f => f.FreeCapacity[b] > 0 && f.GroupsFree[task.GroupA] > 0 && f.GroupsFree[task.GroupB] > 0);
            Container target = null;
            if (goodMatch) target = order.FirstOrDefault(c => c.FreeCapacity[a] > 0 && c.FreeCapacity[b] > 0 && c.GroupsFree[task.GroupA] > 0 && c.GroupsFree[task.GroupB] > 0);
            else
            {
                foreach (Container c in order)
                {
                    if (c.FreeCapacity[a] > 0 || c.FreeCapacity[b] > 0)
                    {
                        target = c;
                        break;
                    }
                }
            }
            if (target == null || random.Next(500) == 0) target = containers[random.Next(containers.Count)];
            if (cont == target) continue;
            cont.RemoveTask((task, a, b));
            target.AddTask((task, a, b));

            if (cont.FreeCapacity[a] == 1) freeCap[a].Add(cont);
            if (cont.FreeCapacity[b] == 1) freeCap[b].Add(cont);
            if (target.FreeCapacity[a] == 0) freeCap[a].Remove(target);
            if (target.FreeCapacity[b] == 0) freeCap[b].Remove(target);
        }
        if (containers.Any(c => !c.IsSolved()))
        {
            if (entry) containerFails++;
            return null;
        }

        if (!entry || !containers.Any(c => c.WantedFixed.Count > 0) || containers.Count < 4 || containers[0].GetTasks().Count() > 100) return containers;
        for (int i = 0; i < 4; i++)
        {
            List<Container> subset = containers.OrderByDescending(c => (c.WantedFixed.Count - c.CountFixedPreserved() + c.TotalFree) * (1 + random.NextDouble()))
                .Take(Math.Min(15, containers.Count * 3 / 4)).ToList();
            int oldSolved = subset.Sum(c => c.CountFixedPreserved());
            foreach (Container c in subset) c.Backup();
            List<(Task task, int a, int b)> missingTasks = [];
            foreach (Container m in subset) missingTasks.AddRange(m.ClearAll());
            List<(Task task, int a, int b)> backlog = [];
            foreach (var t in missingTasks)
            {
                Container target = subset.FirstOrDefault(c => c.WantedFixed.Contains(t.task));
                if (target == null) backlog.Add(t);
                else target.AddFixed(t);
            }
            foreach (var t in backlog) subset[random.Next(subset.Count)].AddTask(t);
            List<Container> subSolve = SolveContainers(subset, maxIterations * 3 / 2, tryPreserve, false);
            int newSolved = subSolve == null ? -1 : subSolve.Sum(c => c.CountFixedPreserved());
            if (newSolved < oldSolved)
            {
                foreach (Container c in subset) c.Restore();
            }
        }
        foreach (Container c in containers) c.LockFixed();
        if (entry) DebugLine(containers.Sum(c => c.CountFixedPreserved()) + " / " + containers.Sum(c => c.WantedFixed.Count));
        return containers;
    }

    private static List<Task> ReduceTasks(List<Task> tasks, int parallel)
    {
        List<Task>[,] grouped = GetTaskPreferences(tasks);
        List<(int i, int j)> toFill = [];
        for (int i = 0; i < Network.N; i++)
        {
            for (int j = 0; j < Network.N; j++)
            {
                int target = (grouped[i, j].Count + parallel - 1) / parallel;
                for (int k = 0; k < target; k++) toFill.Add((i, j));
            }
        }

        List<Task> result = [];
        int maxCap = Network.M * Network.Oxcs[0].ConnectionsDown.Count(c => c.Spine.Group.ID == 0);
        int[] groupFree = Network.Groups.Select(g => maxCap).ToArray();
        int[] leaves = new int[Network.Leaves.Count];
        foreach ((int i, int j) in toFill.OrderBy(f => random.NextDouble()))
        {
            List<Task> candidates = grouped[i, j];
            Task t = candidates.MinBy(c => 2 * Math.Max(leaves[c.LeafA.LeafId], leaves[c.LeafB.LeafId]) + Math.Min(leaves[c.LeafA.LeafId], leaves[c.LeafB.LeafId]));
            leaves[t.LeafA.LeafId]++;
            leaves[t.LeafB.LeafId]++;
            candidates.Remove(t);
            result.Add(t);
            groupFree[t.GroupA]--;
            groupFree[t.GroupB]--;
        }

        if (groupFree.Min() < 0) return null;
        return result;
    }

    private static void RestoreTasks(List<Task> tasks, int parallel)
    {
        List<Task>[,] lookup = GetTaskPreferences(tasks.Where(t => t.Connections != null).ToList());
        PriorityQueue<(Connection conn, int par), int> overshoot = new();
        foreach (Task t in tasks.Where(t => t.Connections == null))
        {
            List<Task> copycats = lookup[t.GroupA, t.GroupB];
            List<List<Connection>> candidates = [];
            foreach (Task c in copycats)
            {
                candidates.Add([c.Connections[1].Spine.ConnectionsDown[t.LeafA.ID],
                    c.Connections[1],
                    c.Connections[1].Spine2.ConnectionsDown[t.LeafB.ID]]);
            }
            t.Connections = candidates.MinBy(c => c.Max(co => co.Tasks.Count) + random.NextDouble());
            foreach (Connection c in t.Connections)
            {
                c.Tasks.Add(t);
                if (c.Tasks.Count > parallel) overshoot.Enqueue((c, c.Tasks.Count), -c.Tasks.Count);
            }
        }

        List<Connection>[,] crosses = new List<Connection>[Network.N, Network.N];
        for (int i = 0; i < Network.N; i++)
        {
            for (int j = 0; j < Network.N; j++) crosses[i, j] = [];
        }
        foreach (Connection c in tasks.Select(t => t.Connections[1]).Distinct()) crosses[c.Spine.Group.ID, c.Spine2.Group.ID].Add(c);

        for (int mut = 0; mut < 10000 && overshoot.Count > 0; mut++)
        {
            (Connection conn, int par) = overshoot.Dequeue();
            if (conn.Tasks.Count != par) continue;
            Task task = conn.Tasks[random.Next(conn.Tasks.Count)];
            task.Reset();
            task.Connections = BuildPath(task, crosses);
            if (task.Connections.Any(c => c.Tasks.Count == parallel))
            {
                int offsetA = random.Next(Network.K);
                int offsetB = random.Next(Network.K);
                int indexA = Oxc.GetGroupPos(task.Connections[1].Spine.Group.ID) + Network.K * random.Next(Network.S / Network.P) + offsetA;
                int indexB = Oxc.GetGroupPos(task.Connections[1].Spine2.Group.ID) + Network.K * random.Next(Network.S / Network.P) + offsetB;
                Oxc oxc = Network.Oxcs.Where(o => o.PortLinks[indexA] == -1 && o.PortLinks[indexB] == -1).OrderBy(o => random.NextDouble()).FirstOrDefault();
                Connection cross = oxc?.Connect(indexA, offsetA, indexB, offsetB);
                if (cross != null)
                {
                    crosses[cross.Spine.Group.ID, cross.Spine2.Group.ID].Add(cross);
                    List<Connection> crossConn = BuildPath(task, crosses);
                    if (crossConn.All(c => c.Tasks.Count < parallel)) task.Connections = crossConn;
                    else cross.Delete(crosses);
                }
            }
            foreach (Connection c in task.Connections)
            {
                c.Tasks.Add(task);
                if (c.Tasks.Count > parallel) overshoot.Enqueue((c, c.Tasks.Count), -c.Tasks.Count);
            }
            task.SpineA = task.Connections[1].Spine;
            task.SpineB = task.Connections[1].Spine2;
            task.Oxc = task.Connections[1].Oxc;
        }
    }

    private static List<Connection> BuildPath(Task task, List<Connection>[,] crosses)
    {
        List<Connection> result = null;
        double parallel = int.MaxValue;
        foreach (Connection conn in crosses[task.GroupA, task.GroupB])
        {
            List<Connection> path = [conn.Spine.ConnectionsDown[task.LeafA.ID], conn, conn.Spine2.ConnectionsDown[task.LeafB.ID]];
            double currentParallel = path.Max(p => p.Tasks.Count) + random.NextDouble();
            if (currentParallel < parallel)
            {
                parallel = currentParallel;
                result = path;
            }
        }
        return result;
    }
}
