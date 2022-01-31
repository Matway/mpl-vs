using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.Folding.Blocks {
  internal sealed class Tagger : VerticalTags<IStructureTag> {
    private readonly ITextBuffer buffer;
    private ITextSnapshot snapshot;
    internal List<Region> regions;

    public Tagger(ITextBuffer buf) {
      buffer = buf;
      snapshot = buffer.CurrentSnapshot;
      regions = new List<Region>();
      ReParse();
      buffer.Changed += BufferChanged;
    }

    public override event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    protected override ITagSpan<IStructureTag> AsTag(Region region, SnapshotSpan range) {
      return new TagSpan<IStructureTag>(range, region.ToStructureTag(range, " ... "));
    }

    protected override IEnumerable<Region> Regions() => this.regions;

    protected override ITextSnapshot Snapshot() => this.snapshot;

    private void BufferChanged(object sender, TextContentChangedEventArgs info) {
      if (info.After != buffer.CurrentSnapshot) {
        return;
      }

      ReParse();
    }

    private void ReParse() {
      var newSnapshot = buffer.CurrentSnapshot;

      var blocks =
        this.buffer
          .ObtainOrAttachTree().Root()
          .AsSequence()
          .Where(a => a.IsScope())
          .Where(a => a.line != newSnapshot.GetLineNumberFromPosition(a.end))
          .GroupBy(a => a.line) // FIXME: GC.
          .Select(a => a.Last())
          .Select(a => {
            var begin   = newSnapshot.GetLineFromLineNumber(a.line);
            var end     = newSnapshot.GetLineNumberFromPosition(a.end);
            var withEnd = a.IsClosedScope();

            return new Region {
              StartLine   = a.line,
              EndLine     = end,
              StartOffset = a.IsLabel() ? a.Next.Next.end : a.begin + 1,
              EndOffset   = a.end - (withEnd ? 1 : 0),

              IsClosed      = withEnd,
              IsSignificant = a.Next.name[1] == '{',

              Header = begin.Extent,
            };
          }).ToList();

      var oldSpans = this.regions.Select(a => a.AsSnapshotSpan(snapshot).TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span);
      var newSpans = new List<Span>(blocks.Select(a => a.AsSpan()));

      var oldSpanCollection = new NormalizedSpanCollection(oldSpans);
      var newSpanCollection = new NormalizedSpanCollection(newSpans);

      var removed = NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

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

      snapshot = newSnapshot;
      regions  = blocks;

      if (changeStart <= changeEnd) {
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, Span.FromBounds(changeStart, changeEnd))));
      }
    }
  }
}