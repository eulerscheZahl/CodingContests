using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
