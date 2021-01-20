using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Core;

namespace MPLVS.ScopeHighlighting {
  [Export(typeof(IViewTaggerProvider))]
  [ContentType(Constants.MPLContentType)]
  [TagType(typeof(TextMarkerTag))]
  internal class Provider : IViewTaggerProvider {
    public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag =>
      view.ObtainOrAttachProperty(() => new Tagger(view)) as ITagger<T>;
  }
}