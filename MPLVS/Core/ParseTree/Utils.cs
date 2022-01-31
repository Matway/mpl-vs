using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using MPLVS.ParseTree;

namespace MPLVS.Core.ParseTree {
  public static class NodesUtils {
    /// <summary>Gives an innermost node which holds a text at given position.</summary>
    /// <returns><c>null</c> if the <paramref name="node"/> has no such a sub-node.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Builder.Node YongestAncestor(this Builder.Node node, int position) =>
      node.AllAncestors(position).LastOrDefault();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Builder.Node YongestAncestor(this Builder.Node node, int start, int end) =>
      node.AllAncestors(start, end).LastOrDefault();

    /// <summary>Gives <c>(oldest-ancestor ... ancestor-of-youngest-ancestor youngest-ancestor)</c>.</summary>
    /// <param name="node">Node which holds the text at the <paramref name="position"/>.</param>
    /// <param name="position">Position in a text for which is needed to find the ancestors.</param>
    /// <returns>An empty generator if the <paramref name="node"/> holds no such a sub-nodes.</returns>
    public static IEnumerable<Builder.Node> AllAncestors(this Builder.Node node, int position) {
      Debug.Assert(position >= 0);

      var tree = node ?? throw new ArgumentNullException(nameof(node));
      while (tree.IsNotEmpty()) {
        tree = tree.OldestAncestor(position);

        if (tree is null) {
          yield break;
        }

        yield return tree;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotEmpty(this Builder.Node node) =>
      node.children is object && node.children.Any();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Builder.Node> AllAncestors(this Builder.Node node, int start, int end) =>
      node.AllAncestors(start).TakeWhile(a => a.end >= end);

    /// <summary>Gives an outermost node which holds a text at given position.</summary>
    /// <returns>
    /// The sub-node which holds a text at the <paramref name="position"/>.
    /// <para/>
    /// Or <c>null</c>, if the <paramref name="node"/> has no such a sub-node.
    /// </returns>
    public static Builder.Node OldestAncestor(this Builder.Node node, int position) {
      if (node is null) { throw new ArgumentNullException(nameof(node)); }

      var idx = node.children.BinarySearch(new Builder.Node { begin = position }, new Comparer());

      return
        idx < 0
        ? null
        : node.children[idx];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Builder.Node OldestAncestor(this Builder.Node node, int start, int end) =>
      node.AllAncestors(start, end).FirstOrDefault();

    static Builder.Node FirstAtSamePosition(this Builder.Node x) => x?.AsReverseSequence().TakeWhile(a => a.begin == x.begin && (a.begin != 0 || a.name != "Program")).LastOrDefault();

    static Builder.Node LastAtSamePosition(this Builder.Node x) => x?.AsSequence().TakeWhile(a => a.begin == x.begin).Last();

    public static Builder.Node FarRight(this Builder.Node x, int position) => x.AfterPosition(position);

    public static Builder.Node NearRight(this Builder.Node x, int position) => x.FarRight(position).FirstAtSamePosition();

    public static Builder.Node NearLeft(this Builder.Node x, int position) => x.Surroundings(position, Strategy.NearLeftFarRight).Left;

    public static Builder.Node FarLeft(this Builder.Node x, int position) => x.Surroundings(position, Strategy.FarLeftFarRight).Left;

    /// <summary>
    /// Find out between which symbols a position is.
    /// </summary>
    /// <remarks>The <c>Left</c> can be the same symbol as the <c>Right</c>.</remarks>
    /// <returns>
    /// <c>Left</c>  - a symbol before the <paramref name="position"/>; <para></para>
    /// <c>Right</c> - a symbol after the <paramref name="position"/>.
    /// </returns>
    public static Pair Surroundings(this Builder.Node node, int position, Strategy strategy) {
      Builder.Node left, right;

      switch (strategy) {
        case Strategy.NearLeftFarRight:
          right = FarRight(node, position);
          left  = right is null ? null : right.begin < position ? right : right.FirstAtSamePosition().Previous;
          break;
        case Strategy.NearLeftNearRight:
          right = NearRight(node, position);
          left  = right is null ? null : right.begin < position ? right : right.Previous;
          break;
        case Strategy.FarLeftNearRight:
          right = NearRight(node, position);
          left  = right is null ? null : right.begin < position ? right : right.Previous.FirstAtSamePosition();
          break;
        case Strategy.FarLeftFarRight:
          right = FarRight(node, position);
          left  = right is null ? null : right.begin < position ? right : right.FirstAtSamePosition().Previous.FirstAtSamePosition();
          break;

        default:
          throw new Exception();
      }

      return
        right is object
        ? new Pair { Left = left, Right = right.end != position ? right : right.AsSequence().Last() }
        // NOTE: Even an empty program has 'EOF' symbol, so probably, this code will be never reached.
        : new Pair { Left = node.VeryLast(), Right = null };
    }

    private static Builder.Node AfterPosition(this Builder.Node node, int position) {
      if (node is null) { throw new ArgumentNullException(nameof(node)); }

      if (!node.IsNotEmpty()) { return null; }

      if (node.end == position && node.name == "Program" && node.VeryLast().IsEof()) { return node.VeryLast(); }

      var current  = node;
      var previous = default(Builder.Node);

      while (current is object && current.IsNotEmpty()) {
        var order = current.children.BinarySearch(new Builder.Node { begin = position }, new Comparer());
        var rhs   = ~order;

        current =
          order < 0
          ? (rhs == current.children.Count ? null : current.children[rhs])
          : current.children[order];

        if (current is object) {
          previous = current;
        }
      }

      return previous;
    }

    public static Builder.Node VeryLast(this Builder.Node x) {
      if (x is null) { throw new ArgumentNullException(nameof(x)); }

      var previous = x?.children?.LastOrDefault();
      var last     = previous;

      while (last is object) {
        previous = last;
        last = last?.children?.LastOrDefault();
      }

      return previous;
    }

    // Standard binary search works only with the items with the same type,
    // i.e. collection and item must have the same base type.
    // So, we workaround it by wrapping our item in the type of the collection's elements.
    // And then we use comparer specially prepared for the workaround.
    private class Comparer : IComparer<Builder.Node> {
      public int Compare(Builder.Node a, Builder.Node b) =>
        a.begin <= b.begin && b.begin < a.end
        ? 0
        : a.end == b.begin && a.IsScope() && !a.IsClosedScope()
          ? 0
          : a.begin - b.begin;
    }
  }

  public struct Pair {
    public Builder.Node Left;
    public Builder.Node Right;

    public IEnumerable<Builder.Node> AsSequence() {
      yield return Left;
      yield return Right;
    }
  }

  public enum Strategy {
    NearLeftFarRight,
    NearLeftNearRight,
    FarLeftNearRight,
    FarLeftFarRight,
  }

  public static class NodeUtils {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(this Builder.Node node) => node.end - node.begin;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScope(this Builder.Node node) =>
      node.name == "Object" || node.name == "Code" || node.name == "List" || node.IsLabel();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScopeStart(this Builder.Node node) =>
      node.name == "'['" || node.name == "'{'" || node.name == "'('" || node.name == "':'";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScopeEnd(this Builder.Node node) =>
      node.name == "']'" || node.name == "'}'" || node.name == "')'" || node.name == "';'";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsClosedScope(this Builder.Node node) =>
      node.children.Last().IsScopeEnd();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEol(this Builder.Node node) =>
      node.name == "LF" || node.name == "CRLF" || node.name == "CR";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEof(this Builder.Node node) =>
      node.name == "EOF";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLabel(this Builder.Node node) =>
      node.name == "Label";

    public static Builder.Node LabelName(this Builder.Node node) {
      if (node is null) {
        throw new ArgumentNullException(nameof(node));
      }

      Debug.Assert(node.children.Any());

      var name = node.children.First();
      Debug.Assert(name.name == "Name");

      return name;
    }

    public static bool IsLooksLikeName(this Builder.Node a) =>
      a.IsName() ||
      a.IsNameMember() ||
      a.IsNameRead() ||
      a.IsNameReadMember() ||
      a.IsNameWrite() ||
      a.IsNameWriteMember();

    public static bool IsName(this Builder.Node node) {
      if (node is null) {
        throw new ArgumentNullException(nameof(node));
      }

      return node.name == "Name";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNameMember(this Builder.Node node) =>
      node.name == "NameMember";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNameRead(this Builder.Node node) =>
      node.name == "NameRead";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNameWrite(this Builder.Node node) =>
      node.name == "NameWrite";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNameReadMember(this Builder.Node node) =>
      node.name == "NameReadMember";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNameWriteMember(this Builder.Node node) =>
      node.name == "NameWriteMember";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumber(this Builder.Node node) =>
      node.name == "Number";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReal(this Builder.Node node) =>
      node.name == "Real";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsString(this Builder.Node node) =>
      node.name == "String";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComment(this Builder.Node node) =>
      node.name == "Comment";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOpeningBrace(this char ch) => Braces.ContainsKey(ch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsClosingBrace(this char ch) => Braces.ContainsValue(ch);

    public static readonly ImmutableDictionary<char, char> Braces = new Dictionary<char, char> {
      ['{'] = '}',
      ['['] = ']',
      ['('] = ')',
      [':'] = ';'
    }.ToImmutableDictionary();
  }
}