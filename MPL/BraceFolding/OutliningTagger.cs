using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace MPL.BraceFolding {
  internal sealed class OutliningTagger : ITagger<IOutliningRegionTag> {
    private readonly string ellipsis = " ... ";
    private readonly Dictionary<char, char> bracePairs;
    private ITextBuffer buffer;
    private ITextSnapshot snapshot;
    private List<Region> regions;

    public OutliningTagger(ITextBuffer buf) {
      bracePairs = new Dictionary<char, char> {
        ['{'] = '}',
        ['['] = ']',
        ['('] = ')'
      };

      buffer = buf;
      snapshot = buffer.CurrentSnapshot;
      regions = new List<Region>();
      ReParse();
      buffer.Changed += BufferChanged;
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
      if (spans.Count == 0) {
        yield break;
      }

      List<Region> currentRegions = regions;
      ITextSnapshot currentSnapshot = snapshot;
      SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
      int startLineNumber = entire.Start.GetContainingLine().LineNumber;
      int endLineNumber = entire.End.GetContainingLine().LineNumber;
      foreach (var region in currentRegions) {
        if (region.StartLine <= endLineNumber && region.EndLine >= startLineNumber) {
          var startPosition = new SnapshotPoint(snapshot, region.StartOffset);
          var endPosition = new SnapshotPoint(snapshot, region.EndOffset + 1);
          yield return new TagSpan<IOutliningRegionTag>(
            new SnapshotSpan(startPosition, endPosition),
            new OutliningRegionTag(false, false, region.Type + ellipsis + bracePairs[region.Type], ""));
        }
      }
    }

    private class Region {
      public int StartLine { get; set; }
      public int EndLine { get; set; }
      public int StartOffset { get; set; }
      public int EndOffset { get; set; }
      public int Level { get; set; }
      public char Type { get; set; }
    }

    private void BufferChanged(object sender, TextContentChangedEventArgs e) {
      if (e.After != buffer.CurrentSnapshot)
        return;
      ReParse();
    }

    private void ReParse() {
      AST.TreeBuilder.Node root = AST.AST.GetASTRoot();
      ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
      List<Region> newRegions = new List<Region>();
      Stack<char> brackets = new Stack<char>();
      Stack<int> offsets = new Stack<int>();
      int level = 0;

      //string text = newSnapshot.GetText();

      void Traverse(AST.TreeBuilder.Node node) {

        if (node.name == "'['" || node.name == "'{'" || node.name == "'('") {
          brackets.Push(node.name[1]);
          offsets.Push(node.begin);
          ++level;
        }

        if (brackets.Count != 0 && node.name[1] == bracePairs[brackets.Peek()]) {
          int beginLine = newSnapshot.GetLineFromPosition(offsets.Peek()).LineNumber;
          int endLine = newSnapshot.GetLineFromPosition(node.begin).LineNumber;
          if (beginLine != endLine) {
            if (newRegions.Count != 0 && newRegions.Last().StartOffset == offsets.Peek() + 1 && newRegions.Last().EndOffset == node.begin - 1) {
              newRegions.Last().Level = level;
              newRegions.Last().StartLine = beginLine;
              newRegions.Last().EndLine = endLine;
              newRegions.Last().StartOffset = offsets.Peek();
              newRegions.Last().EndOffset = node.begin;
              newRegions.Last().Type = brackets.Peek();
            } else {
              newRegions.Add(new Region() {
                Level = level,
                StartLine = beginLine,
                EndLine = endLine,
                StartOffset = offsets.Peek(),
                EndOffset = node.begin,
                Type = brackets.Peek()
              });
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

      List<Span> oldSpans = new List<Span>(this.regions.Select(r => AsSnapshotSpan(r, snapshot)
          .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span));
      List<Span> newSpans = new List<Span>(newRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));

      NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
      NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

      NormalizedSpanCollection removed = NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

      int changeStart = int.MaxValue;
      int changeEnd = -1;

      if (removed.Count > 0) {
        changeStart = removed[0].Start;
        changeEnd = removed[removed.Count - 1].End;
      }

      if (newSpans.Count > 0) {
        changeStart = Math.Min(changeStart, newSpans[0].Start);
        changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
      }

      snapshot = newSnapshot;
      regions = newRegions;

      if (changeStart <= changeEnd) {
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, Span.FromBounds(changeStart, changeEnd))));
      }
    }

    private static SnapshotSpan AsSnapshotSpan(Region region, ITextSnapshot snapshot) {
      var startPosition = new SnapshotPoint(snapshot, region.StartOffset);
      var endPosition = new SnapshotPoint(snapshot, region.EndOffset + 1);
      return new SnapshotSpan(startPosition, endPosition);
    }
  }
}
