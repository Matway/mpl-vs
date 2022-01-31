using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using MPLVS.Core.ParseTree;
using MPLVS.Extensions;

namespace MPLVS.Commands {
  internal class BraceCompletion : VSStd2KCommand {
    public BraceCompletion(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override bool Run(VSConstants.VSStd2KCmdID nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      var typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

      if (typedChar.IsOpeningBrace()) {
        return HandleOpeningBrace(nCmdID, nCmdexecopt, pvaIn, pvaOut, typedChar);
      }

      if (typedChar.IsClosingBrace() && IsEqualToNextCharacter(typedChar)) {
        return HandleClosingBrace();
      }

      return ExecuteNext(nCmdID, nCmdexecopt, pvaIn, pvaOut);
    }

    private bool IsEqualToNextCharacter(char typedChar) =>
      !IsEndOfInput() && (TextView.Caret.Position.BufferPosition.GetChar() == typedChar);

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

    private bool HandleClosingBrace() {
      TextView.Caret.MoveToNextCaretPosition();
      return true;
    }

    private bool HandleOpeningBrace(VSConstants.VSStd2KCmdID nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut, char typedChar) {
      ThreadHelper.ThrowIfNotOnUIThread();

      _ = ExecuteNext(nCmdID, nCmdexecopt, pvaIn, pvaOut);

      var caretPoint = TextView.Caret.Position.BufferPosition;
      TextView.TextBuffer.Insert(TextView.Caret.Position.BufferPosition.Position, NodeUtils.Braces[typedChar].ToString());
      TextView.Caret.MoveTo(caretPoint.TranslateTo(TextView.TextSnapshot, PointTrackingMode.Negative));

      return true;
    }

    protected override bool Activated() {
      ThreadHelper.ThrowIfNotOnUIThread();

      return
        MplPackage.Options.AutoBraceCompletion
        && !TextView.IsCaretInStringOrComment(); // Do we need this?
    }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands() {
      yield return VSConstants.VSStd2KCmdID.TYPECHAR;
    }
  }
}