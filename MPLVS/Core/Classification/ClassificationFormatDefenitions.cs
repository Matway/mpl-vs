using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Classification {
  internal static class Utils {
    public static string MplClass(this string className) =>
      " MPL - " + className;
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplContent")]
  [Name("MplContent")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MplContentClassificationFormat : ClassificationFormatDefinition {
    public MplContentClassificationFormat() {
      ThreadHelper.ThrowIfNotOnUIThread();
      this.DisplayName = "Plain text".MplClass();
    }
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplBuiltin")]
  [Name("MplBuiltin")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MplBuiltinClassificationFormat : ClassificationFormatDefinition {
    public MplBuiltinClassificationFormat() {
      this.DisplayName = "Built-ins".MplClass();
    }
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplComment")]
  [Name("MplComment")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class CommentClassificationFormat : ClassificationFormatDefinition {
    public CommentClassificationFormat() {
      this.DisplayName = "Comments".MplClass();
    }
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplLabel")]
  [Name("MplLabel")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class LabelClassificationFormat : ClassificationFormatDefinition {
    public LabelClassificationFormat() {
      this.DisplayName = "Labels".MplClass();
    }
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplConstant")]
  [Name("MplConstant")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class ConstantClassificationFormat : ClassificationFormatDefinition {
    public ConstantClassificationFormat() {
      this.DisplayName = "Constants".MplClass();
    }
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplObject")]
  [Name("MplObject")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class ObjectClassificationFormat : ClassificationFormatDefinition {
    public ObjectClassificationFormat() {
      this.DisplayName = "Objects".MplClass();
    }
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplList")]
  [Name("MplList")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class ListClassificationFormat : ClassificationFormatDefinition {
    public ListClassificationFormat() {
      this.DisplayName = "Lists".MplClass();
    }
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplText")]
  [Name("MplText")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class TextClassificationFormat : ClassificationFormatDefinition {
    public TextClassificationFormat() {
      this.DisplayName = "Strings".MplClass();
    }
  }

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplCodeBrackets")]
  [Name("MplCodeBrackets")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class CodeBracketsClassificationFormat : ClassificationFormatDefinition {
    public CodeBracketsClassificationFormat() {
      ThreadHelper.ThrowIfNotOnUIThread();
      this.DisplayName = "Code Brackets".MplClass();
    }
  }
}