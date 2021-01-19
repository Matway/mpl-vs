using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MPL.Commands {
  [Export(typeof(IVsTextViewCreationListener))]
  [ContentType(Constants.MPLContentType)]
  [TextViewRole(PredefinedTextViewRoles.Interactive)]
  internal sealed class TextViewCreationListener : IVsTextViewCreationListener {
    [Import]
    IVsEditorAdaptersFactoryService AdapterService { get; set; }

    [Import]
    internal SVsServiceProvider ServiceProvider { get; set; }

    public void VsTextViewCreated(IVsTextView textViewAdapter) {
      IWpfTextView textView = AdapterService.GetWpfTextView(textViewAdapter);
      if (textView == null) {
        return;
      }

      textView.Properties.GetOrCreateSingletonProperty(() => new CommentSelectionCommandHandler(textViewAdapter, textView));
      textView.Properties.GetOrCreateSingletonProperty(() => new FormatDocumentHandler(textViewAdapter, textView));
      textView.Properties.GetOrCreateSingletonProperty(() => new GoToBraceCommandHandler(textViewAdapter, textView));
      textView.Properties.GetOrCreateSingletonProperty(() => new BraceCompletionCommandHandler(textViewAdapter, textView));
      textView.Properties.GetOrCreateSingletonProperty(() => new AngularQuotesCommandHandler(textViewAdapter, textView));
      textView.Properties.GetOrCreateSingletonProperty(() => new BackspaceCommandHandler(textViewAdapter, textView));
      textView.Properties.GetOrCreateSingletonProperty(() => new GoToDefinitionCommandHandler(textViewAdapter, textView));
    }
  }
}