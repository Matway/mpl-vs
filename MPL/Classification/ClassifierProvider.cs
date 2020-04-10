using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MPL.Classification {
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
    internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

    public IClassifier GetClassifier(ITextBuffer buffer) {
      return buffer.Properties.GetOrCreateSingletonProperty(() => new MplClassifier(ClassificationRegistry, buffer));
    }
  }
}
