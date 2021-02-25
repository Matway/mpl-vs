using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using MPLVS.Core;
using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.Commands {
  class GoToFile : VSStd2KCommand {
    public GoToFile(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override bool Run(VSConstants.VSStd2KCmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();

      var caret  = this.TextView.Caret.Position.BufferPosition.Position;
      var symbol = this.TextView.TextBuffer.ObtainOrAttachTree().Root().YongestAncestor(caret);

      if (symbol is null || symbol.name != "String") { return true; }

      var file = FileFromMplString(symbol, this.TextView.TextSnapshot);

      switch (file.Kind) {
        case FileType.Invalid: break;
        case FileType.MplModule: OpenMplModule(file); break;
        case FileType.Regular: Files.GetTextViewForDocument(file.Filename, -1, -1); break;
      }

      return true;
    }

    private void OpenMplModule(FileInfo item) {
      ThreadHelper.ThrowIfNotOnUIThread();

      var mplFile = item.Filename + ".mpl";
      var curentFile = this.TextView.TextBuffer.GetFileName();
      foreach (var project in MplPackage.Instance.GetLoadedProjects().Where(a => a.HasFile(curentFile))) {
        var file = project.GetProjectItems()
                          .Where(a => {
                            try { return string.Equals(Path.GetFileName(a), mplFile, StringComparison.InvariantCultureIgnoreCase); }
                            catch { return false; }
                          })
                          .FirstOrDefault();

        if (file is object) {
          _ = Files.GetTextViewForDocument(file, -1, -1);
          return;
        }
      }
    }

    private static FileInfo FileFromMplString(Builder.Node node, ITextSnapshot snapshot) {
      if (node is null) { throw new ArgumentNullException(nameof(node)); }
      if (snapshot is null) { throw new ArgumentNullException(nameof(snapshot)); }

      try {
        var text   = TranslateMplString(node, snapshot);
        var module = TrimQualifiedImportedName(text);
        var file   = Path.GetFileName(module);

        return
          module == string.Empty
          ? default
          : file is object
            ? file.Length == module.Length
              ? new FileInfo { Kind = FileType.MplModule, Filename = module }
              : new FileInfo { Kind = FileType.Regular, Filename = text }
            : SelecFile(Path.GetFileName(text), text);

        FileInfo SelecFile(string @new, string old) => new FileInfo {
          Kind     = @new.Length == old.Length ? FileType.MplModule : FileType.Regular,
          Filename = @old
        };
      }
      catch { }

      return default;
    }

    private static string TrimQualifiedImportedName(string text) {
      var count = 0;
      foreach (var ch in text.Reverse()) {
        ++count;

        if (!IsMplLetter(ch)) {
          return
            ch == '.'
            ? text.Substring(0, text.Length - count)
            : null;
        }
      }

      return text;
    }

    private static bool IsMplLetter(char ch) => Parser.IsDigit(ch) || Parser.IsMplLetter(ch);

    internal static string TranslateMplString(Builder.Node node, ITextSnapshot input) {
      if (node is null) { throw new ArgumentNullException(nameof(node)); }
      if (input is null) { throw new ArgumentNullException(nameof(input)); }

      if (node.name != "String") { return null; }

      var begin  = node.begin;
      var length = node.end - begin;

      if (length < 3) {
        var lastCharacter = input[length - 1];

        switch (lastCharacter) {
          case '"':  return string.Empty;
          case '\\': return string.Empty;

          default: return lastCharacter.ToString();
        }
      }

      // FIXME: Translate mpl-escape-sequences.
      // FIXME: The last character might be not a closing-quote, or can be escaped-closing-quote which must be included in the result.
      return input.GetText(begin + 1, length - 2);
    }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands() {
      yield return VSConstants.VSStd2KCmdID.OPENFILE;
    }

    private struct FileInfo {
      public FileType Kind;
      public string Filename;
    }

    private enum FileType {
      Invalid,
      MplModule,
      Regular
    }
  }
}