using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using MPL.AST;

namespace MPL.Commands {
  class CaretHandler : IOleCommandTarget {
    private CaretPosition? caret;
    private ITextSnapshotLine currentTextLine;
    private ITextSnapshot currentSnap;
    private int offset;
    private int? maxOffset;
    private int indentationSize;
    private TreeBuilder.Node lastToken;
    private Stack<Stack<TreeBuilder.Node>> bracketsStack;
    private Stack<TreeBuilder.Node> currentLevelBrackets;
    private bool wasPoped;
    private bool notReached;
    private IOleCommandTarget _NextCommandTarget;
    protected readonly IVsTextView VsTextView;
    protected readonly IWpfTextView TextView;
    protected readonly Guid CommandGroupId;
    protected readonly HashSet<uint> CommandIdSet;

    public CaretHandler(IVsTextView vsTextView, IWpfTextView textView) {
      VsTextView = vsTextView;
      TextView = textView;
      maxOffset = null;
      CommandGroupId = typeof(VSConstants.VSStd2KCmdID).GUID;
      CommandIdSet = new HashSet<uint>(SupportedCommands.Select(x => (uint)(x)));

      VsTextView.AddCommandFilter(this, out _NextCommandTarget);
    }

    protected virtual bool IsEnabled { get { return true; } }
    protected virtual bool SupportsAutomation { get { return false; } }

    protected IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands {
      get {
        yield return VSConstants.VSStd2KCmdID.TYPECHAR;
        yield return VSConstants.VSStd2KCmdID.BACKSPACE;
        yield return VSConstants.VSStd2KCmdID.TAB;
        yield return VSConstants.VSStd2KCmdID.RETURN;
        yield return VSConstants.VSStd2KCmdID.LEFT;
        yield return VSConstants.VSStd2KCmdID.LEFT_EXT;
        yield return VSConstants.VSStd2KCmdID.RIGHT;
        yield return VSConstants.VSStd2KCmdID.RIGHT_EXT;
        yield return VSConstants.VSStd2KCmdID.BACKTAB;
        yield return VSConstants.VSStd2KCmdID.BOL;
        yield return VSConstants.VSStd2KCmdID.BOL_EXT;
        yield return VSConstants.VSStd2KCmdID.BOL_EXT_COL;
        yield return VSConstants.VSStd2KCmdID.CANCEL;
        yield return VSConstants.VSStd2KCmdID.DELETETOBOL;
        yield return VSConstants.VSStd2KCmdID.DELETETOEOL;
        yield return VSConstants.VSStd2KCmdID.DELETEWHITESPACE;
        yield return VSConstants.VSStd2KCmdID.DELETEWORDLEFT;
        yield return VSConstants.VSStd2KCmdID.DELETEWORDRIGHT;
        yield return VSConstants.VSStd2KCmdID.END;
        yield return VSConstants.VSStd2KCmdID.END_EXT;
        yield return VSConstants.VSStd2KCmdID.EOL;
        yield return VSConstants.VSStd2KCmdID.EOL_EXT;
        yield return VSConstants.VSStd2KCmdID.EOL_EXT_COL;
        yield return VSConstants.VSStd2KCmdID.FIRSTCHAR;
        yield return VSConstants.VSStd2KCmdID.FIRSTCHAR_EXT;
        yield return VSConstants.VSStd2KCmdID.FIRSTNONWHITENEXT;
        yield return VSConstants.VSStd2KCmdID.FIRSTNONWHITEPREV;
        yield return VSConstants.VSStd2KCmdID.FIRSTNONWHITEPREV;
        yield return VSConstants.VSStd2KCmdID.GOTOBRACE;
        yield return VSConstants.VSStd2KCmdID.GOTOBRACE_EXT;
        yield return VSConstants.VSStd2KCmdID.GOTONEXTBOOKMARK;
        yield return VSConstants.VSStd2KCmdID.GOTOPREVBOOKMARK;
        yield return VSConstants.VSStd2KCmdID.HOME;
        yield return VSConstants.VSStd2KCmdID.HOME_EXT;
        yield return VSConstants.VSStd2KCmdID.LASTCHAR;
        yield return VSConstants.VSStd2KCmdID.LASTCHAR_EXT;
        yield return VSConstants.VSStd2KCmdID.WORDNEXT;
        yield return VSConstants.VSStd2KCmdID.WORDNEXT_EXT;
        yield return VSConstants.VSStd2KCmdID.WORDNEXT_EXT_COL;
        yield return VSConstants.VSStd2KCmdID.WORDPREV;
        yield return VSConstants.VSStd2KCmdID.WORDPREV_EXT;
        yield return VSConstants.VSStd2KCmdID.WORDPREV_EXT_COL;
        yield return VSConstants.VSStd2KCmdID.UNDO;
        yield return VSConstants.VSStd2KCmdID.UNDONOMOVE;
        yield return VSConstants.VSStd2KCmdID.GlobalUndo;
        yield return VSConstants.VSStd2KCmdID.REDO;
        yield return VSConstants.VSStd2KCmdID.REDONOMOVE;
        yield return VSConstants.VSStd2KCmdID.GlobalRedo;

        yield return VSConstants.VSStd2KCmdID.UP;
        yield return VSConstants.VSStd2KCmdID.UP_EXT;
        yield return VSConstants.VSStd2KCmdID.DOWN;
        yield return VSConstants.VSStd2KCmdID.DOWN_EXT;
        yield return VSConstants.VSStd2KCmdID.PAGEDN;
        yield return VSConstants.VSStd2KCmdID.PAGEDN_EXT;
        yield return VSConstants.VSStd2KCmdID.PAGEUP;
        yield return VSConstants.VSStd2KCmdID.PAGEUP_EXT;
        yield return VSConstants.VSStd2KCmdID.TOPLINE;
        yield return VSConstants.VSStd2KCmdID.TOPLINE_EXT;
        yield return VSConstants.VSStd2KCmdID.BOTTOMLINE;
        yield return VSConstants.VSStd2KCmdID.BOTTOMLINE_EXT;
      }
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      var result = _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      if (pguidCmdGroup == CommandGroupId && CommandIdSet.Contains(nCmdID)) {
        var command = (VSConstants.VSStd2KCmdID)nCmdID;

        if (MplPackage.completionSession) {
          return result;
        }

        caret = TextView.Caret.Position;
        if (caret == null) {
          return result;
        }

        currentSnap = TextView.TextSnapshot;
        currentTextLine = currentSnap.GetLineFromPosition(caret.Value.BufferPosition.Position);

        if (maxOffset == null) {
          maxOffset = Math.Abs(TextView.Caret.Position.BufferPosition.Difference(currentTextLine.Start)) + caret.Value.VirtualSpaces;
        }

        switch (command) {
          case VSConstants.VSStd2KCmdID.UP:
          case VSConstants.VSStd2KCmdID.UP_EXT:
          case VSConstants.VSStd2KCmdID.DOWN:
          case VSConstants.VSStd2KCmdID.DOWN_EXT:
          case VSConstants.VSStd2KCmdID.PAGEDN:
          case VSConstants.VSStd2KCmdID.PAGEDN_EXT:
          case VSConstants.VSStd2KCmdID.PAGEUP:
          case VSConstants.VSStd2KCmdID.PAGEUP_EXT:
          case VSConstants.VSStd2KCmdID.TOPLINE:
          case VSConstants.VSStd2KCmdID.TOPLINE_EXT:
          case VSConstants.VSStd2KCmdID.BOTTOMLINE:
          case VSConstants.VSStd2KCmdID.BOTTOMLINE_EXT:
          offset = 0;
          notReached = true;
          lastToken = new TreeBuilder.Node { line = 0 };
          currentLevelBrackets = new Stack<TreeBuilder.Node>();
          bracketsStack = new Stack<Stack<TreeBuilder.Node>>();

          if (TextView.Options.IsConvertTabsToSpacesEnabled()) {
            indentationSize = TextView.Options.GetIndentSize();
          } else {
            indentationSize = 1;
          }

          if (currentTextLine.Length == 0) {
            getOffset();
            TextView.Caret.MoveTo(new VirtualSnapshotPoint(currentTextLine, offset));
            TextView.Caret.EnsureVisible();
          } else {
            if (currentTextLine.Length < maxOffset) {
              TextView.Caret.MoveTo(currentTextLine.End);
            } else {
              TextView.Caret.MoveTo(new VirtualSnapshotPoint(currentTextLine, maxOffset.Value));
            }
          }

          TextView.Caret.EnsureVisible();
          break;
          default:
          maxOffset = Math.Abs(TextView.Caret.Position.BufferPosition.Difference(currentTextLine.Start)) + caret.Value.VirtualSpaces;
          break;
        }
      } else if (pguidCmdGroup == typeof(VSConstants.VSStd97CmdID).GUID && (nCmdID == (uint)VSConstants.VSStd97CmdID.Undo || nCmdID == (uint)VSConstants.VSStd97CmdID.Redo)) {
        caret = TextView.Caret.Position;
        if (caret == null) {
          return result;
        }

        currentSnap = TextView.TextSnapshot;
        currentTextLine = currentSnap.GetLineFromPosition(caret.Value.BufferPosition.Position);
        maxOffset = Math.Abs(TextView.Caret.Position.BufferPosition.Difference(currentTextLine.Start)) + caret.Value.VirtualSpaces;
      }

      return result;
    }

    private void getOffset() {
      AST.AST.Parse(currentSnap.GetText(), out bool parsed);
      var root = AST.AST.GetASTRoot();
      if (root.name == "Program") {
        Traverse(root);
      }
    }

    private void Traverse(TreeBuilder.Node node) {
      if (node.children == null) {
        if (node.begin >= currentTextLine.Start.Position && notReached) {
          offset = bracketsStack.Count * indentationSize;
          notReached = false;
          return;
        }

        switch (node.name) {
          case "']'":
          case "'}'":
          case "')'":
          if (currentLevelBrackets.Count == 0 && bracketsStack.Count > 0 && bracketsStack.Peek().Count != 0) {
            if (node.line == bracketsStack.Peek().Peek().line) {
              bracketsStack.Peek().Pop();
              if (wasPoped) {
                currentLevelBrackets = bracketsStack.Pop();
                wasPoped = false;
              } else if (bracketsStack.Peek().Count == 0) {
                bracketsStack.Pop();
              }
            } else {
              currentLevelBrackets = bracketsStack.Pop();
              currentLevelBrackets.Pop();
            }
          } else if (currentLevelBrackets.Count > 0) {
            currentLevelBrackets.Pop();
          } else if (bracketsStack.Count > 0 && bracketsStack.Peek().Count == 0) {
            bracketsStack.Pop();
          }

          break;
          case "'['":
          case "'{'":
          case "'('":
          if (bracketsStack.Count == 0 && currentLevelBrackets.Count == 0) {
            bracketsStack.Push(new Stack<TreeBuilder.Node>());
            bracketsStack.Peek().Push(node);
          } else if (bracketsStack.Count != 0 && bracketsStack.Peek().Peek().line == node.line) {
            bracketsStack.Peek().Push(node);
          } else if (currentLevelBrackets.Count != 0) {
            bracketsStack.Push(new Stack<TreeBuilder.Node>(currentLevelBrackets));
            currentLevelBrackets.Clear();
            bracketsStack.Peek().Push(node);
            wasPoped = true;
          } else {
            bracketsStack.Push(new Stack<TreeBuilder.Node>());
            bracketsStack.Peek().Push(node);
          }

          break;
        }

        lastToken = node;
        return;
      }

      if (notReached) {
        foreach (var child in node.children) {
          Traverse(child);
        }
      }
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (pguidCmdGroup == CommandGroupId) {
        for (int i = 0; i < cCmds; i++) {
          if (CommandIdSet.Contains(prgCmds[i].cmdID)) {
            if (IsEnabled) {
              prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
              return VSConstants.S_OK;
            }

            prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
          }
        }
      }

      return _NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }
  }
}
