using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    public Span span;
    public NodeType type;
  }

  internal class Classifier : IClassifier {
    internal Classifier(ITextBuffer buffer, Parser parser) {
      this.TextBuffer = buffer;
      this.results = new List<Result>();
      this.parser = parser;

      this.parser.Reset         += this.Reset;
      this.parser.StartCompound += this.StartCompound;
      this.parser.Terminal      += this.Terminal;
      this.parser.Compleate     += this.Compleate;
    }

    private void Compleate(object sender, EventArgs e) {
      var span = new SnapshotSpan(TextBuffer.CurrentSnapshot, 0, TextBuffer.CurrentSnapshot.Length);
      ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(span));
    }

    public ITextBuffer TextBuffer { get; }

    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

    private readonly List<Result> results;
    private readonly Parser parser;
    private bool isLabel;

    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span) {
      var classifications = new List<ClassificationSpan>();

      foreach (var element in results) {
        var intersection = span.Span.Intersection(element.span);
        if (intersection.HasValue) {
          var newSnapshot = new SnapshotSpan(span.Snapshot, intersection.Value);
          var item = new ClassificationSpan(newSnapshot, MplClassifierProvider.Classifications[(int)element.type]);
          classifications.Add(item);
        }
      }

      return classifications;
    }

    public void Reset(object o, EventArgs e) {
      this.isLabel = false;
      this.results.Clear();
    }

    public void StartCompound(object o, (string name, int begin, int line, int column) startInfo) {
      if (startInfo.name == "Label") {
        this.isLabel = true;
      }
    }

    public void Terminal(object o, (string name, int begin, int end, int line, int column) terminalInfo) {
      switch (terminalInfo.name) {
        case "Name": {
          if (isLabel) {
            Add(terminalInfo.begin, terminalInfo.end, NodeType.LABEL);
            isLabel = false;
            break;
          }

          var name = TextBuffer.CurrentSnapshot.GetText(terminalInfo.begin, terminalInfo.end - terminalInfo.begin); // FIXME: GC.
          Add(terminalInfo.begin, terminalInfo.end, Constants.Builtins.Contains(name) ? NodeType.BUILTIN : NodeType.MPLCONTENT);
          break;
        }

        case "SomeError":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.MPLCONTENT);
          break;

        case "Comment":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.COMMENT);
          break;

        case "String":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.TEXT);
          break;

        case "Number":
        case "Real":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.CONSTANT);
          break;

        case "'{'":
        case "'}'":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.OBJECT);
          break;

        case "'['":
        case "']'":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.CODEBRACKETS);
          break;

        case "'('":
        case "')'":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.LIST);
          break;

        case "'.'":
        case "','":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.MPLCONTENT);
          break;

        case "':'":
        case "';'":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.MPLCONTENT);
          break;

        case "NameReadMember":
        case "NameWriteMember":
        case "NameMember":
        case "NameRead":
        case "NameWrite":
          Add(terminalInfo.begin, terminalInfo.end, NodeType.MPLCONTENT);
          break;

        case "LF":
        case "CR":
        case "CRLF":
        case "EOF":
          break;

        default:
          Debug.Fail("Unknown symbol: " + terminalInfo.name);
          break;
      }
    }

    private void Add(int begin, int end, NodeType type) {
      var point = new SnapshotPoint(TextBuffer.CurrentSnapshot, begin);
      var span  = new SnapshotSpan(point, end - begin);
      results.Add(new Result { type = type, span = span });
    }
  }
}