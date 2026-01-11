using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;


public class PhraseGuessing
{
    public static int N;
    public static int P;
    public static double C;
    public static List<Letter> letters;
    private static HashSet<string> previousGuesses = new HashSet<string>();

    public static List<Node> roots = new List<Node>();
#if DEBUG
    public static Random random = new Random(1);
#else
    public static Random random = new Random();
#endif

    private static List<string>[] wordsByLength = new List<string>[50];
    private static List<List<int>>[] orders = new List<List<int>>[50];
    private static List<string> guesses = new List<string>();
    private static List<string> results = new List<string>();
    public static int turn;
    public static void Main(string[] args)
    {
        //Engine.RandomSim();
        Stopwatch sw = Stopwatch.StartNew();
        List<string> wordList = File.ReadAllLines("words_alpha_filtered.txt").Skip(1).ToList();
        for (int i = 0; i < 50; i++)
        {
            roots.Add(new Node());
            orders[i] = new List<List<int>>();
            orders[i].Add(Enumerable.Range(0, i).ToList());
            orders[i].Add(Enumerable.Range(0, i).Reverse().ToList());
            wordsByLength[i] = new List<string>();
        }
        foreach (string word in wordList)
        {
            wordsByLength[word.Length].Add(word);
            foreach (List<int> order in orders[word.Length])
                roots[word.Length].AddWord(word, order);
            for (int i = 0; i < word.Length; i++) Letter.LetterCount[word.Length, i, word[i]]++;
        }

        N = int.Parse(Console.ReadLine());
        P = int.Parse(Console.ReadLine());
        C = double.Parse(Console.ReadLine());
        Letter.Init();
#if DEBUG
        Engine.solution = Console.ReadLine();
#endif

        letters = Enumerable.Range(0, P).Select(i => new Letter()).ToList();

        int remainingTime = 10000;
        for (turn = 1; turn <= 10000; turn++)
        {
            string guess = BuildSentence(turn);
            previousGuesses.Add(guess);
            //Console.Error.WriteLine(string.Join(" ", guess.Select((c, i) => letters[i].Probability(c))));

            Console.WriteLine(guess);

#if DEBUG
            string result = Engine.Judge(guess);
            Console.Error.WriteLine("result: " + result);
            if (result.Length > P)
            {
                Console.Error.WriteLine("solved in: " + turn);
                return;
            }
#else
            //read elapsed time and result
            int elapsedTime = int.Parse(Console.ReadLine());
            remainingTime = 10000 - elapsedTime;
            if (remainingTime < 2000) beamWidth = 50;
            if (remainingTime < 1000) beamWidth = 10;
            if (remainingTime < 500) beamWidth = 5;
            string result = Console.ReadLine();
#endif
            guesses.Add(guess);
            results.Add(result);
            //Engine.PrintStats(turn, guess, result);

            for (int j = 0; j < P; j++)
            {
                letters[j].Update(guess[j], result[j]);
            }
            Letter.UpdateGlobal(guess, result, letters);
        }

        //terminate solution
        Console.WriteLine("-1");
    }

    private static int CountFails(string s)
    {
        int fails = 0;
        for (int i = 0; i < guesses.Count; i++)
        {
            string response = Engine.TrueResult(guesses[i], s);
            for (int j = 0; j < P; j++)
            {
                if (response[j] != results[i][j]) fails++;
            }
        }
        return fails;
    }

    static int counter = 0;
    private static string BuildSentence(int turn)
    {
        double[,] combinationCount = new double[N + 1, P + 50];
        int[,] combinationFrom = new int[N + 1, P + 50];
        for (int i = 0; i < roots.Count; i++) combinationCount[1, i] = Math.Pow(roots[i].Count, 0.5);
        for (int words = 2; words <= N; words++)
        {
            for (int spacePos = 1; spacePos < P; spacePos++)
            {
                if (combinationCount[words - 1, spacePos] == 0) continue;
                for (int i = 1; i < roots.Count && i + spacePos < P; i++)
                {
                    double tmp = combinationCount[words - 1, spacePos] * Math.Pow(roots[i].Count, 0.5) * letters[spacePos].Probability(' ');
                    if (tmp > combinationCount[words, spacePos + i + 1])
                    {
                        combinationCount[words, spacePos + i + 1] = tmp;
                        combinationFrom[words, spacePos + i + 1] = spacePos;
                    }
                }
            }
        }
        List<int> spaces = new List<int> { P };
        for (int words = N; words > 1; words--)
        {
            spaces.Insert(0, combinationFrom[words, spaces.First()]);
        }
        spaces.Insert(0, -1);
        List<int> wordLengths = Enumerable.Range(0, N).Select(i => spaces[i + 1] - spaces[i] - 1).ToList();

        int offset = 0;
        List<List<BeamNode>> beams = new List<List<BeamNode>>();
        foreach (int len in wordLengths)
        {
            beams.Add(BuildWord(offset, len));
            offset += len + 1;
        }

        string result = string.Join(" ", beams.Select(b => b[0].Print()));
        double tolerance = 2;
        while (previousGuesses.Contains(result))
        {
            result = "";
            foreach (var beam in beams)
            {
                List<BeamNode> reduced = beam.Where(b => b.Score * tolerance >= beam[0].Score).ToList();
                result += reduced[random.Next(reduced.Count)].Print() + " ";
            }
            result = result.Trim();
            tolerance *= 2;
        }

        //if (turn == 1)
        //{
        //    int bestLetters = Enumerable.Range('A', 26).Count(c => result.Contains((char)c));
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        string tmp = string.Join(" ", beams.Select(b => b[random.Next(10)].Print()));
        //        int letters = Enumerable.Range('A', 26).Count(c => tmp.Contains((char)c));
        //        if (letters > bestLetters)
        //        {
        //            bestLetters = letters;
        //            result = tmp;
        //        }
        //    }
        //}

        //if (turn < 20)
        //{
        //    double bestScore = Math.Pow(C / 2, CountFails(result)) * beams.Select(b => b[0].Score).Aggregate((a, b) => a * b);
        //    HashSet<string> tested = new HashSet<string> { result };
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        double currentScore = 1;
        //        string currentResult = "";
        //        foreach (var beam in beams)
        //        {
        //            var b = beam[Math.Min(beam.Count, random.Next(13 - N))];
        //            currentScore *= b.Score;
        //            currentResult += " " + b.Print();
        //        }
        //        currentResult = currentResult.Trim();
        //        currentScore *= Math.Pow(C / 2, CountFails(currentResult));
        //        if (currentScore > bestScore)
        //        {
        //            bestScore = currentScore;
        //            result = currentResult;
        //        }
        //    }
        //}

        return result;
    }

    private static int beamWidth = 100;
    private static List<BeamNode> BuildWord(int offset, int length)
    {
        counter++;
        Node node = roots[length];
        // bool spaceBefore = offset == 0 || letters[offset - 1].Probability(' ') > 0.9999;
        // bool spaceAfter = offset + length == P || letters[offset + length].Probability(' ') > 0.9999;
        // if (spaceBefore && spaceAfter)
        // {
        //     List<int> knownPos = new List<int>();
        //     List<char> knownVal = new List<char>();
        //     string key = length.ToString();
        //     for (int i = 0; i < length; i++)
        //     {
        //         char c = letters[i].KnownChar(0.9999);
        //         if (c == '-') continue;
        //         knownPos.Add(i);
        //         knownVal.Add(c);
        //         key += c + "" + i;
        //     }
        //     if (knownPos.Count > 0) node = GetNode(key, knownPos, knownVal, length);
        // }
        BeamNode.offset = offset;
        BeamNode.length = length;
        List<BeamNode> beam = new List<BeamNode> { new BeamNode(node, length) };
        for (int i = 0; i < length; i++)
        {
            List<BeamNode> next = new List<BeamNode>();
            foreach (BeamNode b in beam) next.AddRange(b.Expand());
            next = next.OrderByDescending(n => n.HeuristicScore).ToList();
            if (next.Count > beamWidth) next = next.Take(beamWidth).ToList();
            beam = next;
        }

        List<BeamNode> result = new List<BeamNode>();
        HashSet<string> words = new HashSet<string>();
        double factor = beam[0].Score;
        foreach (BeamNode b in beam)
        {
            if (words.Contains(b.Print())) continue;
            words.Add(b.Print());
            b.Score /= factor;
            result.Add(b);
        }
        return result;
    }

    private static Dictionary<string, Node> customNodes = new Dictionary<string, Node>();
    private static Node GetNode(string key, List<int> knownPos, List<char> knownVal, int length)
    {
        if (customNodes.ContainsKey(key)) return customNodes[key];
        Node result = new Node();
        foreach (string word in wordsByLength[length])
        {
            bool valid = true;
            for (int i = 0; i < knownPos.Count; i++) valid &= word[i] == knownVal[i];
            if (!valid) continue;
            foreach (List<int> order in orders[length])
                result.AddWord(word, order);
        }
        if (result.Count == 0) result = roots[length];
        customNodes[key] = result;
        return result;
    }
}