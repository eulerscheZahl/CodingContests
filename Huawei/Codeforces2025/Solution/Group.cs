using System.Collections.Generic;

public class Group
{
    public int ID;
    public List<Leaf> Leaves = [];
    public List<Spine> Spines = [];

    public override string ToString() => $"ID={ID}";
}
