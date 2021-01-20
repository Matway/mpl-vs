using System;
using System.Collections.Generic;
using System.Linq;

namespace MPLVS.ParseTree {
  public class Builder {
    public class Node {
      public int begin;
      public int end;
      public int line;
      public int column;
      public string name;
      public List<Node> children;
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
      nodes.Clear();
      errors.Clear();

      this.source = source ?? throw new ArgumentNullException(nameof(source));
      nodes.Push(new Node {
        begin    = 0,
        end      = source.Length,
        line     = 0,
        column   = 0,
        name     = "Program",
        children = new List<Node>()
      });

      parser.Initialize(source);
      parser.ParseProgramWithEOF();

      parsed = errors.Count == 0;

      return nodes.Peek();
    }

    public void Reset(object o, EventArgs e) {
      nodes.Clear();
      errors.Clear();
      nodes.Push(new Node {
        begin    = 0,
        end      = source.Length,
        line     = 0,
        column   = 0,
        name     = "Program",
        children = new List<Node>()
      });
    }

    private void AddChild(Node n) {
      nodes.Peek().children.Add(n);
    }

    public void StartCompound(object obj, (string name, int begin, int line, int column) startInfo) {
      if (!IsBlock(startInfo.name)) {
        return;
      }

      var node = new Node {
        begin    = startInfo.begin,
        end      = startInfo.begin,
        name     = startInfo.name,
        line     = startInfo.line,
        column   = startInfo.column,
        children = new List<Node>()
      };

      if (nodes.Any()) {
        AddChild(node);
      }
      nodes.Push(node);
    }

    public void EndCompound(object obj, (string name, int end) endInfo) {
      if (!IsBlock(endInfo.name)) {
        return;
      }

      if (nodes.Count > 1) {
        nodes.Peek().end = endInfo.end;
      }
      nodes.Pop();
    }

    public void Terminal(object obj, (string name, int begin, int end, int line, int column) terminalInfo) {
      AddChild(new Node {
        begin  = terminalInfo.begin,
        end    = terminalInfo.end,
        line   = terminalInfo.line,
        column = terminalInfo.column,
        name   = terminalInfo.name
      });
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