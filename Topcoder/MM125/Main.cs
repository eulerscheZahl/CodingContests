using System;
using System.Threading.Tasks;

public class Lines
{
    private static void Submit()
    {
        Engine engine = new ArenaEngine();
        Strategy strategy = new ArenaStrategy();
        engine.Play(strategy);
    }

    static void Main(string[] args)
    {
        Submit();
    }
}
