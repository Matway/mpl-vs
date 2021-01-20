using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

using MPLVS.Core;
using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.ScopeHighlighting {
  internal sealed class Tagger : StepByStepTagger<ITextMarkerTag> {
    private readonly ITextView textView;
    private static readonly TextMarkerTag tag = new TextMarkerTag("MplScope");
    private Builder.Node last;

    public Tagger(ITextView view) {
      this.textView = view;

      this.textView.Caret.PositionChanged += OnCaretPositionChanged;
      this.textView.TextBuffer.Changed    += OnBufferChanged;

      OnCaretPositionChanged(null, null);
    }

    public override event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    protected override IEnumerable<ITagSpan<ITextMarkerTag>> Tags(Span span) =>
      AllTags().Where(a => a.OverlapsWith(span)).Select(b => {
        var snapshot = new SnapshotSpan(textView.TextSnapshot, b.Start, b.End - b.Start);
        return new TagSpan<ITextMarkerTag>(snapshot, tag);
      });

    private IEnumerable<Span> AllTags() {
      if (last is null) {
        yield break;
      }

      var start = last.children.First();
      var endOfTag = start.end + (start.IsName() ? 1 : 0); // If it is a label, then capture a colon too.
      yield return Span.FromBounds(start.begin, endOfTag);

      // When the text has some syntax errors,
      // then it is possible that the current scope will be w\o a closing symbol.
      var end = last.children.Last();
      if (end.IsScopeEnd()) {
        yield return new Span(end.begin, 1);
      }
    }

    private void OnBufferChanged(object sender, TextContentChangedEventArgs info) =>
      OnCaretPositionChanged(null, null);

    private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs info) {
      var unnormalizedPoint =
        textView.Caret.Position.Point
        .GetPoint(textView.TextBuffer, textView.Caret.Position.Affinity);

      if (!unnormalizedPoint.HasValue && this.last is null) {
        return;
      }

      if (!unnormalizedPoint.HasValue) {
        Notify(null);
        return;
      }

      var point = Normalize(unnormalizedPoint);

      var symbols =
        this.textView.TextBuffer
        .ObtainOrAttachTree().Root()
        .AllAncestors(point.Position)
        .Where(a => a.IsScope()); // Filter out trailing trash like a string\comment etc.

      var current = symbols.LastOrDefault();

      var caretOoutsideOfScope =
        current is object                        // TODO: Explain what is going on here.
        && unnormalizedPoint.Value > point       //
        && current.children.Last().IsScopeEnd(); //

      if (caretOoutsideOfScope) {
        current = null;
      }

      if (!object.ReferenceEquals(last, current)) {
        Notify(current);
      }
    }

    private void Notify(Builder.Node node) {
      this.last = node;

      var snapshot = new SnapshotSpan(textView.TextBuffer.CurrentSnapshot, 0, textView.TextBuffer.CurrentSnapshot.Length);
      TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(snapshot));
    }

    private static SnapshotPoint Normalize(SnapshotPoint? point) =>
      point.Value == point.Value.Snapshot.Length && point.Value != 0
      ? point.Value - 1
      : point.Value;
  }
}