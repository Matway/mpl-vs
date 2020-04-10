using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using MPL.AST;

namespace MPL.Intellisense {
  internal class CompletionSource : ICompletionSource {
    private CompletionSourceProvider _sourceProvider;
    private ITextBuffer _textBuffer;
    private List<Completion> _compList;
    private bool _isDisposed;

    public CompletionSource(CompletionSourceProvider sourceprovider, ITextBuffer textBuffer) {
      _sourceProvider = sourceprovider;
      _textBuffer = textBuffer;
    }

    void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets) {
      List<string> strList = new List<string>();
      _compList = new List<Completion>();

      foreach (string builtin in Constants.MplBuiltins) {
        if (builtin.Length > 1) {
          strList.Add(builtin);
          _compList.Add(new Completion(builtin, builtin, "builtin function", null, null));
        }
      }

      foreach (string name in AST.AST.nameList) {
        if (!strList.Contains(name)) {
          _compList.Add(new Completion(name, name, null, null, null));
        }
      }

      completionSets.Add(new CompletionSet(
          "Tokens",    //the non-localized title of the tab
          "Tokens",    //the display title of the tab
          FindTokenSpanAtPosition(session.GetTriggerPoint(_textBuffer), session),
          _compList,
          null));
    }

    private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session) {
      SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
      ITextStructureNavigator navigator = _sourceProvider.NavigatorService.GetTextStructureNavigator(_textBuffer);
      TextExtent extent = navigator.GetExtentOfWord(currentPoint);
      return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
    }

    public void Dispose() {
      if (!_isDisposed) {
        GC.SuppressFinalize(this);
        _isDisposed = true;
      }
    }
  }
}
