using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.Folding.Comments {
  internal sealed class Tagger : VerticalTags<Tag> {
    private readonly ITextBuffer Buffer;
    private ITextSnapshot Text;
    private List<Region> Spans;

    public Tagger(ITextBuffer buffer) {
      this.Buffer = buffer;
      this.Spans  = new List<Region>();
      this.Text   = this.Buffer.CurrentSnapshot;

      this.Buffer.Changed += this.BufferChanged;

      this.ReParse();
    }

    public override event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    protected override ITagSpan<Tag> AsTag(Region region, SnapshotSpan range) =>
      new TagSpan<Tag>(range, region.ToOutliningTag(range, "# ..."));

    protected override IEnumerable<Region> Regions() => this.Spans;

    protected override ITextSnapshot Snapshot() => this.Text;

    private void BufferChanged(object sender, TextContentChangedEventArgs info) {
      if (info.After != this.Buffer.CurrentSnapshot) { return; }

      this.ReParse();
    }

    private void ReParse() {
      var newRegions = new List<Region>();

      var firstComment = this.Buffer.ObtainOrAttachTree().Root().AsSequence()
                                    .FirstOrDefault(a => a.IsComment() && (a.Previous is null || a.line != a.Previous.line || a.Previous.name == "Program"));
      do {
        var shouldBeComment = true;
        var end             = firstComment;
        while (end?.Next is object &&
               ((shouldBeComment && end.IsComment()) ||
               (!shouldBeComment && end.IsEol()))) {
          shouldBeComment = !shouldBeComment;
          end = end.Next;
        }

        if (end is object) {
          var lastComment = end.AsReverseSequence().Where(a => a.IsComment()).First();

          if (!ReferenceEquals(firstComment, lastComment)) {
            newRegions.Add(new Region {
              StartLine   = firstComment.line,
              EndLine     = lastComment.line,
              StartOffset = firstComment.begin,
              EndOffset   = lastComment.end
            });
          }
        }

        firstComment = end?.Next?.AsSequence()
                                 .FirstOrDefault(a => a.IsComment() && (a.Previous is null || a.line != a.Previous.line || a.Previous.name == "Program"));
      } while (firstComment is object);

      var oldSpans = this.Spans.Select(a => a.AsSnapshotSpan(this.Text).TranslateTo(this.Buffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive).Span);
      var newSpans = new List<Span>(newRegions.Select(a => a.AsSpan()));

      var removed = NormalizedSpanCollection.Difference(new NormalizedSpanCollection(oldSpans), new NormalizedSpanCollection(newSpans));

      var changeStart = int.MaxValue;
      var changeEnd   = int.MinValue;

      if (removed.Any()) {
        changeStart = removed[0].Start;
        changeEnd   = removed[removed.Count - 1].End;
      }

      if (newSpans.Any()) {
        changeStart = Math.Min(changeStart, newSpans[0].Start);
        changeEnd   = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
      }

      this.Text  = this.Buffer.CurrentSnapshot;
      this.Spans = newRegions;

      if (changeStart <= changeEnd) {
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(this.Text, Span.FromBounds(changeStart, changeEnd))));
      }
    }
  }
}