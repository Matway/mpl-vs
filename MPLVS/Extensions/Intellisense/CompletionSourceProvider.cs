using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Intellisense {
  [Export(typeof(ICompletionSourceProvider))]
  [ContentType(Constants.MPLContentType)]
  [Name("token completion")]
  internal class CompletionSourceProvider : ICompletionSourceProvider {
    [Import]
    internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

    public ICompletionSource TryCreateCompletionSource(ITextBuffer buffer) =>
      // TODO: Should we create it like this: buffer?.ObtainOrAttachProperty(() => new CompletionSource(this, buffer)); ?
      new CompletionSource(this, buffer);
  }
}