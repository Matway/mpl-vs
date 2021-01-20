using System;
using System.Collections.Generic;
using System.Diagnostics;
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
  internal class GoToDefinition : VSCommandTarget<VSConstants.VSStd97CmdID> {
    private string selectedName;
    private int selectionEnd;
    private string currName;
    private int definitionBegin;
    private int definitionEnd;
    private bool nameFound = false;

    public GoToDefinition(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override IEnumerable<VSConstants.VSStd97CmdID> SupportedCommands() {
      yield return VSConstants.VSStd97CmdID.GotoDefn;
    }

    protected override bool Execute(VSConstants.VSStd97CmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (!TextView.Selection.IsEmpty) {
        selectedName = TextView.TextBuffer.CurrentSnapshot.GetText(TextView.Selection.SelectedSpans.First().Span);
        selectionEnd = TextView.Selection.End.Position.Position;
        nameFound = true;
      }
      else {
        nameFound = FindTheName();
      }

      if (nameFound) {
        if (FindDefinition()) {
          var defBegin = new SnapshotPoint(TextView.TextSnapshot, definitionBegin);
          var defEnd   = new SnapshotPoint(TextView.TextSnapshot, definitionEnd);

          // FIXME: The file is already open, so just move the caret\view.
          Files.GetTextViewForDocument(TextView.TextBuffer.GetFileName(), defBegin, defEnd - defBegin);
        }
        else {
          //vsRunningDocumentTable.                        //
          //MplPackage.Dte.FullName.ToString();            // TODO: This is an old commented code, consider to reuse it.
          //StreamReader streamReader = File.OpenText(""); //

          FindDefinitionsFromEntireProject();
        }
      }

      return true;
    }

    private bool FindTheName() {
      var root        = TextView.TextBuffer.ObtainOrAttachTree().Root();
      var cursor      = TextView.Caret.Position.BufferPosition.Position;
      var symbol      = root.YongestAncestor(cursor);
      var predecessor = cursor < 1 ? null : root.YongestAncestor(cursor - 1); // FIXME: Do not use YongestAncestor twice, if possible.

      return
        predecessor is object && predecessor.end == cursor && predecessor.name.StartsWith("Name")
        ? FromSomeName(predecessor)
        : symbol is object && FromSomeName(symbol);

      bool FromSomeName(Builder.Node node) {
        var result = false;

        switch (node.name) {
          case "Name":
            Name(node, 0); break;

          case "NameRead":
          case "NameWrite":
          case "NameMember":
            Name(node, 1); break;

          case "NameReadMember":
          case "NameWriteMember":
            Name(node, 2); break;
        }

        return result;

        void Name(Builder.Node name, int offset) {
          selectedName = TextView.TextBuffer.CurrentSnapshot.GetText(name.begin + offset, name.end - name.begin - offset);
          selectionEnd = name.end;

          result = true;
        }
      }
    }

    private bool FindDefinition() {
      var root            = TextView.TextBuffer.ObtainOrAttachTree().Root();
      var definitionFound = false;

      void Traverse(Builder.Node node) {
        if (node.children == null) {
          return;
        }

        if ((node.name == "Code" || node.name == "Object" || node.name == "List") && node.end < selectionEnd) {
          return;
        }

        if (node.name == "Label") {
          var child = node.children[0];
          currName  = TextView.TextBuffer.CurrentSnapshot.GetText(child.begin, child.end - child.begin);
          if (currName == selectedName) {
            definitionBegin = child.begin;
            definitionEnd   = child.end;
            definitionFound = true;
          }
        }

        foreach (var child in node.children) {
          if (child.begin < selectionEnd) {
            Traverse(child);
          }
        }
      }

      Traverse(root);

      return definitionFound;
    }

    private void FindDefinitionsFromEntireProject() {
      var symbols             = this.DefinitionsFromAProject(TextView.TextBuffer.GetFileName());
      var howMachSymbolsFound = symbols.SelectMany(a => a.Labels).Take(2).Count();

      switch (howMachSymbolsFound) {
        case 0: break;

        case 1: {
          var group = symbols.First();
          Files.GetTextViewForDocument(group.File, group.Labels.First().Position, this.selectedName.Length);
          break;
        }

        // 2 means that was found more than one symbol.
        case 2: {
          ShowDefinitions(symbols);
          break;
        }

        default:
          Debug.Fail(howMachSymbolsFound.ToString());
          break;
      }
    }

    private List<Definitions> DefinitionsFromAProject(string root) =>
      Symbols.Files.TreesFromAWholeProject(root)
        .Where(a => a.Root is object)
        .Select(a => new {
          a.File,
          Labels = Symbols.Files.Labels(a.Root)
                                .Select(b => b.LabelName())
                                .Select(b => new { Name = Symbols.Files.Text(b, a.Input), Location = b })
                                .Where(b => b.Name == selectedName)
                                .Select(b => new Location { Line = b.Location.line, Column = b.Location.column, Position = b.Location.begin })
        })
        .Where(a => a.Labels.Any())
        .Select(a => new Definitions { File = a.File, Labels = a.Labels.ToList() })
        .ToList();

    private void ShowDefinitions(IEnumerable<Definitions> symbols) {
      ThreadHelper.ThrowIfNotOnUIThread();

      Output.Pane.Clear();
      Output.Pane.OutputString("Possible definitions of \"" + selectedName + "\":\n");

      foreach (var group in symbols) {
        foreach (var location in group.Labels) {
          Output.Pane.OutputString(group.File + "(" + (location.Line + 1) + "," + (location.Column + 1) + ")\n");
        }
      }

      Output.Activate();
    }

    protected override VSConstants.VSStd97CmdID ConvertFromCommandId(uint id) =>
      (VSConstants.VSStd97CmdID)id;

    protected override uint ConvertFromCommand(VSConstants.VSStd97CmdID command) =>
      (uint)command;

    private struct Definitions {
      public string File;
      public List<Location> Labels;
    }

    private struct Location {
      public int Line, Column, Position;
    }
  }
}