using System.Collections.Generic;
using System.Linq;

public class Convoy
{
    public int ID;
    public List<Car> Cars = new List<Car>();
    public bool AtTarget => Cars[Cars.Count - 1].Cell.DistToTarget[(int)Cars[0].DirectionType] == 1;
    //public bool AtTarget => Cars[Cars.Count - 1].Cell.CrossingsToTarget[(int)Cars[0].DirectionType] == 0 ||
    //    Cars[Cars.Count - 1].Cell.CrossingsToTarget[(int)Cars[0].DirectionType] == 1 && Cars[Cars.Count - 1].Cell.Crossing;
    public bool OnCrossing => Cars.Any(c => c.Cell.Crossing);
    public bool OnSpawn => Cars[0].Cell.DistToSpawn[(int)Cars[0].DirectionType] == 0;
    private static int idCounter;
    public Convoy()
    {
        ID = idCounter++;
    }

    public double Urgency(List<Convoy> convoy)
    {
        double result = 3 * Cars.Count;
        result -= Cars[0].Cell.DistToSpawn[(int)Cars[0].DirectionType];
        result += 2 * Cars.Count(c => c.Cell.Crossing);
        result += 1 * Cars[Cars.Count - 1].Dependants;
        if (convoy.Contains(this)) result += 1000;
        return result;
    }
}