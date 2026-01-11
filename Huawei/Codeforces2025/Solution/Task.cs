using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
