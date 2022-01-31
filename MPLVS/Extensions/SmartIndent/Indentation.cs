using System;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

using MPLVS.Core.ParseTree;

namespace MPLVS.SmartIndent {
  internal class Indentation : ISmartIndent {
    public Indentation(ITextView view) {
      if (view is null) { throw new ArgumentNullException(nameof(view)); }

      this.View = view;
    }

    public int? GetDesiredIndentation(ITextSnapshotLine line) {
      if (line is null || line.LineNumber == 0) { return null; }

      // There is no indentation inside of a string.
      var previous = this.View.TextBuffer.ObtainOrAttachTree().Root().NearLeft(line.Start);
      if (previous is object && previous.IsString() && previous.end >= line.Start) { return null; }

      var row = line.LineNumber;
      _ = this.View.TextBuffer.Properties.TryGetProperty<Folding.Blocks.Tagger>(typeof(Folding.Blocks.Tagger), out var folding);

      // FIXME: At this point, the indentation should never be null.
      // FIXME: Possibly, regions can be outdated. In case if the tagger don't receive an update event yet.
      var indentaion = folding?.regions.TakeWhile(a => a.StartLine < row).Where(a => a.EndLine > row || !a.IsClosed).Count();

      return
        indentaion is object && indentaion.HasValue
        ? (int?)indentaion.Value * this.IndentationSize
        : null;
    }

    private int IndentationSize =>
      this.View.Options.IsConvertTabsToSpacesEnabled()
      ? this.View.Options.GetIndentSize()
      : this.View.Options.GetTabSize();

    public void Dispose() { }

    private readonly ITextView View;
  }
}