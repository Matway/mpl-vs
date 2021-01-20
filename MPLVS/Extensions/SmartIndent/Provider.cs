using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Core;

namespace MPLVS.SmartIndent {
  [Export(typeof(ISmartIndentProvider))]
  [ContentType(Constants.MPLContentType)]
  public sealed class Provider : ISmartIndentProvider {
    public ISmartIndent CreateSmartIndent(ITextView view) {
      if (view is null) {
        throw new System.ArgumentNullException(nameof(view));
      }

      return view.ObtainOrAttachProperty(() => new Indentation(view));
    }
  }
}