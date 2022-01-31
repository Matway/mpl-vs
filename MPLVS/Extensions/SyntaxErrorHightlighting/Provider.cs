using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Extensions.SyntaxErrorHightlighting {
  [Export(typeof(ITaggerProvider))]
  [ContentType(Constants.MPLContentType)]
  [TagType(typeof(IErrorTag))]
  internal class Provider : ITaggerProvider {
    ITagger<IErrorTag> ITaggerProvider.CreateTagger<IErrorTag>(ITextBuffer buffer) {
      if (buffer is null) {
        throw new ArgumentNullException(nameof(buffer));
      }

      return buffer.ObtainOrAttachProperty(() => new Tagger(buffer)) as ITagger<IErrorTag>;
    }
  }
}