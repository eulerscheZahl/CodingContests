using System.Collections.Generic;
using System.Linq;

public class Cell
{
    public int X;
    public int Y;
    public int ID;
    public bool Water;
    public bool Plant;
    public bool Pipe;
    public bool Sprinkler;
    public bool Mandatory;
    public bool Eliminated;
    public int SprinklersInRange;
    public int SprinklersInLine;
    public int[] Dist;
    public Cell[] Neighbors = new Cell[4];
    public Cell[] WalkableNeighbors;
    public List<Cell> CellRangeList = new List<Cell>();
    public Cell[] CellRange;
    public List<Cell> Connections = new List<Cell>();

    public Cell(int x, int y, char c)
    {
        this.X = x;
        this.Y = y;
        this.ID = x + y * Board.Size;
        Water = c == '1';
        Plant = c == '2';
        Pipe = Water;
    }

    private static readonly int[] dx = { 1, 0, -1, 0 };
    private static readonly int[] dy = { 0, 1, 0, -1 };
    public void MakeNeighbors()
    {
        for (int dir = 0; dir < dx.Length; dir++)
        {
            int x = X + dx[dir];
            int y = Y + dy[dir];
            if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size) Neighbors[dir] = Board.Grid[x, y];
        }
        Neighbors = Neighbors.Where(n => n != null).ToArray();
        WalkableNeighbors = Neighbors.Where(n => !n.Plant).ToArray();
    }

    public void UpdateRange(List<Cell> plants)
    {
        foreach (Cell c in plants)
        {
            if ((X - c.X) * (X - c.X) + (Y - c.Y) * (Y - c.Y) > Board.SprinklerRange * Board.SprinklerRange) continue;
            this.CellRangeList.Add(c);
            c.CellRangeList.Add(this);
        }
    }

    public int GetConnectorCost()
    {
        if (Water || Connections.Count == 2 && (Connections[0].X == Connections[1].X || Connections[0].Y == Connections[1].Y)) return 0;
        return Connections.Count * Board.ConnectorCost;
    }

    public override string ToString() => X + "/" + Y;

    public int ExpectedConnectionCost(Cell c)
    {
        if (c.Water) return 1;
        if (Connections.Count == 1 && (Connections[0].X == c.X || Connections[0].Y == c.Y)) return 0;
        else if (Connections.Count == 1) return 2;
        return 1;
    }

    private bool connectionChanged;
    private int inRangeBackup;
    private int inLingBackup;
    private bool sprinklerBackup;
    private List<Cell> connectionBackup;
    public void Backup()
    {
        inRangeBackup = SprinklersInRange;
        inLingBackup = SprinklersInLine;
        sprinklerBackup = Sprinkler;
        connectionBackup = Connections.ToList();
    }

    public void Restore()
    {
        SprinklersInRange = inRangeBackup;
        SprinklersInLine = inLingBackup;
        Sprinkler = sprinklerBackup;
        if (connectionChanged) Connections = connectionBackup.ToList();
        connectionChanged = false;
    }

    public void Connect(Cell cell)
    {
        this.Connections.Add(cell);
        cell.Connections.Add(this);
        this.connectionChanged = true;
        cell.connectionChanged = true;
    }

    public void Disconnect(Cell cell)
    {
        Connections.Remove(cell);
        connectionChanged = true;
    }

    public void Disconnect()
    {
        Connections.Clear();
        connectionChanged = true;
    }

    private int inRangeBackup2;
    private int inLineBackup2;
    private bool sprinklerBackup2;
    private List<Cell> connectionBackup2;
    public void Backup2()
    {
        inRangeBackup2 = SprinklersInRange;
        inLineBackup2 = SprinklersInLine;
        sprinklerBackup2 = Sprinkler;
        connectionBackup2 = Connections.ToList();
    }

    public void Restore2()
    {
        SprinklersInRange = inRangeBackup2;
        SprinklersInLine = inLineBackup2;
        Sprinkler = sprinklerBackup2;
        Connections = connectionBackup2.ToList();
    }

    public void SetSprinkler(bool sprinkler)
    {
        if (sprinkler == Sprinkler) return;
        Sprinkler = sprinkler;
        UpdateSprinklerInSight();
    }

    public void UpdateSprinklerInSight()
    {
        for (int dir = 0; dir < 4; dir++)
        {
            for (int length = 1; length < 2 + Board.SprinklerRange; length++)
            {
                int x = X + length * dx[dir];
                int y = Y + length * dy[dir];
                if (x < 0 || x >= Board.Size || y < 0 || y >= Board.Size || Board.Cells[x + Board.Size * y].Plant) break;
                if (Sprinkler) Board.Cells[x + Board.Size * y].SprinklersInLine++;
                else Board.Cells[x + Board.Size * y].SprinklersInLine--;
            }
        }
    }

    public int MissingCount()
    {
        int result = 0;
        foreach (Cell cell in CellRange)
        {
            if (cell.SprinklersInRange == 0) result++;
        }
        return result;
    }
}
