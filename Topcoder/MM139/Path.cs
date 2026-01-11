using System;
using System.Collections.Generic;
using System.Linq;

public class Path
{
    public int Color => Cells[0].Color;
    public List<Cell> Cells = new List<Cell>();
    public Cell Start => Cells[0];
    public Cell End => Cells[Cells.Count - 1];
    public int Score()
    {
        int result = Start.Value * End.Value;
        foreach (Cell cell in Cells) result -= Board.Penalty * (cell.Paths.Count - 1);
        return result;
    }

    public string Print() => Cells.Count + "\n" + string.Join("\n", Cells.Select(c => c.Print()));

    public int Remove()
    {
        int result = Start.Value * End.Value;
        foreach (Cell cell in Cells)
        {
            cell.Paths.Remove(this);
            result -= Board.Penalty * cell.Paths.Count;
        }
        return result;
    }

    public void Apply()
    {
        foreach (Cell cell in Cells) cell.Paths.Add(this);
    }

    public Cell Partner(Cell toConnect) => toConnect == Start ? End : Start;
}