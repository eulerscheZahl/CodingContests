using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;


public static class Network
{
    public static int N; // groupsCount
    public static int S; // spinesCount (per group)
    public static int L; // leavesCount (per group)
    public static int M; //OxcCount
    public static int K; //OxcSpineLinkCount
    public static int P; // PlaneCount
    public static double ConvergenceRatio => (double)M / P * K / L;
    public static int Convergence => P * L / (M * K);

    public static List<Group> Groups = [];
    public static List<Plane> Planes = [];
    public static List<Leaf> Leaves = [];
    public static List<Spine> Spines = [];
    public static List<Oxc> Oxcs = [];

    public static void Init()
    {
        int R = N * S / P * K; // OxcPortsCount
        for (int i = 0; i < N; i++) Groups.Add(new Group { ID = i });
        for (int i = 0; i < P; i++) Planes.Add(new Plane { ID = i });
        for (int i = 0; i < M; i++)
        {
            Oxc oxc = new Oxc
            {
                ID = i,
                Plane = Planes[i * P / M],
                PortLinks = [.. Enumerable.Range(0, R).Select(i => -1)],
                ConnectionsCrossByPort = new Connection[R],
                PreviousPortLinks = [.. Enumerable.Range(0, R).Select(i => -1)],
            };
            Oxcs.Add(oxc);
            oxc.Plane.Oxcs.Add(oxc);
        }
        for (int i = 0; i < L; i++)
        {
            foreach (Group group in Groups)
            {
                Leaf leaf = new Leaf { ID = i, Group = group };
                Leaves.Add(leaf);
                group.Leaves.Add(leaf);
            }
        }
        foreach (Group group in Groups)
        {
            for (int i = 0; i < S; i++)
            {
                Spine spine = new Spine { ID = i, Group = group, Plane = Planes[i * P / S] };
                Spines.Add(spine);
                group.Spines.Add(spine);
                spine.Plane.Spines.Add(spine);
            }
        }

        foreach (Group group in Groups)
        {
            foreach (Leaf leaf in group.Leaves)
            {
                foreach (Spine spine in group.Spines)
                {
                    leaf.Spines.Add(spine);
                    spine.Leaves.Add(leaf);
                }
            }
        }
        foreach (Plane plane in Planes)
        {
            foreach (Oxc oxc in plane.Oxcs)
            {
                foreach (Spine spine in plane.Spines)
                {
                    oxc.Spines.Add(spine);
                    spine.Oxcs.Add(oxc);
                }
            }
        }

        foreach (Spine spine in Spines)
        {
            foreach (Leaf leaf in spine.Leaves)
            {
                Connection connection = new Connection(leaf, spine, null, 0);
                leaf.ConnectionsUp.Add(connection);
                spine.ConnectionsDown.Add(connection);
            }
            foreach (Oxc oxc in spine.Oxcs)
            {
                for (int k = 0; k < K; k++)
                {
                    Connection connection = new Connection(null, spine, oxc, k);
                    spine.ConnectionsUp.Add(connection);
                    oxc.ConnectionsDown.Add(connection);
                }
            }
        }
    }
}


public class Task
{
    public Leaf LeafA;
    public Leaf LeafB;
    public Spine SpineA;
    public Spine SpineB;
    public int GroupA;
    public int GroupB;
    public Oxc Oxc;
    public List<Connection> Connections;

    public int ContA;
    public int ContB;
    public (Spine spine, int pos) JokerA;
    public (Spine spine, int pos) JokerB;

    public Task(Leaf leafA, Leaf leafB)
    {
        this.LeafA = leafA;
        this.LeafB = leafB;
        GroupA = leafA.Group.ID;
        GroupB = leafB.Group.ID;
    }

    public void Reset()
    {
        if (Connections == null) return;
        foreach (Connection connection in Connections) connection.Tasks.Remove(this);
        Connections = null;
        ResetPartners();
    }

    public string PrintPath()
    {
        return $"{Connections[1].Spine.ID} {Connections[1].Offset} {Connections[1].Oxc.ID} {Connections[1].Spine2.ID} {Connections[1].Offset2}";
    }

    public override string ToString() => $"{GroupA}.{LeafA.ID} -> {GroupB}.{LeafB.ID}";

    public void Validate()
    {
        Debug.Assert(Connections.Count == 3);
        SpineA = Connections[0].Spine;
        SpineB = Connections[2].Spine;
        Debug.Assert(SpineA != null);
        Debug.Assert(SpineB != null);
        Connection c1 = Connections[1];
        Debug.Assert(c1.Spine == LeafA.Spines[c1.Spine.ID]);
        Debug.Assert(c1.Spine2 == LeafB.Spines[c1.Spine2.ID]);
        Oxc = Connections[1].Oxc;
        Debug.Assert(c1.Spine.Oxcs.Contains(Oxc));
        Debug.Assert(Oxc.PortLinks[c1.PortA] == c1.PortB);
        Debug.Assert(Oxc.PortLinks[c1.PortB] == c1.PortA);
        //Debug.Assert(oxc.ConnectionsCrossByPort[c1.PortA] == c1);
        //Debug.Assert(oxc.ConnectionsCrossByPort[c1.PortB] == c1);
    }

    public void ResetPartners()
    {
        JokerA = (null, -1);
        JokerB = (null, -1);
    }
}


public class Oxc : NetworkComponent
{
    public Plane Plane;
    public List<Spine> Spines = [];
    public Connection[] ConnectionsCrossByPort;
    // The port corresponding to OXC number m, Group number i, Spine number j, and link number k has the number i⋅(S/P)⋅K+(j mod(S/P))⋅K+k
    public int[] PortLinks;
    public int[] PreviousPortLinks;
    public int[] OpenGroupConnectors;
    public List<Task> Tasks = [];

    public static int GetPortPos(Spine spine)
    {
        int spinePos = spine.ID % (Network.S / Network.P);
        return (spinePos + spine.Group.ID * Network.S / Network.P) * Network.K;
    }

    public static int GetGroupPos(int group) => group * Network.S / Network.P * Network.K;

    public override string ToString() => $"ID={ID}  PlaneId={Plane.ID}";

    public int CountChanges()
    {
        int result = 0;
        for (int i = 0; i < PortLinks.Length; i++)
        {
            if (PortLinks[i] == PreviousPortLinks[i]) continue;
            if (PortLinks[i] >= 0 && PreviousPortLinks[i] >= 0) result += 2;
            else result++;
        }
        return result / 2;
    }

    public IEnumerable<Task> EndQuery()
    {
        for (int i = 0; i < PortLinks.Length; i++)
        {
            if (PortLinks[i] == -1 && PreviousPortLinks[i] != -1 && PortLinks[PreviousPortLinks[i]] == -1)
            {
                int partner = PreviousPortLinks[i];
                Connection cross = Connect(i, i % Network.K, partner, partner % Network.K);
                List<Connection> path = [cross.Spine.ConnectionsDown[0], cross, cross.Spine2.ConnectionsDown[0]];
                yield return new(path[0].Leaf, path[^1].Leaf) { Connections = path, Oxc = this, SpineA = path[1].Spine, SpineB = path[1].Spine2 };
            }
        }
        PreviousPortLinks = PortLinks.ToArray();
    }

    public Connection Connect(int indexA, int offsetA, int indexB, int offsetB)
    {
        if (ConnectionsCrossByPort[indexA] != null || ConnectionsCrossByPort[indexB] != null) return null;
        Connection cross = new Connection(null, Spines[indexA / Network.K], this, offsetA)
        {
            Offset2 = offsetB,
            Spine2 = Spines[indexB / Network.K],
            PortA = indexA,
            PortB = indexB
        };
        ConnectionsCrossByPort[indexA] = cross;
        ConnectionsCrossByPort[indexB] = cross;
        PortLinks[indexA] = indexB;
        PortLinks[indexB] = indexA;
        return cross;
    }

    public Connection Connect(Task task)
    {
        (int index, int offset) = GetFreePort(task.SpineA);
        (int index2, int offset2) = GetFreePort(task.SpineB);
        Connection conn = Connect(index, offset, index2, offset2);
        task.SpineA = conn.Spine;
        task.SpineB = conn.Spine2;
        task.Oxc = conn.Oxc;
        return conn;
    }

    private (int index, int offset) GetFreePort(Spine spine)
    {
        int index = GetPortPos(spine);
        for (int k = 0; k < Network.K; k++)
        {
            if (PortLinks[index + k] == -1 && (PreviousPortLinks[index + k] == -1 || PortLinks[PreviousPortLinks[index + k]] != -1)) return (index + k, k);
        }
        for (int k = 0; k < Network.K; k++)
        {
            if (PortLinks[index + k] == -1) return (index + k, k);
        }
        return (-1, -1);
    }

    public void ConnectAll(List<Task> tasks)
    {
        List<Task> backlog = [];
        foreach (Task task in tasks)
        {
            bool connected = false;
            int indexA = GetPortPos(task.SpineA);
            int indexB = GetPortPos(task.SpineB);
            for (int kA = 0; !connected && kA < Network.K; kA++)
            {
                for (int kB = 0; !connected && kB < Network.K; kB++)
                {
                    if (ConnectionsCrossByPort[indexA + kA] == null && ConnectionsCrossByPort[indexB + kB] == null &&
                    PreviousPortLinks[indexA + kA] == indexB + kB && PreviousPortLinks[indexB + kB] == indexA + kA)
                    {
                        connected = true;
                        Connection cross = Connect(indexA + kA, kA, indexB + kB, kB);
                        task.Connections = [
                            cross.Spine.ConnectionsDown[task.LeafA.ID],
                            cross,
                            cross.Spine2.ConnectionsDown[task.LeafB.ID]
                        ];
                        foreach (Connection c in task.Connections) c.Tasks.Add(task);
                    }
                }
            }
            if (!connected) backlog.Add(task);
        }

        foreach (Task task in backlog)
        {
            Connection cross = Connect(task);
            task.Connections = [
                cross.Spine.ConnectionsDown[task.LeafA.ID],
                        cross,
                        cross.Spine2.ConnectionsDown[task.LeafB.ID]
            ];
            foreach (Connection c in task.Connections) c.Tasks.Add(task);
        }
    }
}


public class NetworkComponent
{
    public int ID;
    public List<Connection> ConnectionsUp = [];
    public List<Connection> ConnectionsDown = [];
    private static int internalIdCounter;
    private int internalId;
    public NetworkComponent() { internalId = internalIdCounter++; }
    public override int GetHashCode() => internalId;
}


public class TaskPath
{
    public List<Connection> Connections = [];
    public HashSet<Task> Tasks = [];
    public NetworkComponent CurrentPos;

    public TaskPath() { }
    public TaskPath(TaskPath taskPath, Connection connection)
    {
        this.Connections = [.. taskPath.Connections, connection];
        this.Tasks = [.. taskPath.Tasks];
    }
}

public class Container
{
    public List<(Task task, int a, int b)> Fixed = [];
    public HashSet<Task> WantedFixed = [];
    private List<(Task task, int a, int b)>[] keysCovered;
    public int[] GroupsFree;
    private int groupOvershoot;
    public int[] FreeCapacity;
    public int TotalFree;
    public List<int> Overshoot = [];
    public int KeysCount => keysCovered.Length;
    
    public Container(int groupsCount, int freeCapacityPerItem, int totalMax, int groupCap)
    {
        keysCovered = Enumerable.Range(0, groupsCount).Select(i => new List<(Task, int, int)>()).ToArray();
        FreeCapacity = keysCovered.Select(g => freeCapacityPerItem).ToArray();
        TotalFree = totalMax;
        GroupsFree = Network.Groups.Select(g => groupCap).ToArray();
    }

    public void AddFixed((Task task, int a, int b) group)
    {
        if (FreeCapacity[group.a] <= 0 || FreeCapacity[group.b] <= 0)
        {
            AddTask(group);
            return;
        }
        TrackAdd(group);
        Fixed.Add(group);
        WantedFixed.Add(group.task);
    }

    public void RemoveOneFixed()
    {
        if (Fixed.Count == 0) return;
        var f = Fixed[^1];
        keysCovered[f.a].Add(f);
        keysCovered[f.b].Add(f);
        Fixed.RemoveAt(Fixed.Count - 1);
    }

    public void RemoveFixed()
    {
        foreach (var f in Fixed)
        {
            keysCovered[f.a].Add(f);
            keysCovered[f.b].Add(f);
        }
        Fixed.Clear();
    }

    public void AddTask((Task task, int a, int b) group)
    {
        keysCovered[group.a].Add(group);
        keysCovered[group.b].Add(group);
        TrackAdd(group);
    }

    private void TrackAdd((Task task, int a, int b) group)
    {
        group.task.ContA = group.a;
        group.task.ContB = group.b;
        TotalFree--;
        if (--FreeCapacity[group.a] == -1) Overshoot.Add(group.a);
        if (--FreeCapacity[group.b] == -1) Overshoot.Add(group.b);
        if (--GroupsFree[group.task.GroupA] == -1) groupOvershoot++;
        if (--GroupsFree[group.task.GroupB] == -1) groupOvershoot++;
    }

    public void RemoveTask((Task task, int a, int b) group)
    {
        keysCovered[group.a].Remove(group);
        keysCovered[group.b].Remove(group);
        TotalFree++;
        if (++FreeCapacity[group.a] == 0) Overshoot.Remove(group.a);
        if (++FreeCapacity[group.b] == 0) Overshoot.Remove(group.b);
        if (++GroupsFree[group.task.GroupA] == 0) groupOvershoot--;
        if (++GroupsFree[group.task.GroupB] == 0) groupOvershoot--;
    }

    public void MoveJokerA(Task task, int a, int b)
    {
        RemoveTask((task, a, b));
        var newVal = task.JokerA;
        task.JokerA = (task.SpineA, a);
        task.SpineA = newVal.spine;
        task.ContA = newVal.pos;
        AddTask((task, task.ContA, task.ContB));
    }

    public void MoveJokerB(Task task, int a, int b)
    {
        RemoveTask((task, a, b));
        var newVal = task.JokerB;
        task.JokerB = (task.SpineB, b);
        task.SpineB = newVal.spine;
        task.ContB = newVal.pos;
        AddTask((task, task.ContA, task.ContB));
    }

    public bool IsSolved() => Overshoot.Count == 0 && TotalFree >= 0 && groupOvershoot == 0;

    public (Task task, int a, int b) GetRemovableTask()
    {
        if (Overshoot.Count > 0)
        {
            int overshoot = Overshoot[Solution.random.Next(Overshoot.Count)];
            return keysCovered[overshoot][Solution.random.Next(keysCovered[overshoot].Count)];
        }
        int groupNeg = groupOvershoot > 0 ? 0 : GroupsFree.Length;
        while (groupNeg < GroupsFree.Length && GroupsFree[groupNeg] >= 0) groupNeg++;
        while (true)
        {
            int index = Solution.random.Next(keysCovered.Length);
            if (keysCovered[index].Count == 0) continue;
            var result = keysCovered[index][Solution.random.Next(keysCovered[index].Count)];
            if (groupNeg == GroupsFree.Length || result.task.GroupA == groupNeg || result.task.GroupB == groupNeg) return result;
        }
    }

    public (Task task, int a, int b) GetRandomTask()
    {
        if (keysCovered.All(k => k.Count == 0)) return (null, -1, -1);
        while (true)
        {
            int index = Solution.random.Next(keysCovered.Length);
            if (keysCovered[index].Count == 0) continue;
            return keysCovered[index][Solution.random.Next(keysCovered[index].Count)];
        }
    }

    public IEnumerable<Task> GetTasks() => EnumerateValues().Distinct();

    private IEnumerable<Task> EnumerateValues()
    {
        foreach (var f in Fixed) yield return f.task;
        foreach (var group in keysCovered)
        {
            foreach (var f in group) yield return f.task;
        }
    }

    public int CountFixedPreserved() => GetTasks().Count(v => WantedFixed.Contains(v));

    public override string ToString() => CountFixedPreserved() + "/" + WantedFixed.Count;

    public bool LockFixed()
    {
        HashSet<(Task task, int a, int b)> toLock = [];
        foreach (var group in keysCovered)
        {
            foreach (var f in group)
            {
                if (WantedFixed.Contains(f.task)) toLock.Add(f);
            }
        }
        foreach (var l in toLock)
        {
            RemoveTask(l);
            TrackAdd(l);
        }
        Fixed.AddRange(toLock);

        return toLock.Count > 0;
    }

    public IEnumerable<(Task task, int a, int b)> ClearAll()
    {
        RemoveFixed();
        HashSet<(Task task, int a, int b)> distinct = [];
        foreach (var group in keysCovered)
        {
            foreach (var f in group) distinct.Add(f);
        }
        foreach (var f in distinct)
        {
            RemoveTask(f);
            yield return f;
        }
    }

    public void SetSolution(List<Task> tasks)
    {
        keysCovered = [[]];
        Fixed.Clear();
        foreach (Task task in tasks)
        {
            if (WantedFixed.Contains(task)) Fixed.Add((task, 0, 0));
            else keysCovered[0].Add((task, 0, 0));
        }
    }

    private List<(Task task, int a, int b)> backupFixed = [];
    private List<(Task task, int a, int b)>[] backupKeysCovered;
    private int[] backupGroupsFree;
    private int backupGroupOvershoot;
    private int[] backupFreeCapacity;
    private int backupTotalFree;
    private List<int> backupOvershoot = [];
    private (Task t, Spine s, int a, Spine js, int ja)[] taskBackupJokerA;
    private (Task t, Spine s, int b, Spine js, int jb)[] taskBackupJokerB;
    public void Backup()
    {
        backupFixed = [.. Fixed];
        backupKeysCovered = [.. keysCovered.Select(c => c.ToList())];
        backupGroupsFree = [.. GroupsFree];
        backupGroupOvershoot = groupOvershoot;
        backupFreeCapacity = [.. FreeCapacity];
        backupTotalFree = TotalFree;
        backupOvershoot = [.. Overshoot];
        taskBackupJokerA = GetTasks().Select(t => (t, t.SpineA, t.ContA, t.JokerA.spine, t.JokerA.pos)).ToArray();
        taskBackupJokerB = GetTasks().Select(t => (t, t.SpineB, t.ContB, t.JokerB.spine, t.JokerB.pos)).ToArray();
    }

    public void Restore()
    {
        Fixed = [.. backupFixed];
        keysCovered = [.. backupKeysCovered.Select(c => c.ToList())];
        GroupsFree = [.. backupGroupsFree];
        groupOvershoot = backupGroupOvershoot;
        FreeCapacity = [.. backupFreeCapacity];
        TotalFree = backupTotalFree;
        Overshoot = [.. backupOvershoot];
        foreach (var j in taskBackupJokerA)
        {
            j.t.SpineA = j.s;
            j.t.JokerA = (j.js, j.ja);
        }
        foreach (var j in taskBackupJokerB)
        {
            j.t.SpineB = j.s;
            j.t.JokerB = (j.js, j.jb);
        }
    }
}


public class Connection
{
    public Leaf Leaf;
    public Spine Spine;
    public Spine Spine2;
    public Oxc Oxc;
    public int Offset;
    public int Offset2;
    public int PortA;
    public int PortB;
    public List<Task> Tasks = [];

    public Connection(Leaf leaf, Spine spine, Oxc oxc, int offset)
    {
        this.Leaf = leaf;
        this.Spine = spine;
        this.Oxc = oxc;
        this.Offset = offset;
    }

    public override string ToString()
    {
        NetworkComponent[] comps = [Leaf, Spine, Oxc, Spine2];
        return string.Join(" .. ", comps.Where(c => c != null));
    }

    public void Delete(List<Connection>[,] crosses)
    {
        Debug.Assert(Tasks.Count == 0);
        crosses[Spine.Group.ID, Spine2.Group.ID].Remove(this);
        Oxc.ConnectionsCrossByPort[PortA] = null;
        Oxc.ConnectionsCrossByPort[PortB] = null;
        Oxc.PortLinks[PortA] = -1;
        Oxc.PortLinks[PortB] = -1;
    }
}


public class Spine : NetworkComponent
{
    public Plane Plane;
    public Group Group;
    public List<Oxc> Oxcs = [];
    public List<Leaf> Leaves = [];

    public override string ToString() => $"ID={ID}  PlaneId={Plane.ID}  GroupId={Group.ID}";
}


public class Leaf : NetworkComponent
{
    public int LeafId => Group.ID + Network.N * ID;
    public Group Group;
    public List<Spine> Spines = [];

    public override string ToString() => $"ID={ID}  GroupId={Group.ID}";
}


public class Group
{
    public int ID;
    public List<Leaf> Leaves = [];
    public List<Spine> Spines = [];

    public override string ToString() => $"ID={ID}";
}


public class Plane
{
    public int ID;
    public List<Oxc> Oxcs = [];
    public List<Spine> Spines = [];

    public override string ToString() => $"ID={ID}";
}


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

