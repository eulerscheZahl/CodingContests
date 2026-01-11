using System;
using System.Collections.Generic;

public class Car
{
    public Cell Cell;
    public int Value;
    public int Direction;
    public DirectionType DirectionType;
    public bool Horizontal;
    public bool Visited;
    public Convoy Convoy;
    public int MinimumDelay;


    public Car(Cell cell, int value, char direction)
    {
        Cell = cell;
        cell.Car = this;
        Value = value;
        Direction = ">v<^".IndexOf(direction);
        DirectionType = "<>".Contains(direction) ? DirectionType.HORIZONTAL : DirectionType.VERTICAL;
    }

    public Cell Next() => Cell.Next[(int)DirectionType];

    public override string ToString()
    {
        return Cell + " " + ">v<^"[Direction];
    }

    public double Urgency(List<Convoy> convoy) => 1000 * Convoy.Urgency(convoy) + Convoy.ID + 0.01 * Convoy.Cars.IndexOf(this);

    public bool DFS_Visited;
    public int Dependants;
    public int DFS()
    {
        if (DFS_Visited) return Dependants;
        DFS_Visited = true;
        Dependants = 0;
        foreach (Cell n in Cell.Neighbors)
        {
            if (n == null || n.Car == null || n.Car.Next() != this.Cell) continue;
            Dependants += 1 + n.Car.DFS();
        }

        return Dependants;
    }
}