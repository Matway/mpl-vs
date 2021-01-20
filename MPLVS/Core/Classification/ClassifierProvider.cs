using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Core;

namespace MPLVS.Classification {
  /// <summary>
  /// This class causes a classifier to be added to the set of classifiers. Since
  /// the content type is set to "MPL", this classifier applies to all .mpl files
  /// </summary>
  [Export(typeof(IClassifierProvider))]
  [ContentType(Constants.MPLContentType)]
  internal class MplClassifierProvider : IClassifierProvider {
    /// <summary>
    /// Import the classification registry to be used for getting a reference
    /// to the custom classification type later.
    /// </summary>
    [Import]
    private readonly IClassificationTypeRegistryService classificationRegistry = null; // Set via MEF

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
          [NodeType.COMMENT]      = ClassificationRegistry.GetClassificationType("MplComment"),
          [NodeType.LABEL]        = ClassificationRegistry.GetClassificationType("MplLabel"),
          [NodeType.CONSTANT]     = ClassificationRegistry.GetClassificationType("MplConstant"),
          [NodeType.LIST]         = ClassificationRegistry.GetClassificationType("MplList"),
          [NodeType.OBJECT]       = ClassificationRegistry.GetClassificationType("MplObject"),
          [NodeType.TEXT]         = ClassificationRegistry.GetClassificationType("MplText"),
          [NodeType.CODEBRACKETS] = ClassificationRegistry.GetClassificationType("MplCodeBrackets")
        }.Values.ToImmutableList();
      }

      return buffer.ObtainOrAttachTree().Classifier;
    }
  }
}