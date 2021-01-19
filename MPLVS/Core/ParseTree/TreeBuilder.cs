using System.Collections.Generic;
using System.Linq;
using System;

namespace MPL.ParseTree {
  internal class Builder : MPLParser.EventHandler {
    public class Node {
      public int begin;
      public int end;
      public int line;
      public string name;
      public List<Node> children;
    }

    public string source;
    private readonly Stack<Node> nodes;
    protected MPLParser parser;
    public Stack<MPLParser.ParseException> errors;
    public List<string> names;

    public Builder() {
      parser = new MPLParser();
      nodes = new Stack<Node>();
      errors = new Stack<MPLParser.ParseException>();
      names = new List<string>();
    }

    public Node getRoot(string source, out bool parsed) {
      nodes.Clear();
      errors.Clear();
      names.Clear();

      this.source = source;
      nodes.Push(new Node {
        begin = 0,
        end = source.Length,
        line = 0,
        name = "Program",
        children = new List<Node>()
      });

      parser.initialize(source, this);
      parser.parse_ProgramWithEOF();

      parsed = errors.Count == 0;

      return nodes.Peek();
    }

    // TODO: Why the argument is ignored?
    public void reset(string s) {
      nodes.Clear();
      errors.Clear();
      nodes.Push(new Node {
        begin = 0,
        end = source.Length,
        line = 0,
        name = "Program",
        children = new List<Node>()
      });
    }

    private void AddChild(Node n) {
      nodes.Peek().children.Add(n);
    }

    public void startNonterminal(string name, int begin, int line) {
      switch (name) {
        case "Code":
        case "List":
        case "Object":
        case "Label": break;
        default: return;
      }

      var node = new Node {
        begin = begin,
        end = begin,
        name = name,
        line = line,
        children = new List<Node>()
      };

      if (nodes.Any()) AddChild(node);
      nodes.Push(node);
    }

    public void endNonterminal(string name, int end) {
      switch (name) {
        case "Code":
        case "List":
        case "Object":
        case "Label": break;
        default: return;
      }

      if (nodes.Count > 1) nodes.Peek().end = end;
      nodes.Pop();
    }

    public void terminal(string name, int begin, int end, int line) {
      AddChild(new Node {
        begin = begin,
        end = end,
        name = name,
        line = line
      });
    }

    public void pushError(int p, int l, int c, string t, string m) {
      errors.Push(new MPLParser.ParseException(p, l, c, t, m));
    }

    public void getName(string name) {
      if (!names.Contains(name)) {
        names.Add(name);
      }
    }

    public void whitespace(int begin, int end) {
      //throw new NotImplementedException();
    }
  }

  // FIXME: Static class with mutable state.
  internal static class Tree {
    private static readonly Builder builder = new Builder();
    private static Builder.Node root = new Builder.Node();
    public static List<string> nameList = new List<string>();

    public static void Parse (string src, out bool parsed) {
      root = builder.getRoot(src, out parsed);
      nameList = builder.names;
    }

    public static Builder.Node Root() {
      return root;
    }

    public static Stack<MPLParser.ParseException> GetErrorStack() {
      return builder.errors;
    }
  }
}