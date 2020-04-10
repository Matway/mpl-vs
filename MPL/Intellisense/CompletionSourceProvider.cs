using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
namespace MPL.Intellisense {

  [Export(typeof(ICompletionSourceProvider))]
  [ContentType(Constants.MPLContentType)]
  [Name("token completion")]
  internal class CompletionSourceProvider : ICompletionSourceProvider {
    [Import]
    internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

    public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer) {
      return new CompletionSource(this, textBuffer);
    }
  }
}
