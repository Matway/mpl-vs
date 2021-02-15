using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

using MPLVS.Core.ParseTree;
using MPLVS.ParseTree;
using MPLVS.Symbols;

namespace MPLVS.SmartIndent {
  internal class Indentation : ISmartIndent {
    private struct Level {
      public int Depth;
      public int OpenCount;
      public bool IsClosed;
    }

    public Indentation(ITextView view) {
      if (view is null) {
        throw new ArgumentNullException(nameof(view));
      }

      this.view = view;
    }

    public int? GetDesiredIndentation(ITextSnapshotLine line) {
      if (line is null || line.Start.Position == 0) { return null; }

      var root = Core.Utils.ObtainOrAttachTree(this.view.TextBuffer).Root();

      return
        root.name != "Program"
        ? null
        : this.DesiredIndentation(line.LineNumber, root);
    }

    // TODO: What should we do if a caret placed inside of a string?
    private int? DesiredIndentation(int line, Builder.Node node) {
      var currentLineIndentation = 0;
      var nextLineIndentation = 0;
      var lastOpenLine = int.MinValue;
      var levels = new Stack<Level>();
      foreach (var currentLine in Files.Flatten(node, SimplifyInlineBlock()).TakeWhile(a => a.line <= line).GroupBy(a => a.line)) {
        currentLineIndentation = nextLineIndentation;
        ProcessFirstSymbol(ref currentLineIndentation, ref nextLineIndentation, ref lastOpenLine, levels, currentLine);
        ProcessRestSymbols(ref nextLineIndentation, ref lastOpenLine, levels, currentLine);
      }

      return currentLineIndentation * this.IndentationSize;
    }

    private static void ProcessFirstSymbol(ref int currentLineIndentation, ref int nextLineIndentation, ref int lastOpenLine, Stack<Level> levels, IGrouping<int, Builder.Node> currentLine) {
      var first = currentLine.First();

      if (first.IsScopeStart()) {
        lastOpenLine = first.line;
        ++nextLineIndentation;
        levels.Push(new Level { Depth = nextLineIndentation, OpenCount = 1 });
      }
      else if (first.IsScopeEnd()) {
        var last = levels.Pop();

        if (!last.IsClosed) {
          --currentLineIndentation;
          --nextLineIndentation;
          levels.Push(new Level { Depth = nextLineIndentation, OpenCount = last.OpenCount - 1, IsClosed = true });
        }
        else {
          levels.Push(new Level { OpenCount = last.OpenCount - 1, IsClosed = true });
        }

        if (levels.Peek().OpenCount < 1) {
          _ = levels.Pop();
        }
      }
    }

    private static Func<Builder.Node, Builder.Node> SimplifyInlineBlock() => node => {
      if (node.IsScope() && node.children is object && node.children.Any()) {
        var end = node.children.Last();

        if (end.IsScopeEnd() && node.line == end.line && end.end == node.end) {
          return new ParseTree.Builder.Node { name = "", line = node.line };
        }
      }

      return node;
    };

    private static void ProcessRestSymbols(ref int nextLineIndentation, ref int lastOpenLine, Stack<Level> levels, IGrouping<int, Builder.Node> currentLine) {
      foreach (var symbol in currentLine.Skip(1)) {
        if (symbol.IsScopeStart()) {
          if (lastOpenLine != symbol.line) {
            lastOpenLine = symbol.line;
            ++nextLineIndentation;
            levels.Push(new Level { Depth = nextLineIndentation, OpenCount = 1 });
          }
          else {
            var last = levels.Pop();
            levels.Push(new Level { IsClosed = last.IsClosed, OpenCount = last.OpenCount + 1, Depth = last.Depth });
          }
        }
        else if (symbol.IsScopeEnd()) {
          var last = levels.Pop();
          if (last.IsClosed) {
            levels.Push(new Level { IsClosed = true, Depth = last.Depth, OpenCount = last.OpenCount - 1 });
          }
          else {
            levels.Push(new Level { IsClosed = true, Depth = last.Depth - 1, OpenCount = last.OpenCount - 1 });
          }

          nextLineIndentation = levels.Peek().Depth;
          if (levels.Peek().OpenCount < 1) {
            _ = levels.Pop();
          }
        }
      }
    }

    private int IndentationSize =>
      this.view.Options.IsConvertTabsToSpacesEnabled()
      ? this.view.Options.GetIndentSize()
      : this.view.Options.GetTabSize();

    public void Dispose() { }

    private readonly ITextView view;
  }
}