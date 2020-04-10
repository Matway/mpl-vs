using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using MPL.AST;

namespace MPL.Commands {
  internal class GoToDefinitionCommandHandler : VSCommandTarget<VSConstants.VSStd97CmdID> {
    private string selectedName;
    private int selectionEnd;
    private string currName;
    private int definitionBegin;
    private int definitionEnd;
    private bool nameFound = false;

    public GoToDefinitionCommandHandler(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override IEnumerable<VSConstants.VSStd97CmdID> SupportedCommands {
      get {
        yield return VSConstants.VSStd97CmdID.GotoDefn;
      }
    }

    protected override bool Execute(VSConstants.VSStd97CmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (!TextView.Selection.IsEmpty) {
        selectedName = TextView.TextBuffer.CurrentSnapshot.GetText(TextView.Selection.SelectedSpans.First().Span);
        selectionEnd = TextView.Selection.End.Position.Position;
        nameFound = true;
      } else {
        nameFound = findTheName();
      }

      if (nameFound) {
        if (findDefinition()) {
          SnapshotPoint defBegin = new SnapshotPoint(TextView.TextSnapshot, definitionBegin);
          SnapshotPoint defEnd = new SnapshotPoint(TextView.TextSnapshot, definitionEnd);
          TextView.Selection.Clear();
          TextView.Selection.Select(new SnapshotSpan(defBegin, defEnd), false);
          TextView.Caret.MoveTo(defEnd);
          TextView.Caret.EnsureVisible();
          int offset = TextView.TextViewLines.Count / 2 - TextView.TextViewLines.GetIndexOfTextLine(TextView.Caret.ContainingTextViewLine);
          if (offset > 0) {
            TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Up, offset);
          } else {
            TextView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, -offset);
          }
        } else {
          //vsRunningDocumentTable.
          //MplPackage.Dte.FullName.ToString();
          //StreamReader streamReader = File.OpenText("");
        }
      }

      return true;
    }

    private bool findTheName() {
      AST.TreeBuilder.Node root = AST.AST.GetASTRoot();

      int caretPosition = TextView.Caret.Position.BufferPosition.Position;
      bool foundName = false;

      void Traverse(TreeBuilder.Node node) {
        if (node.children == null) {
          if (node.name == "Name") {
            selectedName = TextView.TextBuffer.CurrentSnapshot.GetText(node.begin, node.end - node.begin);
            selectionEnd = node.end;
            foundName = true;
          } else if (node.name == "NameWrite" || node.name == "NameRead") {
            selectedName = TextView.TextBuffer.CurrentSnapshot.GetText(node.begin + 1, node.end - node.begin - 1);
            selectionEnd = node.end;
            foundName = true;
          }

          return;
        }

        foreach (var child in node.children) {
          if (child.begin <= caretPosition && child.end >= caretPosition) {
            Traverse(child);
          }
        }
      }

      Traverse(root);

      return foundName;
    }

    private bool findDefinition() {
      AST.TreeBuilder.Node root = AST.AST.GetASTRoot();
      bool definitionFound = false;

      void Traverse(TreeBuilder.Node node) {
        if (node.children == null) {
          return;
        }

        if ((node.name == "Code" || node.name == "Object" || node.name == "List") && node.end < selectionEnd) {
          return;
        }

        if (node.name == "Label") {
          TreeBuilder.Node child = node.children[0];
          currName = TextView.TextBuffer.CurrentSnapshot.GetText(child.begin, child.end - child.begin);
          if (currName == selectedName) {
            definitionBegin = child.begin;
            definitionEnd = child.end;
            definitionFound = true;
          }
        }

        foreach (var child in node.children) {
          if (child.begin < selectionEnd) {
            Traverse(child);
          }
        }
      }

      Traverse(root);

      return definitionFound;
    }

    protected override VSConstants.VSStd97CmdID ConvertFromCommandId(uint id) {
      return (VSConstants.VSStd97CmdID)id;
    }

    protected override uint ConvertFromCommand(VSConstants.VSStd97CmdID command) {
      return (uint)command;
    }
  }
}
