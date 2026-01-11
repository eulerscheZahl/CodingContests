using System;

public class Coin : Point
{
    public int ID;
    public int Hash;
    private static Random random = new Random(0);

    public Coin(int id, int x, int y) : base(x, y)
    {
        this.ID = id;
        Hash = random.Next();
    }
}
