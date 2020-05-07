using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

using MPL.ParseTree;

namespace MPL.SmartIndent {
  internal class Indentation : ISmartIndent {
    public Indentation(ITextView view) {
      if (view is null) {
        throw new ArgumentNullException(nameof(view));
      }

      this.view = view;
    }

    public int? GetDesiredIndentation(ITextSnapshotLine line) {
      if (line is null) {
        return null;
      }

      var wholeDocument = view.TextBuffer.CurrentSnapshot.GetText();
      var position = line.Start.Position;

      if (position == 0) {
        return null;
      }

      // FIXME: Reuse already existing parse-tree from some other subsystem of the add-in.
      Tree.Parse(wholeDocument, out bool _);
      var root = Tree.Root();

      if (root.name != "Program") {
        return null;
      }

      return DesiredIndentation(position, root);
    }

    private int? DesiredIndentation(int position, Builder.Node root) {
      var nodes = FlatPath(root, position - 1).Where(a => !(a is null))
                                              .ToList();

      if (nodes.Count == 0) {
        return null;
      }

      // Near the closing token.
      if (nodes.Count > 1) {
        var parentOfLast = nodes[nodes.Count - 2];
        var children = parentOfLast.children;
        if (children.Count > 1) {
          var last = nodes.Last();
          if (IsClosingBracket(children.Last()) && ReferenceEquals(children[children.Count - 2], last)) {
            return IndentationLevel(nodes) - IndentationSize;
          }
        }
      }

      // In the middle of scope.
      return IndentationLevel(nodes);
    }

    private int IndentationLevel(List<Builder.Node> nodes) =>
      nodes.Where(a => IsOpeningBracket(a)) // Filter the last item, because it might be a terminal.
           .GroupBy(a => a.line)
           .Count() * IndentationSize;

    private int IndentationSize =>
      view.Options.IsConvertTabsToSpacesEnabled()
          ? view.Options.GetIndentSize()
          : view.Options.GetTabSize();

    // Gives {parent-of-the-next-item, ..., parent-of-the-last-item, last-item}
    public static IEnumerable<Builder.Node> FlatPath(Builder.Node node, int position) {
      var tree = node;
      while (!(tree is null) && !IsEmpty(tree.children)) {
        // We do not check the result of Parent, so the last item can be null, or terminal.
        tree = Parent(tree, position);
        yield return tree;
      }

      bool IsEmpty(IEnumerable<Builder.Node> a) =>
        a is null || !a.Any();
    }

    /// <summary> Searches the node which holds a text at given position. </summary>
    /// <param name="node"></param>
    /// <param name="position"></param>
    /// <returns>
    /// Returns the child of the node, which contains the text,
    /// or contains other sub-node which contains the text, or other sub-node which...
    /// </returns>
    public static Builder.Node Parent(Builder.Node node, int position) {
      // Standard binary search works only with the items with the same type,
      // i.e. collection and item must have the same base type.
      // So, we workaround it by wrapping our item in the type of the collection's elements.
      // And then we use comparer specially prepared for the workaround.
      var idx = node.children.BinarySearch(new Builder.Node { begin = position },
                                           new Comparer());
      return idx < 0
             ? null
             : node.children[idx];
    }

    // NOTE: Do not use this comparer if you don't understand how it works.
    // Because it is working as exactly as you don't expect.
    private class Comparer : IComparer<Builder.Node> {
      public int Compare(Builder.Node a, Builder.Node b) =>
        b.begin >= a.begin && b.begin < a.end
        ? 0
        : a.begin - b.begin;
    }

    private bool IsClosingBracket(Builder.Node a) => ends.Contains(a.name);

    private static readonly string[] ends = new string[] {
      "']'",
      "')'",
      "'}'",
      "';'"
    };

    private static bool IsOpeningBracket(Builder.Node a) => starts.Contains(a.name);

    private static readonly string[] starts = new string[] {
      "Object",
      "Code",
      "List",
      "Label"
    };

    public void Dispose() { }

    private readonly ITextView view;
  }
}