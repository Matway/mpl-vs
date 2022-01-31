using Microsoft.VisualStudio.Text.Tagging;

namespace MPLVS.Extensions {
  internal class Tag : ITextMarkerTag , IOverviewMarkTag {
    public Tag(string textFormatDefinition, string overviewFormatDefinition) {
      this.MarkKindName = textFormatDefinition;
      this.Type         = overviewFormatDefinition;
    }

    public string MarkKindName { get; }
    public string Type { get; }
  }
}