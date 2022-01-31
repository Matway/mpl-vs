using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.SymbolHighlighting {
  [Export(typeof(IViewTaggerProvider))]
  [ContentType(Constants.MPLContentType)]
  [TagType(typeof(Extensions.Tag))]
  internal class Provider : IViewTaggerProvider {
    public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag =>
      textView.ObtainOrAttachProperty(() => new Tagger(textView)) as ITagger<T>;
  }
}