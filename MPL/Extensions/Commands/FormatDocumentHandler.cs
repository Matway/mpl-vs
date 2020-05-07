using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.TextManager.Interop;
using MPL.ParseTree;

namespace MPL.Commands {
  internal class FormatDocumentHandler : VSCommandTarget<VSConstants.VSStd2KCmdID> {
    private SnapshotPoint? caretPoint;
    private string lineEnding = "\n";          //LF('\n') for UNIX or CRLF('\r''\n') for WINDOWS
    private bool leaveAsIs;
    private bool isFirstToken;
    private char indentationCharacter;
    private int indentationSize;
    private string rawSource;
    private int selectionBegin, selectionEnd;
    private bool valid;
    private Builder.Node lastToken;
    private ITextEdit edit;
    private Stack<Stack<Builder.Node>> bracketsStack;
    private Stack<Builder.Node> currentLevelBrackets;
    private bool wasPoped;

    public FormatDocumentHandler(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) { }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands() {
      yield return VSConstants.VSStd2KCmdID.FORMATDOCUMENT;
      yield return VSConstants.VSStd2KCmdID.FORMATSELECTION;
    }

    protected override bool Execute(VSConstants.VSStd2KCmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      lastToken = new Builder.Node { line = 0 };
      currentLevelBrackets = new Stack<Builder.Node>();
      bracketsStack = new Stack<Stack<Builder.Node>>();
      edit = TextView.TextBuffer.CreateEdit();
      wasPoped = false;
      switch (MplPackage.Options.LineEndings) {
        case Options.LineEnding.Unix:
        leaveAsIs = false;
        lineEnding = "\n";
        break;
        case Options.LineEnding.Windows:
        leaveAsIs = false;
        lineEnding = "\r\n";
        break;
        case Options.LineEnding.Document:
        leaveAsIs = true;
        break;
      }

      if (TextView.Options.IsConvertTabsToSpacesEnabled()) {
        indentationCharacter = ' ';
        indentationSize = TextView.Options.GetIndentSize();
      } else {
        indentationCharacter = '\t';
        indentationSize = 1;
      }

      selectionBegin = 0;
      selectionEnd = TextView.TextBuffer.CurrentSnapshot.Length;
      rawSource = TextView.TextBuffer.CurrentSnapshot.GetText();
      caretPoint = TextView.Caret.Position.Point.GetPoint(TextView.TextBuffer, TextView.Caret.Position.Affinity);
      switch (command) {
        case VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
        break;
        case VSConstants.VSStd2KCmdID.FORMATSELECTION:
        if (!TextView.Selection.IsEmpty && caretPoint != null) {
          selectionBegin = TextView.Selection.Start.Position.Position;
          selectionEnd = TextView.Selection.End.Position.Position;
        } else if (TextView.Selection.IsEmpty && caretPoint != null) {
          var line = TextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(caretPoint.Value.Position);
          selectionBegin = line.Start.Position;
          selectionEnd = line.End.Position;
        }

        break;
      }

      GetFormattedProgram();
      if (valid) {
        edit.Apply();
      }

      edit.Dispose();
      return true;
    }

    private void TraverseEdit(Builder.Node node) {
      if (node.children == null) {
        AddDelimeter(node);
        return;
      }

      foreach (var child in node.children) {
        TraverseEdit(child);
      }
    }

    private int GetLine(int position) {
      string s = rawSource.Substring(0, position);
      int sum = 0, lf = 0, cr = 0, crlf = 0;
      for (int i = 0; i < s.Length; i++) {
        if (s[i] == '\n') {
          lf++;
        }

        if (s[i] == '\r') {
          cr++;
          if (i != s.Length - 1) {
            if (s[i + 1] == '\n') {
              crlf++;
              lf++;
              i++;
            }
          }
        }
      }

      sum = lf + cr - crlf;
      return sum;
    }

    private void AddIndentation(Builder.Node currentToken) {
      if (lastToken.end <= selectionEnd && currentToken.begin >= selectionBegin && (lastToken.name == "CRLF" || lastToken.name == "LF" || isFirstToken)) {
        var beginLine = TextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(selectionBegin);
        var changeSpanBegin = Math.Max(lastToken.end, beginLine.Start.Position);
        var endLine = TextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(selectionEnd - 1);
        var changeSpanEnd = Math.Min(currentToken.begin, endLine.End.Position);
        string oldDelimeter = rawSource.Substring(changeSpanBegin, Math.Abs(changeSpanEnd - changeSpanBegin));
        var newDelimeter = new string(indentationCharacter, bracketsStack.Count * indentationSize);
        if (newDelimeter != oldDelimeter) {
          EditInsert(changeSpanBegin, oldDelimeter, newDelimeter);
        }
      }
    }

    private void AddDelimeter(Builder.Node currentToken) {
      switch (currentToken.name) {
        case "EOF":
        if (lastToken.name != "CRLF" && lastToken.name != "LF") {
          if (rawSource.Substring(lastToken.end) != lineEnding && currentToken.end == selectionEnd) {
            edit.Delete(lastToken.end, rawSource.Length - lastToken.end);
            edit.Insert(lastToken.end, lineEnding);
          }
        } else {
          if (rawSource.Substring(lastToken.end) != lineEnding && currentToken.end == selectionEnd) {
            edit.Delete(lastToken.end, rawSource.Length - lastToken.end);
          }
        }

        return;
        case "']'":
        case "'}'":
        case "')'":
        if (currentLevelBrackets.Count == 0 && bracketsStack.Count > 0 && bracketsStack.Peek().Count != 0) {
          if (currentToken.line == bracketsStack.Peek().Peek().line) {
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

        AddIndentation(currentToken);
        break;
        case "CRLF":
        if (currentToken.end <= selectionEnd && currentToken.end >= selectionBegin) {
          if (lastToken.end != currentToken.begin) {
            edit.Delete(lastToken.end, currentToken.begin - lastToken.end);
          }

          if (lineEnding != "\r\n" && !leaveAsIs) {
            EditInsert(currentToken.begin, "\r\n", lineEnding);
          }
        }

        break;
        case "LF":
        if (currentToken.end <= selectionEnd && currentToken.end >= selectionBegin) {
          if (lastToken.end != currentToken.begin) {
            edit.Delete(lastToken.end, currentToken.begin - lastToken.end);
          }

          if (lineEnding != "\n" && !leaveAsIs) {
            EditInsert(currentToken.begin, "\n", lineEnding);
          }
        }

        break;
        default:
        AddIndentation(currentToken);
        break;
      }

      if (currentToken.name == "'['" || currentToken.name == "'{'" || currentToken.name == "'('") {
        if (bracketsStack.Count == 0 && currentLevelBrackets.Count == 0) {
          bracketsStack.Push(new Stack<Builder.Node>());
          bracketsStack.Peek().Push(currentToken);
        } else if (bracketsStack.Count != 0 && bracketsStack.Peek().Peek().line == currentToken.line) {
          bracketsStack.Peek().Push(currentToken);
        } else if (currentLevelBrackets.Count != 0) {
          bracketsStack.Push(new Stack<Builder.Node>(currentLevelBrackets));
          currentLevelBrackets.Clear();
          bracketsStack.Peek().Push(currentToken);
          wasPoped = true;
        } else {
          bracketsStack.Push(new Stack<Builder.Node>());
          bracketsStack.Peek().Push(currentToken);
        }
      }

      lastToken = currentToken;

      if (isFirstToken) {
        isFirstToken = !isFirstToken;
      }
    }

    private void EditInsert(int lastTokenEnd, string oldDel, string newDel) {
      edit.Delete(lastTokenEnd, oldDel.Length);
      edit.Insert(lastTokenEnd, newDel);
    }

    private void GetFormattedProgram() {
      isFirstToken = true;
      ParseTree.Tree.Parse(rawSource, out bool parsed);
      var root = ParseTree.Tree.Root();
      if (root.name == "Program") {
        TraverseEdit(root);
        valid = true;
      } else {
        valid = true;
      }
    }

    private static string Repeat(string value, int count) {
      return new StringBuilder(value.Length * count).Insert(0, value, count).ToString();
    }

    protected override VSConstants.VSStd2KCmdID ConvertFromCommandId(uint id) {
      return (VSConstants.VSStd2KCmdID)id;
    }

    protected override uint ConvertFromCommand(VSConstants.VSStd2KCmdID command) {
      return (uint)command;
    }
  }
}