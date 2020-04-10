using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace MPL.BraceMatching {
  internal sealed class BraceMatchingTagger : ITagger<ITextMarkerTag> {
    private readonly ITextView textView;
    private readonly Dictionary<char, char> bracePairs;
    private readonly Dictionary<char, string> braceKind;
    private readonly TextMarkerTag tag = new TextMarkerTag("MplBraceFound");
    private SnapshotPoint prevPoint;

    public BraceMatchingTagger(ITextView textView) {
      bracePairs = new Dictionary<char, char> {
        ['{'] = '}',
        ['['] = ']',
        ['('] = ')'
      };

      braceKind = new Dictionary<char, string> {
        ['{'] = "Object",
        ['}'] = "Object",
        ['['] = "Code",
        [']'] = "Code",
        ['('] = "List",
        [')'] = "List"
      };

      this.textView = textView;

      this.textView.Caret.PositionChanged += OnCaretPositionChanged;
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (spans[0].Snapshot != textView.TextBuffer.CurrentSnapshot) {
        yield break;
      }

      SnapshotPoint? caretPoint =
        textView.Caret.Position.Point.GetPoint(textView.TextBuffer, textView.Caret.Position.Affinity);

      if (!caretPoint.HasValue || caretPoint.Value.Snapshot.Length == 0) {
        yield break;
      }

      SnapshotPoint currPoint;

      if (caretPoint.Value.Position == caretPoint.Value.Snapshot.Length && caretPoint.Value.Position != 0) {
        currPoint = caretPoint.Value - 1;
      } else {
        currPoint = caretPoint.Value;
      }

      if (MplPackage.Options.SharpStyleBraceMatch) {
        prevPoint = caretPoint.Value.Position != 0 ? caretPoint.Value - 1 : caretPoint.Value;
      } else {
        prevPoint = currPoint;
      }

      char currentCharacter = currPoint.GetChar();
      char lastCharacter = prevPoint.GetChar();
      SnapshotPoint matchedPoint;

      if (bracePairs.ContainsKey(currentCharacter)) {
        if (FindCloseChar(currPoint, braceKind[currentCharacter], out matchedPoint)) {
          yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currPoint, 1), tag);
          yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(matchedPoint, 1), tag);
        }
      } else if (bracePairs.ContainsValue(lastCharacter)) {
        if (FindOpenChar(prevPoint, braceKind[lastCharacter], out matchedPoint)) {
          yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(matchedPoint, 1), tag);
          yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(prevPoint, 1), tag);
        }
      }
    }

    private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e) {
      TagsChanged?.Invoke(this,
        new SnapshotSpanEventArgs(new SnapshotSpan(textView.TextBuffer.CurrentSnapshot, 0,
          textView.TextBuffer.CurrentSnapshot.Length)));
    }

    private bool FindCloseChar(SnapshotPoint start, string kind, out SnapshotPoint end) {
      AST.TreeBuilder.Node root = AST.AST.GetASTRoot();
      int startPos = start.Position;
      int endPos = startPos;
      bool haveFound = false;

      void Traverse(AST.TreeBuilder.Node node) {
        if (node.children == null) {
          return;
        }

        if (node.name == kind && node.begin == startPos && node.children.Exists(x => x.name == "']'" || x.name == "'}'" || x.name == "')'")) {
          endPos = node.end - 1;
          haveFound = true;
        }

        foreach (var child in node.children) {
          if (!haveFound && child.begin <= startPos && child.end > startPos) {
            Traverse(child);
          }
        }
      }

      Traverse(root);

      end = new SnapshotPoint(start.Snapshot, endPos);
      return haveFound;
    }

    private bool FindOpenChar(SnapshotPoint end, string kind, out SnapshotPoint start) {
      AST.TreeBuilder.Node root = AST.AST.GetASTRoot();
      int endPos = end.Position;
      int startPos = endPos;
      bool haveFound = false;

      void Traverse(AST.TreeBuilder.Node node) {
        if (node.children == null) {
          return;
        }

        if (node.name == kind && node.end - 1 == endPos) {
          startPos = node.begin;
          haveFound = true;
        }

        foreach (var child in node.children) {
          if (!haveFound) {
            Traverse(child);
          }
        }
      }

      Traverse(root);

      start = new SnapshotPoint(end.Snapshot, startPos);
      return haveFound;
    }
  }
}