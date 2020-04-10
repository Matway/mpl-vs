using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MPL.Commands {
  internal class CommentSelectionCommandHandler : VSCommandTarget<VSConstants.VSStd2KCmdID> {
    private readonly ITextBuffer buffer;

    public CommentSelectionCommandHandler(IVsTextView vsTextView, IWpfTextView textView)
      : base(vsTextView, textView) {
      buffer = textView.TextBuffer;
    }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands {
      get {
        yield return VSConstants.VSStd2KCmdID.COMMENTBLOCK;
        yield return VSConstants.VSStd2KCmdID.COMMENT_BLOCK;
        yield return VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK;
        yield return VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK;
      }
    }

    protected override bool Execute(VSConstants.VSStd2KCmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      if (TextView.Selection.IsEmpty) {
        using (var edit = buffer.CreateEdit()) {
          var caretPoint =
            TextView.Caret.Position.Point.GetPoint(TextView.TextBuffer, TextView.Caret.Position.Affinity);
          if (caretPoint != null) {
            var line = buffer.CurrentSnapshot.GetLineFromPosition(caretPoint.Value.Position);
            var text = line.GetText();
            switch (command) {
              case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
              case VSConstants.VSStd2KCmdID.COMMENT_BLOCK: {
                var startPos = line.Start.Position +
                               GetOffset(buffer.CurrentSnapshot, line.Start, line.End);

                if (!string.IsNullOrEmpty(text)) {
                  edit.Insert(startPos, "#");
                }

                break;
              }

              case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
              case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK: {
                var i = 0;
                while (i < text.Length && text[i] == ' ') {
                  ++i;
                }

                if (i < text.Length && text[i] == '#') {
                  edit.Delete(line.Start.Position + i, 1);
                  break;
                }

                break;
              }
            }
          }

          edit.Apply();
        }
      }


      var snapshot = buffer.CurrentSnapshot;
      var start = TextView.Selection.Start.Position.Position;
      var end = TextView.Selection.End.Position.Position;
      var width = Math.Abs(snapshot.GetLineFromPosition(start).Start.Position - start);
      // this is what we will store start offset in to get maximal indentation amount
      int? insertStartOffset = null;
      using (var edit = buffer.CreateEdit()) {
        var lineNum = 0;
        while (start < end) {
          var line = snapshot.GetLineFromPosition(start);
          var text = line.GetText();
          switch (command) {
            case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
            case VSConstants.VSStd2KCmdID.COMMENT_BLOCK: {
              int startPos;
              if (lineNum == 0 && TextView.Selection.Start.Position.Position != line.Start.Position) {
                startPos = TextView.Selection.Start.Position.Position;
              } else {
                if (insertStartOffset == null) {
                  insertStartOffset = GetOffset(snapshot, start, end);
                }

                if (TextView.Selection.Mode == TextSelectionMode.Stream) {
                  startPos = line.Start.Position + insertStartOffset.GetValueOrDefault();
                } else {
                  startPos = line.Start.Position + width;
                }
              }

              if (!string.IsNullOrEmpty(text)) {
                edit.Insert(startPos, "#");
              }

              break;
            }

            case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
            case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK: {
              var i = 0;
              while (i < text.Length && text[i] == ' ') {
                ++i;
              }

              if (i < text.Length && text[i] == '#') {
                edit.Delete(line.Start.Position + i, 1);
                break;
              }

              break;
            }
          }

          start = line.EndIncludingLineBreak.Position;
          lineNum++;
        }

        edit.Apply();
      }

      return true;
    }

    private static int GetOffset(ITextSnapshot snapshot, int start, int end) {
      var offset = int.MaxValue;
      while (start < end) {
        var line = snapshot.GetLineFromPosition(start);
        var text = line.GetText();

        for (var i = 0; i < text.Length; ++i) {
          if (!char.IsWhiteSpace(text[i])) {
            offset = Math.Min(offset, i);
          }
        }

        start = line.EndIncludingLineBreak.Position;
      }

      return offset == int.MaxValue ? 0 : offset;
    }

    protected override VSConstants.VSStd2KCmdID ConvertFromCommandId(uint id) {
      return (VSConstants.VSStd2KCmdID) id;
    }

    protected override uint ConvertFromCommand(VSConstants.VSStd2KCmdID command) {
      return (uint) command;
    }
  }
}