using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Classification;
using MPLVS.ParseTree;

namespace MPLVS.Core {
  public static class Utils {
    /// <summary>Alias to <c>(T)owner.Properties.GetProperty(typeof(T))</c>.</summary>
    /// <remarks>Do not use it in a cycle due to performance issue.</remarks>
    public static T ObtainProperty<T>(this IPropertyOwner owner) {
      if (owner is null) {
        throw new ArgumentNullException(nameof(owner));
      }

      if (owner.Properties.TryGetProperty<T>(typeof(T), out var result)) {
        return result;
      }

      throw new InvalidOperationException("The specified type could not be found inside the property bag");
    }

    /// <summary>Alias to <c>buffer.Properties.GetOrCreateSingletonProperty(() => new Tree(buffer))</c>.</summary>
    /// <remarks>Do not use it in a cycle due to performance issue.</remarks>
    public static Tree ObtainOrAttachTree(this ITextBuffer buffer) {
      if (buffer is null) {
        throw new ArgumentNullException(nameof(buffer));
      }

      return buffer.ObtainOrAttachProperty(() => new Tree(buffer));
    }

    /// <summary>Alias to <c>owner.Properties.GetOrCreateSingletonProperty&lt;T&gt;(creator)</c>.</summary>
    /// <remarks>Do not use it in a cycle due to performance issue.</remarks>
    public static T ObtainOrAttachProperty<T>(this IPropertyOwner owner, Func<T> creator) where T : class {
      if (owner is null) { throw new ArgumentNullException(nameof(owner)); }
      if (creator is null) { throw new ArgumentNullException(nameof(creator)); }

      return owner.Properties.GetOrCreateSingletonProperty(creator);
    }
  }

  public class Tree {
    internal readonly ITextBuffer TextBuffer;
    internal readonly Builder builder = new Builder();
    private Builder.Node root;

    private bool parsed;

    public bool Parsed { get => this.parsed; private set => this.parsed = value; }
    public string Text { get => this.builder.source; }
    internal Classifier Classifier { get; }

    internal Tree(ITextBuffer buffer) {
      this.TextBuffer = buffer ?? throw new System.ArgumentNullException(nameof(buffer));

      this.TextBuffer.Changed += this.ReParse;

      this.Classifier = new Classifier(TextBuffer, builder.parser);

      ReParse(null, null);
    }

    private void ReParse(object sender, TextContentChangedEventArgs info) {
      var src = TextBuffer.CurrentSnapshot.GetText();
      root    = builder.GetRoot(src, out parsed);
    }

    public Builder.Node Root() => root;

    public Stack<Parser.SyntaxError> GetErrors() => builder.errors;
  }
}