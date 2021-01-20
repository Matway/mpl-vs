using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MPLVS.Commands.Guillemets {
  internal sealed class AngularQuotes : VSStd2KCommand {
    public AngularQuotes(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override bool Run(VSConstants.VSStd2KCmdID nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      var typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
      var point     = TextView.Caret.Position.BufferPosition;

      if (typedChar == '<') {
        if (point.Position != 0 && TextView.TextBuffer.CurrentSnapshot.GetText(point.Position - 1, 1) == "<") {
          TextView.TextBuffer.Replace(new Span(point.Position - 1, 1), "«");
        }
        else {
          return ExecuteNext(nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
      }
      else if (typedChar == '>') {
        if (point.Position != 0 && TextView.TextBuffer.CurrentSnapshot.GetText(point.Position - 1, 1) == ">") {
          TextView.TextBuffer.Replace(new Span(point.Position - 1, 1), "»");
        }
        else {
          return ExecuteNext(nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
      }
      else {
        return ExecuteNext(nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      return true;
    }

    protected override bool Activated() {
      ThreadHelper.ThrowIfNotOnUIThread();
      return MplPackage.Options.AngularQuotes;
    }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands() {
      yield return VSConstants.VSStd2KCmdID.TYPECHAR;
    }
  }
}