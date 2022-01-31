using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Classification;
using MPLVS.ParseTree;

namespace MPLVS {
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
    private int Version = -1;

    private bool parsed;

    public bool Parsed { get => this.parsed; private set => this.parsed = value; }
    public string Text { get => this.builder.source; }
    internal Classifier Classifier { get; }

    internal Tree(ITextBuffer buffer) {
      this.TextBuffer = buffer ?? throw new System.ArgumentNullException(nameof(buffer));
      this.Classifier = new Classifier(TextBuffer, builder.parser);

      ReParse(null, null);
    }

    public void ReParse(object sender, TextContentChangedEventArgs info) {
      if (!this.Outdated()) { return; }

      this.root    = this.builder.GetRoot(this.TextBuffer.CurrentSnapshot.GetText(), out this.parsed);
      this.Version = this.TextBuffer.CurrentSnapshot.Version.VersionNumber;
    }

    public Builder.Node Root() {
      if (this.Outdated()) { this.ReParse(null, null); }
      return this.root;
    }

    internal bool Outdated() => this.Version != this.TextBuffer.CurrentSnapshot.Version.VersionNumber;

    public Stack<Parser.SyntaxError> GetErrors() => builder.errors;
  }
}