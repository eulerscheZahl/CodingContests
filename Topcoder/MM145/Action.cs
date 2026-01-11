public class Action
{
    public Lift Lift;
    public int Target;
    public double Score;

    public Action(Lift lift, int target, double score)
    {
        Lift = lift;
        Target = target;
        Score = score;
    }
}
