using System;
using System.Collections.Generic;
using System.Linq;

using static MPLVS.ParseTree.Builder;

namespace MPLVS.ParseTree {
  public static class Utils {
    public static IEnumerable<Node> AsSequence(this Node node) {
      if (node is null) { throw new ArgumentNullException(nameof(node)); }

      var next = node;
      do {
        yield return next;
        next = next.Next;
      } while (next is object);

      yield break;
    }

    public static IEnumerable<Node> AsReverseSequence(this Node node) {
      if (node is null) { throw new ArgumentNullException(nameof(node)); }

      var previous = node;
      do {
        yield return previous;
        previous = previous.Previous;
      } while (previous is object);

      yield break;
    }
  }

  public class Builder {
    private Node Last;

    public class Node : IEquatable<Node> {
      public int begin;
      public int end;
      public int line;
      public int column;
      public string name;
      public List<Node> children;

      // TODO: Replace the stuff below by 'unrolled list'.
      public Node Previous, Next;

      /// <summary>Do not use this method, because it is not equality comparer.</summary>
      [Obsolete("Do not use this method, because it is not equality comparer.")]
      public bool Equals(Node other) => this.begin == other.begin && this.end == other.end;
    }

    public string source;
    private readonly Stack<Node> nodes;
    internal Parser parser;
    public Stack<Parser.SyntaxError> errors;

    public Builder() {
      parser = new Parser();
      nodes  = new Stack<Node>();
      errors = new Stack<Parser.SyntaxError>();

      parser.Reset         += Reset;
      parser.EndCompound   += EndCompound;
      parser.StartCompound += StartCompound;
      parser.Terminal      += Terminal;
      parser.PushError     += PushError;
    }

    public Node GetRoot(string source, out bool parsed) {
      this.source = source ?? throw new ArgumentNullException(nameof(source));

      this.Reset(null, null);

      parser.Initialize(source);
      parser.ParseProgramWithEOF();

      parsed = errors.Count == 0;

      return nodes.Peek();
    }

    public void Reset(object o, EventArgs e) {
      this.nodes.Clear();
      this.errors.Clear();

      this.Last = new Node {
        begin = 0,
        end = this.source.Length,
        line = 0,
        column = 0,
        name = "Program",
        children = new List<Node>()
      };

      nodes.Push(this.Last);
    }

    private void AddChild(Node n) {
      nodes.Peek().children.Add(n);
    }

    public void StartCompound(object obj, Parser.CompoundStart startInfo) {
      if (!IsBlock(startInfo.Name)) {
        return;
      }

      var node = new Node {
        begin    = startInfo.Begin,
        end      = startInfo.Begin,
        name     = startInfo.Name,
        line     = startInfo.Line,
        column   = startInfo.Column,
        children = new List<Node>()
      };

      this.AddSiblings(node);

      if (nodes.Any()) {
        AddChild(node);
      }

      nodes.Push(node);
    }

    public void EndCompound(object obj, Parser.CompoundEnd endInfo) {
      if (!IsBlock(endInfo.Name)) {
        return;
      }

      if (nodes.Count > 1) {
        nodes.Peek().end = endInfo.End;
      }
      nodes.Pop();
    }

    public void Terminal(object obj, Parser.TerminalStart terminalInfo) {
      var current = new Node {
        begin  = terminalInfo.Begin,
        end    = terminalInfo.End,
        line   = terminalInfo.Line,
        column = terminalInfo.Column,
        name   = terminalInfo.Name
      };

      this.AddSiblings(current);

      AddChild(current);
    }

    private void AddSiblings(Node current) {
      current.Previous = this.Last;
      this.Last.Next   = current;

      this.Last = current;
    }

    public void PushError(object obj, Parser.SyntaxError error) =>
      errors.Push(error);

    public static bool IsBlock(string text) => starts.Contains(text);

    private static readonly string[] starts = new string[] {
      "Object",
      "Code",
      "List",
      "Label"
    };
  }
}