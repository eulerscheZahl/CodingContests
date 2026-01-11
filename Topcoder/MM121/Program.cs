using System;
using System.Collections.Generic;
using System.Linq;

public class SoccerTournament
{
    static double[,] goalProbability = new double[13, 13];
    static double[,][] distributionCurve = new double[13, 13][];
    static System.Diagnostics.Stopwatch sw;
    static void ComputeProbablity(int x)
    {
        for (int attack = 1; attack <= 12; attack++)
        {
            for (int defense = 1; defense <= 12; defense++)
            {
                distributionCurve[attack, defense] = new double[4];
                double goals = 0;
                for (int a = 1; a <= attack; a++)
                {
                    for (int d = 1; d <= defense; d++)
                    {
                        if (a > d) goals++;
                    }
                }
                double p = goals / (attack * defense);
                for (int a = 0; a <= 3 * x; a++)
                {
                    double probA = Math.Pow(p, a) * Math.Pow(1 - p, 3 * x - a) * Binom(3 * x, a);
                    goalProbability[attack, defense] += probA * Math.Floor((double)a / x + 0.5);
                    distributionCurve[attack, defense][(int)((double)a / x + 0.5)] += probA;
                }
            }
        }
    }

    private static double Binom(int n, int k)
    {
        return Factorial(n) / Factorial(n - k) / Factorial(k);
    }

    private static double Factorial(int k)
    {
        double result = 1;
        for (int i = 2; i <= k; i++) result *= i;
        return result;
    }

    static double EstimateGoals(double attack, double defense)
    {
        double v1 = goalProbability[(int)attack, (int)defense];
        double v2 = goalProbability[(int)attack + 1, (int)defense];
        double v3 = goalProbability[(int)attack, (int)defense + 1];
        double v4 = goalProbability[(int)attack + 1, (int)defense + 1];

        double v12 = v2 * (attack - (int)attack) + v1 * ((int)attack + 1 - attack);
        double v34 = v4 * (attack - (int)attack) + v3 * ((int)attack + 1 - attack);

        double result = v34 * (defense - (int)defense) + v12 * ((int)defense + 1 - defense);
        return result;
    }

    static double[] Add(double[] a, double[] b)
    {
        double[] result = new double[a.Length];
        for (int i = 0; i < a.Length; i++) result[i] = a[i] + b[i];
        return result;
    }

    static double[] Mult(double[] a, double f)
    {
        double[] result = new double[a.Length];
        for (int i = 0; i < a.Length; i++) result[i] = a[i] * f;
        return result;
    }

    static double[] EstimateCurve(double attack, double defense)
    {
        double[] v1 = distributionCurve[(int)attack, (int)defense];
        double[] v2 = distributionCurve[(int)attack + 1, (int)defense];
        double[] v3 = distributionCurve[(int)attack, (int)defense + 1];
        double[] v4 = distributionCurve[(int)attack + 1, (int)defense + 1];

        double[] v12 = Add(Mult(v2, attack - (int)attack), Mult(v1, (int)attack + 1 - attack));
        double[] v34 = Add(Mult(v4, attack - (int)attack), Mult(v3, (int)attack + 1 - attack));

        double[] result = Add(Mult(v34, defense - (int)defense), Mult(v12, (int)defense + 1 - defense));
        return result;
    }

    public static int RoundGoals(double goals)
    {
        //if (goals > 2.55) return 3;
        //if (goals < 0.45) return 0;
        return (int)Math.Round(goals);
    }

    class Match
    {
        public Team T1, T2;
        public double G1, G2;
        public Match(Team t1, Team t2)
        {
            this.T1 = t1;
            this.T2 = t2;
            this.G1 = EstimateGoals(t1.Attack, t2.Defense);
            this.G2 = EstimateGoals(t2.Attack, t1.Defense);
            double floatDiff = Math.Abs(G1 - G2);
            int intDiff = Math.Abs(RoundGoals(G1) - RoundGoals(G2));
            // if (floatDiff < 0.2 && intDiff == 1) g2 = g1;
            // if (floatDiff > 0.8 && intDiff == 0)
            // {
            //     if (g2 < 2.5) g2++;
            //     else g1--;
            // }

        }

        public int Outcome => Math.Sign(RoundGoals(G1) - RoundGoals(G2));
        public double Probability;
        public double ExpectedScore(List<Match> matches)
        {
            return matches.Where(m => m.Outcome == this.Outcome).Sum(m => m.Probability) + 2 * this.Probability;
        }

        public List<Match> GetMatches()
        {
            List<Match> result = new List<Match>();
            double[] t1Scored = EstimateCurve(T1.Attack, T2.Defense);
            double[] t2Scored = EstimateCurve(T2.Attack, T1.Defense);
            for (int a = 0; a < 4; a++)
            {
                for (int b = 0; b < 4; b++)
                {
                    Match m = new Match(T1, T2)
                    {
                        G1 = a,
                        G2 = b,
                        Probability = t1Scored[a] * t2Scored[b]
                    };
                    result.Add(m);
                }
            }
            return result.OrderByDescending(m => m.ExpectedScore(result)).ToList();
        }

        public string PrintOutcome()
        {
            List<Match> matches = GetMatches().ToList();
            Match best = matches.OrderByDescending(m => m.ExpectedScore(matches)).First();
            return best.G1 + " " + best.G2;
        }

        public double ExpectationDiff()
        {
            double floatDiff = Math.Abs(G1 - G2);
            int intDiff = Math.Abs(RoundGoals(G1) - RoundGoals(G2));
            if (intDiff >= 1 && floatDiff >= 1) return 0;
            return Math.Abs(floatDiff - intDiff);
        }

        public bool Fitted = false;
        public void TryFitMatch()
        {
            if (Fitted) return;

            if (G1 < G2 && RoundGoals(G1) == RoundGoals(G2) && T1.ExpectedPoints > T1.Points && T2.ExpectedPoints < T2.Points)
            {
                Match match = GetMatches().First(m => m.Outcome == -1);
                this.G1 = match.G1;
                this.G2 = match.G2;
                T1.ExpectedPoints -= D;
                T2.ExpectedPoints += W - D;
            }

            else if (G1 > G2 && RoundGoals(G1) == RoundGoals(G2) && T1.ExpectedPoints < T1.Points && T2.ExpectedPoints > T2.Points)
            {
                Match match = GetMatches().First(m => m.Outcome == 1);
                this.G1 = match.G1;
                this.G2 = match.G2;
                T1.ExpectedPoints += W - D;
                T2.ExpectedPoints -= D;
            }

            else if (RoundGoals(G1) - 1 == RoundGoals(G2) && T1.ExpectedPoints > T1.Points && T2.ExpectedPoints < T2.Points)
            {
                Match match = GetMatches().First(m => m.Outcome == 0);
                this.G1 = match.G1;
                this.G2 = match.G2;
                T1.ExpectedPoints += D - W;
                T2.ExpectedPoints += D;
            }

            else if (RoundGoals(G2) - 1 == RoundGoals(G1) && T1.ExpectedPoints < T1.Points && T2.ExpectedPoints > T2.Points)
            {
                Match match = GetMatches().First(m => m.Outcome == 0);
                this.G1 = match.G1;
                this.G2 = match.G2;
                T2.ExpectedPoints += D - W;
                T1.ExpectedPoints += D;
            }
        }

        public override string ToString()
        {
            return RoundGoals(G1) + " " + RoundGoals(G2);
        }

        public string Debug()
        {
            return G1 + " " + G2;
        }

        internal void EnsureWin(Team team)
        {
            if (Fitted) return;
            TryFitMatch();
            Fitted = true;
            int g1 = RoundGoals(G1);
            int g2 = RoundGoals(G2);
            if (team == T1 && g1 <= g2)
            {
                Match match = GetMatches().First(m => m.Outcome == 1);
                this.G1 = match.G1;
                this.G2 = match.G2;
            }
            if (team == T2 && g1 >= g2)
            {
                Match match = GetMatches().First(m => m.Outcome == -1);
                this.G1 = match.G1;
                this.G2 = match.G2;
            }
        }

        internal void CheckValidity()
        {
            int g1 = RoundGoals(G1);
            int g2 = RoundGoals(G2);
            Match replacement = this;
            List<Match> candidates = GetMatches();
            if (T1.MaxLosses == 0 && g1 < g2) replacement = candidates.First(c => c.Outcome == 0);
            else if (T1.MaxDraws == 0 && g1 == g2) replacement = candidates.First(c => c.Outcome != 0);
            else if (T1.MaxWins == 0 && g1 > g2) replacement = candidates.First(c => c.Outcome == 0);
            else if (T2.MaxLosses == 0 && g1 > g2) replacement = candidates.First(c => c.Outcome == 0);
            else if (T2.MaxDraws == 0 && g1 == g2) replacement = candidates.First(c => c.Outcome != 0);
            else if (T2.MaxWins == 0 && g1 < g2) replacement = candidates.First(c => c.Outcome == 0);
            this.G1 = replacement.G1;
            this.G2 = replacement.G2;
        }
    }

    class Team
    {
        public int ID;
        public double Attack = 5, Defense = 5;
        public int GoalsScored, GoalsReceived, Points;

        public double ExpectedScored, ExpectedReceived;
        public int ExpectedPoints;

        public int MaxWins, MinDraws, MaxDraws, MaxLosses;

        public Team(int id, int goalsScored, int goalsReceived, int points, int maxDraws)
        {
            this.ID = id;
            this.GoalsScored = goalsScored;
            this.GoalsReceived = goalsReceived;
            this.Points = points;
            this.MinDraws = int.MaxValue;

            for (int win = 0; win < N; win++)
            {
                for (int draw = 0; draw + win < N && draw <= maxDraws; draw++)
                {
                    if (W * win + D * draw == points)
                    {
                        MaxWins = Math.Max(MaxWins, win);
                        MaxDraws = Math.Max(MaxDraws, draw);
                        MinDraws = Math.Min(MinDraws, draw);
                        MaxLosses = Math.Max(MaxLosses, N - 1 - win - draw);
                    }
                }
            }
        }

        public Team(Team t)
        {
            this.ID = t.ID;
            this.Attack = t.Attack;
            this.Defense = t.Defense;
            this.GoalsScored = t.GoalsScored;
            this.GoalsReceived = t.GoalsReceived;
            this.Points = t.Points;
            this.MaxWins = t.MaxWins;
            this.MinDraws = t.MinDraws;
            this.MaxDraws = t.MaxDraws;
            this.MaxLosses = t.MaxLosses;
        }

        public void Play(List<Team> teams)
        {
            ExpectedPoints = 0;
            ExpectedScored = 0;
            ExpectedReceived = 0;
            foreach (Team team in teams)
            {
                if (team.ID == this.ID) continue;
                double a = EstimateGoals(this.Attack, team.Defense);
                double d = EstimateGoals(team.Attack, this.Defense);
                ExpectedScored += a;
                ExpectedReceived += d;
                if (Math.Round(a) > Math.Round(d)) ExpectedPoints += W;
                if (Math.Round(a) == Math.Round(d)) ExpectedPoints += D;
            }
        }

        public override string ToString()
        {
            return $"team {ID}: attack={Attack} defense={Defense}";
        }

        public Match ExpectedResult(Team team)
        {
            return new Match(this, team);
        }

        public double ExpectationPenalty()
        {
            return Math.Abs(GoalsScored - ExpectedScored) + Math.Abs(GoalsReceived - ExpectedReceived);
        }

        internal void FitStrength(List<Team> teams)
        {
            double oldDefense = Defense;
            double minDefense = 1;
            double maxDefense = 11;
            while (maxDefense - minDefense > 1e-3)
            {
                Defense = (maxDefense + minDefense) / 2;
                Play(teams);
                if (this.ExpectedReceived < this.GoalsReceived) maxDefense = Defense;
                else minDefense = Defense;
            }
            Defense = 0.25 * Defense + 0.75 * oldDefense;

            double oldAttack = Attack;
            double minAttack = 1;
            double maxAttack = 11;
            while (maxAttack - minAttack > 1e-3)
            {
                Attack = (maxAttack + minAttack) / 2;
                Play(teams);
                if (this.ExpectedScored > this.GoalsScored) maxAttack = Attack;
                else minAttack = Attack;
            }
            Attack = 0.25 * Attack + 0.75 * oldAttack;
        }

        internal string TableRow()
        {
            return $"Team{ID} {ExpectedScored} {ExpectedReceived} {ExpectedPoints}";
        }

        internal double Rank()
        {
            return 1e12 * ExpectedPoints + 1e3 * (ExpectedScored - ExpectedReceived) + ExpectedScored;
        }

        internal void EnsureWins(List<Match> matches)
        {
            matches.ForEach(m =>
            {
                m.EnsureWin(this);
            });
        }
    }

    public string[] FindSolution(int[] scored, int[] conceded, int[] points)
    {
        int maxDraws = N * (N - 1) / 2;
        if (W != 2 * D)
        {
            int sum = points.Sum();
            int win = maxDraws;
            int draw = 0;
            while (W * win + D * 2 * draw != sum)
            {
                win--;
                draw++;
            }
            maxDraws = draw;
            //Console.Error.WriteLine("real draws: " + draw + "     my draws: " + matches.Count(m => m.Outcome == 0));
        }

        ComputeProbablity(X);
        List<Team> teams = new List<Team>();
        for (int i = 0; i < N; i++) teams.Add(new Team(i, scored[i], conceded[i], points[i], maxDraws));
        if (teams.Sum(t => t.MinDraws) == 2 * maxDraws) teams.ForEach(t => t.MaxDraws = t.MinDraws);

        for (int i = 0; i < 100; i++)
        {
            teams.ForEach(t => t.Play(teams));
            List<Team> backup = teams.Select(t => new Team(t)).ToList();
            foreach (Team team in teams)
            {
                team.FitStrength(backup);
            }
        }
        teams.ForEach(t => t.Play(teams));
        // foreach (Team team in teams) Console.Error.WriteLine(team);

        // string strengths = "team 0 attackStrength 4 defenceStrength 3\nteam 1 attackStrength 4 defenceStrength 5\nteam 2 attackStrength 9 defenceStrength 9\nteam 3 attackStrength 10 defenceStrength 6\nteam 4 attackStrength 5 defenceStrength 1\nteam 5 attackStrength 9 defenceStrength 7\nteam 6 attackStrength 8 defenceStrength 7\nteam 7 attackStrength 8 defenceStrength 10\nteam 8 attackStrength 3 defenceStrength 9\nteam 9 attackStrength 10 defenceStrength 6\nteam 10 attackStrength 6 defenceStrength 2\nteam 11 attackStrength 5 defenceStrength 7\nteam 12 attackStrength 7 defenceStrength 8\nteam 13 attackStrength 3 defenceStrength 3\nteam 14 attackStrength 10 defenceStrength 1\nteam 15 attackStrength 10 defenceStrength 9\nteam 16 attackStrength 6 defenceStrength 1\nteam 17 attackStrength 10 defenceStrength 1\nteam 18 attackStrength 6 defenceStrength 5\nteam 19 attackStrength 10 defenceStrength 8\nteam 20 attackStrength 8 defenceStrength 10\nteam 21 attackStrength 2 defenceStrength 5\nteam 22 attackStrength 3 defenceStrength 4\nteam 23 attackStrength 7 defenceStrength 9\nteam 24 attackStrength 6 defenceStrength 3\nteam 25 attackStrength 3 defenceStrength 5\nteam 26 attackStrength 8 defenceStrength 10\nteam 27 attackStrength 2 defenceStrength 9\nteam 28 attackStrength 2 defenceStrength 2\nteam 29 attackStrength 5 defenceStrength 9\nteam 30 attackStrength 6 defenceStrength 9\nteam 31 attackStrength 4 defenceStrength 7\nteam 32 attackStrength 8 defenceStrength 9\nteam 33 attackStrength 8 defenceStrength 4\nteam 34 attackStrength 8 defenceStrength 8\nteam 35 attackStrength 3 defenceStrength 10\nteam 36 attackStrength 4 defenceStrength 2\nteam 37 attackStrength 2 defenceStrength 4\nteam 38 attackStrength 10 defenceStrength 5\nteam 39 attackStrength 9 defenceStrength 1\nteam 40 attackStrength 7 defenceStrength 10\nteam 41 attackStrength 9 defenceStrength 10\nteam 42 attackStrength 4 defenceStrength 7\nteam 43 attackStrength 2 defenceStrength 9\nteam 44 attackStrength 6 defenceStrength 7\nteam 45 attackStrength 9 defenceStrength 2\nteam 46 attackStrength 5 defenceStrength 5\nteam 47 attackStrength 5 defenceStrength 8\nteam 48 attackStrength 8 defenceStrength 7\nteam 49 attackStrength 10 defenceStrength 4\n";
        // foreach (string line in strengths.Trim().Split('\n'))
        // {
        //     string[] parts = line.Split();
        //     int id = int.Parse(parts[1]);
        //     if (id >= teams.Count) break;
        //     teams[id].Attack = int.Parse(parts[3]); teams[id].Defense = int.Parse(parts[5]);
        // }


        List<Match> matches = new List<Match>();
        for (int team1 = 0; team1 < N; team1++)
        {
            //Console.Error.WriteLine("team " + team1 + " point: real " + teams[team1].Points + "    expected " + teams[team1].ExpectedPoints);
            for (int team2 = team1 + 1; team2 < N; team2++)
            {
                matches.Add(teams[team1].ExpectedResult(teams[team2]));
                //Console.Error.WriteLine(team1 + "-" + team2 + ":  " + matches.Last().Debug());
            }
        }
        // return matches.Select(m => m.GetMatches()[0].ToString()).ToArray();

        // fix winrates
        List<Team> teamByScore = teams.OrderByDescending(t => t.Points).ToList();
        while (teamByScore.Count > 0 && teamByScore[0].Points == W * (teamByScore.Count - 1))
        {
            teamByScore[0].EnsureWins(matches);
            teamByScore.RemoveAt(0);
        }
        List<Match> sorted = matches.OrderByDescending(m => m.ExpectationDiff()).ToList();
        foreach (Match m in sorted.Where(s => s.ExpectationDiff() > (0.5 - 3.0 / N))) m.TryFitMatch();

        foreach (Team team in teams.OrderByDescending(t => t.Rank()))
        {
            // Console.Error.WriteLine(team.TableRow());
        }

        matches.ForEach(m => m.CheckValidity());

        Console.Error.WriteLine("\nmax:" + (3 * N * (N - 1) / 2) + "  \tN:" + N + "\t X:" + X + "\t\ttime: " + sw.ElapsedMilliseconds + "ms");
        //return matches.Select(m => m.PrintOutcome()).ToArray();
        return matches.Select(m => m.ToString()).ToArray();
    }

    static string ReadLine()
    {
        string line = Console.ReadLine();
        //Console.Error.WriteLine(line);
        return line;
    }

    static int N;
    static int W;
    static int D;
    static int X;
    static void Main(string[] args)
    {
        //TestEstimation();
        N = int.Parse(ReadLine());
        W = int.Parse(ReadLine());
        D = int.Parse(ReadLine());
        X = int.Parse(ReadLine());

        int[] scored = new int[N];
        int[] conceded = new int[N];
        int[] points = new int[N];

        for (int i = 0; i < N; i++)
        {
            string[] temp = ReadLine().Split(' ');
            scored[i] = int.Parse(temp[0]);
            conceded[i] = int.Parse(temp[1]);
            points[i] = int.Parse(temp[2]);
        }

        SoccerTournament prog = new SoccerTournament();
        sw = System.Diagnostics.Stopwatch.StartNew();
        string[] ret = prog.FindSolution(scored, conceded, points);

        Console.WriteLine(ret.Length);
        for (int i = 0; i < ret.Length; i++)
            Console.WriteLine(ret[i]);
    }

    private static void TestEstimation()
    {
        X = 10;

        Random random = new Random(0);
        ComputeProbablity(X);

        double score = 0;
        double expected = 0;
        for (int i = 0; i < 1e7; i++)
        {
            Team t1 = new Team(1, 0, 0, 0, 0)
            {
                Attack = random.Next(2, 10),
                Defense = random.Next(1, 10)
            };
            Team t2 = new Team(2, 0, 0, 0, 0)
            {
                Attack = random.Next(2, 10),
                Defense = random.Next(1, 10)
            };
            Match match = new Match(t1, t2);
            List<Match> matches = match.GetMatches();
            match = matches[0];
            expected += match.ExpectedScore(matches);

            int scored1 = 0;
            int scored2 = 0;
            for (int simulation = 0; simulation < X; simulation++)
            {
                for (int round = 0; round < 3; round++)
                {
                    int attack = random.Next(1, (int)t1.Attack + 1);
                    int defence = random.Next(1, (int)t2.Defense + 1);
                    if (attack > defence) scored1++;     //team 1 scores!

                    attack = random.Next(1, (int)t2.Attack + 1);
                    defence = random.Next(1, (int)t1.Defense + 1);
                    if (attack > defence) scored2++;     //team 2 scores!
                }
            }

            scored1 = (int)(0.5 + scored1 * 1.0 / X);
            scored2 = (int)(0.5 + scored2 * 1.0 / X);

            if (scored1 == match.G1 && scored2 == match.G2) score += 3;
            else if (Math.Sign(scored1 - scored2) == Math.Sign(match.G1 - match.G2)) score++;

            if (i % 10000 == 0) Console.WriteLine(100 * expected / (3 * (i + 1)));
        }
    }
}