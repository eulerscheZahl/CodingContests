using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

public class Board
{
    public static int Size;
    public static int Area => Size * Size;
    public static double AmbulanceProbability;
    public static int AmbulanceValue;
    public static Cell[,] Grid;
    public static List<Road> Roads = new List<Road>();
    public List<Car> Cars = new List<Car>();
    public List<Cell> Crossings = new List<Cell>();
    public List<Circle> Circles = new List<Circle>();
    private List<Road> freeSpawners = new List<Road>();

    public Board() { }

    public void ReadInitial()
    {
        Size = int.Parse(Console.ReadLine().Split().Last());
        int roadCount = int.Parse(Console.ReadLine().Split().Last());
        AmbulanceProbability = double.Parse(Console.ReadLine().Split().Last());
        AmbulanceValue = int.Parse(Console.ReadLine().Split().Last());

        Grid = new Cell[Size, Size];
        int x = 0, y = 0;
        while (y < Size)
        {
            string line = Console.ReadLine();
            if (line.StartsWith("Grid")) line = Console.ReadLine();
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
        foreach (Cell cell in Grid) cell.MakeNeighbors();
        foreach (Cell cell in Grid)
        {
            Road road = cell.BuildRoad();
            if (road != null) Roads.Add(road);
            if (cell.Crossing) Crossings.Add(cell);
        }
        foreach (Road road in Roads) road.CreateStatistics();
        BuildCircles(25);
        Circles = Circles.OrderBy(c => c.Cells.Count).ToList();
        List<Circle> primitives = new List<Circle>();
        foreach (Circle circle in Circles)
        {
            primitives.Add(circle);
            foreach (Cell cell in circle.Cells) cell.Circles.Add(circle);
        }
        foreach (Cell cell in Grid) cell.MakeNeighborCircles();
        Circles = primitives;
    }

    public void BuildCircles(int maxLength)
    {
        Cell[] path = new Cell[Area];
        bool[] visited = new bool[Area];
        foreach (Cell cell in Grid)
        {
            if (!cell.Crossing) continue;
            if (!cell.Neighbors.Any(n => n != null && n.ID < cell.ID && n.Next.Contains(cell))) continue;
            path[0] = cell;
            visited[cell.ID] = true;
            BuildCircle(path, visited, 1, maxLength);
            visited[cell.ID] = false;
        }
    }

    private void BuildCircle(Cell[] circle, bool[] visited, int count, int maxLength)
    {
        if (circle[count - 1].Next.Contains(circle[0]))
        {
            Circles.Add(new Circle(circle.Take(count).ToList(), visited.ToArray()));
            return;
        }
        if (count == maxLength) return;
        foreach (Cell next in circle[count - 1].Next)
        {
            if (next == null || visited[next.ID] || next.ID < circle[0].ID) continue;
            visited[next.ID] = true;
            circle[count] = next;
            BuildCircle(circle, visited, count + 1, maxLength);
            visited[next.ID] = false;
        }
    }

    private int elapsedTime;
    public void ReadCurrent()
    {
#if DEBUG
        Console.Error.WriteLine(Size);
        Console.Error.WriteLine(Roads.Count);
        Console.Error.WriteLine(AmbulanceProbability);
        Console.Error.WriteLine(AmbulanceValue);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++) Console.Error.Write(Grid[x, y].Input());
            Console.Error.WriteLine();
        }
#endif
        int carCount = int.Parse(Console.ReadLine());
#if DEBUG
        Console.Error.WriteLine(carCount);
#endif
        Cars.Clear();
        for (int i = 0; i < carCount; i++)
        {
            string[] temp = Console.ReadLine().Split();
            int carY = int.Parse(temp[0]);
            int carX = int.Parse(temp[1]);
            int carVal = int.Parse(temp[2]);
            char carDir = temp[3][0];
#if DEBUG
            Console.Error.WriteLine(string.Join(" ", temp));
#endif
            Car car = new Car(Grid[carX, carY], carVal, carDir);
            if (car.Next() == null) continue; // will reach the target, no matter what
            Cars.Add(car);
        }
        elapsedTime = int.Parse(Console.ReadLine());
        foreach (Road r in freeSpawners) r.UpdateSpawnRate();
#if DEBUG
        Console.Error.WriteLine(elapsedTime);
        elapsedTime = 0;
#endif
        //elapsedTime = 0;
    }

    private bool completelyStuck = false;
    public void PlayTurn(int currentTurn)
    {
        if (elapsedTime > 9700)
        {
            Console.WriteLine(0);
            return;
        }
        DirectionType[] initialDirections = new DirectionType[Area];
        DirectionType[] bestDirections = new DirectionType[Area];
        foreach (Cell cell in Grid) initialDirections[cell.ID] = cell.LightDirection;
        List<Car> initialCars = Cars.ToList();
        List<Cell> initialCells = Cars.Select(c => c.Cell).ToList();

        BuildCircles();
        HeuristicTurn();
        foreach (Cell cell in Grid) bestDirections[cell.ID] = cell.LightDirection;
        freeSpawners = Roads.Where(r => r.Cells[0].Car == null).ToList();
        if (elapsedTime < 9000)
        {
            double bestScore = ComputeScore(currentTurn + 1);

            List<Car> stuck = new List<Car>();
            for (int i = 0; i < Cars.Count; i++)
            {
                if (Cars[i].Cell == initialCells[i]) stuck.Add(Cars[i]);
            }
            for (int i = 0; i < Cars.Count; i++) Cars[i].Cell = initialCells[i];
            BuildCircles();
            List<Car> moving = initialCars.Except(stuck).ToList();
            moving = moving.Where(m => m.Cell.Crossing || m.Next() != null && m.Next().Crossing && !m.Convoy.Cars.Contains(m.Next().Car)).ToList();
            stuck = stuck.Where(s => s.Next().Car == null).ToList();
            Dictionary<Car, double> improvements = new Dictionary<Car, double>();
            foreach (Car c in stuck)
            {
                Cars = initialCars.ToList();
                for (int i = 0; i < initialCars.Count; i++) initialCars[i].Cell = initialCells[i];
                BuildCircles();
                HeuristicTurn(new List<Car> { c });
                double stuckScore = ComputeScore(currentTurn + 1);
                if (stuckScore > bestScore) improvements[c] = stuckScore;
            }

            List<Car> priority = new List<Car>();
            bool isStuck = computeStuck;
            foreach (Car c in improvements.Keys.OrderByDescending(k => improvements[k]))
            {
                priority.Add(c);
                Cars = initialCars.ToList();
                for (int i = 0; i < initialCars.Count; i++) initialCars[i].Cell = initialCells[i];
                BuildCircles();
                HeuristicTurn(priority);
                double stuckScore = ComputeScore(currentTurn + 1);
                if (stuckScore > bestScore)
                {
                    bestScore = stuckScore;
                    isStuck = computeStuck;
                    foreach (Cell cell in Grid) bestDirections[cell.ID] = cell.LightDirection;
                    freeSpawners = Roads.Where(r => r.Cells[0].Car == null).ToList();
                }
                else priority.Remove(c);
            }

            if (!completelyStuck && isStuck)
            {
                completelyStuck = true;
                foreach (Car c in moving)
                {
                    Cars = initialCars.ToList();
                    for (int i = 0; i < initialCars.Count; i++) initialCars[i].Cell = initialCells[i];
                    BuildCircles();
                    HeuristicTurn(priority, c);
                    double movingScore = ComputeScore(currentTurn + 1);
                    if (movingScore > bestScore)
                    {
                        bestScore = movingScore;
                        completelyStuck = computeStuck;
                        foreach (Cell cell in Grid) bestDirections[cell.ID] = cell.LightDirection;
                        freeSpawners = Roads.Where(r => r.Cells[0].Car == null).ToList();
                    }
                }
            }

#if DEBUG
            Console.Error.WriteLine("prio: " + string.Join(" - ", priority));
#endif
        }

        List<string> actions = new List<string>();
        foreach (Cell cell in Grid)
        {
            cell.LightDirection = bestDirections[cell.ID];
            if (cell.Crossing && cell.LightDirection != initialDirections[cell.ID]) actions.Add(cell.Y + " " + cell.X);
        }
        Console.WriteLine(actions.Count);
        foreach (string a in actions) Console.WriteLine(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildCircles()
    {
        foreach (Cell c in Grid) c.Car = null;
        foreach (Circle c in Circles) c.Clear();
        foreach (Car car in Cars)
        {
            car.Cell.Car = car;
            foreach (Circle c in car.Cell.Circles) c.AddCar(car);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BuildDependencies()
    {
        foreach (Car car in Cars) car.DFS_Visited = false;
        foreach (Car car in Cars) car.DFS();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildConvoys()
    {
        List<Car> toBuild = Cars.OrderBy(c => c.Cell.DistToSpawn[(int)c.DirectionType]).ToList();
        foreach (Car car in toBuild) car.Convoy = null;
        foreach (Car car in toBuild)
        {
            if (car.Convoy != null) continue;
            Convoy convoy = new Convoy();
            convoy.Cars.Add(car);
            Cell cell = car.Next();
            while (cell != null && cell.Car != null && cell.Car.Direction == car.Direction)
            {
                convoy.Cars.Add(cell.Car);
                cell = cell.Car.Next();
            }
            for (int i = 0; i < convoy.Cars.Count; i++)
            {
                Car c = convoy.Cars[i];
                c.Convoy = convoy;
                c.MinimumDelay = i + 2;
            }
        }
    }

    private bool computeStuck;
    private double ComputeScore(int startTurn)
    {
        DirectionType[] initialDirections = new DirectionType[Area];
        foreach (Cell cell in Grid) initialDirections[cell.ID] = cell.LightDirection;
        List<Car> initialCars = Cars.ToList();
        List<Cell> initialCells = Cars.Select(c => c.Cell).ToList();

        double result = 0.2 * Roads.Where(r => r.Cells[0].Car == null).Sum(r => r.SpawnRate);
        double factor = 1;
        int lastTurn = Math.Min(999, startTurn + 2 * Size + 5);
        if (startTurn + Size < 999)
        {
            foreach (Car c in Cars) c.Value = 1;
        }
        for (int turn = startTurn; turn < lastTurn && Cars.Count > 0; turn++)
        {
            List<Car> keep = new List<Car>();
            foreach (Car car in Cars)
            {
                if (car.Next() == null)
                {
                    result += factor * car.Value;
                    foreach (Circle c in car.Cell.Circles) c.RemoveCar(car);
                    car.Cell.Car = null;
                }
                else keep.Add(car);
            }
            Cars = keep;
            factor -= 0.01;
            result += factor * 1e-4 * Roads.Where(r => r.Cells[0].Car == null).Sum(r => r.SpawnRate);

            HeuristicTurn();
        }

        foreach (Cell cell in Grid) cell.LightDirection = initialDirections[cell.ID];
        computeStuck = lastTurn < 999 && Cars.Count > 0;
        Cars = initialCars;
        for (int i = 0; i < Cars.Count; i++) Cars[i].Cell = initialCells[i];

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HeuristicTurn(List<Car> forceMove = null, Car foceStuck = null)
    {
        BuildDependencies();
        BuildConvoys();
        bool[] directionsSet = new bool[Area];
        List<Convoy> convoys = new List<Convoy>();
        if (forceMove != null) convoys = forceMove.Select(f => f.Convoy).Distinct().ToList();
        List<Car> cars = Cars.OrderByDescending(c => c.Urgency(convoys)).ToList();
        if (foceStuck != null)
        {
            if (foceStuck.Cell.Crossing && !directionsSet[foceStuck.Cell.ID])
            {
                directionsSet[foceStuck.Cell.ID] = true;
                foceStuck.Cell.MakeStuck(foceStuck);
                cars.Remove(foceStuck);
            }
            else
            {
                Cell next = foceStuck.Next();
                if (next.Crossing && !directionsSet[next.ID])
                {
                    directionsSet[next.ID] = true;
                    next.MakeStuck(foceStuck);
                    cars.Remove(foceStuck);
                }
            }
        }
        if (forceMove != null)
        {
            foreach (Car car in forceMove)
            {
                List<Cell> toAdjust = new List<Cell>();
                if (car.Cell.Crossing) toAdjust.Add(car.Cell);
                Cell next = car.Next();
                if (next.Crossing) toAdjust.Add(next);
                if (toAdjust.Any(a => directionsSet[a.ID] && a.LightDirection != car.DirectionType)) continue;
                foreach (Cell t in toAdjust)
                {
                    directionsSet[t.ID] = true;
                    t.MakeMove(car);
                }
                if (next.Car == null)
                {
                    foreach (Circle c in car.Cell.NextCircles[car.Direction]) c.AddCar(car);
                    foreach (Circle c in next.NextCircles[(car.Direction + 2) % 4]) c.RemoveCar(car);
                    next.Car = car;
                    car.Cell.Car = null;
                    car.Cell = next;
                    cars.Remove(car);
                }
            }
        }
        for (int i = 0; cars.Count > 0; i++)
        {
            List<Car> skipped = new List<Car>();
            foreach (Car car in cars)
            {
#if DEBUG
                //if (car.Cell == Grid[9, 9]) Debugger.Break();
#endif
                Cell next = car.Next();
                if (next.Car != null)
                {
                    skipped.Add(car);
                    continue;
                }
                bool canDrive = true;
                if (!car.Convoy.OnCrossing && !car.Convoy.OnSpawn && next.Crossing)
                {
                    Cell path = next;
                    for (int dist = 1; canDrive && path != null && path.Crossing; dist++)
                    {
                        if (path.Car != null && path.Car.MinimumDelay >= dist) canDrive = false;
                        path = path.Next[(int)car.DirectionType];
                    }
                }
                foreach (Circle circle in car.Cell.NextCircles[car.Direction])
                {
                    if (!canDrive) break;
                    if (circle.CanAdd(car)) continue;
#if DEBUG
                    circle.CanAdd(car);
#endif
                    canDrive = false;
                }

                List<Cell> toAdjust = new List<Cell>();
                if (car.Cell.Crossing) toAdjust.Add(car.Cell);
                if (next.Crossing) toAdjust.Add(next);
                if (toAdjust.Any(a => directionsSet[a.ID] && a.LightDirection != car.DirectionType))
                {
                    skipped.Add(car);
                    continue;
                }
                if (!canDrive && toAdjust.Any(a => a.LightDirection != car.DirectionType || !directionsSet[a.ID]))
                {
                    skipped.Add(car);
                    continue;
                }
                foreach (Cell t in toAdjust)
                {
                    directionsSet[t.ID] = true;
                    t.MakeMove(car);
                }
                if (next.Car == null)
                {
                    foreach (Circle c in car.Cell.NextCircles[car.Direction]) c.AddCar(car);
                    foreach (Circle c in next.NextCircles[(car.Direction + 2) % 4]) c.RemoveCar(car);
                    next.Car = car;
                    car.Cell.Car = null;
                    car.Cell = next;
                }
            }
            if (skipped.Count == cars.Count) break;
            cars = skipped;
        }

        // make remaining cars stuck
        foreach (Car car in cars)
        {
            if (car.Cell.Crossing && !directionsSet[car.Cell.ID])
            {
                directionsSet[car.Cell.ID] = true;
                car.Cell.MakeStuck(car);
                continue;
            }
            Cell next = car.Next();
            if (next == null) continue;
            if (next.Crossing && !directionsSet[next.ID])
            {
                directionsSet[next.ID] = true;
                next.MakeStuck(car);
            }
        }
    }
}