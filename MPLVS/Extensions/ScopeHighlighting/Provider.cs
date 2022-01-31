using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.ScopeHighlighting {
  [Export(typeof(IViewTaggerProvider))]
  [ContentType(Constants.MPLContentType)]
  [TagType(typeof(Extensions.Tag))]
  internal class Provider : IViewTaggerProvider {
    public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag {
      var roles = view.Roles?.FirstOrDefault(a => a == "ENHANCED_SCROLLBAR_PREVIEW") is object;

      return view.TextBuffer != buffer || roles ? null : view.ObtainOrAttachProperty(() => new Tagger(view)) as ITagger<T>;
    }
  }
}