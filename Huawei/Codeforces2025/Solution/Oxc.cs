using System;
using System.Collections.Generic;
using System.Linq;

public class Oxc : NetworkComponent
{
    public Plane Plane;
    public List<Spine> Spines = [];
    public Connection[] ConnectionsCrossByPort;
    // The port corresponding to OXC number m, Group number i, Spine number j, and link number k has the number i⋅(S/P)⋅K+(j mod(S/P))⋅K+k
    public int[] PortLinks;
    public int[] PreviousPortLinks;
    public int[] OpenGroupConnectors;
    public List<Task> Tasks = [];

    public static int GetPortPos(Spine spine)
    {
        int spinePos = spine.ID % (Network.S / Network.P);
        return (spinePos + spine.Group.ID * Network.S / Network.P) * Network.K;
    }

    public static int GetGroupPos(int group) => group * Network.S / Network.P * Network.K;

    public override string ToString() => $"ID={ID}  PlaneId={Plane.ID}";

    public int CountChanges()
    {
        int result = 0;
        for (int i = 0; i < PortLinks.Length; i++)
        {
            if (PortLinks[i] == PreviousPortLinks[i]) continue;
            if (PortLinks[i] >= 0 && PreviousPortLinks[i] >= 0) result += 2;
            else result++;
        }
        return result / 2;
    }

    public IEnumerable<Task> EndQuery()
    {
        for (int i = 0; i < PortLinks.Length; i++)
        {
            if (PortLinks[i] == -1 && PreviousPortLinks[i] != -1 && PortLinks[PreviousPortLinks[i]] == -1)
            {
                int partner = PreviousPortLinks[i];
                Connection cross = Connect(i, i % Network.K, partner, partner % Network.K);
                List<Connection> path = [cross.Spine.ConnectionsDown[0], cross, cross.Spine2.ConnectionsDown[0]];
                yield return new(path[0].Leaf, path[^1].Leaf) { Connections = path, Oxc = this, SpineA = path[1].Spine, SpineB = path[1].Spine2 };
            }
        }
        PreviousPortLinks = PortLinks.ToArray();
    }

    public Connection Connect(int indexA, int offsetA, int indexB, int offsetB)
    {
        if (ConnectionsCrossByPort[indexA] != null || ConnectionsCrossByPort[indexB] != null) return null;
        Connection cross = new Connection(null, Spines[indexA / Network.K], this, offsetA)
        {
            Offset2 = offsetB,
            Spine2 = Spines[indexB / Network.K],
            PortA = indexA,
            PortB = indexB
        };
        ConnectionsCrossByPort[indexA] = cross;
        ConnectionsCrossByPort[indexB] = cross;
        PortLinks[indexA] = indexB;
        PortLinks[indexB] = indexA;
        return cross;
    }

    public Connection Connect(Task task)
    {
        (int index, int offset) = GetFreePort(task.SpineA);
        (int index2, int offset2) = GetFreePort(task.SpineB);
        Connection conn = Connect(index, offset, index2, offset2);
        task.SpineA = conn.Spine;
        task.SpineB = conn.Spine2;
        task.Oxc = conn.Oxc;
        return conn;
    }

    private (int index, int offset) GetFreePort(Spine spine)
    {
        int index = GetPortPos(spine);
        for (int k = 0; k < Network.K; k++)
        {
            if (PortLinks[index + k] == -1 && (PreviousPortLinks[index + k] == -1 || PortLinks[PreviousPortLinks[index + k]] != -1)) return (index + k, k);
        }
        for (int k = 0; k < Network.K; k++)
        {
            if (PortLinks[index + k] == -1) return (index + k, k);
        }
        return (-1, -1);
    }

    public void ConnectAll(List<Task> tasks)
    {
        List<Task> backlog = [];
        foreach (Task task in tasks)
        {
            bool connected = false;
            int indexA = GetPortPos(task.SpineA);
            int indexB = GetPortPos(task.SpineB);
            for (int kA = 0; !connected && kA < Network.K; kA++)
            {
                for (int kB = 0; !connected && kB < Network.K; kB++)
                {
                    if (ConnectionsCrossByPort[indexA + kA] == null && ConnectionsCrossByPort[indexB + kB] == null &&
                    PreviousPortLinks[indexA + kA] == indexB + kB && PreviousPortLinks[indexB + kB] == indexA + kA)
                    {
                        connected = true;
                        Connection cross = Connect(indexA + kA, kA, indexB + kB, kB);
                        task.Connections = [
                            cross.Spine.ConnectionsDown[task.LeafA.ID],
                            cross,
                            cross.Spine2.ConnectionsDown[task.LeafB.ID]
                        ];
                        foreach (Connection c in task.Connections) c.Tasks.Add(task);
                    }
                }
            }
            if (!connected) backlog.Add(task);
        }

        foreach (Task task in backlog)
        {
            Connection cross = Connect(task);
            task.Connections = [
                cross.Spine.ConnectionsDown[task.LeafA.ID],
                        cross,
                        cross.Spine2.ConnectionsDown[task.LeafB.ID]
            ];
            foreach (Connection c in task.Connections) c.Tasks.Add(task);
        }
    }
}
