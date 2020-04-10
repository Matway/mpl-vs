using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace MPL.Commands {
  class AngularQuotesCommandHandler : IOleCommandTarget {
    private char typedChar;
    private SnapshotPoint point;
    private IOleCommandTarget _NextCommandTarget;
    protected readonly IVsTextView VsTextView;
    protected readonly IWpfTextView TextView;

    public AngularQuotesCommandHandler(IVsTextView vsTextView, IWpfTextView textView) {
      VsTextView = vsTextView;
      TextView = textView;

      VsTextView.AddCommandFilter(this, out _NextCommandTarget);
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
      ThreadHelper.ThrowIfNotOnUIThread();
      return _NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR && MplPackage.Options.AngularQuotes) {
        typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        point = TextView.Caret.Position.BufferPosition;
      } else {
        return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      if (typedChar == '<') {
        if (point.Position != 0 && TextView.TextBuffer.CurrentSnapshot.GetText(point.Position - 1, 1) == "<") {
          TextView.TextBuffer.Replace(new Span(point.Position - 1, 1), "«");
        } else {
          return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
      } else if (typedChar == '>') {
        if (point.Position != 0 && TextView.TextBuffer.CurrentSnapshot.GetText(point.Position - 1, 1) == ">") {
          TextView.TextBuffer.Replace(new Span(point.Position - 1, 1), "»");
        } else {
          return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
      } else {
        return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      return VSConstants.S_OK;
    }
  }
}
