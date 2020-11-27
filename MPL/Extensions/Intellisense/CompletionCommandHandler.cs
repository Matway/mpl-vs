using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using MPL.ParseTree;

namespace MPL.Intellisense {
  internal class CompletionCommandHandler : IOleCommandTarget {
    private IOleCommandTarget _nextCommandHandler;
    private ITextView _textView;
    private CompletionHandlerProvider _provider;
    private ICompletionSession _currentSession;

    internal CompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, CompletionHandlerProvider provider) {
      _textView = textView;
      _provider = provider;

      //add the command to the command chain
      textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (pguidCmdGroup == VSConstants.VSStd2K) {
        switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID) {
          case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
          case VSConstants.VSStd2KCmdID.COMPLETEWORD:
          prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
          return VSConstants.S_OK;
        }
      }

      return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (VsShellUtilities.IsInAutomationFunction(_provider.ServiceProvider)) {
        return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      if (IsStringOrComment()) {
        this.Cancel();
        return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }

      int retVal = VSConstants.S_OK;
      bool itIsCompletionSession = false;
      uint commandID = nCmdID; //make a copy of this so we can look at it after forwarding some commands
      char typedChar = char.MinValue;

      if (pguidCmdGroup == VSConstants.VSStd2K) {
        switch ((VSConstants.VSStd2KCmdID)nCmdID) {
          case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
          case VSConstants.VSStd2KCmdID.COMPLETEWORD:
          itIsCompletionSession = this.StartSession();
          break;
          case VSConstants.VSStd2KCmdID.RETURN: //commit char
          case VSConstants.VSStd2KCmdID.TAB: //commit char
          itIsCompletionSession = this.Commit();
          break;
          case VSConstants.VSStd2KCmdID.CANCEL:
          itIsCompletionSession = this.Cancel();
          break;
          case VSConstants.VSStd2KCmdID.TYPECHAR:
          typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
          if (char.IsWhiteSpace(typedChar) || NotLetter(typedChar)) {
            itIsCompletionSession = this.Cancel();
          }

          break;
        }
      }

      if (!itIsCompletionSession) { // pass along the command so the char is added to the buffer
        retVal = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
      }


      switch ((VSConstants.VSStd2KCmdID)nCmdID) {
        case VSConstants.VSStd2KCmdID.TYPECHAR:
        if (!NotLetter(typedChar) && !char.IsWhiteSpace(typedChar)) {
          if (_currentSession == null || _currentSession.IsDismissed) { // If there is no active session, bring up completion
            if (StartSession()) {
              _currentSession.Filter();
            }
          } else { //the completion session is already active, so just filter
            _currentSession.Filter();
          }

          itIsCompletionSession = true;
        }

        break;
        case VSConstants.VSStd2KCmdID.BACKSPACE:
        case VSConstants.VSStd2KCmdID.DELETE: // redo the filter if there is a deletion
        if (_currentSession != null && !_currentSession.IsDismissed) {
          _currentSession.Filter();
        }

        itIsCompletionSession = true;
        break;
      }

      if (itIsCompletionSession) {
        return VSConstants.S_OK;
      }

      return retVal;
    }

    private bool StartSession() {
      SnapshotPoint? caretPoint = _textView.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);

      if (MplPackage.completionSession || !caretPoint.HasValue) {
        return false;
      }

      _currentSession = _provider.CompletionBroker.CreateCompletionSession(_textView, caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive), true);

      if (_currentSession != null) {
        _currentSession.Dismissed += this.OnSessionDismissed;
        _currentSession.Start();

        MplPackage.completionSession = true;
        return true;
      }

      return false;
    }

    private bool Cancel() {
      if (_currentSession == null)
        return false;

      _currentSession.Dismiss();

      return false;
    }

    private bool Commit() {
      if (_currentSession != null && !_currentSession.IsDismissed) { //check for a selection
        if (_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected) { //if the selection is fully selected, commit the current session
          _currentSession.Commit();
          return true;
        } else {
          _currentSession.Dismiss(); //if there is no selection, dismiss the session
          return false;
        }
      }

      return false;
    }

    private bool IsStringOrComment() {
      Builder.Node root = ParseTree.Tree.Root();
      bool notStringOrComment = true;
      bool notReached = true;

      void Traverse(Builder.Node node) {
        if (node.children == null) {
          if (notReached && node.begin < _textView.Caret.Position.BufferPosition.Position && node.end >= _textView.Caret.Position.BufferPosition.Position) {
            if (node.name == "Comment" || (node.name == "String" && node.end != _textView.Caret.Position.BufferPosition.Position)) {
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

    private bool NotLetter(char c) {
      return !(c != '.' && c != ';' && c != ':' && c != ',' && c != '!' && c != '@' && c != '{' && c != '}' && c != '(' && c != ')' && c != 0x002D && c != 0x0022 && c != 0x0023 && c != 0x005B && c != 0x005D && c != 0x0009 && c != 0x000A && c != 0x000D);
    }

    private void OnSessionDismissed(object sender, EventArgs e) {
      _currentSession.Dismissed -= OnSessionDismissed;
      _currentSession = null;
      MplPackage.completionSession = false;
    }
  }
}
