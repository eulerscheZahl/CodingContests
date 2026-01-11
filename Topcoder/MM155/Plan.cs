using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Plan
{
    public List<Cell> Path;
    public int[] Visited;
    public int Score;
    public Plan(List<Cell> path, int[] visited)
    {
        this.Path = path;
        this.Visited = visited;
        for (int i = 0; i < path.Count; i++) Score += (i + 1) * path[i].Mult;
    }

    public void Print()
    {
        Console.WriteLine(Path.Count);
        foreach (Cell c in Path) Console.WriteLine(c.Y + " " + c.X);
    }

    public BeamNode GetBeamNode()
    {
        BeamNode result = new BeamNode(Path.Last());
        for (int i = Path.Count - 2; i >= 0; i--) result = new BeamNode(Path[i], result, true);
        return result;
    }

    public int PartialScore(int startIndex)
    {
        int result = 0;
        for (int i = startIndex; i < Path.Count; i++) result += (i - startIndex + 1) * Path[i].Mult;
        return result;
    }
}