public class Sheep
{
    public Cell Cell;
    public int Wool;
    public int InitialWool;
    public int[,] Dist;

    public Sheep(Cell cell, int wool)
    {
        this.Cell = cell;
        this.Wool = wool;
        this.InitialWool = wool;
        cell.Sheep = this;
    }

    public override string ToString()
    {
        return $"{Wool}: {Cell}";
    }
}
