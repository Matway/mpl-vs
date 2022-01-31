using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MPLVS.Commands {
  internal class GoToBrace : VSStd2KCommand {
    public GoToBrace(IVsTextView vsTextView, IWpfTextView textView) : base(vsTextView, textView) =>
      this.tagger = TextView.ObtainOrAttachProperty(() => new ScopeHighlighting.Tagger(TextView));

    protected override bool Run(VSConstants.VSStd2KCmdID command, uint options, IntPtr pvaIn, IntPtr pvaOut) {
      var tags = Tags();

      if (!tags.Any()) {
        return true;
      }

      Debug.Assert(tags.Count == 1 || tags.Count == 2);

      var begin = tags.First().Span.Start;
      var end   = tags.Last().Span.Start;
      var from  = TextView.Caret.Position.BufferPosition.Position;

      Debug.Assert(begin.Position <= end.Position);

      MoveCaret(TextView.Caret, from == begin.Position ? end : begin);

      return true;
    }

    protected override IEnumerable<VSConstants.VSStd2KCmdID> SupportedCommands() {
      yield return VSConstants.VSStd2KCmdID.GOTOBRACE;
      //TODO: Support yield return VSConstants.VSStd2KCmdID.GOTOBRACE_EXT;
    }

    private static void MoveCaret(ITextCaret caret, SnapshotPoint to) {
      caret.MoveTo(to);
      caret.EnsureVisible();
    }

    private List<ITagSpan<Extensions.Tag>> Tags() {
      var wholeDocument  = new SnapshotSpan(TextView.TextSnapshot, 0, TextView.TextSnapshot.Length);
      var entireDocument = new NormalizedSnapshotSpanCollection(wholeDocument);
      return this.tagger.GetTags(entireDocument).ToList();
    }

    private readonly ScopeHighlighting.Tagger tagger;
  }
}