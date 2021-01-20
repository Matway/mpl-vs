using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Classification {
  internal static class MplClassificationTypes {
    [Export]
    [Name("MplContent")]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition MplContentType = null;

    [Export]
    [Name("MplBuiltin")]
    [BaseDefinition("MplContent")]
    internal static ClassificationTypeDefinition BuiltinType = null;

    [Export]
    [Name("MplComment")]
    [BaseDefinition("MplContent")]
    internal static ClassificationTypeDefinition CommentType = null;

    [Export]
    [Name("MplLabel")]
    [BaseDefinition("MplContent")]
    internal static ClassificationTypeDefinition LabelType = null;

    [Export]
    [Name("MplConstant")]
    [BaseDefinition("MplContent")]
    internal static ClassificationTypeDefinition ConstantType = null;

    [Export]
    [Name("MplObject")]
    [BaseDefinition("MplContent")]
    internal static ClassificationTypeDefinition ObjectType = null;

    [Export]
    [Name("MplList")]
    [BaseDefinition("MplContent")]
    internal static ClassificationTypeDefinition ListType = null;

    [Export]
    [Name("MplText")]
    [BaseDefinition("MplContent")]
    internal static ClassificationTypeDefinition TextType = null;

    [Export]
    [Name("MplCodeBrackets")]
    [BaseDefinition("MplContent")]
    internal static ClassificationTypeDefinition CodeBracketsType = null;
  }
}