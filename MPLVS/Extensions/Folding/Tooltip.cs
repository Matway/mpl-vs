using System;

using Microsoft.VisualStudio.Text;

namespace MPLVS.Folding {
  public class Tooltip {
    private readonly SnapshotSpan span;
    private readonly Lazy<string> hint;
    public Tooltip(SnapshotSpan snapshot) {
      this.span = snapshot;
      this.hint = new Lazy<string>(() => span.GetText());
    }

    public override string ToString() => this.hint.Value;
  }
}