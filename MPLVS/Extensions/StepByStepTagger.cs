using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace MPLVS.Extensions {
  // TODO: Rename this class.
  // TODO: What should we do with the tags which covers more than one requested span?
  //       For now, they will be included in the result several times.
  public abstract class StepByStepTagger<T> : ITagger<T> where T : ITag {
    public IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans) =>
      spans.SelectMany(a => Tags(a.Span));

    protected abstract IEnumerable<ITagSpan<T>> Tags(Span span);

    public virtual event EventHandler<SnapshotSpanEventArgs> TagsChanged;
  }
}