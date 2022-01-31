using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

using MPLVS.Core.ParseTree;
using MPLVS.ParseTree;

namespace MPLVS {
  [Guid("5b6692e7-a860-4b7d-9242-a6781510dee0")]
  public class MplLanguage : LanguageService {
    private LanguagePreferences preferences;

    public override int ValidateBreakpointLocation(IVsTextBuffer buffer, int line, int col, TextSpan[] pCodeSpan) {
      if (pCodeSpan is null) { return VSConstants.S_OK; }

      pCodeSpan[0].iStartLine  = line;
      pCodeSpan[0].iStartIndex = col;
      pCodeSpan[0].iEndLine    = line;
      pCodeSpan[0].iEndIndex   = col + 1;

      return AdjustBreakpoint(buffer, pCodeSpan);
    }

    private static int AdjustBreakpoint(IVsTextBuffer buffer, TextSpan[] pCodeSpan) {
      var lines = BufferFromVsBuffer(buffer);

      if (lines is null) { return VSConstants.S_OK; }

      var line     = pCodeSpan[0].iStartLine;
      var column   = pCodeSpan[0].iStartIndex;
      var position = (lines.CurrentSnapshot.GetLineFromLineNumber(line).Start + column).Position;

      var symbol =
        lines.ObtainOrAttachTree().Root()
             .Surroundings(position, Core.ParseTree.Strategy.FarLeftNearRight).AsSequence()
             .LastOrDefault(a => a is object && !a.IsComment() && !a.IsEol() && !a.IsEof());

      if (symbol is null) { return VSConstants.S_FALSE; }

      var token =
        symbol.Previous?.Previous is object && symbol.Previous.Previous.IsLabel()
        ? symbol.Previous.Previous
        : symbol;

      pCodeSpan[0] = AsTextSpan(token, lines);

      return VSConstants.S_OK;
    }

    public static TextSpan AsTextSpan(Builder.Node node, ITextBuffer buffer) {
      var endLine = buffer.CurrentSnapshot.GetLineFromPosition(node.end);

      return new TextSpan {
        iStartLine  = node.line,
        iStartIndex = node.column,
        iEndLine    = endLine.LineNumber,
        iEndIndex   = node.end - endLine.Start.Position
      };
    }

    private static ITextBuffer BufferFromVsBuffer(IVsTextBuffer buffer) =>
      MplPackage.Instance?.GetComponentModelService<IVsEditorAdaptersFactoryService>()?.GetDocumentBuffer(buffer);

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