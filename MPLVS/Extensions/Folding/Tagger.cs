using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using MPLVS.Core;
using MPLVS.Core.ParseTree;
using MPLVS.Extensions;

namespace MPLVS.Folding {
  internal sealed class Tagger : StepByStepTagger<IOutliningRegionTag> {
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

    protected override IEnumerable<ITagSpan<IOutliningRegionTag>> Tags(Span span) {
      var currentRegions  = regions;
      var currentSnapshot = snapshot;
      var start           = snapshot.GetLineNumberFromPosition(span.Start);
      var end             = snapshot.GetLineNumberFromPosition(span.End);
      var window          = Span.FromBounds(start, end);

      return regions.Where(a => Span.FromBounds(a.startLine, a.endLine).IntersectsWith(window)).Select(b => {
        var snapshot = new SnapshotSpan(this.snapshot, b.startOffset + 1, b.endOffset - b.startOffset - 1);
        return new TagSpan<IOutliningRegionTag>(snapshot, b.ToOutliningTag(snapshot));
      });
    }

    internal struct Region {
      public int startLine;
      public int endLine;
      public int startOffset;
      public int endOffset;
      public int level;
      public char type;
    }

    private void BufferChanged(object sender, TextContentChangedEventArgs info) {
      if (info.After != buffer.CurrentSnapshot) {
        return;
      }
      ReParse();
    }

    private void ReParse() {
      var root        = buffer.ObtainOrAttachTree().Root();
      var newSnapshot = buffer.CurrentSnapshot;
      var newRegions  = new List<Region>();
      var brackets    = new Stack<char>();
      var offsets     = new Stack<int>();
      var level       = 0;

      void Traverse(ParseTree.Builder.Node node) {
        if (node.IsScopeStart()) {
          brackets.Push(node.name[1]);
          offsets.Push(node.begin);
          ++level;
        }

        if (brackets.Any() && node.name[1] == Core.ParseTree.Utils.Braces[brackets.Peek()]) {
          var beginLine = newSnapshot.GetLineFromPosition(offsets.Peek()).LineNumber;
          var endLine   = newSnapshot.GetLineFromPosition(node.begin).LineNumber;
          if (beginLine != endLine) {
            var region = new Region() {
              level       = level,
              startLine   = beginLine,
              endLine     = endLine,
              startOffset = offsets.Peek(),
              endOffset   = node.begin,
              type        = brackets.Peek()
            };

            if (newRegions.Any()
                && newRegions.Last().startOffset == offsets.Peek() + 1
                && newRegions.Last().endOffset == node.begin - 1) {
              newRegions[newRegions.Count - 1] = region;
            }
            else {
              newRegions.Add(region);
            }
          }

          --level;
          offsets.Pop();
          brackets.Pop();
        }

        if (node.children == null) {
          return;
        }

        foreach (var child in node.children) {
          Traverse(child);
        }
      }

      Traverse(root);

      var oldSpans = this.regions.Select(a => AsSnapshotSpan(a, snapshot).TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span);
      var newSpans = new List<Span>(newRegions.Select(a => AsSnapshotSpan(a, newSnapshot).Span));

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

    private static SnapshotSpan AsSnapshotSpan(Region region, ITextSnapshot snapshot) {
      var startPosition = new SnapshotPoint(snapshot, region.startOffset);
      var endPosition = new SnapshotPoint(snapshot, region.endOffset + 1);
      return new SnapshotSpan(startPosition, endPosition);
    }
  }
}