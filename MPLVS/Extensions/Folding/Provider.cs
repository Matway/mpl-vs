using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Core;

namespace MPLVS.Folding {
  [Export(typeof(ITaggerProvider))]
  [ContentType(Constants.MPLContentType)]
  [TagType(typeof(IOutliningRegionTag))]
  internal class Provider : ITaggerProvider {
    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
      return buffer.ObtainOrAttachProperty(() => new Tagger(buffer)) as ITagger<T>;
    }
  }
}