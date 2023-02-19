using System.ComponentModel;

using Microsoft.VisualStudio.Shell;

namespace MPLVS {
  public class Options : DialogPage {
    public enum LineEnding { Unix, Windows, Document }

    [Category("Code formatting")]
    [DisplayName("Line endings")]
    [Description("After you format a document, it will have line endings as in:\n  Unix (default) - LF\n  Windows - CRLF\n  Document - leave as is")]
    [DefaultValue(LineEnding.Unix)]
    public LineEnding LineEndings { get; set; } = LineEnding.Unix;

    [Category("Code completion")]
    [DisplayName("Disable auto-completion")]
    [Description("...")]
    [DefaultValue(false)]
    public bool AutocompletionOff { get; set; } = false;

    [Category("Code completion")]
    [DisplayName("Project-wide symbol search")]
    [Description("Search symbols for auto-completion not only in current file, but in other project files too.\nCurrent state of this feature is prototype, and if it is enabled, then code editing may be slowdown drastically.")]
    [DefaultValue(false)]
    public bool AutocompletionProjectWideSearch { get; set; } = false;

    [Category("Brace completion")]
    [DisplayName("Auto brace completion")]
    [Description("Defines if bracket will be completed automatically")]
    [DefaultValue(true)]
    public bool AutoBraceCompletion { get; set; } = true;

    [Category("Guillemets")]
    [DisplayName("Guillemets (angular quotes)")]
    [Description("When entering < or > symbols, but there is the same symbol on the left, transforms them into « or » symbols.")]
    [DefaultValue(true)]
    public bool AngularQuotes { get; set; } = true;
  }
}