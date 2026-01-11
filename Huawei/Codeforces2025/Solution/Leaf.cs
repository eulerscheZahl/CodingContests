using System.Collections.Generic;

public class Leaf : NetworkComponent
{
    public int LeafId => Group.ID + Network.N * ID;
    public Group Group;
    public List<Spine> Spines = [];

    public override string ToString() => $"ID={ID}  GroupId={Group.ID}";
}
