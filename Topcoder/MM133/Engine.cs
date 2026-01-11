using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Engine
{
    public static string solution = "WILDEBEESTS TRANSITIONAL GEBANGA SNIFFERS CLARINO LEAFMOLD POLYMETAMERIC ALBUMIMETER ILLUMINISTIC IGNATIA";
    private static Random random = new Random(0);

    public static string TrueResult(string guess, string solution)
    {
        char Correct = '*';
        char Partial = '+';
        char Wrong = '.';
        if (guess == solution) return new string('*', PhraseGuessing.P + 1);
        char[] _out = new char[PhraseGuessing.P];
        bool[] usedPhrase = new bool[PhraseGuessing.P];
        bool[] usedGuess = new bool[PhraseGuessing.P];

        for (int i = 0; i < PhraseGuessing.P; i++)
        {
            char c = guess[i];

            //correct letter in the right location
            if (c == solution[i])
            {
                _out[i] = Correct;
                usedGuess[i] = true;
                usedPhrase[i] = true;
            }
        }

        int wrong = 0;
        int partial = 0;

        for (int i = 0; i < PhraseGuessing.P; i++)
        {
            if (usedGuess[i]) continue;

            char c = guess[i];

            bool found = false;
            for (int k = 0; k < PhraseGuessing.P; k++)
            {
                //correct letter in the wrong location
                if (!usedPhrase[k] && c == solution[k])
                {
                    found = true;
                    usedPhrase[k] = true;
                    _out[i] = Partial;
                    partial++;
                    break;
                }
            }

            //letter not in the phrase
            if (!found)
            {
                _out[i] = Wrong;
                wrong++;
            }

            usedGuess[i] = true;
        }

        string trueResult = new string(_out);
        return trueResult;
    }

    public static string Judge(string guess)
    {
        char[] _out = TrueResult(guess, solution).ToCharArray();

        //Now corrupt the output :)
        for (int i = 0; i < PhraseGuessing.P; i++)
            if (random.NextDouble() < PhraseGuessing.C)
                _out[i] = ".+*"[random.Next(3)];


        string corruptResult = new string(_out);
        return corruptResult;
    }

    internal static void PrintStats(int guesses, string guess, string result)
    {
        List<char> chars = Enumerable.Range('A', 26).Select(c => (char)c).ToList();
        List<string> lines = new List<string> { "phrase: " + solution + "\nguess:  " + guess + "\nresult: " + result };
        chars.Insert(0, ' ');
        for (int i = 0; i < PhraseGuessing.P; i++)
        {
            lines.Add("Position " + i + "  - " + solution[i]);
            foreach (char c in chars)
                lines.Add($"'{c}':  '.':{PhraseGuessing.letters[i].Freq[c, '.']}  '+':{PhraseGuessing.letters[i].Freq[c, '+']}  '*':{PhraseGuessing.letters[i].Freq[c, '*']}  Prob:{PhraseGuessing.letters[i].Probability(c)}");
            lines.Add("-------");
        }
        File.WriteAllLines("log/" + guesses.ToString("000") + ".txt", lines.ToArray());
    }

    public static void RandomSim()
    {
        int[] freq = new int[26];
        double c = 0.05;
        for (int i = 0; i < 1e8; i++)
        {
            int secret = random.Next(26);
            // guess A 2 times, expect 2 *
            bool outcomeStar = secret == 0;
            if (secret == 0 && random.NextDouble() < c && random.Next(3) != 0) outcomeStar = false;
            if (secret != 0 && random.NextDouble() < c && random.Next(3) == 0) outcomeStar = true;
            if (!outcomeStar) continue;
            outcomeStar = secret == 0;
            if (secret == 0 && random.NextDouble() < c && random.Next(3) != 0) outcomeStar = false;
            if (secret != 0 && random.NextDouble() < c && random.Next(3) == 0) outcomeStar = true;
            if (!outcomeStar) continue;

            //guess B once, expect *
            outcomeStar = secret == 1;
            if (secret == 1 && random.NextDouble() < c && random.Next(3) != 0) outcomeStar = false;
            if (secret != 1 && random.NextDouble() < c && random.Next(3) == 0) outcomeStar = true;
            if (!outcomeStar) continue;
            freq[secret]++;
        }
    }
}