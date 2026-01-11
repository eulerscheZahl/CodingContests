using System.Collections.Generic;
using System.Linq;

public class FloorQueue
{
    public int SpawnCount;
    public List<int> Queue = new List<int>();
    public int Floor;
    public bool Full => Queue.Count == 4;

    public override string ToString() => $"Queue {Floor + 1}: {string.Concat(Queue.Select(b => b > Floor ? "U" : "D"))}";

    public void Parse(string line)
    {
        string[] parts = line.Trim().Split();
        SpawnCount = int.Parse(parts[0]);
        Floor = int.Parse(parts[1]);
        for (int i = 2; i < parts.Length; i++) Queue.Add(int.Parse(parts[i]));
    }
}