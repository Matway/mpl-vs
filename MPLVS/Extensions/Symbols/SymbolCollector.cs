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
using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.Symbols {
  internal class SymbolCollector {
    private readonly IWpfTextView View;

    public Classifier Classifier { get; }

    private readonly IClassificationType Label;

    public SymbolCollector(IWpfTextView textView) {
      this.Classifier = textView.TextBuffer.ObtainOrAttachTree().Classifier;
      this.Label      = MplClassifierProvider.Classifications[(int)NodeType.LABEL];
      this.View       = textView ?? throw new ArgumentNullException(nameof(textView));
    }

    public IEnumerable<ClassificationSpan> Symbols() {
      var snapshot      = View.TextBuffer.CurrentSnapshot;
      var wholeDocument = new SnapshotSpan(snapshot, Span.FromBounds(0, snapshot.Length));

      // FIXME: GC.
      return this.Classifier.GetClassificationSpans(wholeDocument).Where(a => a.ClassificationType.Equals(this.Label));
    }
  }

  internal struct OriginAndTree {
    public string File;
    public string Input;
    public Builder.Node Root;

    public static OriginAndTree FromTree(Tree tree) {
      ThreadHelper.ThrowIfNotOnUIThread();

      return new OriginAndTree {
        File  = tree.TextBuffer.GetFileName(),
        Input = tree.builder.source,
        Root  = tree.Root()
      };
    }

    public static OriginAndTree FromNode(Builder.Node node, string input, string file) {
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

    public static IEnumerable<Builder.Node> Labels(Builder.Node node) =>
      node.AsSequence().Where(NodeUtils.IsLabel);

    public static string Text(Builder.Node node, string source) {
      if (node is null)                 { throw new ArgumentNullException(nameof(node)); }
      if (string.IsNullOrEmpty(source)) { throw new ArgumentException($"'{nameof(source)}' cannot be null or empty.", nameof(source)); }

      return source.Substring(node.begin, node.Length());
    }
  }
}