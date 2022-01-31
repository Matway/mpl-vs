using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.SymbolHighlighting {
  internal sealed class Tagger : ITagger<Tag> {
    private readonly ITextView View;
    private Builder.Node Last;
    private string LastName = string.Empty;
    private List<Name> Names = new List<Name>();
    private int Version = -1;

    private static readonly Tag Tag = new Tag("MplCurrentSymbol", "MplCurrentSymbol");

    private struct Name { public string Text; public Span Location; }

    public Tagger(ITextView view) {
      this.View = view;

      this.View.Caret.PositionChanged += OnCaretPositionChanged;

      // FIXME: Workaround for vs view's taggers.
      //this.View.TextBuffer.Changed    += OnBufferChanged;
      this.View.TextBuffer.ChangedHighPriority += this.OnBufferChangedHighPriority;

      this.OnBufferChanged(null, null);
    }

    private void OnBufferChangedHighPriority(object sender, TextContentChangedEventArgs e) => this.OnBufferChanged(sender, e);

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    public IEnumerable<ITagSpan<Tag>> GetTags(NormalizedSnapshotSpanCollection spans) {
      if (string.IsNullOrEmpty(this.LastName)) { return Array.Empty<ITagSpan<Tag>>(); }

      // FIXME: Lambda 'a => Tags(a.Span)' will capture 'this'.
      return spans.SelectMany(a => this.Tags(a.Span));
    }

    private IEnumerable<ITagSpan<Tag>> Tags(Span span) {
      var text = this.View.TextBuffer.CurrentSnapshot;
      return this.AllTags().Where(a => a.OverlapsWith(span)).Select(b => {
        var snapshot = new SnapshotSpan(text, b.Start, b.End - b.Start);
        return new TagSpan<Tag>(snapshot, Tag);
      });
    }

    private IEnumerable<Span> AllTags() {
      if (this.Last is null) {
        return Array.Empty<Span>();
      }

      var name = this.LastName; // NOTE: This variable will be captured by the lambda.
      var tags = this.Names.Where(a => a.Text == name).Select(a => a.Location);

      return tags.Skip(1).Any() ? tags : Array.Empty<Span>();
    }

    private void OnBufferChanged(object sender, TextContentChangedEventArgs info) {
      if (this.Version == this.View.TextBuffer.CurrentSnapshot.Version.VersionNumber) { return; }

      this.Names = this.View.TextBuffer.ObtainOrAttachTree().Root()
                                       .AsSequence()
                                       .Where(a => a.IsLooksLikeName())
                                       .Select(a => a.NameSpan())
                                       .Select(a => new Name { Text = this.View.TextBuffer.CurrentSnapshot.GetText(a), Location = a })
                                       .ToList();

      this.Version = this.View.TextBuffer.CurrentSnapshot.Version.VersionNumber;

      this.OnCaretPositionChanged(null, null);
    }

    private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs info) {
      var point = this.View.Caret.Position;
      var caret = point.Point.GetPoint(this.View.TextBuffer.CurrentSnapshot, point.Affinity) ?? point.BufferPosition;

      var symbol = this.View.TextBuffer.ObtainOrAttachTree().Root()
                                       .Surroundings(caret, Strategy.NearLeftFarRight).AsSequence()
                                       .FirstOrDefault(a => a is object && a.begin <= caret && caret <= a.end && a.IsLooksLikeName());

      if (ReferenceEquals(this.Last, symbol)) { return; }

      this.LastName = symbol is null ? string.Empty : this.View.TextBuffer.CurrentSnapshot.GetText(symbol.NameSpan());

      this.Notify(symbol);
    }

    private void Notify(Builder.Node node) {
      this.Last = node;

      var snapshot = new SnapshotSpan(View.TextBuffer.CurrentSnapshot, 0, View.TextBuffer.CurrentSnapshot.Length);
      TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(snapshot));
    }
  }
}