using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Core;
using MPLVS.Symbols;

namespace MPLVS.Commands {
  [Export(typeof(IVsTextViewCreationListener))]
  [ContentType(Constants.MPLContentType)]
  [TextViewRole(PredefinedTextViewRoles.Interactive)]
  internal sealed class TextViewCreationListener : IVsTextViewCreationListener {
    [Import]
    private IVsEditorAdaptersFactoryService AdapterService { get; set; }

    [Import]
    internal SVsServiceProvider ServiceProvider { get; set; }

    public void VsTextViewCreated(IVsTextView textViewAdapter) {
      var textView = AdapterService.GetWpfTextView(textViewAdapter);
      if (textView == null) {
        return;
      }

      OpenedBuffers.VsTextViewCreated(textViewAdapter, textView);

      textView.ObtainOrAttachProperty(() => new CommentSelection(textViewAdapter, textView));
      textView.ObtainOrAttachProperty(() => new FormatDocument(textViewAdapter, textView));
      textView.ObtainOrAttachProperty(() => new GoToBrace(textViewAdapter, textView));
      textView.ObtainOrAttachProperty(() => new BraceCompletion(textViewAdapter, textView));
      textView.ObtainOrAttachProperty(() => new Guillemets.AngularQuotes(textViewAdapter, textView));
      textView.ObtainOrAttachProperty(() => new Guillemets.Backspace(textViewAdapter, textView));
      textView.ObtainOrAttachProperty(() => new GoToDefinition(textViewAdapter, textView));
    }
  }
}