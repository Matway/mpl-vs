using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MPL.SmartIndent {
  [Export(typeof(ISmartIndentProvider))]
  [ContentType(Constants.MPLContentType)]
  public sealed class Provider : ISmartIndentProvider {
    public ISmartIndent CreateSmartIndent(ITextView a) => new Indentation(a);
  }
}