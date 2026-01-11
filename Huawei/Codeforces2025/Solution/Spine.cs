using System.Collections.Generic;

public class Spine : NetworkComponent
{
    public Plane Plane;
    public Group Group;
    public List<Oxc> Oxcs = [];
    public List<Leaf> Leaves = [];

    public override string ToString() => $"ID={ID}  PlaneId={Plane.ID}  GroupId={Group.ID}";
}
