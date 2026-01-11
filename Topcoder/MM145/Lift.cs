using System.Linq;

public class Lift
{
    public int ID;
    public int Floor;
    public bool Open;
    public bool Closed => !Open;
    public int[] Targets;
    public int TargetCount;
    public bool Full => TargetCount == 4;

    public Lift()
    {
        Targets = new int[Board.FloorCount];
    }

    public Lift(Lift lift)
    {
        this.ID = lift.ID;
        this.Floor = lift.Floor;
        this.Open = lift.Open;
        this.Targets = lift.Targets.ToArray();
        this.TargetCount = lift.TargetCount;
    }

    public override string ToString() => $"Lift {Floor + 1}: {(Open ? "open" : "closed")} {string.Join(" ", Targets)}";

    public void Parse(string line)
    {
        string[] parts = line.Split();
        ID = int.Parse(parts[0]);
        Floor = int.Parse(parts[1]);
        Open = bool.Parse(parts[2]);
        Targets = parts.Skip(3).Select(int.Parse).ToArray();
        TargetCount = Targets.Sum();
    }
}
