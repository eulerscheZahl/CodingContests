using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;

public partial class Board
{
    public static int Size;
    public static int ConnectorCost;
    public static int PipeCost;
    public static int SprinklerCost;
    public static int SprinklerRange;
    public static int Area => Size * Size;
    public static Cell[,] Grid;
    public static Cell[] Cells;
    public static Cell[] WalkableCells;

    public Board() { }

    private static Random random = new Random(0);
    private static Stopwatch sw;
    public void ReadInitial()
    {
        Size = int.Parse(Console.ReadLine().Split().Last());
        ConnectorCost = int.Parse(Console.ReadLine().Split().Last());
        PipeCost = int.Parse(Console.ReadLine().Split().Last());
        SprinklerCost = int.Parse(Console.ReadLine().Split().Last());
        SprinklerRange = int.Parse(Console.ReadLine().Split().Last());

        Grid = new Cell[Size, Size];
        int x = 0, y = 0;
        while (y < Size)
        {
            string line = Console.ReadLine();
            if (line.StartsWith("Grid")) line = Console.ReadLine();
            line = line.Replace(" ", "");
            foreach (char c in line)
            {
                Grid[x, y] = new Cell(x, y, c);

                x++;
                if (x == Size)
                {
                    x = 0;
                    y++;
                }
            }
        }
        sw = Stopwatch.StartNew();
        List<Cell> cells = new List<Cell>();
        foreach (Cell cell in Grid)
        {
            cells.Add(cell);
            cell.MakeNeighbors();
        }
        Cells = cells.OrderBy(c => c.ID).ToArray();

        int[] distToWater = BFS(Cells.Where(c => c.Water).ToList());
        WalkableCells = Cells.Where(c => distToWater[c.ID] < 1000000).ToArray();
        List<Cell> plants = Cells.Where(c => c.Plant).ToList();
        foreach (Cell cell in WalkableCells)
        {
            cell.Dist = BFS(new List<Cell> { cell });
            if (!cell.Water) cell.UpdateRange(plants);
        }
        EliminateSprinklers(plants);
    }



    private static void EliminateSprinklers(List<Cell> plants)
    {
        Node[] nodes = Cells.Select(c => new Node { Cell = c }).OrderBy(c => c.Cell.ID).ToArray();
        foreach (Node node in nodes) node.Neighbors = new HashSet<Node>(node.Cell.WalkableNeighbors.Select(n => nodes[n.ID]));
        List<Cell> water = Cells.Where(c => c.Water).ToList();
        foreach (Cell c in water) nodes[c.ID].Neighbors.UnionWith(water.Where(w => w != c).Select(w => nodes[w.ID]));
        ConnectedGroup.SearchBiconnected(nodes);
        bool updated = true;
        while (updated)
        {
            int elim = Cells.Count(c => c.Eliminated);
            foreach (ConnectedGroup g in ConnectedGroup.groups) g.Visited = false;
            Stack<ConnectedGroup> stack = new Stack<ConnectedGroup>();
            stack.Push(ConnectedGroup.groups.Last());
            stack.Peek().Visited = true;
            Stack<List<Cell>> path = new Stack<List<Cell>>();
            path.Push(new List<Cell>());
            while (stack.Count > 0)
            {
                ConnectedGroup q = stack.Pop();
                List<Cell> lastPath = path.Pop();
                foreach (var entry in q.Neighbors)
                {
                    foreach (ConnectedGroup n in entry.Value.Where(v => !v.Visited))
                    {
                        n.Visited = true;
                        stack.Push(n);
                        path.Push(lastPath.ToList());
                        Cell gate = entry.Key.Cell;
                        Cell inner = null, outer = null;
                        if (gate.Neighbors.Count(ne => n.Nodes.Any(no => no.Cell == ne)) == 1)
                            inner = gate.Neighbors.First(ne => n.Nodes.Any(no => no.Cell == ne));
                        if (q != ConnectedGroup.groups.Last() && gate.Neighbors.Count(ne => !n.Nodes.Any(no => no.Cell == ne)) == 1)
                            outer = gate.Neighbors.First(ne => !n.Nodes.Any(no => no.Cell == ne));

                        List<Cell> componentReachable = n.Nodes.SelectMany(node => node.Cell.CellRangeList).Distinct().ToList();
                        bool componentMandatory = componentReachable.Any(r => r.CellRangeList.All(a => n.Nodes.Select(node => node.Cell).Contains(a)));
                        foreach (Cell add in new[] { outer, gate, inner }.Where(a => a != null))
                        {
                            path.Peek().Add(add);
                            foreach (Node node in n.Nodes.Where(no => !path.Peek().Contains(no.Cell)))
                            {
                                if (node.Cell.CellRangeList.Count > 0 && path.Peek().Any(p => node.Cell.CellRangeList.All(plant => p.CellRangeList.Contains(plant))))
                                {
                                    node.Cell.Eliminated = true;
                                    Console.Error.WriteLine("eliminate by component: " + node.Cell);
                                    foreach (Cell plant2 in node.Cell.CellRangeList) plant2.CellRangeList.Remove(node.Cell);
                                    node.Cell.CellRangeList.Clear();
                                }
                            }
                            if (componentMandatory)
                            {
                                foreach (Cell wayPoint in path.Peek())
                                {
                                    foreach (Cell plant in wayPoint.CellRangeList.ToList())
                                    {
                                        foreach (Cell redundant in plant.CellRangeList.ToList())
                                        {
                                            if (path.Peek().Contains(redundant) && path.Peek().IndexOf(redundant) <= path.Peek().IndexOf(wayPoint) || redundant.CellRangeList.Any(p => !wayPoint.CellRangeList.Contains(p))) continue;
                                            foreach (Cell plant2 in redundant.CellRangeList) plant2.CellRangeList.Remove(redundant);
                                            redundant.CellRangeList.Clear();
                                            redundant.Eliminated = true;
                                            Console.Error.WriteLine("eliminate by component waypoint: " + redundant);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (Size > 15) // for larger boards: always cover all plants
            {
                foreach (Cell plant in plants)
                {
                    if (plant.CellRangeList.Count > 1) continue;
                    Cell sprinkler = plant.CellRangeList[0];
                    sprinkler.Mandatory = true;
                    foreach (Cell plant2 in sprinkler.CellRangeList.ToList())
                    {
                        foreach (Cell sprinkler2 in plant2.CellRangeList.ToList())
                        {
                            if (sprinkler == sprinkler2) continue;
                            plant2.CellRangeList.Remove(sprinkler2);
                            sprinkler2.CellRangeList.Remove(plant2);
                            if (sprinkler2.CellRangeList.Count == 0)
                            {
                                Console.Error.WriteLine("eliminate by mandatory: " + sprinkler2);
                                sprinkler2.Eliminated = true;
                            }
                        }
                    }
                }
            }
            updated = elim != Cells.Count(c => c.Eliminated);
        }
        foreach (Cell cell in Cells) cell.CellRange = cell.CellRangeList.ToArray();
        Console.Error.WriteLine("mandatory: " + Cells.Count(c => c.Mandatory) + "  eliminated: " + Cells.Count(c => c.Eliminated) + " @" + sw.ElapsedMilliseconds + " ms");
    }

    private static int randomSprinklerRange = 3;
    public List<string> Solve()
    {
        List<Cell> plants = Cells.Where(c => c.Plant).ToList();
        List<Cell> water = Cells.Where(c => c.Water).ToList();
        foreach (Cell c in water)
        {
            c.SetSprinkler(true); // init counter
            c.Sprinkler = false;
        }
        int bestScore = Area * plants.Count + ConnectPlants(plants.ToList());
        foreach (Cell cell in Cells) { cell.Backup(); cell.Backup2(); }

        int runs = 0;
        int maxMutate = 5 + (50 - Size) / 15;
        int stuck = 0;
        int runScore = bestScore;
        while (sw.ElapsedMilliseconds < Program.MAX_TIME)
        {
            runs++; stuck++;
            int currentScore = runScore;
            // remove some random sprinklers in close neighborhood
            List<Cell> sprinklers = WalkableCells.Where(c => c.Sprinkler).ToList();
            randomSprinklerRange = 3;
            if (sprinklers.Count < 100) randomSprinklerRange += (100 - sprinklers.Count) / 10;
            int removeCount = random.Next(1, Math.Min(1 + sprinklers.Count, maxMutate));
            List<Cell> nowDryPlants = new List<Cell>();
            if (sprinklers.Count >= removeCount)
            {
                Cell toRemove = sprinklers[random.Next(sprinklers.Count)];
                Cell center = toRemove;
                for (int i = 0; i < removeCount; i++)
                {
                    currentScore += RemoveSprinkler(toRemove, nowDryPlants);
                    sprinklers.Remove(toRemove);
                    if (sprinklers.Count > 0) toRemove = sprinklers.OrderBy(s => Math.Abs(s.X - center.X) + Math.Abs(s.Y - center.Y) + random.Next(3)).First();
                    if (Size < 25 && random.Next(3) == 0) center = toRemove;
                }
            }

            if (currentScore <= runScore && IsAllConnected(water)) // remove made it better
            {
                UpdateScores(ref bestScore, runs, ref stuck, ref runScore, currentScore);
            }

            // water plants that got dry now
            if (sprinklers.Count == 0) nowDryPlants = plants.ToList();
            List<Cell> missingPlants = plants.Where(p => p.SprinklersInRange == 0).ToList();
            int addPlants = random.Next(3);
            if (addPlants >= missingPlants.Count) nowDryPlants.AddRange(missingPlants);
            else
            {
                for (int i = 0; i < addPlants; i++) nowDryPlants.Add(missingPlants[random.Next(missingPlants.Count)]);
            }
            currentScore += ConnectPlants(nowDryPlants);

            // glue regions back together
            currentScore += ConnectRegions(water);

            // move sprinklers without changing pipes
            sprinklers = WalkableCells.Where(c => c.Sprinkler && !c.Mandatory).ToList();
            int moveCount = random.Next(1, Math.Min(1 + sprinklers.Count, maxMutate));
            List<Cell> toMove = new List<Cell>();
            if (sprinklers.Count >= moveCount)
            {
                Cell toRemove = sprinklers[random.Next(sprinklers.Count)];
                for (int i = 0; i < moveCount; i++)
                {
                    sprinklers.Remove(toRemove);
                    toMove.Add(toRemove);
                    if (sprinklers.Count > 0) toRemove = sprinklers.OrderBy(s => Math.Abs(s.X - toRemove.X) + Math.Abs(s.Y - toRemove.Y) + random.Next(3)).First();
                }
            }
            currentScore += MoveSprinklers(toMove);

            if (stuck == 15 * Size)
            {
                stuck = 0;
                runScore = currentScore;
                foreach (Cell cell in Cells) cell.Backup();
            }
            if (currentScore <= runScore)
            {
                UpdateScores(ref bestScore, runs, ref stuck, ref runScore, currentScore);
            }
            else
            {
                currentScore = runScore;
                foreach (Cell cell in Cells) cell.Restore();
            }
        }

        Console.Error.WriteLine("TESTCASE_INFO {\"Mutations\": " + runs + "}");
        Console.Error.WriteLine("runs: " + runs + "   score: " + bestScore);
        List<string> result = new List<string>();
        foreach (Cell cell in Cells)
        {
#if DEBUG
            if (cell.Mandatory) result.Add($"M {cell.Y} {cell.X}");
            if (cell.Eliminated) result.Add($"E {cell.Y} {cell.X}");
#endif
            cell.Restore2();
            foreach (Cell conn in cell.Connections.Where(c => c.ID < cell.ID))
            {
                result.Add($"P {cell.Y} {cell.X} {conn.Y} {conn.X}");
            }
        }
        foreach (Cell s in WalkableCells.Where(c => c.Sprinkler)) result.Add($"S {s.Y} {s.X}");

        return result;
    }

    private static void UpdateScores(ref int bestScore, int runs, ref int stuck, ref int runScore, int currentScore)
    {
        if (currentScore < runScore) stuck = 0;
        runScore = currentScore;
        foreach (Cell cell in Cells) cell.Backup();
        if (currentScore < bestScore)
        {
            Console.Error.WriteLine("runs: " + runs + "   score: " + currentScore);
            bestScore = currentScore;
            foreach (Cell cell in Cells) cell.Backup2();
        }
    }

    private static int[] visited = new int[2500];
    private static int visitCounter;
    private static bool IsAllConnected(List<Cell> water)
    {
        int readIndex = 0, writeIndex = 0; visitCounter++;
        foreach (Cell w in water) bfsQueue[writeIndex++] = w;
        foreach (Cell w in water) visited[w.ID] = visitCounter;
        while (readIndex < writeIndex)
        {
            Cell c = bfsQueue[readIndex++];
            foreach (Cell n in c.Connections)
            {
                if (visited[n.ID] == visitCounter) continue;
                visited[n.ID] = visitCounter;
                bfsQueue[writeIndex++] = n;
            }
        }
        return !WalkableCells.Any(c => visited[c.ID] != visitCounter && (c.Sprinkler || c.Connections.Count > 0));
    }

    private static int MoveSprinklers(List<Cell> oldSprinklers)
    {
        int deltaScore = 0;
        List<Cell> dry = new List<Cell>();
        foreach (Cell cell in oldSprinklers)
        {
            cell.SetSprinkler(false);
            deltaScore -= SprinklerCost;
            foreach (Cell c in cell.CellRange)
            {
                c.SprinklersInRange--;
                if (c.SprinklersInRange == 0)
                {
                    deltaScore += Area;
                    dry.Add(c);
                }
            }
            if (deltaScore < 0)
            {
                deltaScore += ClearPipes(oldSprinklers);
                return deltaScore;
            }
        }
        List<Cell> newSprinklers = new List<Cell>();
        foreach (Cell d in dry)
        {
            if (d.SprinklersInRange > 0) continue;
            List<Cell> candidates = d.CellRange.Where(c => c.Connections.Count > 0).ToList();
            Cell sp = candidates[random.Next(candidates.Count)]; // TODO priority to covering many?
            newSprinklers.Add(sp);
            sp.SetSprinkler(true);
            deltaScore += SprinklerCost;
            foreach (Cell c in sp.CellRange)
            {
                c.SprinklersInRange++;
                if (c.SprinklersInRange == 1) deltaScore -= Area;
            }
        }

        if (deltaScore > 0)
        {
            deltaScore = 0;
            foreach (Cell cell in newSprinklers)
            {
                cell.SetSprinkler(false);
                foreach (Cell c in cell.CellRange)
                {
                    c.SprinklersInRange--;
                }
            }
            foreach (Cell cell in oldSprinklers)
            {
                cell.SetSprinkler(true);
                foreach (Cell c in cell.CellRange)
                {
                    c.SprinklersInRange++;
                }
            }
        }
        else deltaScore += ClearPipes(oldSprinklers);

        return deltaScore;
    }

    private static int ClearPipes(List<Cell> sprinklers)
    {
        int deltaScore = 0;
        foreach (Cell s in sprinklers.Where(s => s.Connections.Count == 1 && !s.Sprinkler))
        {
            Cell cell = s;
            while (cell.Connections.Count == 1 && !cell.Water && !cell.Sprinkler)
            {
                Cell n = cell.Connections[0];
                deltaScore -= PipeCost;
                deltaScore -= cell.GetConnectorCost() + n.GetConnectorCost();
                n.Disconnect(cell);
                cell.Disconnect(n);
                deltaScore += cell.GetConnectorCost() + n.GetConnectorCost();
                cell = n;
            }
        }
        return deltaScore;
    }

    private static int ConnectRegions(List<Cell> water)
    {
        int readIndex = 0, writeIndex = 0; visitCounter++;
        foreach (Cell w in water) bfsQueue[writeIndex++] = w;
        foreach (Cell w in water) visited[w.ID] = visitCounter;
        List<Cell> connected = water.ToList();
        while (readIndex < writeIndex)
        {
            Cell c = bfsQueue[readIndex++];
            foreach (Cell n in c.Connections)
            {
                if (visited[n.ID] == visitCounter) continue;
                visited[n.ID] = visitCounter;
                connected.Add(n);
                bfsQueue[writeIndex++] = n;
            }
        }
        List<Cell> missing = WalkableCells.Where(c => visited[c.ID] != visitCounter && (c.Sprinkler || c.Connections.Count > 0)).ToList();
        if (missing.Count == 0) return 0;
        int[] dist = BFS(connected);
        int deltaScore = 0;
        while (missing.Count > 0)
        {
            Cell current = missing.OrderBy(s => dist[s.ID] + random.Next(4)).First();
            Cell attach = current;
            connected.Clear();
            while (dist[current.ID] > 0)
            {
                Cell prev = current.WalkableNeighbors
                    .Where(n => dist[n.ID] == dist[current.ID] - 1)
                    .OrderBy(n => current.ExpectedConnectionCost(n) + random.NextDouble())
                    .First();
                if (prev.Connections.Count > 0 && visited[prev.ID] != visitCounter)
                {
                    current = prev;
                    attach = prev;
                    continue;
                }
                deltaScore += PipeCost;
                deltaScore -= current.GetConnectorCost() + prev.GetConnectorCost();
                current.Connect(prev);
                deltaScore += current.GetConnectorCost() + prev.GetConnectorCost();
                current = prev;
            }

            connected.Add(attach);
            readIndex = 0; writeIndex = 1;
            bfsQueue[0] = attach;
            visited[attach.ID] = visitCounter;
            while (readIndex < writeIndex)
            {
                Cell c = bfsQueue[readIndex++];
                foreach (Cell n in c.Connections)
                {
                    if (visited[n.ID] == visitCounter) continue;
                    visited[n.ID] = visitCounter;
                    bfsQueue[writeIndex++] = n;
                    connected.Add(n);
                }
            }
            missing = missing.Where(c => visited[c.ID] != visitCounter).ToList();
            if (missing.Count > 0) IncrementalBFS(dist, connected);
        }
        return deltaScore;
    }

    private static int bonusRange = 4;
    private static int rangeIncrease = 0;
    private static int rangeUsed = 0;
    private static int ConnectPlants(List<Cell> plants)
    {
        if (plants.Count == 0) return 0;
        if (rangeIncrease > 3 && rangeUsed < 50 * rangeIncrease)
        {
            bonusRange++;
            rangeUsed = 0;
            rangeIncrease = 0;
            Console.Error.WriteLine("increase BFS range to " + bonusRange);
        }
        int minX = Math.Max(0, plants.Min(p => p.X) - bonusRange);
        int maxX = Math.Min(Size - 1, plants.Max(p => p.X) + bonusRange);
        int minY = Math.Max(0, plants.Min(p => p.Y) - bonusRange);
        int maxY = Math.Min(Size - 1, plants.Max(p => p.Y) + bonusRange);
        List<Cell> connected = new List<Cell>();
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (Grid[x, y].Water || Grid[x, y].Connections.Count > 0) connected.Add(Grid[x, y]);
            }
        }
        int deltaScore = 0;
        List<Cell> sprinklers = new List<Cell>();
        int[] dist = BFS(connected, minX, maxX, minY, maxY);
        int[] distBackup = dist.ToArray();
        foreach (Cell plant in plants.Where(p => p.CellRange.Length == 1).ToList())
        {
            plants.Remove(plant);
            if (plant.SprinklersInRange > 0) continue;
            Cell target = plant.CellRange[0];
            IncrementalBFS(dist, new List<Cell> { target }, minX, maxX, minY, maxY);
            sprinklers.Add(target);
            target.SetSprinkler(true);
            foreach (Cell c in target.CellRange)
            {
                c.SprinklersInRange++;
                if (c.SprinklersInRange == 1) deltaScore -= Area;
            }
        }
        for (int k = 0; k < plants.Count; k++)
        {
            int j = random.Next(k, plants.Count);
            Cell plant = plants[j];
            plants[j] = plants[k];
            if (plant.SprinklersInRange > 0) continue;
            Cell target = plant.CellRange.OrderBy(s => -s.MissingCount() + dist[s.ID] + (s.SprinklersInLine > 0 ? 0 : 2) + randomSprinklerRange * random.NextDouble()).First();
            IncrementalBFS(dist, new List<Cell> { target }, minX, maxX, minY, maxY);
            sprinklers.Add(target);
            target.SetSprinkler(true);
            foreach (Cell c in target.CellRange)
            {
                c.SprinklersInRange++;
                if (c.SprinklersInRange == 1) deltaScore -= Area;
            }
        }

        switch (random.Next(8))
        {
            case 0: sprinklers = sprinklers.OrderBy(s => s.X).ThenBy(s => s.Y).ToList(); break;
            case 1: sprinklers = sprinklers.OrderBy(s => s.X).ThenBy(s => -s.Y).ToList(); break;
            case 2: sprinklers = sprinklers.OrderBy(s => -s.X).ThenBy(s => s.Y).ToList(); break;
            case 3: sprinklers = sprinklers.OrderBy(s => -s.X).ThenBy(s => -s.Y).ToList(); break;
            case 4: sprinklers = sprinklers.OrderBy(s => s.Y).ThenBy(s => s.X).ToList(); break;
            case 5: sprinklers = sprinklers.OrderBy(s => s.Y).ThenBy(s => -s.X).ToList(); break;
            case 6: sprinklers = sprinklers.OrderBy(s => -s.Y).ThenBy(s => s.X).ToList(); break;
            case 7: sprinklers = sprinklers.OrderBy(s => -s.Y).ThenBy(s => -s.X).ToList(); break;
        }
        foreach (Cell s in sprinklers.ToList())
        {
            if (s.CellRange.All(p => p.SprinklersInRange > 1))
            {
                sprinklers.Remove(s);
                s.SetSprinkler(false);
                foreach (Cell p in s.CellRange)
                {
                    p.SprinklersInRange--;
                }
            }
        }

        dist = distBackup;
        visitCounter++;
        while (sprinklers.Count > 0)
        {
            Cell sprinkler = sprinklers.OrderBy(s => dist[s.ID] + random.Next(4)).First();
            sprinklers.Remove(sprinkler);
            foreach (Cell plant in sprinkler.CellRange)
            {
                if (visited[plant.ID] == visitCounter) continue;
                visited[plant.ID] = visitCounter;
                foreach (Cell s2 in plant.CellRange)
                {
                    if (!s2.Sprinkler || visited[s2.ID] == visitCounter) continue;
                    visited[s2.ID] = visitCounter;
                    if (s2.CellRange.All(p2 => p2.SprinklersInRange > 1))
                    {
                        deltaScore += RemoveSprinkler(s2, null);
                        dist = BFS(WalkableCells.Where(c => c.Water || c.Connections.Count > 0).ToList(), minX, maxX, minY, maxY);
                    }
                }
            }
            if (dist[sprinkler.ID] == 1000000)
            {
                minX = 0; maxX = Size - 1; minY = 0; maxY = Size - 1;
                rangeIncrease++;
                dist = BFS(WalkableCells.Where(c => c.Water || c.Connections.Count > 0).ToList(), minX, maxX, minY, maxY);
            }
            Cell current = sprinkler;
            connected.Clear();
            if (dist[current.ID] > 0) connected.Add(current);
            deltaScore += SprinklerCost;
            while (dist[current.ID] > 0)
            {
                Cell prev = current.WalkableNeighbors
                    .Where(n => dist[n.ID] == dist[current.ID] - 1)
                    .OrderBy(n => current.ExpectedConnectionCost(n) + random.NextDouble())
                    .First();
                deltaScore += PipeCost;
                deltaScore -= current.GetConnectorCost() + prev.GetConnectorCost();
                current.Connect(prev);
                deltaScore += current.GetConnectorCost() + prev.GetConnectorCost();
                current = prev;
                connected.Add(current);
            }
            IncrementalBFS(dist, connected, minX, maxX, minY, maxY);
        }
        return deltaScore;
    }

    private static int RemoveSprinkler(Cell sprinkler, List<Cell> nowDryPlants)
    {
        int deltaScore = -SprinklerCost;
        sprinkler.SetSprinkler(false);
        foreach (Cell p in sprinkler.CellRange)
        {
            p.SprinklersInRange--;
            if (p.SprinklersInRange == 0)
            {
                if (nowDryPlants != null) nowDryPlants.Add(p);
                deltaScore += Area;
            }
        }
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(sprinkler);
        while (queue.Count > 0)
        {
            Cell q = queue.Dequeue();
            foreach (Cell n in q.Connections)
            {
                if (!n.Connections.Contains(q)) continue;
                deltaScore -= PipeCost;
                if (n.Sprinkler || n.Water)
                {
                    deltaScore -= n.GetConnectorCost();
                    n.Disconnect(q);
                    deltaScore += n.GetConnectorCost();
                }
                else queue.Enqueue(n);
            }
            deltaScore -= q.GetConnectorCost();
            q.Disconnect();
        }
        return deltaScore;
    }

    private static Cell[] bfsQueue = new Cell[2500];
    public static int[] BFS(List<Cell> start) => BFS(start, 0, Size - 1, 0, Size - 1);
    public static int[] BFS(List<Cell> start, int minX, int maxX, int minY, int maxY)
    {
        if (start.Count == 1 && start[0].Dist != null && minX == 0 && minY == 0 && maxX == Size - 1 && maxY == Size - 1) return start[0].Dist.ToArray();
        int[] dist = new int[Area];
        for (int i = 0; i < dist.Length; i++) dist[i] = 1000000;
        int readIndex = 0, writeIndex = 0;
        foreach (Cell s in start)
        {
            if (s.X < minX || s.X > maxX || s.Y < minY || s.Y > maxY) continue;
            dist[s.ID] = 0;
            bfsQueue[writeIndex++] = s;
        }
        while (readIndex < writeIndex)
        {
            Cell c = bfsQueue[readIndex++];
            foreach (Cell n in c.WalkableNeighbors)
            {
                if (dist[n.ID] != 1000000) continue;
                if (n.X < minX || n.X > maxX || n.Y < minY || n.Y > maxY) continue;
                dist[n.ID] = dist[c.ID] + 1;
                bfsQueue[writeIndex++] = n;
            }
        }
        return dist;
    }

    public static void IncrementalBFS(int[] dist, List<Cell> start) => IncrementalBFS(dist, start, 0, Size - 1, 0, Size - 1);
    private static void IncrementalBFS(int[] dist, List<Cell> start, int minX, int maxX, int minY, int maxY)
    {
        int readIndex = 0, writeIndex = 0;
        foreach (Cell s in start)
        {
            if (s.X < minX || s.X > maxX || s.Y < minY || s.Y > maxY) continue;
            dist[s.ID] = 0;
            bfsQueue[writeIndex++] = s;
        }
        while (readIndex < writeIndex)
        {
            Cell c = bfsQueue[readIndex++];
            foreach (Cell n in c.WalkableNeighbors)
            {
                if (dist[n.ID] <= dist[c.ID] + 1) continue;
                if (n.X < minX || n.X > maxX || n.Y < minY || n.Y > maxY) continue;
                dist[n.ID] = dist[c.ID] + 1;
                bfsQueue[writeIndex++] = n;
            }
        }
    }
}
