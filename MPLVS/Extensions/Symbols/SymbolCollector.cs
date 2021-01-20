using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

using MPLVS.Classification;
using MPLVS.Core;
using MPLVS.Core.ParseTree;
using MPLVS.Extensions;

namespace MPLVS.Symbols {
  internal class SymbolCollector {
    private readonly IWpfTextView textView;

    public Classifier classifier { get; }

    private readonly IClassificationType label;

    public SymbolCollector(IWpfTextView textView) {
      this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
      this.classifier = textView.TextBuffer.ObtainOrAttachTree().Classifier;
      this.label = MplClassifierProvider.Classifications[(int)NodeType.LABEL];
    }

    public IEnumerable<ClassificationSpan> Symbols() {
      var snapshot = textView.TextBuffer.CurrentSnapshot;
      var wholeDocument = new SnapshotSpan(snapshot, Span.FromBounds(0, snapshot.Length));

      // FIXME: GC.
      return this.classifier.GetClassificationSpans(wholeDocument).Where(a => a.ClassificationType.Equals(this.label));
    }
  }

  internal struct OriginAndTree {
    public string File;
    public string Input;
    public ParseTree.Builder.Node Root;

    public static OriginAndTree FromTree(Tree tree) {
      ThreadHelper.ThrowIfNotOnUIThread();

      return new OriginAndTree {
        File  = tree.TextBuffer.GetFileName(),
        Input = tree.builder.source,
        Root  = tree.Root()
      };
    }

    public static OriginAndTree FromNode(ParseTree.Builder.Node node, string input, string file) {
      return new OriginAndTree {
        File  = file,
        Input = input,
        Root  = node
      };
    }
  }

  internal static class Files {
    public static IEnumerable<OriginAndTree> TreesFromAWholeProject(ITextBuffer root, IEnumerable<string> excepts = null) =>
      TreesFromAWholeProject(root.GetFileName(), excepts);

    public static IEnumerable<OriginAndTree> TreesFromAWholeProject(string root, IEnumerable<string> excepts = null) {
      ThreadHelper.ThrowIfNotOnUIThread();

      var files =
        MplPackage.Instance
          .GetLoadedProjects()
          .Where(a => a.HasFile(root)) // FIXME: There is only one current project, so use it.
          .SelectMany(a => a.GetProjectItems())
          .Where(IsRootedReferenceToMplFile)
          .Union(new List<string>())
          .OrderBy(a => a);

      var exclude = excepts is null ? new List<string>() : excepts.ToList();
      var requstedFiles =
        (exclude.Any() ? files.Where(a => !exclude.Contains(a))
                       : files).ToList();

      var openBuffers   = OpenedBuffers.TextBuffers;
      var openFiles     = openBuffers.Select(a => a.GetFileName());
      var uptudateFiles = requstedFiles.Intersect(openFiles).ToList();
      var outdatedFiles = requstedFiles.Except(uptudateFiles);

      var newParsers =
        outdatedFiles
          .AsParallel()
          .Select(file => {
            try {
              // FIXME: Use some kind of file\symbol-cache and not re-read all the files from the disk.
              using (var reader = new StreamReader(file, Encoding.UTF8)) {
                var text   = reader.ReadToEnd();
                var parser = new ParseTree.Builder();

                var node = parser.GetRoot(text, out var _);

                // TODO: File path must be relative to the root.
                return OriginAndTree.FromNode(node, parser.source, file);
              }
            }
            catch {
              return new OriginAndTree() { File = file };
            }
          });

      var oldParsers =
        openBuffers.Select(a => OriginAndTree.FromTree(a.ObtainOrAttachTree()))
                   .Where(a => uptudateFiles.Contains(a.File))
                   .ToList();

      return newParsers.Concat(oldParsers.AsParallel()).OrderBy(a => a.File);
    }

    private static bool IsRootedReferenceToMplFile(string path) {
      try {
        return
          Path.IsPathRooted(path)
          // TODO: What about other extensions like .fast .easy and so on?
          // FIXME: GC.
          && Path.GetExtension(path).Equals(".mpl", StringComparison.InvariantCultureIgnoreCase);
      }
      catch {
        return false;
      }
    }

    public static IEnumerable<ParseTree.Builder.Node> Labels(ParseTree.Builder.Node node) =>
      Flatten(node).Where(a => a.IsLabel());

    public static IEnumerable<ParseTree.Builder.Node> Flatten(ParseTree.Builder.Node node) {
      IEnumerable<ParseTree.Builder.Node> empty = Array.Empty<ParseTree.Builder.Node>();

      if (node is null) { return empty; }

      var payload = empty.Append(node);

      return
        node.children is object
        ? payload.Concat(node.children.SelectMany(a => Flatten(a)))
        : payload;
    }

    public static string Text(ParseTree.Builder.Node node, string source) {
      if (node is null)                 { throw new ArgumentNullException(nameof(node)); }
      if (string.IsNullOrEmpty(source)) { throw new ArgumentException($"'{nameof(source)}' cannot be null or empty.", nameof(source)); }

      return source.Substring(node.begin, node.end - node.begin);
    }
  }
}