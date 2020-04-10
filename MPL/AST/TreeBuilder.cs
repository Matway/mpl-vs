using System.Collections.Generic;
using System.Linq;
using System;

namespace MPL.AST {
  internal class TreeBuilder : MPLParser.EventHandler {
    public class Node {
      public int begin;
      public int end;
      public int line;
      public string name;
      public List<Node> children;
    }

    public string source;
    private readonly Stack<Node> programStack;
    protected MPLParser parser;
    public Stack<MPLParser.ParseException> errorStack;
    public List<string> nameList;
    
    public TreeBuilder() {
      parser = new MPLParser();
      programStack = new Stack<Node>();
      errorStack = new Stack<MPLParser.ParseException>();
      nameList = new List<string>();
    }

    public Node getRoot(string source, out bool parsed) {
      programStack.Clear();
      errorStack.Clear();
      nameList.Clear();
      this.source = source;
        programStack.Push(new Node {
          begin = 0,
          end = source.Length,
          line = 0,
          name = "Program",
          children = new List<Node>()
        });
        parser.initialize(source, this);
        parser.parse_ProgramWithEOF();
        parsed = true;
      if (errorStack.Count > 0) {
        parsed = false;
      }

      return programStack.Peek();
    }

    public void reset(string s) {
      programStack.Clear();
      errorStack.Clear();
      programStack.Push(new Node {
        begin = 0,
        end = source.Length,
        line = 0,
        name = "Program",
        children = new List<Node>()
      });
    }

    private void AddChild(Node n) {
      programStack.Peek().children.Add(n);
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

      if (programStack.Any()) AddChild(node);
      programStack.Push(node);
    }

    public void endNonterminal(string name, int end) {
      switch (name) {
        case "Code":
        case "List":
        case "Object":
        case "Label": break;
        default: return;
      }

      if (programStack.Count() > 1) programStack.Peek().end = end;
      programStack.Pop();
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
      errorStack.Push(new MPLParser.ParseException(p, l, c, t, m));
    }

    public void getName(string name) {
      if (!nameList.Contains(name)) {
        nameList.Add(name);
      }
    }

    public void whitespace(int begin, int end) {
      //throw new NotImplementedException();
    }
  }

  internal static class AST {
    private static TreeBuilder treeBuilder;
    private static TreeBuilder.Node astRoot;
    public static List<string> nameList;

    static AST() {
      treeBuilder = new TreeBuilder();
      astRoot = new TreeBuilder.Node();
      nameList = new List<string>();
    }

    public static void Parse (string src, out bool parsed) {
      astRoot = treeBuilder.getRoot(src, out parsed);
      nameList = treeBuilder.nameList;
    }

    public static TreeBuilder.Node GetASTRoot() {
      return astRoot;
    }

    public static Stack<MPLParser.ParseException> GetErrorStack() {
      return treeBuilder.errorStack;
    }
  }
}