using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

using MPLVS.Core.ParseTree;
using MPLVS.ParseTree;

namespace MPLVS.SmartIndent {
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

      var position = line.Start.Position;
      if (position == 0) {
        return null;
      }

      var root = Core.Utils.ObtainOrAttachTree(view.TextBuffer).Root();

      if (root.name != "Program") {
        return null;
      }

      return DesiredIndentation(position, root);
    }

    private int? DesiredIndentation(int position, Builder.Node root) {
      var ancestors = Utils.AllAncestors(root, position - 1).ToList();

      if (!ancestors.Any()) {
        return null;
      }

      // TODO: Revise this explanation.
      // If at the right side of a caret is a closing symbol ) ; ] }
      // then indentation must be less than indentation of current block.
      //
      // In the beginning of this method, in the left side of a caret always will be a new-line character.
      // But it doesn't mean that it will be a new-line symbol.
      // For example, if caret placed inside a string, then this new-line character will be a part of a string,
      // but not the new-line symbol like LF or CRLF.
      // So, there are two possible situations:
      //   a) \n caret a-closing-symbol
      //   b) \n caret not-a-closing-symbol
      // And the \n can be: the part of a string; a new-line symbol.
      // So finally we can have 4 different situations:
      // 1) Code: \n|      Parse-tree: \n       Ancestors: (\n)
      //
      // 2) Code: (\n|)    Parse-tree: list     Ancestors: (list \n)
      //                               / | \
      //                              ( \n  )
      //
      // 3) Code: ("\n|")  Parse-tree:  list    Ancestors: (list string)
      //                              /   |  \
      //                             ( string )
      //
      // 4) Code: (\n|...) Parse-tree: list     Ancestors: (list \n)
      //                              / | | \
      //                             ( \n ... )
      //
      // Here, we interested only in 2nd case (\n|).
      // For it, Ancestors will return (list \n) and for much complicated code it will return (... list \n).
      // In this sequence of symbols (list \n), the list will contain the closing bracket as the last child.
      // And instead of the list, there can be object, code, or label.
      // But the Ancestors will return similar elements for 2nd and 4th cases, it will be (list \n),
      // so we must check the parse-tree, if it is contains something between caret and closing brace.
      if (ancestors.Count > 1) {
        var last         = ancestors.Last();
        var parentOfLast = ancestors[ancestors.Count - 2];
        var children     = parentOfLast.children;
        if (last.IsEol()) {
          if (children.Count > 1) {
            if (children.Last().IsScopeEnd()) {
              // Check if the closing bracket and a caret at the same line,
              // and between them stands nothing except tabs/spaces.
              if (ReferenceEquals(children[children.Count - 2], last)) {
                return IndentationLevel(ancestors) - IndentationSize;
              }
            }
          }
        }

        // If it is not the end-of-line, then it should be a string.
        // TODO: What should we do if a caret placed inside of a string?
      }

      // In the middle of scope.
      return IndentationLevel(ancestors);
    }

    private int IndentationLevel(List<Builder.Node> nodes) =>
      nodes.Where(a => Builder.IsBlock(a.name)) // Filter the last item, because it might be a terminal.
           .GroupBy(a => a.line)
           .Count() * IndentationSize;

    private int IndentationSize =>
      view.Options.IsConvertTabsToSpacesEnabled()
          ? view.Options.GetIndentSize()
          : view.Options.GetTabSize();

    public void Dispose() { }

    private readonly ITextView view;
  }
}