using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

public class Board
{
    public static int Size;
    public static int BridgeCount;
    public int[,] Grid;
    public Board()
    {
        Size = int.Parse(Console.ReadLine());
        BridgeCount = int.Parse(Console.ReadLine());

        Queue<int> nums = new Queue<int>();
        Grid = new int[Size, Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (nums.Count == 0)
                {
                    foreach (int n in Console.ReadLine().Trim().Split().Select(int.Parse)) nums.Enqueue(n);
                }
                Grid[x, y] = nums.Dequeue();
            }
        }
    }

    private static Random random = new Random(0);
    static int[] dx = { 0, 1, 0, -1 };
    static int[] dy = { 1, 0, -1, 0 };
    List<Component>[] BridgeUses = Enumerable.Range(0, 1800).Select(i => new List<Component>()).ToArray();
    List<int>[] BridgeBuddies = Enumerable.Range(0, 1800).Select(i => new List<int>()).ToArray();
    Bridge[] BridgeDummies = new Bridge[1800];
    public List<Bridge> Solve()
    {
        Stopwatch sw = Stopwatch.StartNew();
        List<Point> islands = new List<Point>();
        partial = new HashSet<Component>[Size, Size, 4 * BridgeCount + 1];
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int i = 0; i <= 4 * BridgeCount; i++) partial[x, y, i] = new HashSet<Component>();
                if (Grid[x, y] > 0)
                {
                    islands.Add(new Point(x, y));
                    for (int dir = 0; dir < 2; dir++)
                    {
                        int x_ = x + dx[dir];
                        int y_ = y + dy[dir];
                        while (x_ < Size && y_ < Size && Grid[x_, y_] == 0)
                        {
                            x_ = x_ + dx[dir];
                            y_ = y_ + dy[dir];
                        }
                        if (x_ < Size && y_ < Size && Grid[x_, y_] != 0)
                        {
                            Bridge bridge = new Bridge(new Point(x, y), new Point(x_, y_), 0);
                            BridgeDummies[bridge.Index] = bridge;
                        }
                    }
                }
            }
        }
        foreach (Bridge bridge in BridgeDummies)
        {
            if (bridge.From.Equals(bridge.To)) continue;
            int offset = bridge.From.X == bridge.To.X ? 1 : 0;
            for (int range = 1; ; range++)
            {
                Bridge test = new Bridge(new Point(bridge.From.X + dx[offset] * range, bridge.From.Y + dy[offset] * range),
                                         new Point(bridge.To.X + dx[offset] * range, bridge.To.Y + dy[offset] * range), 0);
                if (test.From.X < 0 || test.From.X >= Size || test.From.Y < 0 || test.From.Y >= Size ||
                    test.To.X < 0 || test.To.X >= Size || test.To.Y < 0 || test.To.Y >= Size) break;
                if (Grid[test.From.X, test.From.Y] != 0 && Grid[test.To.X, test.To.Y] != 0 && BridgeDummies[test.Index].Equals(test))
                {
                    BridgeBuddies[bridge.Index].Add(test.Index);
                    BridgeBuddies[test.Index].Add(bridge.Index);
                    break;
                }
                if ((Grid[test.From.X, test.From.Y] != 0) || (Grid[test.To.X, test.To.Y] != 0)) break;
            }
        }

        int[,] initial = (int[,])Grid.Clone();
        HashSet<Component> components = BuildComponents(islands, 4000, sw);
        Console.Error.WriteLine("components: " + components.Count + "  in " + sw.ElapsedMilliseconds);

        MergeComponents(components, sw, 5000, true);
        Console.Error.WriteLine("combined large components: " + components.Count + "  in " + sw.ElapsedMilliseconds);
        MergeComponents(components, sw, 5000);
        Console.Error.WriteLine("combined components: " + components.Count + "  in " + sw.ElapsedMilliseconds);
        foreach (Component c in components)
        {
            foreach (Bridge b in c.Bridges) BridgeUses[b.Index].Add(c);
        }

        Console.Error.WriteLine("grouped bridges: " + components.Count + "  in " + sw.ElapsedMilliseconds);

        int bestScore = 0;
        List<Component> bestComponents = new List<Component>();
        List<Component> ordered = components.OrderByDescending(c => c.Score - 0.00001 * c.Covered.Count).ToList();
        int startIndex = 0;
        int runs = 0;
        int buildTime = 9500;
        while (sw.ElapsedMilliseconds < buildTime)
        {
            runs++;
            List<Component> currentComponents = FillBoard(ordered, islands, ordered[startIndex], buildTime, sw, runs);
            int currentScore = currentComponents.Sum(c => c.Score);
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                bestComponents = currentComponents;
            }
            // Console.Error.WriteLine(currentScore + " => " + bestScore + " @" + sw.ElapsedMilliseconds);
            startIndex = random.Next(ordered.Count);
            if (runs > 100) FisherYates(ordered);
        }
        Console.Error.WriteLine("final score: " + bestScore + " with " + runs + " runs  in " + sw.ElapsedMilliseconds);
        Console.Error.WriteLine("bridges: " + bestComponents.Sum(b => b.Bridges.Count));

        return bestComponents.SelectMany(c => c.Bridges).ToList();
    }

    private List<Component> FillBoard(List<Component> components, List<Point> islands, Component starting, int buildTime, Stopwatch sw, int run)
    {
        List<Component> result = new List<Component>();
        bool[,] visited = new bool[Size, Size];
        starting = ExtendComponent(starting, islands, visited, buildTime, sw, run);
        result.Add(starting);
        foreach (Component comp in components)
        {
            bool canAdd = true;
            foreach (Point p in comp.Covered)
            {
                if (visited[p.X, p.Y])
                {
                    canAdd = false;
                    break;
                }
            }
            if (!canAdd) continue;
            Component extended = ExtendComponent(comp, islands, visited, buildTime, sw, run);
            result.Add(extended);
        }
        return result;
    }

    private List<T> FisherYates<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int index = random.Next(i, list.Count);
            T swap = list[index];
            list[index] = list[i];
            list[i] = swap;
        }
        return list;
    }

    private Component ExtendComponent(Component component, List<Point> islands, bool[,] visited, int buildTime, Stopwatch sw, int run)
    {
        foreach (Point p in component.Covered) visited[p.X, p.Y] = true;

        int failCount = 0;
        HashSet<Point> failedDeadEnd = new HashSet<Point>();
        HashSet<Point> failedChainDeadend = new HashSet<Point>();
        HashSet<Bridge> failedBuddyBridge = new HashSet<Bridge>();
        HashSet<Bridge> failedSingleBridge = new HashSet<Bridge>();
        HashSet<Bridge> failedChainBridge = new HashSet<Bridge>();
        bool doSingleBridge = run < 100 || random.Next(2) == 0;
        bool doMultiBridge = run < 100 || random.Next(2) == 0;
        bool doBuddyBridge = run < 100 || random.Next(2) == 0;
        bool doChainBridge = run < 100 || random.Next(2) == 0;
        bool doDeadEnd = run < 100 || random.Next(2) == 0;
        bool doDeadEndChain = run < 100 || random.Next(2) == 0;
        while (failCount < 2)
        {
            if (sw.ElapsedMilliseconds > buildTime) break;
            if (failCount == 1)
            {
                failedDeadEnd.Clear();
                failedDeadEnd.Clear();
                failedBuddyBridge.Clear();
                failedSingleBridge.Clear();
                failedChainBridge.Clear();
            }
            failCount++;

            Dictionary<int, List<Component>> filteredPartial = new Dictionary<int, List<Component>>();
            List<Bridge> bridges = FisherYates(component.Bridges.Where(b => b.Count > 1).ToList());
            foreach (Bridge bridge in bridges)
            {
                if (!doSingleBridge) break;
                if (failedSingleBridge.Contains(bridge)) continue;
                for (int removeCount = 1; removeCount < bridge.Count; removeCount++)
                {
                    if (!filteredPartial.ContainsKey(bridge.From.X * 10000 + bridge.From.Y * 100 + removeCount)) filteredPartial[bridge.From.X * 10000 + bridge.From.Y * 100 + removeCount] = partial[bridge.From.X, bridge.From.Y, removeCount].Where(p => !p.Covered.Any(cov => visited[cov.X, cov.Y])).OrderByDescending(p => p.IslandCovered.Count).ToList();
                    if (!filteredPartial.ContainsKey(bridge.To.X * 10000 + bridge.To.Y * 100 + removeCount)) filteredPartial[bridge.To.X * 10000 + bridge.To.Y * 100 + removeCount] = partial[bridge.To.X, bridge.To.Y, removeCount].Where(p => !p.Covered.Any(cov => visited[cov.X, cov.Y])).OrderByDescending(p => p.IslandCovered.Count).ToList();
                    List<Component> p1 = filteredPartial[bridge.From.X * 10000 + bridge.From.Y * 100 + removeCount];
                    List<Component> p2 = filteredPartial[bridge.To.X * 10000 + bridge.To.Y * 100 + removeCount];
                    foreach (Component c1 in p1)
                    {
                        if (sw.ElapsedMilliseconds > buildTime) break;
                        foreach (Component c2 in p2)
                        {
                            if (!c1.CheckParallel(c2)) continue;
                            Component combined = new Component();
                            combined.Bridges.AddRange(component.Bridges);
                            combined.Bridges.Remove(bridge);
                            combined.Bridges.Add(new Bridge(bridge.From, bridge.To, bridge.Count - removeCount));
                            combined.Bridges.AddRange(c1.Bridges);
                            combined.Bridges.AddRange(c2.Bridges);
                            combined.FinalizeComponent(Grid);
                            component = combined;
                            failCount = 0;
                            foreach (Point p in c1.Covered) visited[p.X, p.Y] = true;
                            foreach (Point p in c2.Covered) visited[p.X, p.Y] = true;
                            goto repeatExpand;
                        }
                    }
                }
                failedSingleBridge.Add(bridge);
            }

            bridges = FisherYates(component.Bridges.Where(b => b.Count < BridgeCount).ToList());
            Dictionary<Point, List<Bridge>> freq = new Dictionary<Point, List<Bridge>>();
            foreach (Bridge b in component.Bridges)
            {
                if (!freq.ContainsKey(b.From)) freq[b.From] = new List<Bridge>();
                freq[b.From].Add(b);
                if (!freq.ContainsKey(b.To)) freq[b.To] = new List<Bridge>();
                freq[b.To].Add(b);
            }
            foreach (Bridge bridge in bridges)
            {
                if (!doChainBridge) break;
                if (failedChainBridge.Contains(bridge)) continue;
                for (int removeCount = 1; removeCount + bridge.Count <= BridgeCount; removeCount++)
                {
                    // find neighboring bridges
                    List<Bridge> froms = freq[bridge.From].Where(b => b.Count > removeCount && !b.Equals(bridge)).ToList();
                    List<Bridge> tos = freq[bridge.To].Where(b => b.Count > removeCount && !b.Equals(bridge)).ToList();
                    foreach (Bridge from in froms)
                    {
                        foreach (Bridge to in tos)
                        {
                            Point _from = from.Partner(bridge.From);
                            Point _to = to.Partner(bridge.To);
                            if (!filteredPartial.ContainsKey(_from.X * 10000 + _from.Y * 100 + removeCount)) filteredPartial[_from.X * 10000 + _from.Y * 100 + removeCount] = partial[_from.X, _from.Y, removeCount].Where(p => !p.Covered.Any(cov => visited[cov.X, cov.Y])).OrderByDescending(p => p.IslandCovered.Count).ToList();
                            if (!filteredPartial.ContainsKey(_to.X * 10000 + _to.Y * 100 + removeCount)) filteredPartial[_to.X * 10000 + _to.Y * 100 + removeCount] = partial[_to.X, _to.Y, removeCount].Where(p => !p.Covered.Any(cov => visited[cov.X, cov.Y])).OrderByDescending(p => p.IslandCovered.Count).ToList();
                            List<Component> p1 = filteredPartial[_from.X * 10000 + _from.Y * 100 + removeCount];
                            List<Component> p2 = filteredPartial[_to.X * 10000 + _to.Y * 100 + removeCount];
                            foreach (Component c1 in p1)
                            {
                                if (sw.ElapsedMilliseconds > buildTime) break;
                                foreach (Component c2 in p2)
                                {
                                    if (!c1.CheckParallel(c2)) continue;
                                    Component combined = new Component();
                                    combined.Bridges.AddRange(component.Bridges);
                                    combined.Bridges.Remove(bridge);
                                    combined.Bridges.Remove(from);
                                    combined.Bridges.Remove(to);
                                    combined.Bridges.Add(new Bridge(bridge.From, bridge.To, bridge.Count + removeCount));
                                    combined.Bridges.Add(new Bridge(from.From, from.To, from.Count - removeCount));
                                    combined.Bridges.Add(new Bridge(to.From, to.To, to.Count - removeCount));
                                    combined.Bridges.AddRange(c1.Bridges);
                                    combined.Bridges.AddRange(c2.Bridges);
                                    combined.FinalizeComponent(Grid);
                                    component = combined;
                                    failCount = 0;
                                    foreach (Point p in c1.Covered) visited[p.X, p.Y] = true;
                                    foreach (Point p in c2.Covered) visited[p.X, p.Y] = true;
                                    goto repeatExpand;
                                }
                            }
                        }
                    }
                }
                failedChainBridge.Add(bridge);
            }

            bridges = FisherYates(component.Bridges.ToList());
            foreach (Bridge bridge in bridges)
            {
                if (sw.ElapsedMilliseconds > buildTime) break;
                if (!doBuddyBridge) break;
                if (failedBuddyBridge.Contains(bridge)) continue;
                foreach (int buddy in BridgeBuddies[bridge.Index])
                {
                    foreach (Component c in BridgeUses[buddy].OrderByDescending(b => b.IslandCovered.Count))
                    {
                        if (c.Covered.Any(cov => visited[cov.X, cov.Y])) continue;
                        Bridge partner = c.Bridges.First(br => br.Index == buddy);
                        if (bridge.Count + partner.Count < 3) continue;
                        Bridge b1 = new Bridge(bridge.From, partner.From, 1);
                        Bridge b2 = new Bridge(bridge.To, partner.To, 1);
                        if (b1.Middle().Any(p => visited[p.X, p.Y] || c.Covered.Contains(p))) continue;
                        if (b2.Middle().Any(p => visited[p.X, p.Y] || c.Covered.Contains(p))) continue;

                        Component combined = new Component();
                        combined.Bridges.AddRange(component.Bridges);
                        combined.Bridges.AddRange(c.Bridges);
                        combined.Bridges.Remove(bridge);
                        combined.Bridges.Remove(partner);
                        if (bridge.Count > 1) combined.Bridges.Add(new Bridge(bridge.From, bridge.To, bridge.Count - 1));
                        if (partner.Count > 1) combined.Bridges.Add(new Bridge(partner.From, partner.To, partner.Count - 1));
                        combined.Bridges.Add(b1);
                        combined.Bridges.Add(b2);
                        combined.FinalizeComponent(Grid);
                        component = combined;
                        failCount = 0;
                        foreach (Point p in b1.Middle()) visited[p.X, p.Y] = true;
                        foreach (Point p in b2.Middle()) visited[p.X, p.Y] = true;
                        foreach (Point p in c.Covered) visited[p.X, p.Y] = true;
                        goto repeatExpand;
                    }
                }
                failedBuddyBridge.Add(bridge);
            }

            List<Point> deadEnds = freq.Keys.Where(p => freq[p].Count == 1).ToList();
            foreach (Point p in deadEnds)
            {
                if (sw.ElapsedMilliseconds > buildTime) break;
                if (!doDeadEnd) break;
                if (failedDeadEnd.Contains(p)) continue;
                HashSet<Point> endCovered = new HashSet<Point> { p };
                Bridge bridge = freq[p][0];
                foreach (Point m in bridge.Middle()) endCovered.Add(m);
                List<Bridge> bridgeCovered = new List<Bridge> { bridge };
                Point q = bridge.Partner(p);
                for (int remove = 1; remove <= 5; remove++)
                {
                    foreach (Component part in partial[q.X, q.Y, bridge.Count])
                    {
                        if (part.IslandCovered.Count <= remove) continue;
                        if (part.Covered.Any(cov => visited[cov.X, cov.Y] && !endCovered.Contains(cov))) continue;
                        foreach (Point v in endCovered) visited[v.X, v.Y] = false;
                        Component combined = new Component();
                        combined.Bridges.AddRange(component.Bridges);
                        combined.Bridges.AddRange(part.Bridges);
                        foreach (Bridge b in bridgeCovered) combined.Bridges.Remove(b);
                        combined.FinalizeComponent(Grid);
                        //Console.Error.WriteLine(component.IslandCovered.Count + " => " + combined.IslandCovered.Count + " @" + remove);
                        component = combined;
                        failCount = 0;
                        foreach (Point m in part.Covered) visited[m.X, m.Y] = true;
                        goto repeatExpand;
                    }
                    if (freq[q].Count != 2) break;
                    bridge = freq[q][0].Equals(bridge) ? freq[q][1] : freq[q][0];
                    endCovered.Add(q);
                    q = bridge.Partner(q);
                    foreach (Point m in bridge.Middle()) endCovered.Add(m);
                    bridgeCovered.Add(bridge);
                }
                failedDeadEnd.Add(p);
            }

            // foreach (Point p in component.IslandCovered)
            // {
            //     for (int power = 1; power < BridgeCount; power++)
            //     {
            //         if (!filteredPartial.ContainsKey(p.X * 10000 + p.Y * 100 + power)) filteredPartial[p.X * 10000 + p.Y * 100 + power] = partial[p.X, p.Y, power].Where(p => !p.Covered.Any(cov => visited[cov.X, cov.Y])).OrderByDescending(p => p.IslandCovered.Count).ToList();
            //         if (filteredPartial[p.X * 10000 + p.Y * 100 + power].Count == 0) continue;
            //         bool[,] visitedClone = (bool[,])visited.Clone();
            //         Component combined = Chain(new List<Bridge>(), new HashSet<int>(), p, false, p, power, freq, filteredPartial, visitedClone, component);
            //         if (combined != null)
            //         {
            //             foreach (Component starter in filteredPartial[p.X * 10000 + p.Y * 100 + power])
            //             {
            //                 foreach (Point m in starter.Covered) visited[m.X, m.Y] = true;
            //                 combined = Chain(new List<Bridge>(), new HashSet<int>(), p, false, p, power, freq, filteredPartial, visited, component);
            //                 if (combined != null)
            //                 {
            //                     combined.Bridges.AddRange(starter.Bridges);
            //                     combined.FinalizeComponent(Grid);
            //                     combined.Validate(Grid,visited);
            //                     component = combined;
            //                     failCount = 0;
            //                     goto repeatExpand;
            //                 }
            //                 foreach (Point m in starter.Covered) visited[m.X, m.Y] = false;
            //             }
            //         }
            //     }
            // }

            foreach (Point p in islands.Where(p => Grid[p.X, p.Y] < BridgeCount))
            {
                if (!doDeadEndChain) break;
                if (failedChainDeadend.Contains(p)) continue;
                if (sw.ElapsedMilliseconds > buildTime) break;
                Component combined = null;
                bool[,] initialVisited = (bool[,])visited.Clone();
                if (component.IslandCovered.Contains(p))
                {
                    if (freq[p].Count > 1) continue;
                    Bridge bridge = freq[p][0];
                    HashSet<int> chainVisited = new HashSet<int> { bridge.Index };
                    combined = Chain(new List<Bridge> { bridge }, chainVisited, bridge.Partner(p), true, p, Grid[p.X, p.Y], freq, filteredPartial, visited, component);
                }
                else
                {
                    freq[p] = new List<Bridge>();
                    combined = Chain(new List<Bridge> { }, new HashSet<int>(), p, false, p, Grid[p.X, p.Y], freq, filteredPartial, visited, component);
                }
                if (combined != null)
                {
                    //Console.Error.WriteLine(component.IslandCovered.Count + " => " + combined.IslandCovered.Count);
                    component = combined;
                    failCount = 0;
                    goto repeatExpand;
                }
                failedChainDeadend.Add(p);
            }

        repeatExpand:;
        }

        return component;
    }

    private Component Chain(List<Bridge> bridges, HashSet<int> chainVisited, Point p, bool add, Point start, int power, Dictionary<Point, List<Bridge>> freq, Dictionary<int, List<Component>> filteredPartial, bool[,] visited, Component component)
    {
        if (add)
        {
            if (!filteredPartial.ContainsKey(p.X * 10000 + p.Y * 100 + power)) filteredPartial[p.X * 10000 + p.Y * 100 + power] = partial[p.X, p.Y, power].Where(pa => !pa.Covered.Any(cov => visited[cov.X, cov.Y])).OrderByDescending(pa => pa.IslandCovered.Count).ToList();
            foreach (Component part in filteredPartial[p.X * 10000 + p.Y * 100 + power])
            {
                if (part.IslandCovered.Count > 1 && !part.Covered.Any(pa => visited[pa.X, pa.Y]))
                {
                    Component combined = new Component();
                    combined.Bridges.AddRange(component.Bridges);
                    combined.Bridges.AddRange(part.Bridges);
                    for (int i = bridges.Count - 1; i >= 0; i--)
                    {
                        Bridge b = bridges[i];
                        combined.Bridges.Remove(b);
                        if (!add)
                        {
                            combined.Bridges.Add(new Bridge(b.From, b.To, b.Count + power));
                        }
                        else
                        {
                            if (b.Count > power) combined.Bridges.Add(new Bridge(b.From, b.To, b.Count - power));
                            else foreach (Point m in b.Middle()) visited[m.X, m.Y] = false;
                        }
                        add = !add;
                    }
                    combined.FinalizeComponent(Grid);
                    foreach (Point m in part.Covered) visited[m.X, m.Y] = true;
                    visited[start.X, start.Y] = bridges[0].Count == 0;
                    return combined;
                }
            }
        }

        // build new bridges
        if (add)
        {
            for (int dir = 0; dir < 4; dir++)
            {
                for (int i = 1; ; i++)
                {
                    int x = p.X + i * dx[dir];
                    int y = p.Y + i * dy[dir];
                    if (x < 0 || x >= Size || y < 0 || y >= Size) break;
                    if (Grid[x, y] == 0 && visited[x, y]) break;
                    if (Grid[x, y] != 0)
                    {
                        Point q_ = new Point(x, y);
                        if (!freq.ContainsKey(q_)) break;
                        Bridge bridge = new Bridge(p, q_, 0);
                        if (i == 1) break;
                        int index_ = bridge.Index;
                        if (chainVisited.Contains(index_)) break;
                        chainVisited.Add(index_);
                        bridges.Add(bridge);
                        foreach (Point m in bridge.Middle()) visited[m.X, m.Y] = true;
                        Component combined_ = Chain(bridges, chainVisited, q_, !add, start, power, freq, filteredPartial, visited, component);
                        if (combined_ != null) return combined_;
                        bridges.RemoveAt(bridges.Count - 1);
                        foreach (Point m in bridge.Middle()) visited[m.X, m.Y] = false;
                        break;
                    }
                }
            }
        }

        // modify existing bridges
        foreach (Bridge b in freq[p])
        {
            if (add && b.Count + power > BridgeCount) continue;
            if (!add && b.Count <= power) continue;
            int index = b.Index;
            if (chainVisited.Contains(index)) continue;
            chainVisited.Add(index);
            Point q = b.Partner(p);
            bridges.Add(b);
            Component combined = Chain(bridges, chainVisited, q, !add, start, power, freq, filteredPartial, visited, component);
            if (combined != null) return combined;
            bridges.RemoveAt(bridges.Count - 1);
            // chainVisited.Remove(index);
        }
        return null;
    }

    private void MergeComponents(HashSet<Component> components, Stopwatch sw, int timeLimit, bool bySize = false)
    {
        List<int[]> tasks = new List<int[]>();
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int i = 1; i <= Grid[x, y] / 2; i++) tasks.Add(new int[] { x, y, i });
            }
        }

        for (int i = 0; i < tasks.Count && sw.ElapsedMilliseconds < timeLimit; i++)
        {
            int index = random.Next(i, tasks.Count);
            int[] swap = tasks[index];
            tasks[index] = tasks[i];
            tasks[i] = swap;
            int x = swap[0], y = swap[1], v = swap[2];
            HashSet<Component> p1 = partial[x, y, v];
            HashSet<Component> p2 = partial[x, y, Grid[x, y] - v];
            if (bySize)
            {
                p1 = new HashSet<Component>(p1.OrderByDescending(c => c.IslandCovered.Count).Take(Math.Min(20, p1.Count)));
                p2 = new HashSet<Component>(p2.OrderByDescending(c => c.IslandCovered.Count).Take(Math.Min(20, p2.Count)));
            }
            foreach (Component c1 in p1)
            {
                if (sw.ElapsedMilliseconds > timeLimit) break;
                List<Point> usedNeighbors = new List<Point>();
                for (int dir = 0; dir < 4; dir++)
                {
                    Point p = new Point(x + dx[dir], y + dy[dir]);
                    if (p.X >= 0 && p.X < Size && p.Y >= 0 && p.Y < Size && c1.Covered.Contains(p)) usedNeighbors.Add(p);
                }
                foreach (Component c2 in p2)
                {
                    if (usedNeighbors.Any(n => c2.Covered.Contains(n))) continue;
                    bool valid = true;
                    foreach (Point p in c1.Covered)
                    {
                        if (c2.Covered.Contains(p))
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (!valid) continue;
                    Component c = new Component();
                    c.Bridges.AddRange(c1.Bridges);
                    c.Bridges.AddRange(c2.Bridges);
                    c.FinalizeComponent(Grid);
                    components.Add(c);
                }
            }
        }
    }

    private HashSet<Component> BuildComponents(List<Point> islands, int timeLimit, Stopwatch sw)
    {
        int[,] backup = (int[,])Grid.Clone();

        HashSet<Component> result = new HashSet<Component>();
        while (sw.ElapsedMilliseconds < timeLimit)
        {
            Point start = islands[random.Next(islands.Count)];
            Component component = RandomRun(start, backup);
            Grid = (int[,])backup.Clone();
            if (component == null) continue;

            if (result.Add(component)) lastFound = randomRuns;
            if (!stuck && randomRuns > lastFound + 1000) Console.Error.WriteLine("stuck at " + sw.ElapsedMilliseconds);
            stuck |= randomRuns > lastFound + 1000;
        }

        return result;
    }

    private HashSet<Component>[,,] partial;
    bool stuck = false;
    long randomRuns = 0;
    long lastFound = 0;
    Point[] open = new Point[900];
    private Component RandomRun(Point start, int[,] backup)
    {
        //if (stuck && start.X == 1 && start.Y == 9) Debugger.Break();
        randomRuns++;
        Component result = new Component();
        int readIndex = 0;
        int writeIndex = 1;
        open[0] = start;
        HashSet<Point> blocked = new HashSet<Point>();

        while (readIndex < writeIndex)
        {
            Point p = open[readIndex++];
            if (Grid[p.X, p.Y] <= 0) continue;
            List<Point> reachable = new List<Point>();
            for (int dir = 0; dir < 4; dir++)
            {
                int x = p.X + dx[dir];
                int y = p.Y + dy[dir];
                while (x >= 0 && x < Size && y >= 0 && y < Size && Grid[x, y] == 0)
                {
                    x += dx[dir];
                    y += dy[dir];
                }
                if (x >= 0 && x < Size && y >= 0 && y < Size && Grid[x, y] > 0) reachable.Add(new Point(x, y));
            }
            int freedom = reachable.Sum(r => Grid[r.X, r.Y]) - Grid[p.X, p.Y];
            if (freedom < 0) return null;
            while (reachable.Count > 0 && Grid[p.X, p.Y] > 0)
            {
                Point q = reachable[random.Next(reachable.Count)];
                reachable.Remove(q);
                int maxBridges = Math.Min(BridgeCount, Math.Min(Grid[p.X, p.Y], Grid[q.X, q.Y]));
                int bridges = maxBridges;
                if (stuck)
                {
                    bridges = (int)(1 + Math.Pow(random.Next(maxBridges * maxBridges * maxBridges), 1.0 / 3));
                    if (maxBridges - bridges > freedom) bridges = maxBridges;
                }
                if (bridges == 0) continue;
                result.AddBridge(new Bridge(p, q, bridges), Grid);
                if (Grid[q.X, q.Y] > 0) open[writeIndex++] = q;
            }
            if (Grid[p.X, p.Y] > 0) return null;

            if (writeIndex == readIndex + 1)
            {
                p = open[readIndex];
                if (partial[p.X, p.Y, backup[p.X, p.Y] - Math.Max(0, Grid[p.X, p.Y])].Add(new Component(result, p))) lastFound = randomRuns;
            }
        }

        result.FinalizeComponent(backup);
        return result;
    }
}