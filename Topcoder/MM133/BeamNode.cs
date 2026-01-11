using System;
using System.Collections.Generic;

public class BeamNode
{
    public char[] Text;
    public Node Node;
    public double Score = 1;
    public double HeuristicScore => Score * Node.Count;
    public static int length;
    public static int offset;

    public BeamNode(Node node, int length)
    {
        this.Text = new char[length];
        this.Node = node;
    }

    public BeamNode(Node node, BeamNode beam)
    {
        this.Node = node;
        this.Text = (char[])beam.Text.Clone();
        this.Text[node.Index] = node.C;
        this.Score = beam.Score * Math.Pow(Letter.LetterCount[length, node.Index, node.C], 0.2) * PhraseGuessing.letters[node.Index + offset].Probability(node.C);
    }

    public IEnumerable<BeamNode> Expand()
    {
        foreach (Node child in Node.Children) yield return new BeamNode(child, this);
    }

    public override string ToString() => Print() + ": " + Score + " - " + HeuristicScore;

    public string Print() => new string(Text);
}