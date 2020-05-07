using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace MPL.Classification {
  internal enum NodeType {
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

  internal class ParserResults {
    public struct Result {
      public Span span;
      public NodeType type;
    }

    public ParserResults(int offset) {
      this.offset = offset;
      results = new List<Result>();
    }

    public int offset;
    public List<Result> results;
    private bool labelDefenition, labelResetDefenition;
    private int labelStartIndex, labelResetIndex;

    public void Parse(MPL.ParseTree.Builder.Node node) {
      if (node.children == null) {
        terminal(node.name, node.begin, node.end);
        return;
      } else {
        startNonterminal(node.name);
        foreach (var child in node.children) {
          Parse(child);
        }
        endNonterminal(node.name);
      }
    }

    public void reset(string s) {
      results.Clear();
    }

    public void startNonterminal(string name) {
      if (name == "Label") {
        labelDefenition = true;
      }
      if (name == "LabelReset") {
        labelResetDefenition = true;
      }
    }

    public void endNonterminal(string name) {
      if (name == "Label") {
        labelDefenition = false;
      }

      if (name == "LabelReset") {
        labelResetDefenition = false;
      }
    }

    public void terminal(string name, int begin, int end) {
      begin += offset;
      end += offset;

      if (labelDefenition) {
        if (name == "Name") {
          labelStartIndex = begin;
        } else if (name == "':'") {
          results.Add(new Result { span = new Span(labelStartIndex, begin - labelStartIndex), type = NodeType.LABEL });
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.MPLCONTENT });
          labelDefenition = false;
        }
      } else if (labelResetDefenition) {
        if (name == "Name") {
          labelResetIndex = begin;
        } else if (name == "':!'") {
          results.Add(new Result { span = new Span(labelResetIndex, begin - labelStartIndex), type = NodeType.LABEL });
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.MPLCONTENT });
          labelResetDefenition = false;
        }
      } else {
        switch (name) {
        case "Comment":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.COMMENT });
          break;
        case "Number":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.CONSTANT });
          break;
        case "Real":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.CONSTANT });
          break;
        case "String":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.TEXT });
          break;
        case "'{'":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.OBJECT });
          break;
        case "'}'":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.OBJECT });
          break;
        case "'('":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.LIST });
          break;
        case "')'":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.LIST });
          break;
        case "'['":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.CODEBRACKETS });
          break;
        case "']'":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.CODEBRACKETS });
          break;
        case "Builtin":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.BUILTIN });
          break;
        case "Name":
        case "NameReadMember":
        case "NameWriteMember":
        case "NameMember":
        case "NameRead":
        case "NameWrite":
        case "SomeError":
        case "':!'":
        case "';'":
        case "':'":
        case "','":
          results.Add(new Result { span = new Span(begin, end - begin), type = NodeType.MPLCONTENT });
          break;
        }
      }
    }

    public void whitespace(int begin, int end) {
      throw new NotImplementedException();
    }
  }

  #region Classifier

  /// <summary>
  /// Classifier that classifies all text as an instance of the OrdinaryClassifierType
  /// </summary>
  internal class MplClassifier : ParserResults, IClassifier {
    private readonly Dictionary<NodeType, IClassificationType> classificationTypes;
    static readonly IVsOutputWindowPane generalPane;

    public static T getPropertyFromBuffer<T>(ITextBuffer buffer) {
      foreach (var item in buffer.Properties.PropertyList) {
        if (item.Value is T) {
          return (T)item.Value;
        }
      }

      throw new InvalidOperationException("The specified type could not be found inside the property bag");
    }

    public void textChanged(object o, TextContentChangedEventArgs e) {
      ThreadHelper.ThrowIfNotOnUIThread();
      ParseTree.Tree.Parse(e.After.GetText(), out bool parsed);
      results.Clear();
      var root = ParseTree.Tree.Root();
      Parse(root);
      //results.Add(new Result { span = new Span(0, e.After.Length - 1), type = NodeType.MPLCONTENT });
      var errorStack = ParseTree.Tree.GetErrorStack();
      generalPane.Clear();
      if (!parsed) {
        try {
          foreach (var error in errorStack) {
            var document = getPropertyFromBuffer<ITextDocument>(e.Before.TextBuffer);
            string message = document.FilePath + error.getMessage() + '\n';
            generalPane.OutputString(message);
            generalPane.Activate();
          }
        } catch (InvalidOperationException er) {
          generalPane.OutputString(er.Message);
          generalPane.Activate();
        }
      }

      ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(new SnapshotSpan(e.After, 0, e.After.Length)));
    }

    static MplClassifier() {
      ThreadHelper.ThrowIfNotOnUIThread();
      IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

      Guid generalPaneGuid = VSConstants.GUID_OutWindowGeneralPane;
      if (outWindow != null) {
        outWindow.GetPane(ref generalPaneGuid, out generalPane);

        if (generalPane == null) {
          outWindow.CreatePane(generalPaneGuid, "Output", 1, 1);
          outWindow.GetPane(ref generalPaneGuid, out generalPane);
          Debug.Assert(generalPane != null);
        }
      }
    }

    internal MplClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer) : base(0) {
      ThreadHelper.ThrowIfNotOnUIThread();
      ParseTree.Tree.Parse(buffer.CurrentSnapshot.GetText(), out bool parsed);

      classificationTypes = new Dictionary<NodeType, IClassificationType> {
        [NodeType.MPLCONTENT] = registry.GetClassificationType("MplContent"),
        [NodeType.BUILTIN] = registry.GetClassificationType("MplBuiltin"),
        [NodeType.COMMENT] = registry.GetClassificationType("MplComment"),
        [NodeType.LABEL] = registry.GetClassificationType("MplLabel"),
        [NodeType.CONSTANT] = registry.GetClassificationType("MplConstant"),
        [NodeType.LIST] = registry.GetClassificationType("MplList"),
        [NodeType.OBJECT] = registry.GetClassificationType("MplObject"),
        [NodeType.TEXT] = registry.GetClassificationType("MplText"),
        [NodeType.CODEBRACKETS] = registry.GetClassificationType("MplCodeBrackets")
      };

      var root = ParseTree.Tree.Root();
      Parse(root);
      //results.Add(new Result{span = new Span(0, buffer.CurrentSnapshot.Length - 1), type = NodeType.MPLCONTENT});
      var errorStack = ParseTree.Tree.GetErrorStack();
      if (!parsed) {
        try {
          generalPane.Clear();
          foreach (var error in errorStack) {
            var document = getPropertyFromBuffer<ITextDocument>(buffer);
            string message = document.FilePath + error.getMessage() + '\n';
            generalPane.OutputString(message);
            generalPane.Activate();
          }
        } catch (InvalidOperationException er) {
          generalPane.OutputString(er.Message);
          generalPane.Activate();
        }
      }

      buffer.Changed += textChanged;
    }

    /// <summary>
    /// This method scans the given SnapshotSpan for potential matches for this classification.
    /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
    /// </summary>
    /// <param name="span"></param>
    /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span) {
      List<ClassificationSpan> classifications = new List<ClassificationSpan>();

      foreach (var element in results) {
        var intersection = span.Span.Intersection(element.span);
        if (intersection.HasValue) {
          SnapshotSpan newSnapshot = new SnapshotSpan(span.Snapshot, intersection.Value);
          classifications.Add(new ClassificationSpan(newSnapshot, classificationTypes[element.type]));
        }
      }

      return classifications;
    }

#pragma warning disable 67
    // This event gets raised if a non-text change would affect the classification in some way,
    // for example typing /* would cause the classification to change in C# without directly
    // affecting the span.
    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67
  }

  #endregion //Classifier

}