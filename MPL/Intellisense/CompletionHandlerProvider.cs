using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MPL.Intellisense {

  [Export(typeof(IVsTextViewCreationListener))]
  [Name("token completion handler")]
  [ContentType(Constants.MPLContentType)]
  [TextViewRole(PredefinedTextViewRoles.Editable)]
  internal class CompletionHandlerProvider : IVsTextViewCreationListener {
    [Import]
    internal IVsEditorAdaptersFactoryService AdapterService = null;

    [Import]
    internal ICompletionBroker CompletionBroker { get; set; }

    [Import]
    internal SVsServiceProvider ServiceProvider { get; set; }

    public void VsTextViewCreated(IVsTextView textViewAdapter) {
      ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
      if (textView == null) {
        return;
      }

      //Func<MplCompletionCommandHandler> createCommandHandler = delegate () { return new MplCompletionCommandHandler(textViewAdapter, textView, this); };
      textView.Properties.GetOrCreateSingletonProperty(() => new CompletionCommandHandler(textViewAdapter, textView, this));
    }
  }
}
