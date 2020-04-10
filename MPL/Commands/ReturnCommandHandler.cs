using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.TextManager.Interop;
using MPL.AST;

namespace MPL.Commands {
  internal class ReturnCommandHandler : VSCommandTarget<VSConstants.VSStd2KCmdID> {
    private ITextEdit edit;
    private char indentationCharacter;
    private int indentationSize;
    private int selectionBegin, selectionEnd;
    private CaretPosition? caret;
    private readonly string lineEnding = "\n";          //LF('\n') for UNIX or CRLF('\r''\n') for WINDOWS
    private TreeBuilder.Node lastToken;
    private string rawSource;
    private ITextSnapshotLine startLine;
    private bool valid;
    private bool notIndented;
    private Stack<Stack<TreeBuilder.Node>> bracketsStack;
    private Stack<TreeBuilder.Node> currentLevelBrackets;
    private bool wasPoped;
    private bool isVirtual;

    public ReturnCommandHandler(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands {
      get {
        yield return VSConstants.VSStd2KCmdID.RETURN;
      }
    }

    protected override bool Execute(VSConstants.VSStd2KCmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();

      if (MplPackage.completionSession) {
        return false;
      }

      caret = TextView.Caret.Position;
      if (caret == null) {
        return true;
      }

      lastToken = new TreeBuilder.Node { line = 0 };
      currentLevelBrackets = new Stack<TreeBuilder.Node>();
      bracketsStack = new Stack<Stack<TreeBuilder.Node>>();
      edit = TextView.TextBuffer.CreateEdit();
      wasPoped = false;
      isVirtual = false;

      if (TextView.Options.IsConvertTabsToSpacesEnabled()) {
        indentationCharacter = ' ';
        indentationSize = TextView.Options.GetIndentSize();
      } else {
        indentationCharacter = '\t';
        indentationSize = 1;
      }

      rawSource = TextView.TextBuffer.CurrentSnapshot.GetText();

      if (!TextView.Selection.IsEmpty) {
        selectionBegin = TextView.Selection.Start.Position.Position;
        selectionEnd = TextView.Selection.End.Position.Position;
      } else {
        selectionBegin = caret.Value.BufferPosition.Position;
        selectionEnd = selectionBegin;
      }

      startLine = TextView.TextSnapshot.GetLineFromPosition(selectionBegin);
      if (startLine.GetText().Substring(0, selectionBegin - startLine.Start.Position) == new String(' ', selectionBegin - startLine.Start.Position)) {
        selectionBegin = startLine.Start.Position;
      }

      edit.Delete(selectionBegin, selectionEnd - selectionBegin);
      edit.Insert(selectionBegin, lineEnding);

      if (MplPackage.Options.AutoIndent) {
        notIndented = true;
        if (rawSource.Length == selectionEnd || rawSource[selectionEnd].ToString() == lineEnding) {
          isVirtual = true;
        }

        GetFormattedProgram();
      }

      if (valid) {
        edit.Apply();
      }

      edit.Dispose();
      return true;
    }

    private void GetFormattedProgram() {
      AST.AST.Parse(rawSource, out bool parsed);
      var root = AST.AST.GetASTRoot();
      if (root.name == "Program") {
        TraverseAutoIndent(root);
        valid = true;
      } else {
        valid = true;
      }
    }

    private void TraverseAutoIndent(TreeBuilder.Node node) {
      if (node.children == null) {
        if (node.begin >= selectionBegin && notIndented) {
          string newIndent;
          string oldIndent = "";
          if ((node.name == "')'" || node.name == "']'" || node.name == "'}'") && selectionBegin >= lastToken.end) {
            if (currentLevelBrackets.Count == 0 && bracketsStack.Count > 0 && bracketsStack.Peek().Count != 0) {
              if (node.line == bracketsStack.Peek().Peek().line) {
                bracketsStack.Peek().Pop();
                if (wasPoped) {
                  currentLevelBrackets = bracketsStack.Pop();
                  wasPoped = false;
                } else if (bracketsStack.Peek().Count == 0) {
                  bracketsStack.Pop();
                }
              } else {
                currentLevelBrackets = bracketsStack.Pop();
                currentLevelBrackets.Pop();
              }
            } else if (currentLevelBrackets.Count > 0) {
              currentLevelBrackets.Pop();
            } else if (bracketsStack.Count > 0 && bracketsStack.Peek().Count == 0) {
              bracketsStack.Pop();
            }
          }

          if (isVirtual) {
            VirtualSnapshotPoint virtPoint = new VirtualSnapshotPoint(caret.Value.BufferPosition, bracketsStack.Count * indentationSize);
            TextView.Caret.MoveTo(virtPoint);
          } else {
            newIndent = new string(indentationCharacter, bracketsStack.Count * indentationSize);
            if (selectionBegin >= lastToken.end && selectionBegin != node.begin) {
              oldIndent = rawSource.Substring(selectionBegin, node.begin - selectionBegin);
            }

            EditInsert(selectionBegin, oldIndent, newIndent);
          }

          notIndented = false;
        }

        switch (node.name) {
          case "']'":
          case "'}'":
          case "')'":
          if (currentLevelBrackets.Count == 0 && bracketsStack.Count > 0 && bracketsStack.Peek().Count != 0) {
            if (node.line == bracketsStack.Peek().Peek().line) {
              bracketsStack.Peek().Pop();
              if (wasPoped) {
                currentLevelBrackets = bracketsStack.Pop();
                wasPoped = false;
              } else if (bracketsStack.Peek().Count == 0) {
                bracketsStack.Pop();
              }
            } else {
              currentLevelBrackets = bracketsStack.Pop();
              currentLevelBrackets.Pop();
            }
          } else if (currentLevelBrackets.Count > 0) {
            currentLevelBrackets.Pop();
          } else if (bracketsStack.Count > 0 && bracketsStack.Peek().Count == 0) {
            bracketsStack.Pop();
          }

          break;
          case "'['":
          case "'{'":
          case "'('":
          if (bracketsStack.Count == 0 && currentLevelBrackets.Count == 0) {
            bracketsStack.Push(new Stack<TreeBuilder.Node>());
            bracketsStack.Peek().Push(node);
          } else if (bracketsStack.Count != 0 && bracketsStack.Peek().Peek().line == node.line) {
            bracketsStack.Peek().Push(node);
          } else if (currentLevelBrackets.Count != 0) {
            bracketsStack.Push(new Stack<TreeBuilder.Node>(currentLevelBrackets));
            currentLevelBrackets.Clear();
            bracketsStack.Peek().Push(node);
            wasPoped = true;
          } else {
            bracketsStack.Push(new Stack<TreeBuilder.Node>());
            bracketsStack.Peek().Push(node);
          }

          break;
        }

        lastToken = node;
        return;
      }

      if (notIndented) {
        foreach (var child in node.children) {
          TraverseAutoIndent(child);
        }
      }
    }

    private void EditInsert(int lastTokenEnd, string oldDel, string newDel) {
      edit.Delete(lastTokenEnd, oldDel.Length);
      edit.Insert(lastTokenEnd, newDel);
    }

    protected override VSConstants.VSStd2KCmdID ConvertFromCommandId(uint id) {
      return (VSConstants.VSStd2KCmdID)id;
    }

    protected override uint ConvertFromCommand(VSConstants.VSStd2KCmdID command) {
      return (uint)command;
    }
  }
}