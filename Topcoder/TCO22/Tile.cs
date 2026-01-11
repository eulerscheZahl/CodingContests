using System.Linq;

public class Tile
{
    public static Tile Single = new Tile("100000000") { Index = -1 };
    public int[] Grid = new int[9];
    public int Index;
    public int MaxRows;
    public int FruitType;
    public int FruitsSet;
    public bool[] XUsed = new bool[3];
    public bool[] YUsed = new bool[3];
    public Tile(string s)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                Grid[x * 3 + y] = s[x + 3 * y] - '0';
                if (Grid[x * 3 + y] != 0)
                {
                    FruitType = Grid[x * 3 + y];
                    FruitsSet++;
                    XUsed[x] = true;
                    YUsed[y] = true;
                }
            }
        }
        MaxRows = XUsed.Count(x => x) + YUsed.Count(y => y);
    }

    public bool CanPlace(Board board, int placeX, int placeY)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (Grid[x * 3 + y] == 0) continue;
                if (x + placeX < 0 || x + placeX >= Board.Size || y + placeY < 0 || y + placeY >= Board.Size || board.Grid[(x + placeX) * Board.Size + y + placeY] != 0) return false;
            }
        }
        return true;
    }

    public void Place(Board board, int placeX, int placeY, bool updateCounts = true)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                int fruit = Grid[x * 3 + y];
                if (fruit == 0) continue;
                board.Grid[(x + placeX) * Board.Size + y + placeY] = fruit;
                if (!updateCounts) continue;
                board.RowCounts[(y + placeY) * Board.FruitCountPlus + fruit]++;
                board.RowCounts[(y + placeY) * Board.FruitCountPlus]++;
                board.ColCounts[(x + placeX) * Board.FruitCountPlus + fruit]++;
                board.ColCounts[(x + placeX) * Board.FruitCountPlus]++;
            }
        }
    }

    public void Unplace(Board board, int placeX, int placeY)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                int fruit = Grid[x * 3 + y];
                if (fruit == 0) continue;
                board.Grid[(x + placeX) * Board.Size + y + placeY] = 0;
            }
        }
    }

    public string GetInputs()
    {
        string result = "";
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                result += Grid[x * 3 + y];
            }
        }
        return result;
    }
}