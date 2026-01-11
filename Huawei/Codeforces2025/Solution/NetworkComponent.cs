using System.Collections.Generic;

public class NetworkComponent
{
    public int ID;
    public List<Connection> ConnectionsUp = [];
    public List<Connection> ConnectionsDown = [];
    private static int internalIdCounter;
    private int internalId;
    public NetworkComponent() { internalId = internalIdCounter++; }
    public override int GetHashCode() => internalId;
}
