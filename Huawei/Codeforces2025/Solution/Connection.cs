using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
