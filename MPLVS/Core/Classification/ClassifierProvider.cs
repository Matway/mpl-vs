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
    private readonly IClassificationTypeRegistryService classificationRegistry = null;

    internal static IClassificationTypeRegistryService ClassificationRegistry = null;
    internal static ImmutableList<IClassificationType> Classifications        = null;

    public IClassifier GetClassifier(ITextBuffer buffer) {
      if (buffer is null) {
        throw new ArgumentNullException(nameof(buffer));
      }

      if (ClassificationRegistry is null) {
        ClassificationRegistry = classificationRegistry;

        Classifications = new SortedDictionary<NodeType, IClassificationType> {
          [NodeType.MPLCONTENT]   = ClassificationRegistry.GetClassificationType("MplContent"),
          [NodeType.BUILTIN]      = ClassificationRegistry.GetClassificationType("MplBuiltin"),
          [NodeType.LABEL]        = ClassificationRegistry.GetClassificationType("MplLabel"),
          [NodeType.COMMENT]      = ClassificationRegistry.GetClassificationType("MplComment"),
          [NodeType.CONSTANT]     = ClassificationRegistry.GetClassificationType("MplConstant"),
          [NodeType.OBJECT]       = ClassificationRegistry.GetClassificationType("MplObject"),
          [NodeType.LIST]         = ClassificationRegistry.GetClassificationType("MplList"),
          [NodeType.TEXT]         = ClassificationRegistry.GetClassificationType("MplText"),
          [NodeType.CODEBRACKETS] = ClassificationRegistry.GetClassificationType("MplCodeBrackets")
        }.Values.ToImmutableList();
      }

      return buffer.ObtainOrAttachTree().Classifier;
    }
  }
}