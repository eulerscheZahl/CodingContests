using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Guard
{
    public int ID;
    public int Hash;
    public bool IsBlocking;
    private static Random random = new Random(1);
    public VisionRange[] Path;
    private Point[] Pos;

    public Guard(int id, VisionRange[] path)
    {
        this.ID = id;
        Path = path;
        Hash = random.Next();
        Pos = path.Select(p => p.GuardPos).ToArray();
    }

    public bool CanSee(Point p, bool crouch, int turn)
    {
        VisionRange vision = Path[turn % Path.Length];
        return vision.CanSee(p, crouch);
    }

    public Point GetPos(int turn) => Pos[turn % Path.Length];

    public bool MightDetect(Point p, int turn)
    {
        VisionRange vision = Path[turn % Path.Length];
        return VisionRange.IsPointInRect(p, vision.StandRectExtended);
    }
}
