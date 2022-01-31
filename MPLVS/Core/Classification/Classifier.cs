using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace MPLVS.Classification {
  public enum NodeType {
    MPLCONTENT,
    BUILTIN,
    LABEL,
    COMMENT,
    CONSTANT,
    OBJECT,
    LIST,
    TEXT,
    CODEBRACKETS
  }

  public struct Result {
    public Span Span;
    public NodeType Type;
  }

  internal class Classifier : IClassifier {
    internal Classifier(ITextBuffer buffer, Parser parser) {
      this.TextBuffer = buffer;
      this.Results    = new List<Result>();
      this.Parser     = parser;

      this.Parser.Reset         += this.Reset;
      this.Parser.StartCompound += this.StartCompound;
      this.Parser.Terminal      += this.Terminal;
      this.Parser.Compleate     += this.Compleate;
    }

    private void Compleate(object sender, EventArgs e) {
      var span = new SnapshotSpan(this.TextBuffer.CurrentSnapshot, 0, this.TextBuffer.CurrentSnapshot.Length);
      ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(span));
    }

    public ITextBuffer TextBuffer { get; }

    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

    private readonly List<Result> Results;
    private readonly Parser Parser;
    private bool IsLabel;

    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span) {
      var before = this.Results.BinarySearch(new Result { Span = span.Span }, Intersect);
      var after  = ~before;

      if (before < 0 && after == this.Results.Count) { return Array.Empty<ClassificationSpan>(); }

      return this.Results.From(before >= 0 ? before : after).TakeWhile(a => a.Span.Start <= span.End).Select(a => {
        var intersection = span.Intersection(a.Span).Value;
        var snapshot     = new SnapshotSpan(span.Snapshot, intersection);
        return new ClassificationSpan(snapshot, MplClassifierProvider.Classifications[(int)a.Type]);
      }).ToList();
    }

    public void Reset(object o, EventArgs e) {
      this.IsLabel = false;
      this.Results.Clear();
    }

    public void StartCompound(object o, Parser.CompoundStart startInfo) {
      if (startInfo.Name == "Label") {
        this.IsLabel = true;
      }
    }

    public void Terminal(object o, Parser.TerminalStart terminalInfo) {
      switch (terminalInfo.Name) {
        case "Name": {
          if (IsLabel) {
            Add(terminalInfo.Begin, terminalInfo.End, NodeType.LABEL);
            IsLabel = false;
            break;
          }

          var name = TextBuffer.CurrentSnapshot.GetText(terminalInfo.Begin, terminalInfo.End - terminalInfo.Begin); // FIXME: GC.
          Add(terminalInfo.Begin, terminalInfo.End, Constants.Builtins.Contains(name) ? NodeType.BUILTIN : NodeType.MPLCONTENT);
          break;
        }

        case "SomeError":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.MPLCONTENT);
          break;

        case "Comment":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.COMMENT);
          break;

        case "String":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.TEXT);
          break;

        case "Number":
        case "Real":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.CONSTANT);
          break;

        case "'{'":
        case "'}'":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.OBJECT);
          break;

        case "'['":
        case "']'":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.CODEBRACKETS);
          break;

        case "'('":
        case "')'":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.LIST);
          break;

        case "'.'":
        case "','":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.MPLCONTENT);
          break;

        case "':'":
        case "';'":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.MPLCONTENT);
          break;

        case "NameReadMember":
        case "NameWriteMember":
        case "NameMember":
        case "NameRead":
        case "NameWrite":
          Add(terminalInfo.Begin, terminalInfo.End, NodeType.MPLCONTENT);
          break;

        case "LF":
        case "CR":
        case "CRLF":
        case "EOF":
          break;

        default:
          Debug.Fail("Unknown symbol: " + terminalInfo.Name);
          break;
      }
    }

    private void Add(int begin, int end, NodeType type) {
      var point = new SnapshotPoint(TextBuffer.CurrentSnapshot, begin);
      var span  = new SnapshotSpan(point, end - begin);
      Results.Add(new Result { Type = type, Span = span });
    }

    private static readonly IComparer<Result> Intersect =
      Collections.AsComparer((Result x, Result y) =>
        x.Span.IntersectsWith(new Span(y.Span.Start, 0)) ? 0 : x.Span.Start < y.Span.Start ? -1 : 1
      );
  }

  public static class Collections {
    public static IEnumerable<T> From<T>(this IList<T> results, int from) {
      if (results is null) { throw new ArgumentNullException(nameof(results)); }

      var cout = results.Count;

      if (from < 0 || cout <= from) { throw new ArgumentOutOfRangeException(); }

      while (from < cout) {
        yield return results[from++];
      }
    }

    public static IComparer<T> Comparer<T>() { return null; }

    public class FromFuncComparer<T> : IComparer<T> {
      public int Compare(T x, T y) => this.Implementaion(x, y);

      public FromFuncComparer(Func<T, T, int> a) => this.Implementaion = a;

      private readonly Func<T, T, int> Implementaion;
    }

    public static IComparer<T> AsComparer<T>(Func<T, T, int> a) => new FromFuncComparer<T>(a);
  }

  public static class LambdaComparer {
  }
}