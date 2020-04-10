using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using MPL.AST;

namespace MPL.Commands {
  class BraceCompletionCommandHandler : IOleCommandTarget {
    private char typedChar;
    private SnapshotPoint point;
    private readonly Dictionary<char, char> bracePairs;
    private IOleCommandTarget _NextCommandTarget;
    protected readonly IVsTextView VsTextView;
    protected readonly IWpfTextView TextView;

    public BraceCompletionCommandHandler(IVsTextView vsTextView, IWpfTextView textView) {
      VsTextView = vsTextView;
      TextView = textView;
      bracePairs = new Dictionary<char, char> {
        ['{'] = '}',
        ['['] = ']',
        ['('] = ')'
      };

      VsTextView.AddCommandFilter(this, out _NextCommandTarget);
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
      ThreadHelper.ThrowIfNotOnUIThread();
      return _NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR && MplPackage.Options.AutoBraceCompletion) {
        typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        point = TextView.Caret.Position.BufferPosition;
        if (IsStringOrComment()) {
          return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
      } else {
        return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      if (typedChar == '[' || typedChar == '{' || typedChar == '(') {
        var nextCommand = _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        var caretPoint = TextView.Caret.Position.BufferPosition;
        TextView.TextBuffer.Insert(TextView.Caret.Position.BufferPosition.Position, bracePairs[typedChar].ToString());
        TextView.Caret.MoveTo(caretPoint.TranslateTo(TextView.TextSnapshot, PointTrackingMode.Negative));
      } else if ((typedChar == ']' || typedChar == '}' || typedChar == ')') && TextView.Caret.Position.BufferPosition.GetChar() == typedChar) {
        TextView.Caret.MoveToNextCaretPosition();
      } else {
        return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      return VSConstants.S_OK;
    }

    private bool IsStringOrComment() {
      TreeBuilder.Node root = AST.AST.GetASTRoot();
      bool notStringOrComment = true;
      bool notReached = true;

      void Traverse(TreeBuilder.Node node) {
        if (node.children == null) {
          if (notReached && node.begin < point.Position && node.end >= point.Position) {
            if (node.name == "Comment" || (node.name == "String" && node.end != point.Position)) {
              notStringOrComment = false;
            }

            notReached = false;
            return;
          }

          return;
        }

        foreach (var child in node.children) {
          Traverse(child);
        }
      }

      Traverse(root);

      return !notStringOrComment;
    }  
  }
}
