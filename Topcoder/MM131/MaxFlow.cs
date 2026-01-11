using System;
using System.Collections.Generic;
using System.Linq;

public class MaxFlow
{
    class Node
    {
        public int Dist;
        public Cell Cell;
        public bool In;
        public List<Edge> Edges = new List<Edge>();

        public Node(Cell cell, bool isIn)
        {
            this.Cell = cell;
            this.In = isIn;
        }

        public void ConnectWith(Node partner, int cap, int backCap)
        {
            Edge edgeFrom = new Edge { From = this, To = partner, Capacity = cap, InitialCapacity = cap };
            Edge edgeTo = new Edge { From = partner, To = this, Capacity = backCap, InitialCapacity = backCap, Partner = edgeFrom };
            edgeFrom.Partner = edgeTo;
            Edges.Add(edgeFrom);
            partner.Edges.Add(edgeTo);
        }

        public void ClearEdges()
        {
            foreach (Edge e in Edges) e.To.Edges.Remove(e.Partner);
            Edges.Clear();
        }

        public override string ToString()
        {
            string pre = In ? "In:" : "Out:";
            if (Cell == null) return pre;
            return $"{pre} ({Cell.X}/{Cell.Y})";
        }
    }

    class Edge
    {
        public Node From;
        public Node To;
        public Edge Partner;
        public int Capacity;
        public int InitialCapacity;

        public override string ToString() => $"{From} => {To}: {Capacity}";
    }

    static int[] dx = { 0, 1, 0, -1 };
    static int[] dy = { -1, 0, 1, 0 };
    private Node source = new Node(null, false);
    private Node target = new Node(null, true);
    private List<Node> graph;
    public int Flow;
    public MaxFlow(Board board, Ring ring, List<Cell> edges)
    {
        BuildGraph(board, ring, edges);
    }

    private List<Node> BuildGraph(Board board, Ring ring, List<Cell> edge)
    {
        Node[,] nodeIn = new Node[Board.Size, Board.Size];
        Node[,] nodeOut = new Node[Board.Size, Board.Size];
        graph = new List<Node> { source, target };
        for (int x = 0; x < Board.Size; x++)
        {
            for (int y = 0; y < Board.Size; y++)
            {
                if (board.Grid[x, y].Tile == Tile.TREE || board.Grid[x, y].Tile == Tile.BOX) continue;
                nodeIn[x, y] = new Node(board.Grid[x, y], true);
                nodeOut[x, y] = new Node(board.Grid[x, y], false);
                graph.Add(nodeIn[x, y]);
                graph.Add(nodeOut[x, y]);
                nodeIn[x, y].ConnectWith(nodeOut[x, y], 1, 0);
            }
        }

        for (int x = 0; x < Board.Size; x++)
        {
            for (int y = 0; y < Board.Size; y++)
            {
                if (nodeOut[x, y] == null) continue;
                for (int dir = 0; dir < 4; dir++)
                {
                    int x_ = x + dx[dir];
                    int y_ = y + dy[dir];
                    if (x_ < 0 || x_ >= Board.Size || y_ < 0 || y_ >= Board.Size || nodeIn[x_, y_] == null) continue;
                    nodeOut[x, y].ConnectWith(nodeIn[x_, y_], 1000, 0);
                }
            }
        }

        foreach (Cell s in edge)
        {
            nodeIn[s.X, s.Y].ClearEdges();
            nodeIn[s.X, s.Y].ConnectWith(nodeOut[s.X, s.Y], 1, 0);
            source.ConnectWith(nodeOut[s.X, s.Y], 1000, 0);
        }
        foreach (Cell t in ring.Presents)
        {
            List<Edge> tIn = nodeIn[t.X, t.Y].Edges.ToList();
            nodeIn[t.X, t.Y].ClearEdges();
            nodeOut[t.X, t.Y].ClearEdges();
            foreach (Edge e in tIn)
                e.To.ConnectWith(target, 1000, 0);
        }
        return graph;
    }

    public void InvertDirection()
    {
        Node tmp = source;
        source = target;
        target = tmp;
        foreach (Node node in graph)
        {
            node.In = !node.In;
            foreach (Edge edge in node.Edges) edge.Capacity = edge.Partner.InitialCapacity;
        }
    }

    private void ApplyFlow()
    {
        bool repeat = true;
        while (repeat)
        {
            repeat = false;
            foreach (Node node in graph) node.Dist = int.MaxValue;
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(source);
            source.Dist = 0;
            while (queue.Count > 0)
            {
                Node q = queue.Dequeue();
                if (q == target)
                {
                    Flow++;
                    repeat = true;

                    // construct path
                    while (q.Dist != 0)
                    {
                        foreach (Edge e in q.Edges)
                        {
                            if (e.To.Dist == q.Dist - 1 && e.Partner.Capacity > 0)
                            {
                                e.Partner.Capacity--;
                                e.Capacity++;
                                q = e.To;
                                break;
                            }
                        }
                    }
                    break;
                }
                foreach (Edge e in q.Edges)
                {
                    if (e.Capacity == 0 || e.To.Dist != int.MaxValue) continue;
                    Node t = e.To;
                    t.Dist = 1 + q.Dist;
                    queue.Enqueue(t);
                }
            }
        }
    }

    public HashSet<Cell> Border(Board board, Ring ring)
    {
        ApplyFlow();
        foreach (Node node in graph) node.Dist = int.MaxValue;
        Queue<Node> queue = new Queue<Node>();
        queue.Enqueue(source);
        source.Dist = 0;
        bool[,] reachable = new bool[Board.Size, Board.Size];
        while (queue.Count > 0)
        {
            Node q = queue.Dequeue();
            if (q.Cell != null) reachable[q.Cell.X, q.Cell.Y] = true;
            foreach (Edge e in q.Edges)
            {
                if (e.Capacity == 0) continue;
                if (e.To.Dist != int.MaxValue) continue;
                e.To.Dist = 1 + q.Dist;
                queue.Enqueue(e.To);
            }
        }

        HashSet<Cell> result = new HashSet<Cell>();
        bool[,] shadowed = new bool[Board.Size, Board.Size];
        for (int y = 0; y < Board.Size; y++)
        {
            for (int x = 0; x < Board.Size; x++)
            {
                if (board.Grid[x, y].Tile == Tile.BOX) result.Add(board.Grid[x, y]);
                if (reachable[x, y] || board.Grid[x, y].Tile == Tile.TREE || board.Grid[x, y].Tile == Tile.BOX) continue;
                for (int dir = 0; dir < 4; dir++)
                {
                    int x_ = x + dx[dir];
                    int y_ = y + dy[dir];
                    if (x_ < 0 || x_ >= Board.Size || y_ < 0 || y_ >= Board.Size || shadowed[x_, y_]) continue;
                    shadowed[x_, y_] = true;
                    if (reachable[x_, y_]) result.Add(board.Grid[x_, y_]);
                }
            }
        }

        // final cleanup as some boxes can be removed from shrinked border
        if (board.Turn > 0)
        {
            reachable = new bool[Board.Size, Board.Size];
            Queue<Cell> bfs = new Queue<Cell>();
            foreach (Cell present in ring.Presents)
            {
                bfs.Enqueue(present);
                reachable[present.X, present.Y] = true;
            }
            while (bfs.Count > 0)
            {
                Cell q = bfs.Dequeue();
                foreach (Cell cell in q.Neighbors)
                {
                    if (cell.Tile == Tile.TREE || reachable[cell.X, cell.Y]) continue;
                    reachable[cell.X, cell.Y] = true;
                    if (result.Contains(cell)) continue;
                    bfs.Enqueue(cell);
                }
            }
            result = new HashSet<Cell>(result.Where(c => reachable[c.X, c.Y]));
        }
        return result;
    }
}