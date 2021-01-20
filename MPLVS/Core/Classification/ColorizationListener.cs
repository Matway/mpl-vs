using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Classification {
  [Export(typeof(IWpfTextViewCreationListener))]
  [ContentType(Constants.MPLContentType)]
  [TextViewRole(PredefinedTextViewRoles.Interactive)]
  internal class ColorizationViewCreationListener : IWpfTextViewCreationListener {
    [Import]
    internal IEditorFormatMapService FormatMapService = null;

    // TODO: Do we need this?
    public void TextViewCreated(IWpfTextView textView) {
      ThreadHelper.ThrowIfNotOnUIThread();

      var formatMap       = FormatMapService.GetEditorFormatMap(textView);
      var mplContent      = formatMap.GetProperties("MplContent");
      var mplCodeBrackets = formatMap.GetProperties("MplCodeBrackets");
    }
  }
}