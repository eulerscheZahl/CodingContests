using System;
using System.Collections.Generic;
using System.Linq;

public class Letter
{
    public double[] Prob = new double['Z' + 1];
    public int[,] Freq = new int['Z' + 1, '.' + 1];
    public static int[,,] LetterCount = new int[50, 50, 'Z' + 1];
    private static string alphabet = " ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static double[,] binomial = new double[1000, 1000];
    private static List<double[]>[] expectedCurves = new List<double[]>[50];

    public static void Init()
    {
        for (int n = 0; n < 1000; n++)
        {
            binomial[n, 0] = 1;
            for (int k = 1; k <= n; k++) binomial[n, k] = binomial[n - 1, k - 1] + binomial[n - 1, k];
        }
        for (int i = 0; i < byLetter.Length; i++) byLetter[i] = new List<List<char>>();

        for (int guessCount = 0; guessCount < expectedCurves.Length; guessCount++)
        {
            List<double[]> curves = new List<double[]>();
            for (int realCount = 0; realCount <= guessCount; realCount++)
            {
                double[] exp = new double[guessCount + 1];
                curves.Add(exp);
                double[] firstCurve = BuildCurve(realCount, 1 - PhraseGuessing.C / 3);
                double[] lastCurve = BuildCurve(guessCount - realCount, 2 * PhraseGuessing.C / 3);
                for (int i = 0; i < firstCurve.Length; i++)
                {
                    for (int j = 0; j < lastCurve.Length; j++) exp[i + j] += firstCurve[i] * lastCurve[j];
                }
            }
            expectedCurves[guessCount] = curves;
        }
    }

    private static double[] BuildCurve(int max, double p)
    {
        double[] result = new double[max + 1];
        for (int i = 0; i <= max; i++)
            result[i] = Math.Pow(p, i) * Math.Pow(1 - p, max - i) * binomial[max, i];
        return result;
    }

    public Letter()
    {
        foreach (char c in alphabet) Prob[c] = 0.5;
    }

    public double Probability(char c)
    {
        return Prob[c];
    }

    internal void Update(char guess, char result)
    {
        double C = PhraseGuessing.C;
        Freq[guess, result]++;
        Freq[' ', '.'] = 0;

        double[] individialProb = new double['Z' + 1];
        foreach (char c in alphabet)
        {
            individialProb[c] = 1;
            foreach (char c2 in alphabet)
            {
                double p_A_A = 1 - C + C / 3;
                double p_nA_A = C / 3;
                if (c == ' ') p_A_A = 1 - C / 2;
                if (c2 == ' ') p_nA_A = C / 2;
                double p_A_nA = 1 - p_A_A;
                double p_nA_nA = 1 - p_nA_A;
                int star = Freq[c2, '*'];
                int notStar = Freq[c2, '+'] + Freq[c2, '.'];
                if (c == c2) individialProb[c] *= Math.Pow(p_A_A, star) * Math.Pow(p_A_nA, notStar);
                else individialProb[c] *= Math.Pow(p_nA_A, star) * Math.Pow(p_nA_nA, notStar);
            }
        }

        double sum = individialProb.Sum();
        Prob = individialProb.Select(p => p / sum).ToArray();
    }

    private static List<List<char>>[] byLetter = new List<List<char>>['Z' + 1];
    internal static void UpdateGlobal(string guess, string result, List<Letter> letters)
    {
        for (char c = 'A'; c <= 'Z'; c++)
        {
            byLetter[c].Add(Enumerable.Range(0, guess.Length).Where(i => guess[i] == c).Select(i => result[i]).ToList());
            int max = byLetter[c].Max(b => b.Count);
            double[] prob = Enumerable.Range(0, max + 1).Select(i => 1.0).ToArray();
            for (int realCount = 0; realCount <= max; realCount++)
            {
                foreach (List<char> let in byLetter[c])
                {
                    int correct = let.Count(l => l != '.');
                    prob[realCount] *= expectedCurves[let.Count][Math.Min(realCount, let.Count)][correct];
                }
            }
            double maxProb = prob.Max();
            prob = prob.Select(p => p / maxProb).ToArray();
            if (prob.Length > 1 && prob[0] == 1 && prob[1] < 0.2)
            {
                foreach (Letter l in letters) l.Prob[c] = Math.Min(l.Prob[c], prob[1]);
            }
        }
    }

    internal char KnownChar(double confidence)
    {
        foreach (char c in alphabet)
        {
            if (Prob[c] > confidence) return c;
        }
        return '-';
    }
}