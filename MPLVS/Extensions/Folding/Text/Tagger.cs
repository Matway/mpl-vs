using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.Folding.Text {
  internal sealed class Tagger : VerticalTags<Tag> {
    private readonly ITextBuffer buffer;
    private ITextSnapshot snapshot;
    private List<Region> regions;

    public Tagger(ITextBuffer buf) {
      buffer = buf;
      snapshot = buffer.CurrentSnapshot;
      regions = new List<Region>();
      ReParse();
      buffer.Changed += BufferChanged;
    }

    public override event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    protected override ITagSpan<Tag> AsTag(Region region, SnapshotSpan range) =>
      new TagSpan<Tag>(range, region.ToOutliningTag(range, "\" ... \""));

    protected override IEnumerable<Region> Regions() => this.regions;

    protected override ITextSnapshot Snapshot() => this.snapshot;

    private void BufferChanged(object sender, TextContentChangedEventArgs info) {
      if (info.After != buffer.CurrentSnapshot) {
        return;
      }
      ReParse();
    }

    private void ReParse() {
      var root        = buffer.ObtainOrAttachTree().Root();
      var newSnapshot = buffer.CurrentSnapshot;

      var newRegions = root.AsSequence().Where(a => a.IsString()).Select(a => new Region() {
        StartLine   = a.line,
        EndLine     = newSnapshot.GetLineFromPosition(a.end).LineNumber,
        StartOffset = a.begin,
        EndOffset   = a.end
      }).Where(a => a.IsMultiLine()).ToList();

      var oldSpans = this.regions.Select(a => a.AsSnapshotSpan(snapshot).TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span);
      var newSpans = new List<Span>(newRegions.Select(a => a.AsSpan()));

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
      regions  = newRegions;

      if (changeStart <= changeEnd) {
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, Span.FromBounds(changeStart, changeEnd))));
      }
    }
  }
}