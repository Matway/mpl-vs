using System;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

using MPLVS.Core.ParseTree;
using MPLVS.ParseTree;

namespace MPLVS.Extensions.TextStructure {
  class Navigator : ITextStructureNavigator {
    // UNDONE: How we should put stuff into this property?
    public IContentType ContentType => null;

    public TextExtent GetExtentOfWord(SnapshotPoint currentPosition) {
      var symbol = currentPosition.Snapshot.TextBuffer.ObtainOrAttachTree().Root().YongestAncestor(currentPosition.Position);

      return
        symbol is null
        ? TokenSpan(currentPosition)
        : symbol.IsName() || symbol.IsNumber() || symbol.IsReal() || symbol.IsScopeStart() || symbol.IsScopeEnd()
          ? new TextExtent(new SnapshotSpan(currentPosition.Snapshot, symbol.begin, symbol.Length()), true)
          : symbol.IsNameRead() || symbol.IsNameWrite() || symbol.IsNameMember()
            ? AdjastedSpan(currentPosition, symbol, 1)
            : symbol.IsNameReadMember() || symbol.IsNameWriteMember()
              ? AdjastedSpan(currentPosition, symbol, 2)
              : TokenSpan(currentPosition);
    }

    public SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan) {
      var snapshot = activeSpan.Snapshot.TextBuffer.CurrentSnapshot;
      var start    = activeSpan.Start.Position;
      var end      = activeSpan.End.Position;

      var ancestors = activeSpan.Snapshot.TextBuffer.ObtainOrAttachTree().Root()
                                                    .AllAncestors(start, end)
                                                    .Where(a => a.begin != start || a.end != end)
                                                    .LastThree(); // FIXME: It should be '.LastTwo()'. In current function, the third ancestor is never used.

      var symbol =
        ancestors.FirstFromEnd is null
        ? null
        : ancestors.FirstFromEnd.IsEol()
          ? ancestors.FirstFromEnd.Previous is object && ancestors.FirstFromEnd.Previous.IsComment()
            ? ancestors.FirstFromEnd.Previous
            : ancestors.SecontFromEnd
          : ancestors.FirstFromEnd;

      return
        symbol is null
        ? new SnapshotSpan(snapshot, 0, snapshot.Length)
        : new SnapshotSpan(snapshot, symbol.begin, symbol.Length());
    }

    public SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan) => throw new NotImplementedException();

    public SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan) => throw new NotImplementedException();

    public SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan) => throw new NotImplementedException();

    // If a caret placed into .name .!name .@name !name @name,
    // then we want to handle separately 'name' and each character of a prefix.
    // Basically, we have three cases:
    //   1) |.@name - caret at the beginning of a symbol.
    //   2) .|@name - caret at the middle of a prefix.
    //   3) .@|name - caret at the beginning or middle of the name.
    // In the 1st and 2nd cases, we will return span of just one character after the caret.
    // In the 3rd case, we will return the entire span of the name. W\o prefix.
    private static TextExtent AdjastedSpan(SnapshotPoint currentPosition, Builder.Node symbol, int ofset) {
      var position = currentPosition.Position;
      var begin    = symbol.begin;
      var prefix   = begin == position || begin + (ofset - 1) == position;
      var start    = prefix ? position : begin + ofset;
      var length   = prefix ? 1 : symbol.end - start;
      var span     = new SnapshotSpan(currentPosition.Snapshot, start, length);

      return new TextExtent(span, true);
    }

    internal static TextExtent TokenSpan(SnapshotPoint point) {
      var text = point.Snapshot;

      if (text.Length <= point.Position) { return new TextExtent(new SnapshotSpan(point, 0), false); }

      var character = text[point.Position];

      // NOTE: GetExtentOfWord's documentation says "If the returned extent consists only of insignificant whitespace,
      //       it should include all of the adjacent whitespace, including newline characters, spaces, and tabs."
      //       But we will not obey this requirement. Otherwise a caret sometimes will jump through several lines at once.
      if (IsNewLine(character)) {
        var line = text.GetLineFromPosition(point);
        var span = new SnapshotSpan(line.End, line.LineBreakLength);
        return new TextExtent(span, false);
      }

      return
        IsWhiteSpace(character)
        ? ExpandWhile(point, false, IsWhiteSpace)
        : char.IsLetterOrDigit(character)
          ? ExpandWhile(point, true, char.IsLetterOrDigit)
          : new TextExtent(new SnapshotSpan(point, 1), true);
    }

    private static TextExtent ExpandWhile(SnapshotPoint point, bool isSignificant, Predicate<char> test) {
      var text = point.Snapshot;

      var rhs = point.Position;
      while (++rhs < text.Length && test(text[rhs])) { }

      var lhs = point.Position;
      while (--lhs >= 0 && test(text[lhs])) { }

      return new TextExtent(new SnapshotSpan(point.Snapshot, lhs + 1, rhs - lhs - 1), isSignificant);
    }

    private static bool IsWhiteSpace(char character) => character == ' ' || character == '\t';

    private static bool IsNewLine(char character) => character == '\r' || character == '\n';
  }
}