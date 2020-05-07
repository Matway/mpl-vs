using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MPL.BraceMatching {
  [Export(typeof(IViewTaggerProvider))]
  [ContentType(Constants.MPLContentType)]
  [TagType(typeof(TextMarkerTag))]
  internal class BraceMatchingTaggerProvider : IViewTaggerProvider {
    public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
      return buffer.Properties.GetOrCreateSingletonProperty(() => new BraceMatchingTagger(textView)) as ITagger<T>;
    }
  }
}