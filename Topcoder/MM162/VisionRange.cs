using System;
using System.Linq;

public class VisionRange
{
    public Point GuardPos;
    private Point[] crouchTriangle;
    private Point[] standTriangle;
    public Point[] CrouchRect;
    public Point[] StandRect;
    public Point[] StandRectExtended;

    public VisionRange(Point[] points)
    {
        GuardPos = points[0];
        crouchTriangle = new Point[] { points[1], points[2] };
        standTriangle = new Point[] { points[3], points[4] };
        CrouchRect = new Point[] { new Point(Math.Min(GuardPos.X, crouchTriangle.Min(c => c.X)), Math.Min(GuardPos.Y, crouchTriangle.Min(c => c.Y))), new Point(Math.Max(GuardPos.X, crouchTriangle.Max(c => c.X)), Math.Max(GuardPos.Y, crouchTriangle.Max(c => c.Y))) };
        StandRect = new Point[] { new Point(Math.Min(GuardPos.X, standTriangle.Min(c => c.X)), Math.Min(GuardPos.Y, standTriangle.Min(c => c.Y))), new Point(Math.Max(GuardPos.X, standTriangle.Max(c => c.X)), Math.Max(GuardPos.Y, standTriangle.Max(c => c.Y))) };
        StandRectExtended = new Point[] { new Point(StandRect[0].X - 400, StandRect[0].Y - 400), new Point(StandRect[1].X + 400, StandRect[1].Y + 400) };
    }

    public bool CanSee(Point p, bool crouch)
    {
        if (p.Equals(GuardPos)) return false;
        if (crouch)
        {
            if (!IsPointInRect(p, CrouchRect)) return false;
            return IsPointInsideTriangle(p, GuardPos, crouchTriangle[0], crouchTriangle[1]);
        }
        if (!IsPointInRect(p, StandRect)) return false;
        return IsPointInsideTriangle(p, GuardPos, standTriangle[0], standTriangle[1]);
    }

    public static bool IsPointInRect(Point p, Point[] square)
    {
        return p.X >= square[0].X && p.X <= square[1].X && p.Y >= square[0].Y && p.Y <= square[1].Y;
    }

    private int RelativeCCW(int x1, int y1, int x2, int y2, int px, int py)
    {
        x2 -= x1;
        y2 -= y1;
        px -= x1;
        py -= y1;
        long ccw = px * y2 - py * x2;
        if (ccw == 0)
        {
            ccw = px * x2 + py * y2;
            if (ccw > 0)
            {
                px -= x2;
                py -= y2;
                ccw = px * x2 + py * y2;
                if (ccw < 0) ccw = 0;
            }
        }
        return (ccw < 0) ? -1 : ((ccw > 0) ? 1 : 0);
    }

    private bool IsPointInsideTriangle(Point p, Point p1, Point p2, Point p3)
    {
        int ccw1 = RelativeCCW(p1.X, p1.Y, p2.X, p2.Y, p.X, p.Y);
        int ccw2 = RelativeCCW(p2.X, p2.Y, p3.X, p3.Y, p.X, p.Y);
        int ccw3 = RelativeCCW(p3.X, p3.Y, p1.X, p1.Y, p.X, p.Y);

        return (ccw1 >= 0 && ccw2 >= 0 && ccw3 >= 0) || (ccw1 <= 0 && ccw2 <= 0 && ccw3 <= 0);
    }
}
