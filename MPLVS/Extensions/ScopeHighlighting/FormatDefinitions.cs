using System.ComponentModel.Composition;
using System.Windows.Media;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Classification;

namespace MPLVS.ScopeHighlighting {
  [Export(typeof(EditorFormatDefinition))]
  [Name("MplScope")]
  [UserVisible(true)]
  internal class FormatDefinition : MarkerFormatDefinition {
    public FormatDefinition() {
      ThreadHelper.ThrowIfNotOnUIThread();

      DisplayName = "Scope".MplClass();
      ZOrder      = 5;

      ForegroundColor = Color.FromArgb(33, 255, 132, 9);
      BackgroundColor = Color.FromArgb(33, 91,  46,  0);
    }
  }
}