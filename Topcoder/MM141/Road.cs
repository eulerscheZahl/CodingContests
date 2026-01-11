using System.Collections.Generic;
using System.Linq;

public class Road
{
    public DirectionType Direction;
    public List<Cell> Cells = new List<Cell>();

    public void CreateStatistics()
    {
        for (int i = 0; i < Cells.Count; i++)
        {
            Cells[i].DistToSpawn[(int)Direction] = i;
            Cells[i].DistToTarget[(int)Direction] = Cells.Count - 1 - i;
            Cells[i].CrossingsToSpawn[(int)Direction] = Cells.Take(i + 1).Count(c => c.Crossing);
            Cells[i].CrossingsToTarget[(int)Direction] = Cells.Skip(i).Count(c => c.Crossing);
            if (i + 1 < Cells.Count) Cells[i].Next[(int)Direction] = Cells[i + 1];
        }
    }

    public double SpawnRate;
    private int spawns;
    private int turns;

    public void UpdateSpawnRate()
    {
        turns++;
        if (Cells[0].Car != null) spawns++;
        SpawnRate = (double)spawns / turns;
    }
}