using Microsoft.VisualStudio.Text.Tagging;

namespace MPLVS.Extensions {
  internal class Tag : ITextMarkerTag , IOverviewMarkTag {
    public Tag(string textFormadDefenition, string overviewFormatDefinition) {
      this.MarkKindName = textFormadDefenition;
      this.Type         = overviewFormatDefinition;
    }

    public string MarkKindName { get; }
    public string Type { get; }
  }
}