using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MPLVS {
  internal abstract class VSStd2KCommand : ActivatabelCommand<VSConstants.VSStd2KCmdID> {
    protected VSStd2KCommand(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override bool Activated() => true;
    protected sealed override VSConstants.VSStd2KCmdID ConvertFromCommandId(uint id) => (VSConstants.VSStd2KCmdID)id;
    protected sealed override uint ConvertFromCommand(VSConstants.VSStd2KCmdID command) => (uint)command;
  }

  internal abstract class ActivatabelCommand<T> : VSCommandTarget<T> {
    protected ActivatabelCommand(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected abstract bool Activated();
    protected abstract bool Run(T command, uint options, IntPtr pvaIn, IntPtr pvaOut);

    protected sealed override bool Execute(T command, uint options, IntPtr pvaIn, IntPtr pvaOut) =>
      Activated() ? Run(command, options, pvaIn, pvaOut)
                  : ExecuteNext(command, options, pvaIn, pvaOut);
  }

  internal abstract class VSCommandTarget<T> : IOleCommandTarget {
    private readonly IOleCommandTarget _NextCommandTarget;
    protected readonly IVsTextView VsTextView;
    protected readonly IWpfTextView TextView;
    protected readonly Guid CommandGroupId;
    protected readonly HashSet<uint> CommandIdSet;

    protected VSCommandTarget(IVsTextView vsTextView, IWpfTextView textView) {
      VsTextView = vsTextView;
      TextView = textView;
      CommandGroupId = typeof(T).GUID;
      CommandIdSet = new HashSet<uint>(SupportedCommands().Select(a => ConvertFromCommand(a)));

      _NextCommandTarget = AttachTo(vsTextView, this);
    }

    protected virtual bool IsEnabled => true;
    protected virtual bool SupportsAutomation => false;

    protected abstract IEnumerable<T> SupportedCommands();
    protected abstract T ConvertFromCommandId(uint id);
    protected abstract uint ConvertFromCommand(T command);

    protected abstract bool Execute(T command, uint options, IntPtr pvaIn, IntPtr pvaOut);

    protected bool ExecuteNext(T command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      var pguidCmdGroup = CommandGroupId;

      return _NextCommandTarget.Exec(ref pguidCmdGroup, ConvertFromCommand(command), options, pvaIn, pvaOut) == VSConstants.S_OK;
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (pguidCmdGroup == CommandGroupId && CommandIdSet.Contains(nCmdID)) {
        var result = Execute(ConvertFromCommandId(nCmdID), nCmdexecopt, pvaIn, pvaOut);

        if (result) {
          return VSConstants.S_OK;
        }
      }

      return _NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (pguidCmdGroup == CommandGroupId) {
        for (var i = 0; i < cCmds; i++) {
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

    private static IOleCommandTarget AttachTo(IVsTextView view, IOleCommandTarget command) {
      view.AddCommandFilter(command, out var next);
      return next;
    }
  }
}