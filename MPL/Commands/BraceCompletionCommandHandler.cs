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
using System.Runtime.CompilerServices;

namespace MPL.Commands {
  class BraceCompletionCommandHandler : IOleCommandTarget {
    private static readonly Dictionary<char, char> bracePairs = new Dictionary<char, char> {
      ['{'] = '}',
      ['['] = ']',
      ['('] = ')',
      [':'] = ';'  // We treat label start (:) and label end (;) as a brace pair.
                   // So now, we have auto-completion of closing ; character after label start.
    };

    private IOleCommandTarget _NextCommandTarget;
    protected readonly IWpfTextView TextView;

    public BraceCompletionCommandHandler(IVsTextView vsTextView, IWpfTextView textView) {
      TextView = textView;
      vsTextView.AddCommandFilter(this, out _NextCommandTarget);
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
      ThreadHelper.ThrowIfNotOnUIThread();
      return _NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();

      if (!IsBraceCompletionNeeded(ref pguidCmdGroup, nCmdID)) {
        goto noCompletionNeeded;
      }

      var typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

      if (IsOpeningBrace(typedChar)) {
        return HandleOpeningBrace(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut, typedChar);
      }

      if (IsClosingBrace(typedChar) && IsEqualToNextCharacter(typedChar)) {
        return HandleClosingBrace();
      }

    noCompletionNeeded:
      return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
    }

    private bool IsEqualToNextCharacter(char typedChar) =>
      IsEndOfInput()
      ? false
      : TextView.Caret.Position.BufferPosition.GetChar() == typedChar;

    // TODO: Maybe vs api have some tools for that.
    // FIXME: Get rid of the exception.
    private bool IsEndOfInput() {
      try {
        _ = TextView.Caret.Position.BufferPosition.GetChar();
        return false;
      }
      catch (Exception) {
        return true;
      }
    }

    private int HandleClosingBrace() {
      TextView.Caret.MoveToNextCaretPosition();
      return VSConstants.S_OK;
    }

    private int HandleOpeningBrace(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut, char typedChar) {
      ThreadHelper.ThrowIfNotOnUIThread();

      _ = _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

      var caretPoint = TextView.Caret.Position.BufferPosition;
      TextView.TextBuffer.Insert(TextView.Caret.Position.BufferPosition.Position, bracePairs[typedChar].ToString());
      TextView.Caret.MoveTo(caretPoint.TranslateTo(TextView.TextSnapshot, PointTrackingMode.Negative));

      return VSConstants.S_OK;
    }

    private bool IsBraceCompletionNeeded(ref Guid pguidCmdGroup, uint nCmdID) {
      ThreadHelper.ThrowIfNotOnUIThread();

      return
        MplPackage.Options.AutoBraceCompletion
        && IsActiveInput(ref pguidCmdGroup, nCmdID)
        && !IsStringOrComment();
    }

    private static bool IsActiveInput(ref Guid pguidCmdGroup, uint nCmdID)
      => pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR;

    private static bool IsOpeningBrace(char a) => bracePairs.ContainsKey(a);
    private static bool IsClosingBrace(char a) => bracePairs.ContainsValue(a);

    private bool IsStringOrComment() {
      TreeBuilder.Node root = AST.AST.GetASTRoot();
      var notStringOrComment = true;
      var notReached = true;
      var point = TextView.Caret.Position.BufferPosition;

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
