using System.Collections.Generic;

public class Action
{
    public Cell Source;
    public bool Build;
    public Cell Target;

    public Action(Cell source, Cell target, bool build)
    {
        this.Source = source;
        this.Target = target;
        this.Build = build;
    }

    public override string ToString()
    {
        string dir = "U";
        if (Target.X > Source.X) dir = "R";
        if (Target.X < Source.X) dir = "L";
        if (Target.Y > Source.Y) dir = "D";
        return Source.Y + " " + Source.X + " " + (Build ? "B" : "M") + " " + dir;
    }
}