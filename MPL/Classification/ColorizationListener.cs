using System.Windows;
using System.Windows.Media;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;

namespace MPL.Classification {

  [Export(typeof(IWpfTextViewCreationListener))]
  [ContentType(Constants.MPLContentType)]
  [TextViewRole(PredefinedTextViewRoles.Interactive)]
  internal class ColorizationViewCreationListener : IWpfTextViewCreationListener {

    [Import]
    internal IEditorFormatMapService FormatMapService = null;
      
    public void TextViewCreated(IWpfTextView textView) {
      ThreadHelper.ThrowIfNotOnUIThread();

      IEditorFormatMap formatMap = FormatMapService.GetEditorFormatMap(textView);

      ResourceDictionary mplContent = formatMap.GetProperties("MplContent");
      ResourceDictionary mplCodeBrackets = formatMap.GetProperties("MplCodeBrackets");

      if (MplPackage.Options.SolarizedTheme) {
        if (MplPackage.Options.DarkThemesList.Contains(MplPackage.GetThemeName())) {
          //dark theme
          textView.Background = Constants.backgroundDarkBrush;
        } else {
          //light theme
          textView.Background = Constants.backgroundLightBrush;
        }

        formatMap.BeginBatchUpdate();

        mplContent[EditorFormatDefinition.ForegroundColorId] = MplPackage.MplContentColor;
        mplContent[EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush(MplPackage.MplContentColor);
        formatMap.SetProperties("MplContent", mplContent);

        mplCodeBrackets[EditorFormatDefinition.ForegroundColorId] = MplPackage.MplEmphasizedColor;
        mplCodeBrackets[EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush(MplPackage.MplEmphasizedColor);
        formatMap.SetProperties("MplCodeBrackets", mplCodeBrackets);

        formatMap.EndBatchUpdate();
      }
    }
  }
}
