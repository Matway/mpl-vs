using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;

namespace MPL.Classification {

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplContent")]
  [Name("MplContent")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MplContentClassificationFormat : ClassificationFormatDefinition {
    public MplContentClassificationFormat() {
      ThreadHelper.ThrowIfNotOnUIThread();
      this.DisplayName = "MPL - Plain text";
      if (MplPackage.Options.SolarizedTheme) {
        this.ForegroundColor = MplPackage.MplContentColor;
      }
    }
  }

  #endregion

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplBuiltin")]
  [Name("MplBuiltin")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class MplBuiltinClassificationFormat : ClassificationFormatDefinition {
    public MplBuiltinClassificationFormat() {
      this.DisplayName = "MPL - Builtin functions";
      this.ForegroundColor = Color.FromRgb(108, 113, 196); // violet
    }
  }

  #endregion

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplComment")]
  [Name("CommentClassificationFormat")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class CommentClassificationFormat : ClassificationFormatDefinition {
    public CommentClassificationFormat() {
      this.DisplayName = "MPL - Comments";
      this.ForegroundColor = Color.FromRgb(211, 54, 130); //magenta(purple)
    }
  }

  #endregion

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplLabel")]
  [Name("LabelClassificationFormat")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class LabelClassificationFormat : ClassificationFormatDefinition {
    public LabelClassificationFormat() {
      this.DisplayName = "MPL - Labels";
      this.ForegroundColor = Color.FromRgb(38, 139, 210); //blue
    }
  }

  #endregion

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplConstant")]
  [Name("ConstantClassificationFormat")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class ConstantClassificationFormat : ClassificationFormatDefinition {
    public ConstantClassificationFormat() {
      this.DisplayName = "MPL - Constants";
      this.ForegroundColor = Color.FromRgb(42, 161, 152); //cyan
    }
  }

  #endregion

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplObject")]
  [Name("ObjectClassificationFormat")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class ObjectClassificationFormat : ClassificationFormatDefinition {
    public ObjectClassificationFormat() {
      this.DisplayName = "MPL - Objects";
      this.ForegroundColor = Color.FromRgb(181, 137, 0); //yellow
    }
  }

  #endregion

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplList")]
  [Name("ListClassificationFormat")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class ListClassificationFormat : ClassificationFormatDefinition {
    public ListClassificationFormat() {
      this.DisplayName = "MPL - Lists";
      this.ForegroundColor = Color.FromRgb(133, 153, 0); //green

    }
  }

  #endregion

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplText")]
  [Name("TextClassificationFormat")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class TextClassificationFormat : ClassificationFormatDefinition {
    public TextClassificationFormat() {
      this.DisplayName = "MPL - Strings";
      this.ForegroundColor = Color.FromRgb(203, 75, 22); //orange
    }
  }

  #endregion

  #region Format definition

  [Export(typeof(EditorFormatDefinition))]
  [ClassificationType(ClassificationTypeNames = "MplCodeBrackets")]
  [Name("CodeBracketsClassificationFormat")]
  [UserVisible(true)]
  [Order(Before = Priority.Default)]
  internal sealed class CodeBracketsClassificationFormat : ClassificationFormatDefinition {
    public CodeBracketsClassificationFormat() {
      ThreadHelper.ThrowIfNotOnUIThread();
      this.DisplayName = "MPL - Code Brackets";
      if (MplPackage.Options.SolarizedTheme) {
        this.ForegroundColor = MplPackage.MplEmphasizedColor;
      }
    }
  }

  #endregion

}