using System.Collections.Generic;

public class Plane
{
    public int ID;
    public List<Oxc> Oxcs = [];
    public List<Spine> Spines = [];

    public override string ToString() => $"ID={ID}";
}
