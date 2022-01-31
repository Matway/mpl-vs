using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Classification {
  [Export(typeof(IClassifierProvider))]
  [ContentType(Constants.MPLContentType)]
  internal class MplClassifierProvider : IClassifierProvider {
    [Import]
    private readonly IClassificationTypeRegistryService ClassificationRegistry = null;

    internal static IClassificationTypeRegistryService Registry        = null;
    internal static ImmutableList<IClassificationType> Classifications = null;

    public IClassifier GetClassifier(ITextBuffer buffer) {
      if (buffer is null) {
        throw new ArgumentNullException(nameof(buffer));
      }

      if (Registry is null) {
        Registry = this.ClassificationRegistry;

        Classifications = new SortedDictionary<NodeType, IClassificationType> {
          [NodeType.MPLCONTENT]   = Registry.GetClassificationType("MplContent"),
          [NodeType.BUILTIN]      = Registry.GetClassificationType("MplBuiltin"),
          [NodeType.LABEL]        = Registry.GetClassificationType("MplLabel"),
          [NodeType.COMMENT]      = Registry.GetClassificationType("MplComment"),
          [NodeType.CONSTANT]     = Registry.GetClassificationType("MplConstant"),
          [NodeType.OBJECT]       = Registry.GetClassificationType("MplObject"),
          [NodeType.LIST]         = Registry.GetClassificationType("MplList"),
          [NodeType.TEXT]         = Registry.GetClassificationType("MplText"),
          [NodeType.CODEBRACKETS] = Registry.GetClassificationType("MplCodeBrackets")
        }.Values.ToImmutableList();
      }

      return buffer.ObtainOrAttachTree().Classifier;
    }
  }
}