using System.Collections.Generic;

public class TaskPath
{
    public List<Connection> Connections = [];
    public HashSet<Task> Tasks = [];
    public NetworkComponent CurrentPos;

    public TaskPath() { }
    public TaskPath(TaskPath taskPath, Connection connection)
    {
        this.Connections = [.. taskPath.Connections, connection];
        this.Tasks = [.. taskPath.Tasks];
    }
}