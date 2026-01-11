using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Circle
{
    public List<Cell> Cells;
    public bool[] Visited;
    public int CarDirections;
    public int[] CarsInside = new int[4];

    public Circle(List<Cell> cells, bool[] visited)
    {
        this.Cells = cells;
        this.Visited = visited;
    }

    public void Clear()
    {
        for (int i = 0; i < CarsInside.Length; i++) CarsInside[i] = 0;
        CarDirections = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanAdd(Car car)
    {
        // if (Visited[car.Cell.ID]) return true; // already inside - check covered by not calling from those
        if ((CarDirections | (1 << car.Direction)) != 15) return true;
        //return false;
        Cell next = car.Next();
        while (next != null && Visited[next.ID])
        {
            if (next.Car != null && next.Car.Direction != car.Direction)
            {
                Cell nnext = next.Car.Next();
                if (!next.Car.Convoy.AtTarget)
                    return false;
                //if (nnext.Car != null) return false;
            }
            next = next.Neighbors[(int)car.Direction];
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddCar(Car car)
    {
        CarsInside[car.Direction]++;
        if (CarsInside[car.Direction] == 1) CarDirections |= 1 << car.Direction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveCar(Car car)
    {
        CarsInside[car.Direction]--;
        if (CarsInside[car.Direction] == 0) CarDirections ^= 1 << car.Direction;
    }

    public override string ToString()
    {
        return string.Join(" - ", Cells);
    }
}