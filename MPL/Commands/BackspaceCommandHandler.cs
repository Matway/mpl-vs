using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;

namespace MPL.Commands {
  class BackspaceCommandHandler : IOleCommandTarget {
    private char deletedChar;
    private SnapshotPoint point;
    private IOleCommandTarget _NextCommandTarget;
    protected readonly IVsTextView VsTextView;
    protected readonly IWpfTextView TextView;

    public BackspaceCommandHandler(IVsTextView vsTextView, IWpfTextView textView) {
      VsTextView = vsTextView;
      TextView = textView;

      VsTextView.AddCommandFilter(this, out _NextCommandTarget);
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID) {
        for (int i = 0; i < cCmds; i++) {
          if ((uint)VSConstants.VSStd2KCmdID.BACKSPACE == prgCmds[i].cmdID) {
            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
            return VSConstants.S_OK;
          }
        }
      }

      return _NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE && MplPackage.Options.AngularQuotes && TextView.Selection.IsEmpty) {
        point = TextView.Caret.Position.BufferPosition;
        if (point.Position == 0) {
          return VSConstants.S_OK;
        }

        deletedChar = point.Snapshot.GetText(point.Position - 1, 1)[0];
      } else {
        return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      if (deletedChar == '«') {
        TextView.TextBuffer.Replace(new Span(point.Position - 1, 1), "<<");
      } else if (deletedChar == '»') {
        TextView.TextBuffer.Replace(new Span(point.Position - 1, 1), ">>");
      } else {
        return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      return VSConstants.S_OK;
    }
  }
}
