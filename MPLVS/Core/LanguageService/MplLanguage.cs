using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MPLVS {
  [Guid("5b6692e7-a860-4b7d-9242-a6781510dee0")]
  public class MplLanguage : LanguageService {
    private LanguagePreferences preferences;

    public override int ValidateBreakpointLocation(IVsTextBuffer buffer, int line, int col, TextSpan[] pCodeSpan) {
      if (pCodeSpan != null) {
        pCodeSpan[0].iStartLine  = line;
        pCodeSpan[0].iStartIndex = col;
        pCodeSpan[0].iEndLine    = line;
        pCodeSpan[0].iEndIndex   = col + 1;
      }

      return VSConstants.S_OK;
    }

    public override LanguagePreferences GetLanguagePreferences() {
      return
        preferences ?? (preferences =
          new LanguagePreferences(this.Site, typeof(MplLanguage).GUID, this.Name) {
            EnableCodeSense             = true,
            EnableMatchBraces           = true,
            EnableCommenting            = true,
            EnableShowMatchingBrace     = true,
            EnableMatchBracesAtCaret    = true,
            EnableFormatSelection       = true,
            HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES,
            LineNumbers                 = true,
            MaxErrorMessages            = 100,
            AutoOutlining               = false,
            MaxRegionTime               = 2000,
            ShowNavigationBar           = true,
            InsertTabs                  = false,
            IndentSize                  = 7,
            AutoListMembers             = false,
            EnableQuickInfo             = false,
            ParameterInformation        = false
          });
    }

    public MplLanguage(object site) {
      ThreadHelper.ThrowIfNotOnUIThread();
      SetSite(site);
    }

    public override string GetFormatFilterList() =>
      "MPL File (*.mpl, *.fast, *.smart, *.easy)|*.mpl;*.fast;*.smart,*.easy";

    public override IScanner GetScanner(IVsTextLines buffer) => null;

    //public override Source CreateSource(IVsTextLines buffer) {
    //  return new MplSource(this, buffer, new MplColorizer(this, buffer, null));
    //}

    public override string Name => Constants.LanguageName;

    public override AuthoringScope ParseSource(ParseRequest req) => null;

    public override void Dispose() {
      try {
        if (preferences != null) {
          preferences.Dispose();
          preferences = null;
        }
      }
      finally {
        base.Dispose();
      }
    }
  }
}