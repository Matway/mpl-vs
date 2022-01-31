using System.Windows.Controls;
using System.Windows.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace MPLVS.Folding {
  public class Tag : StructureTag {
    public Tag(ITextSnapshot snapshot, Span? outliningSpan = null, Span? headerSpan = null, Span? guideLineSpan = null, int? guideLineHorizontalAnchor = null, string type = null, bool isCollapsible = false, bool isDefaultCollapsed = false, bool isImplementation = false, object collapsedForm = null, object collapsedHintForm = null) : base(snapshot, outliningSpan, headerSpan, guideLineSpan, guideLineHorizontalAnchor, type, isCollapsible, isDefaultCollapsed, isImplementation, collapsedForm, collapsedHintForm) {

    }

    public override object GetCollapsedHintForm() =>
      this.OutliningSpan.HasValue
      ? Tooltip.FlromSpan(this.Snapshot, this.OutliningSpan.Value)
      : base.GetCollapsedHintForm();
  }

  public class Tooltip {
    private static readonly FontFamily Fonts = new FontFamily("Cascadia Code, Consolas, Courier New");

    public static TextBlock FlromSpan(ITextSnapshot snapshot, Span span) => FromSnapshot(new SnapshotSpan(snapshot, span));

    public static TextBlock FromSnapshot(SnapshotSpan snapshot) => new TextBlock {
      FontFamily = Fonts,
      Text       = snapshot.GetText()
    };
  }
}