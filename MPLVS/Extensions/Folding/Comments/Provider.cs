using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Folding.Comments {
  [Export(typeof(ITaggerProvider))]
  [ContentType(Constants.MPLContentType)]
  [TagType(typeof(Tag))]
  internal class Provider : ITaggerProvider {
    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
      buffer.ObtainOrAttachProperty(() => new Tagger(buffer)) as ITagger<T>;
  }
}