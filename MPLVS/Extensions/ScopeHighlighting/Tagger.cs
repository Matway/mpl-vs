using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.ScopeHighlighting {
  internal sealed class Tagger : ITagger<Tag> {
    public Tagger(ITextView view) {
      this.View = view;

      this.View.Caret.PositionChanged += this.OnCaretPositionChanged;
      this.View.TextBuffer.Changed    += this.OnBufferChanged;

      this.OnCaretPositionChanged(null, null);
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    public IEnumerable<ITagSpan<Tag>> GetTags(NormalizedSnapshotSpanCollection spans) {
      this.OnCaretPositionChanged(null, null);
      return this.Tags(new Span(0, this.View.TextSnapshot.Length));
    }

    private IEnumerable<ITagSpan<Tag>> Tags(Span span) {
      var text = this.View.TextSnapshot;
      return this.AllTags().Where(a => a.OverlapsWith(span)).Select(b => {
        var snapshot = new SnapshotSpan(text, b.Start, b.End - b.Start);
        return new TagSpan<Tag>(snapshot, Tag);
      });
    }

    private IEnumerable<Span> AllTags() {
      var last = this.Last;

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
      this.OnCaretPositionChanged(null, null);

    private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs info) {
      var current = this.View.TextBuffer.ObtainOrAttachTree().Root()
                                        .AllAncestors(this.View.Caret.Position.BufferPosition)
                                        .LastThree().AsSequence() // TODO: Maybe the last two will be enough.
                                        .LastOrDefault(a => a is object && a.IsScope());

      if (ReferenceEquals(this.Last, current)) { return; }

      this.Notify(current);
    }

    private void Notify(Builder.Node node) {
      this.Last = node;

      var current = this.View.TextBuffer.CurrentSnapshot;
      var changed = new SnapshotSpan(current, 0, current.Length);
      TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(changed));
    }

    private Builder.Node Last;
    private readonly ITextView View;
    internal static readonly Tag Tag = new Tag("MplScope", "MplScope");
  }
}