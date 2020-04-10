using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;

namespace MPL.BraceMatching {
  [Export(typeof(EditorFormatDefinition))]
  [Name("MplBraceFound")]
  [UserVisible(true)]
  internal class BraceFoundFormatDefinition : MarkerFormatDefinition {
    public BraceFoundFormatDefinition() {
      ThreadHelper.ThrowIfNotOnUIThread();
      DisplayName = "MPL - Brace Matching";
      ZOrder = 5;
      if (MplPackage.Options.SolarizedTheme) {
        BackgroundColor = MplPackage.MplBraceMatchingColor;
      } else {
        BackgroundColor = Colors.LightBlue;
      }
    }
  }
}