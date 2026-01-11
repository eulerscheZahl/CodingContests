using System.Collections.Generic;

public class Node
{
    public char C;
    public int Count;
    public bool Terminal;
    public int Index;
    public List<Node> Children = new List<Node>();

    public void AddWord(string s, List<int> order, int index = 0)
    {
        Count++;
        if (s.Length == index)
        {
            Terminal = true;
            return;
        }
        Node child = null;
        foreach (Node n in Children)
        {
            if (n.C == s[order[index]] && n.Index == order[index])
            {
                child = n;
                break;
            }
        }
        if (child == null)
        {
            child = new Node { C = s[order[index]], Index = order[index] };
            Children.Add(child);
        }
        child.AddWord(s, order, index + 1);
    }
}
