using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MPL.Commands {
  internal class GoToBraceCommandHandler : VSCommandTarget<VSConstants.VSStd2KCmdID> {
    private readonly Dictionary<char, char> bracePairs;
    private readonly Dictionary<char, string> braceKind;
    private SnapshotPoint prevPoint;

    public GoToBraceCommandHandler(IVsTextView vsTextView, IWpfTextView textView)
      : base(vsTextView, textView) {
      bracePairs = new Dictionary<char, char> {
        ['{'] = '}',
        ['['] = ']',
        ['('] = ')'
      };

      braceKind = new Dictionary<char, string> {
        ['{'] = "Object",
        ['}'] = "Object",
        ['['] = "Code",
        [']'] = "Code",
        ['('] = "List",
        [')'] = "List"
      };
    }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands() {
      yield return VSConstants.VSStd2KCmdID.GOTOBRACE;
    }

    protected override bool Execute(VSConstants.VSStd2KCmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      ThreadHelper.ThrowIfNotOnUIThread();
      SnapshotPoint caretPoint = TextView.Caret.Position.BufferPosition;

      if (TextView.TextSnapshot.Length == 0) {
        return true;
      }

      SnapshotPoint currPoint;
      if (caretPoint.Position == caretPoint.Snapshot.Length && caretPoint.Snapshot.Length != 0) {
        currPoint = caretPoint - 1;
      } else {
        currPoint = caretPoint;
      }

      prevPoint = caretPoint.Position != 0 ? caretPoint - 1 : caretPoint;

      char currentCharacter = currPoint.GetChar();
      char lastCharacter = prevPoint.GetChar();
      SnapshotPoint matchedPoint;
      if (MplPackage.Options.SharpStyleBraceMatch) {
        if (bracePairs.ContainsKey(currentCharacter)) {
          if (FindCloseChar(currPoint, braceKind[currentCharacter], out matchedPoint)) {
            matchedPoint = matchedPoint + 1;
            TextView.Caret.MoveTo(TextView.GetTextViewLineContainingBufferPosition(matchedPoint), 0);
            for (int i = 0; i < matchedPoint.GetContainingLine().Start.Difference(matchedPoint); ++i) {
              TextView.Caret.MoveToNextCaretPosition();
            }
          }
        } else if (bracePairs.ContainsValue(lastCharacter)) {
          if (FindOpenChar(prevPoint, braceKind[lastCharacter], out matchedPoint)) {
            TextView.Caret.MoveTo(TextView.GetTextViewLineContainingBufferPosition(matchedPoint), 0);
            for (int i = 0; i < matchedPoint.GetContainingLine().Start.Difference(matchedPoint); ++i) {
              TextView.Caret.MoveToNextCaretPosition();
            }
          }
        } else if (bracePairs.ContainsKey(lastCharacter)) {
          if (FindCloseChar(prevPoint, braceKind[lastCharacter], out matchedPoint)) {
            matchedPoint = matchedPoint + 1;
            TextView.Caret.MoveTo(TextView.GetTextViewLineContainingBufferPosition(matchedPoint), 0);
            for (int i = 0; i < matchedPoint.GetContainingLine().Start.Difference(matchedPoint); ++i) {
              TextView.Caret.MoveToNextCaretPosition();
            }
          }
        } else if (bracePairs.ContainsValue(currentCharacter)) {
          if (FindOpenChar(currPoint, braceKind[currentCharacter], out matchedPoint)) {
            TextView.Caret.MoveTo(TextView.GetTextViewLineContainingBufferPosition(matchedPoint), 0);
            for (int i = 0; i < matchedPoint.GetContainingLine().Start.Difference(matchedPoint); ++i) {
              TextView.Caret.MoveToNextCaretPosition();
            }
          }
        }
      } else {
        if (bracePairs.ContainsKey(currentCharacter)) {
          if (FindCloseChar(currPoint, braceKind[currentCharacter], out matchedPoint)) {
            matchedPoint = matchedPoint + 1;
            TextView.Caret.MoveTo(TextView.GetTextViewLineContainingBufferPosition(matchedPoint), 0);
            for (int i = 0; i < matchedPoint.GetContainingLine().Start.Difference(matchedPoint); ++i) {
              TextView.Caret.MoveToNextCaretPosition();
            }
          }
        } else if (bracePairs.ContainsValue(currentCharacter)) {
          if (FindOpenChar(currPoint, braceKind[currentCharacter], out matchedPoint)) {
            TextView.Caret.MoveTo(TextView.GetTextViewLineContainingBufferPosition(matchedPoint), 0);
            for (int i = 0; i < matchedPoint.GetContainingLine().Start.Difference(matchedPoint); ++i) {
              TextView.Caret.MoveToNextCaretPosition();
            }
          }
        } else if (bracePairs.ContainsKey(lastCharacter)) {
          if (FindCloseChar(prevPoint, braceKind[lastCharacter], out matchedPoint)) {
            matchedPoint = matchedPoint + 1;
            TextView.Caret.MoveTo(TextView.GetTextViewLineContainingBufferPosition(matchedPoint), 0);
            for (int i = 0; i < matchedPoint.GetContainingLine().Start.Difference(matchedPoint); ++i) {
              TextView.Caret.MoveToNextCaretPosition();
            }
          }
        } else if (bracePairs.ContainsValue(lastCharacter)) {
          if (FindOpenChar(prevPoint, braceKind[lastCharacter], out matchedPoint)) {
            TextView.Caret.MoveTo(TextView.GetTextViewLineContainingBufferPosition(matchedPoint), 0);
            for (int i = 0; i < matchedPoint.GetContainingLine().Start.Difference(matchedPoint); ++i) {
              TextView.Caret.MoveToNextCaretPosition();
            }
          }
        }
      }

      TextView.Caret.EnsureVisible();

      return true;
    }

    private bool FindCloseChar(SnapshotPoint start, string kind, out SnapshotPoint end) {
      ParseTree.Builder.Node root = ParseTree.Tree.Root();
      int startPos = start.Position;
      int endPos = startPos;
      bool haveFound = false;

      void Traverse(ParseTree.Builder.Node node) {
        if (node.children == null) {
          return;
        }

        if (node.name == kind && node.begin == startPos && node.children.Exists(x => x.name == "']'" || x.name == "'}'" || x.name == "')'")) {
          endPos = node.end - 1;
          haveFound = true;
        }

        foreach (var child in node.children) {
          if (!haveFound && child.begin <= startPos && child.end > startPos) {
            Traverse(child);
          }
        }
      }

      Traverse(root);

      end = new SnapshotPoint(start.Snapshot, endPos);
      return haveFound;
    }

    private bool FindOpenChar(SnapshotPoint end, string kind, out SnapshotPoint start) {
      ParseTree.Builder.Node root = ParseTree.Tree.Root();
      int endPos = end.Position;
      int startPos = endPos;
      bool haveFound = false;

      void Traverse(ParseTree.Builder.Node node) {
        if (node.children == null) {
          return;
        }

        if (node.name == kind && node.end - 1 == endPos) {
          startPos = node.begin;
          haveFound = true;
        }

        foreach (var child in node.children) {
          if (!haveFound) {
            Traverse(child);
          }
        }
      }

      Traverse(root);

      start = new SnapshotPoint(end.Snapshot, startPos);
      return haveFound;
    }

    protected override VSConstants.VSStd2KCmdID ConvertFromCommandId(uint id) {
      return (VSConstants.VSStd2KCmdID) id;
    }

    protected override uint ConvertFromCommand(VSConstants.VSStd2KCmdID command) {
      return (uint) command;
    }
  }
}
