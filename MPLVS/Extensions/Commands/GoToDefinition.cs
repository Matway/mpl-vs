using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using MPLVS.Core.ParseTree;
using MPLVS.Extensions;
using MPLVS.ParseTree;

namespace MPLVS.Commands {
  internal class GoToDefinition : VSCommandTarget<VSConstants.VSStd97CmdID> {
    public GoToDefinition(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override IEnumerable<VSConstants.VSStd97CmdID> SupportedCommands() {
      yield return VSConstants.VSStd97CmdID.GotoDefn;
    }

    protected override bool Execute(VSConstants.VSStd97CmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      var name    = this.PickUpName();
      var symbols = this.GatherSymbols(name);

      ShowSymbols(name, symbols);

      return true;
    }

    private string PickUpName() {
      return this.Selection() ?? CurrentName();

      string CurrentName() {
        var name = this.CurrentSymbol();

        if (name is null) { return null; }

        return new SnapshotSpan(this.TextView.TextBuffer.CurrentSnapshot, name.ToSpan()).GetText();
      }
    }

    private Builder.Node CurrentSymbol() {
      var caret        = this.TextView.Caret.Position.BufferPosition;
      var surroundings = this.TextView.TextBuffer.ObtainOrAttachTree().Root().Surroundings(caret, Strategy.NearLeftFarRight);

      return surroundings.AsSequence().FirstOrDefault(a => a is object && a.begin <= caret && a.end >= caret && a.IsLooksLikeName())?.ExtractName();
    }

    private string Selection() {
      if (this.TextView.Selection.IsEmpty) { return null; }

      // FIXME: We should select not just the first span, but the span near the master/main caret.
      return this.TextView.TextSnapshot.GetText(this.TextView.Selection.SelectedSpans.First());
    }

    private IEnumerable<Definitions> GatherSymbols(string name) {
      if (name is null) { return Array.Empty<Definitions>(); }

      var symbol     = this.CurrentSymbol();
      var nearSymbol = default(Definitions);
      if (symbol is object) {
        nearSymbol = this.FindDefinition(name, symbol);
      }

      return
        nearSymbol.Labels is object
        ? new List<Definitions> { nearSymbol }
        : DefinitionsFromAProject(name, this.TextView.TextBuffer.GetFileName());
    }

    // FIXME: We should distinguish name under caret and name from selection.
    //        The name from selection always should trigger project-wide symbol search.
    private static void ShowSymbols(string name, IEnumerable<Definitions> symbols) {
      ThreadHelper.ThrowIfNotOnUIThread();

      _ = Output.Pane.Clear();

      var howMachSymbolsFound = symbols.SelectMany(a => a.Labels).Take(2).Count();

      switch (howMachSymbolsFound) {
        case 0: {
          _ =
            string.IsNullOrEmpty(name)
            ? Output.Pane.OutputString("Go to Definition: A name not found. Place the caret near a name, and try again.")
            : Output.Pane.OutputString($"Go to Definition: Possible definitions of \"{name}\" were not found.");

          Output.Activate();
          return;
        }

        case 1: {
          var group = symbols.First();
          _ = Files.GetTextViewForDocument(group.File, group.Labels.First().Position, name.Length);
          return;
        }

        // 2 means that was found more than one symbol.
        case 2: {
          ShowDefinitions(name, symbols);
          return;
        }

        default:
          Debug.Fail(howMachSymbolsFound.ToString());
          return;
      }
    }

    private Definitions FindDefinition(string name, Builder.Node from) {
      var location =
        from.AsReverseSequence()
            .Where(a => a.IsLabel())
            .Select(a => a.LabelName())
            .Select(a => new { Node = a, Text = this.TextView.TextBuffer.CurrentSnapshot.GetText(a.begin, a.Length()) }) // FIXME: GC.
            .FirstOrDefault(a => a.Text == name)?.Node;

      if (location is null) { return default; }

      return new Definitions {
        File = this.TextView.TextBuffer.GetFileName(), // UNDONE: What should we do if the view has no corresponding file?
        Labels = new List<Location> {
          new Location {
            Line = location.line, Column = location.column, Position = location.begin
          }
        }
      };
    }

    private static List<Definitions> DefinitionsFromAProject(string name, string root) =>
      Symbols.Files.TreesFromAWholeProject(root)
        .Where(a => a.Root is object)
        .Select(a => new {
          a.File,
          Labels = Symbols.Files.Labels(a.Root)
                                .Select(b => b.LabelName())
                                .Select(b => new { Name = Symbols.Files.Text(b, a.Input), Location = b })
                                .Where(b => b.Name == name)
                                .Select(b => new Location { Line = b.Location.line, Column = b.Location.column, Position = b.Location.begin })
        })
        .Where(a => a.Labels.Any())
        .Select(a => new Definitions { File = a.File, Labels = a.Labels.ToList() })
        .ToList();

    private static void ShowDefinitions(string name, IEnumerable<Definitions> symbols) {
      ThreadHelper.ThrowIfNotOnUIThread();

      _ = Output.Pane.OutputString($"Go to Definition: Possible definitions of \"{name}\":\n");

      foreach (var group in symbols) {
        foreach (var location in group.Labels) {
          _ = Output.Pane.OutputString($"{group.File}({location.Line + 1},{location.Column + 1})\n");
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