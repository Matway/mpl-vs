using System;
using System.Collections.Generic;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MPLVS.Commands.Guillemets {
  internal class Backspace : VSStd2KCommand {
    public Backspace(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override bool Run(VSConstants.VSStd2KCmdID nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      var point = TextView.Caret.Position.BufferPosition;

      if (point.Position == 0) {
        return true;
      }

      var deletedChar = point.Snapshot.GetText(point.Position - 1, 1)[0];

      string replace;
      switch (deletedChar) {
        case '«': replace = "<<"; break;
        case '»': replace = ">>"; break;

        default:
          return ExecuteNext(nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      TextView.TextBuffer.Replace(new Span(point.Position - 1, 1), replace);
      return true;
    }

    protected override bool Activated() {
      ThreadHelper.ThrowIfNotOnUIThread();
      return MplPackage.Options.AngularQuotes && TextView.Selection.IsEmpty;
    }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands() {
      yield return VSConstants.VSStd2KCmdID.BACKSPACE;
    }
  }
}