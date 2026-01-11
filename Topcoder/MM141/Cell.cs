using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public class Cell
{
    public int X;
    public int Y;
    public char C;
    public int ID;
    public Cell[] Neighbors = new Cell[4];
    public Road[] Roads = new Road[2];
    public Cell[] Next = new Cell[2];
    public int[] DistToTarget = new int[2];
    public int[] DistToSpawn = new int[2];
    public int[] CrossingsToTarget = new int[2];
    public int[] CrossingsToSpawn = new int[2];
    public bool Crossing;
    public DirectionType LightDirection;
    public List<Circle> Circles = new List<Circle>();
    public List<Circle>[] NextCircles = new List<Circle>[4];
    public Car Car;

    public Cell(int x, int y, char c)
    {
        this.X = x;
        this.Y = y;
        this.ID = x + y * Board.Size;
        this.C = c;
        Crossing = C == '-' || C == '|';
        LightDirection = C == '-' ? DirectionType.HORIZONTAL : DirectionType.VERTICAL;
    }

    private static int[] dx = { 1, 0, -1, 0 };
    private static int[] dy = { 0, 1, 0, -1 };
    public void MakeNeighbors()
    {
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = X + dx[dir];
            int y = Y + dy[dir];
            if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size) Neighbors[dir] = Board.Grid[x, y];
        }
    }

    public override string ToString() => X + "/" + Y;

    public Road BuildRoad()
    {
        string roadStart = ">v<^";
        for (int i = 0; i < roadStart.Length; i++)
        {
            if (C != roadStart[i]) continue;
            Cell prev = Neighbors[(i + 2) % 4];
            if (prev != null && (prev.C == roadStart[i] || prev.Crossing)) continue;
            Road road = new Road();
            road.Cells.Add(this);
            Cell next = Neighbors[i];
            while (next != null && (next.C == roadStart[i] || next.Crossing))
            {
                road.Cells.Add(next);
                next = next.Neighbors[i];
            }
            foreach (Cell cell in road.Cells)
            {
                if (roadStart[i] == '>' || roadStart[i] == '<') road.Direction = DirectionType.HORIZONTAL;
                else road.Direction = DirectionType.VERTICAL;
                cell.Roads[(int)road.Direction] = road;
            }
            return road;
        }
        return null;
    }

    public void MakeNeighborCircles()
    {
        for (int i = 0; i < 4; i++)
        {
            if (Neighbors[i] == null) NextCircles[i] = new List<Circle>();
            else NextCircles[i] = Neighbors[i].Circles.Except(this.Circles).ToList();
        }
    }

    public string Input()
    {
        if (Crossing && LightDirection == DirectionType.HORIZONTAL) return "-";
        if (Crossing && LightDirection == DirectionType.VERTICAL) return "|";
        return C.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakeMove(Car car)
    {
        if (car.DirectionType == LightDirection) return;
        LightDirection = car.DirectionType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakeStuck(Car car)
    {
        if (car.DirectionType != LightDirection) return;
        LightDirection = (DirectionType)(1 - (int)LightDirection);
    }
}