using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;


namespace MPLVS.Extensions {
  public abstract class HorizontalTags<T> : ITagger<T> where T : ITag {
    // TODO: What should we do with the tags which covers more than one given spans?
    //       For now, they will be included in the result several times.
    public IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans) =>
      // FIXME: Lambda 'a => this.Tags(a.Span)' will capture 'this'.
      spans.SelectMany(a => this.Tags(a.Span)).ToList();

    protected abstract IEnumerable<ITagSpan<T>> Tags(Span span);

    public virtual event EventHandler<SnapshotSpanEventArgs> TagsChanged;
  }

  public abstract class VerticalTags<T> : HorizontalTags<T> where T : ITag {
    protected abstract ITextSnapshot Snapshot();
    protected abstract IEnumerable<Region> Regions();

    protected abstract ITagSpan<T> AsTag(Region region, SnapshotSpan range);

    protected override IEnumerable<ITagSpan<T>> Tags(Span span) {
      var start    = this.Snapshot().GetLineNumberFromPosition(span.Start);
      var end      = this.Snapshot().GetLineNumberFromPosition(span.End);
      var window   = Span.FromBounds(start, end);

      var snapshot = this.Snapshot();

      return this.Regions().Where(a => Span.FromBounds(a.StartLine, a.EndLine).IntersectsWith(window)).Select(b => {
        var range = new SnapshotSpan(snapshot, b.StartOffset, b.EndOffset - b.StartOffset);
        return this.AsTag(b, range);
      });
    }
  }

  public struct Region {
    public int StartLine;
    public int EndLine;
    public int StartOffset;
    public int EndOffset;

    public bool IsClosed;
    public bool IsSignificant;

    public Span Header;

    internal SnapshotSpan AsSnapshotSpan(ITextSnapshot snapshot) {
      var begin  = new SnapshotPoint(snapshot, this.StartOffset);
      var length = this.EndOffset - this.StartOffset;
      return new SnapshotSpan(begin, length);
    }

    internal Span AsSpan() {
      return Span.FromBounds(this.StartOffset, this.EndOffset);
    }

    internal bool IsMultiLine() => this.StartLine != this.EndLine;
  }
}