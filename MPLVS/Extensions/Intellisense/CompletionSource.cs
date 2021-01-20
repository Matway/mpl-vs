using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

using MPLVS.Core;
using MPLVS.Core.ParseTree;
using MPLVS.Extensions;

namespace MPLVS.Intellisense {
  internal class CompletionSource : ICompletionSource {
    private readonly CompletionSourceProvider _sourceProvider;
    private readonly ITextBuffer _textBuffer;
    private bool _isDisposed;

    public CompletionSource(CompletionSourceProvider sourceprovider, ITextBuffer textBuffer) {
      _sourceProvider = sourceprovider;
      _textBuffer     = textBuffer;
    }

    void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets) {
      ThreadHelper.ThrowIfNotOnUIThread();

      var names   = this.CurrentSymbols(MplPackage.Options.AutocompletionProjectWideSearch);
      var symbols = names.Select(a => new Completion(a.Name, a.Name, a.Origins, null, null));
      var span    = FindTokenSpanAtPosition(session.GetTriggerPoint(_textBuffer), session);

      completionSets.Add(new CompletionSet("all", "Symbols", span, symbols, null));
    }

    private IEnumerable<Symbol> CurrentSymbols(bool forWholeProject) {
      ThreadHelper.ThrowIfNotOnUIThread();

      var currentFile      = this._textBuffer.GetFileName();
      var currentDirectory = Path.GetDirectoryName(currentFile);

      if (forWholeProject) {
        return AssemblySymbols(currentDirectory, Symbols.Files.TreesFromAWholeProject(currentFile));
      }

      return
        AssemblySymbols(currentDirectory, new[] {
          new Symbols.OriginAndTree {
            File  = currentFile,
            Input = this._textBuffer.CurrentSnapshot.GetText(),
            Root  = this._textBuffer.ObtainOrAttachTree().Root()
          }
        });
    }

    private static IEnumerable<Symbol> AssemblySymbols(string currentDirectory, IEnumerable<Symbols.OriginAndTree> trees) =>
      trees.Where(a => a.Root is object)
           .Select(a => new { File = PathNetCore.GetRelativePath(currentDirectory, a.File), a.Input, Names = Symbols.Files.Labels(a.Root).Select(b => Symbols.Files.Text(b.LabelName(), a.Input)) })
           .Select(a => new { a.File, Names = a.Names.Union(Array.Empty<string>()) })
           .SelectMany(a => a.Names.Select(Label => new { a.File, Label }))
           .Concat(Constants.Builtins.Select(a => new { File = "Built-in", Label = a })) // FIXME: GC.
           .OrderBy(a => a.Label)
           .GroupBy(a => a.Label)
           .Select(a => new Symbol { Name = a.Key, Origins = string.Join("\n", a.Select(b => b.File).OrderBy(b => b)) });

    private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session) {
      var cursor       = session.TextView.Caret.Position.BufferPosition;
      var currentPoint = cursor > 0 ? cursor - 1 : cursor;
      var navigator    = _sourceProvider.NavigatorService.GetTextStructureNavigator(_textBuffer);
      var extent       = navigator.GetExtentOfWord(currentPoint);

      return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
    }

    public void Dispose() {
      if (!_isDisposed) {
        GC.SuppressFinalize(this);
        _isDisposed = true;
      }
    }

    private struct Symbol {
      public string Name;
      public string Origins;
    }
  }
}