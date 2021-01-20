using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using MPLVS.ParseTree;

namespace MPLVS.Core.ParseTree {
  public static class Utils {
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
      while (IsNotEmpty(tree)) {
        tree = tree.OldestAncestor(position);

        if (tree is null) {
          yield break;
        }

        yield return tree;
      }

      bool IsNotEmpty(Builder.Node treeNode) =>
        !(treeNode.children is null) && treeNode.children.Any();
    }

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
      if (node is null) {
        throw new ArgumentNullException(nameof(node));
      }

      var idx = node.children.BinarySearch(new Builder.Node { begin = position },
                                               new Comparer());
      return
        idx < 0
        ? null
        : node.children[idx];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Builder.Node OldestAncestor(this Builder.Node node, int start, int end) =>
      node.AllAncestors(start, end).FirstOrDefault();

    // Standard binary search works only with the items with the same type,
    // i.e. collection and item must have the same base type.
    // So, we workaround it by wrapping our item in the type of the collection's elements.
    // And then we use comparer specially prepared for the workaround.
    private class Comparer : IComparer<Builder.Node> {
      public int Compare(Builder.Node a, Builder.Node b) =>
        b.begin >= a.begin && b.begin < a.end
        ? 0
        : a.begin - b.begin;
    }

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

    public static bool IsName(this Builder.Node node) {
      if (node is null) {
        throw new ArgumentNullException(nameof(node));
      }

      return node.name == "Name";
    }

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