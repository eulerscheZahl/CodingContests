using System;
using System.Collections.Generic;
using System.Linq;

public class ConnectedGroup
{
    public HashSet<Node> Nodes { get; private set; }
    public Dictionary<Node, HashSet<ConnectedGroup>> Neighbors { get; private set; }
    public bool Visited { get; set; }
    public int NodeCount { get; set; }

    public ConnectedGroup(HashSet<Node> nodes)
    {
        this.Nodes = nodes;
        Neighbors = new Dictionary<Node, HashSet<ConnectedGroup>>();
    }

    public override string ToString()
    {
        return string.Join(" ", string.Join(" ", Nodes) + ": " + NodeCount);
    }

    private static int counter = 0;
    private static Stack<Node> visitedNodes = new Stack<Node>();
    public static List<ConnectedGroup> groups = new List<ConnectedGroup>();
    private static Node root = null;
    public static void SearchBiconnectedRecurs(Node v)
    {
        v.Visited = true;
        v.Depth = counter;
        v.DFSNumber = counter;
        counter++;
        foreach (Node w in v.Neighbors)
        {
            if (!w.Visited)
            {
                w.Parent = v;
                visitedNodes.Push(w);
                SearchBiconnectedRecurs(w);
                if (w.Depth >= w.DFSNumber && v != root)
                {
                    Node output = null;
                    ConnectedGroup group = new ConnectedGroup(new HashSet<Node>());
                    while (output != w)
                    {
                        output = visitedNodes.Pop();
                        group.Nodes.Add(output);
                    }
                    group.Nodes.Add(v);
                    foreach (ConnectedGroup g in groups)
                    {
                        foreach (Node n in group.Nodes)
                        {
                            if (g.Nodes.Contains(n))
                            {
                                if (!g.Neighbors.ContainsKey(n))
                                    g.Neighbors[n] = new HashSet<ConnectedGroup>();
                                g.Neighbors[n].Add(group);
                                if (!group.Neighbors.ContainsKey(n))
                                    group.Neighbors[n] = new HashSet<ConnectedGroup>();
                                group.Neighbors[n].Add(g);
                            }
                        }
                    }
                    groups.Add(group);
                }
                v.Depth = Math.Min(v.Depth, w.Depth);
            }
            else if (w != v.Parent && v.DFSNumber > w.DFSNumber)
            {
                v.Depth = Math.Min(v.Depth, w.DFSNumber);
            }
        }
    }

    public static void SearchBiconnected(Node[] grid)
    {
        root = grid.First(g => g.Cell.Water);
        SearchBiconnectedRecurs(root);
        while (visitedNodes.Count > 0)
        {
            ConnectedGroup group = new ConnectedGroup(new HashSet<Node>());
            while (visitedNodes.Count > 0)
            {
                Node next = visitedNodes.Pop();
                group.Nodes.Add(next);
                if (next.Parent == root)
                    break;
            }
            group.Nodes.Add(root);
            foreach (ConnectedGroup g in groups)
            {
                foreach (Node v in group.Nodes)
                {
                    if (g.Nodes.Contains(v))
                    {
                        if (!g.Neighbors.ContainsKey(v))
                            g.Neighbors[v] = new HashSet<ConnectedGroup>();
                        g.Neighbors[v].Add(group);
                        if (!group.Neighbors.ContainsKey(v))
                            group.Neighbors[v] = new HashSet<ConnectedGroup>();
                        group.Neighbors[v].Add(g);
                    }
                }
            }
            groups.Add(group);
        }
    }
}

public class Node
{
    public Cell Cell;
    public bool Visited;
    public int Depth;
    public int DFSNumber;
    public Node Parent;

    public HashSet<Node> Neighbors = new HashSet<Node>();

    public override string ToString() => Cell.ToString();
}
