using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace MPLVS.Extensions.SyntaxErrorHightlighting {
  internal class Tagger : HorizontalTags<IErrorTag> {
    private readonly ITextBuffer buffer;

    public Tagger(ITextBuffer buffer) {
      this.buffer = buffer;
      this.buffer.Changed += this.OnChanged;
    }

    private void OnChanged(object sender, TextContentChangedEventArgs e) {
      // TODO: I don't know what to put into SnapshotSpanEventArgs, so i will put just something.
      //       But, probably, this is inefficient way to update tags.

      /*
      ................   If we have the snapshot-span
       # #  ##   #       which includes these tags
      .#.#..##...#....   then we say that we have this state

      An example:
      .#.#..##...#.... old state
      .#..#.....       new state

      Which span should we put to the event?
      .^^^^^^^^^       a) span from where tags are and where tags were
      .^^^^^^^..       b) span from ???
      ...^^^^^^^       c) span which includes only changed tags
      ...^^^^^..       d) span from ???
      .^^^^.....       e) span from the part which includes only new tags

      Another example:
      .#..#.....       old state
      .#.#..##...#.... new state

      .^^^^^^^^^^^.... a) span from where tags are and where tags were
      ...^^^^^^^^^.... c) span which includes only changed tags
      .^^^^^^^^^^^.... e) span from the part which includes only new tags

      So for simplicity i will put the entire snapshot-span.
      */
      var span = new SnapshotSpan(this.buffer.CurrentSnapshot, 0, this.buffer.CurrentSnapshot.Length);
      this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
    }

    protected override IEnumerable<ITagSpan<IErrorTag>> Tags(Span x) {
      var snapshot = this.buffer.CurrentSnapshot;

      // FIXME: Tree.GetErrors gives a sorted collection, so we must use some kind of binary search.
      var errors = this.buffer.ObtainOrAttachTree().GetErrors();
      return errors.Where(a => Span.FromBounds(a.Begin, a.End).IntersectsWith(x)).Select(a => {
        var span   = new SnapshotSpan(snapshot, Span.FromBounds(a.Begin, a.End));
        var mesage = new ErrorTag(PredefinedErrorTypeNames.SyntaxError, a.RawMessage);
        return new TagSpan<IErrorTag>(span, mesage);
      });
    }

    public override event EventHandler<SnapshotSpanEventArgs> TagsChanged;
  }
}