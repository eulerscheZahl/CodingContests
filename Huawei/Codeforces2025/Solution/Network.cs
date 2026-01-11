using System.Collections.Generic;
using System.Linq;

public static class Network
{
    public static int N; // groupsCount
    public static int S; // spinesCount (per group)
    public static int L; // leavesCount (per group)
    public static int M; //OxcCount
    public static int K; //OxcSpineLinkCount
    public static int P; // PlaneCount
    public static double ConvergenceRatio => (double)M / P * K / L;
    public static int Convergence => P * L / (M * K);

    public static List<Group> Groups = [];
    public static List<Plane> Planes = [];
    public static List<Leaf> Leaves = [];
    public static List<Spine> Spines = [];
    public static List<Oxc> Oxcs = [];

    public static void Init()
    {
        int R = N * S / P * K; // OxcPortsCount
        for (int i = 0; i < N; i++) Groups.Add(new Group { ID = i });
        for (int i = 0; i < P; i++) Planes.Add(new Plane { ID = i });
        for (int i = 0; i < M; i++)
        {
            Oxc oxc = new Oxc
            {
                ID = i,
                Plane = Planes[i * P / M],
                PortLinks = [.. Enumerable.Range(0, R).Select(i => -1)],
                ConnectionsCrossByPort = new Connection[R],
                PreviousPortLinks = [.. Enumerable.Range(0, R).Select(i => -1)],
            };
            Oxcs.Add(oxc);
            oxc.Plane.Oxcs.Add(oxc);
        }
        for (int i = 0; i < L; i++)
        {
            foreach (Group group in Groups)
            {
                Leaf leaf = new Leaf { ID = i, Group = group };
                Leaves.Add(leaf);
                group.Leaves.Add(leaf);
            }
        }
        foreach (Group group in Groups)
        {
            for (int i = 0; i < S; i++)
            {
                Spine spine = new Spine { ID = i, Group = group, Plane = Planes[i * P / S] };
                Spines.Add(spine);
                group.Spines.Add(spine);
                spine.Plane.Spines.Add(spine);
            }
        }

        foreach (Group group in Groups)
        {
            foreach (Leaf leaf in group.Leaves)
            {
                foreach (Spine spine in group.Spines)
                {
                    leaf.Spines.Add(spine);
                    spine.Leaves.Add(leaf);
                }
            }
        }
        foreach (Plane plane in Planes)
        {
            foreach (Oxc oxc in plane.Oxcs)
            {
                foreach (Spine spine in plane.Spines)
                {
                    oxc.Spines.Add(spine);
                    spine.Oxcs.Add(oxc);
                }
            }
        }

        foreach (Spine spine in Spines)
        {
            foreach (Leaf leaf in spine.Leaves)
            {
                Connection connection = new Connection(leaf, spine, null, 0);
                leaf.ConnectionsUp.Add(connection);
                spine.ConnectionsDown.Add(connection);
            }
            foreach (Oxc oxc in spine.Oxcs)
            {
                for (int k = 0; k < K; k++)
                {
                    Connection connection = new Connection(null, spine, oxc, k);
                    spine.ConnectionsUp.Add(connection);
                    oxc.ConnectionsDown.Add(connection);
                }
            }
        }
    }
}
