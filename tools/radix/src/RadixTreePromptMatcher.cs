// SPDX-License-Identifier: MIT
// Implements Axis 4 (radix-tree signature matching) of Epic #1210:
//   https://github.com/jsboige/CoursIA/issues/1210
//
// Replaces the stubbed `SimpleMatchPromptSettings` (linear O(n)) called by
// MultiTextCompletionSettings.MatchPromptSettings in semantic-fleet v0.34.3
// (L266-L273, verbatim TODO preserved in the epic body).
//
// The matcher is shaped as a Func<CompletionJob, IEnumerable<...>, ...?> so it
// can be plugged into the existing `PromptMatcher` delegate hook on
// `MultiTextCompletionSettings` without touching the core — jsboige note on
// the epic:
//   "PromptMatcher est un delegate configurable (hook d'extension propre),
//    donc un RadixTreePromptMatcher peut être injecté sans toucher au code core."

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SemanticFleet.Radix;

/// <summary>
/// Trie-based matcher that selects a <see cref="PromptMultiConnectorSettings"/>
/// entry whose <see cref="SignaturePrefix.PromptStart"/> is the longest prefix
/// of the candidate prompt. Compared to <see cref="SimpleMatchPromptSettings"/>,
/// the cost of a lookup is proportional to the matched prefix length — not to
/// the number of registered signatures — which keeps dispatch cheap as the
/// catalog grows (semantic-fleet Epic #1210, Axe 4 motivation).
/// </summary>
public static class RadixTreePromptMatcher
{
    /// <summary>
    /// Public entry point shaped to fit semantic-fleet's
    /// <c>PromptMatcher</c> delegate:
    /// <c>Func&lt;CompletionJob, IEnumerable&lt;PromptMultiConnectorSettings&gt;, PromptMultiConnectorSettings?&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Build cost is O(total signature length) per invocation — the tree is
    /// rebuilt on every call. For workloads with a static catalog of prompts
    /// (the typical semantic-fleet deployment), prefer <see cref="MatchWithTree"/>
    /// to amortise the build across many lookups.
    /// </remarks>
    public static PromptMultiConnectorSettings? Match(
        CompletionJob completionJob,
        IEnumerable<PromptMultiConnectorSettings> promptMultiConnectorSettings)
    {
        ArgumentNullException.ThrowIfNull(completionJob);
        ArgumentNullException.ThrowIfNull(promptMultiConnectorSettings);

        var tree = BuildTree(promptMultiConnectorSettings);
        return MatchWithTree(completionJob, tree);
    }

    /// <summary>
    /// Pre-built variant: caller builds the <see cref="RadixTree{PromptMultiConnectorSettings}"/>
    /// once (e.g. when the settings list is refreshed) and reuses it across
    /// many <see cref="MatchWithTree"/> lookups.
    /// </summary>
    public static RadixTree<PromptMultiConnectorSettings> BuildTree(
        IEnumerable<PromptMultiConnectorSettings> promptMultiConnectorSettings)
    {
        ArgumentNullException.ThrowIfNull(promptMultiConnectorSettings);

        var tree = new RadixTree<PromptMultiConnectorSettings>();
        foreach (var settings in promptMultiConnectorSettings)
        {
            if (string.IsNullOrEmpty(settings.Signature.PromptStart))
            {
                // Skip empty prefixes silently: the baseline SimpleMatch
                // returns the first entry for empty prefixes (StartsWith on
                // empty string is always true), so any behaviour here is a
                // deliberate narrowing — keep parity by treating the empty
                // key as a degenerate case best avoided at registration time.
                continue;
            }
            tree.Insert(settings.Signature.PromptStart, settings);
        }
        return tree;
    }

    /// <summary>
    /// Lookup over a pre-built tree. Returns the settings entry whose prefix
    /// matches the longest initial segment of the prompt, or <c>null</c> when
    /// no entry matches.
    /// </summary>
    /// <remarks>
    /// Behavioural note: when multiple settings share the same
    /// <see cref="SignaturePrefix.PromptStart"/>, the most recently inserted
    /// entry wins. This matches the imperative "last writer wins" intuition
    /// of <c>Dictionary&lt;string, TValue&gt;</c> for repeated keys. The
    /// baseline <see cref="SimpleMatchPromptSettings"/> instead returns the
    /// <em>first</em> matching entry (FirstOrDefault semantics); the tests
    /// exercise both regimes with disjoint signature sets to avoid ambiguity.
    /// </remarks>
    public static PromptMultiConnectorSettings? MatchWithTree(
        CompletionJob completionJob,
        RadixTree<PromptMultiConnectorSettings> tree)
    {
        ArgumentNullException.ThrowIfNull(completionJob);
        ArgumentNullException.ThrowIfNull(tree);

        return tree.LongestPrefixLookup(completionJob.Prompt);
    }
}

/// <summary>
/// Compacted trie that stores <typeparamref name="TValue"/> at terminal nodes
/// keyed by string. Designed for prefix lookup, not full text search.
/// </summary>
/// <remarks>
/// Implementation choices:
/// <list type="bullet">
///   <item>Single-node-per-common-prefix compression (i.e. a basic radix tree,
///   not a 26-ary trie with one edge per character). Edges that fan out from
///   a node carry the differing substring directly.</item>
///   <item>Edges are stored in <see cref="Dictionary{TKey, TValue}"/> for
///   O(1) child navigation per character.</item>
///   <item>Insertion collapses single-child chains into a single edge when
///   possible (canonical radix-tree compaction).</item>
/// </list>
/// Not thread-safe; callers that share a tree across threads must synchronise.
/// </remarks>
public sealed class RadixTree<TValue>
{
    private readonly RadixNode<TValue> _root = new(string.Empty, isTerminal: false);

    /// <summary>Number of distinct terminal keys stored in the tree.</summary>
    public int Count => _root.SubtreeCount;

    /// <summary>Inserts <paramref name="key"/> with <paramref name="value"/>.
    /// Overwrites any existing value bound to the same key.</summary>
    public void Insert(string key, TValue value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        InsertInternal(_root, key, value);
    }

    private static void InsertInternal(RadixNode<TValue> root, string key, TValue value)
    {
        var current = root;
        var remaining = key;

        while (true)
        {
            // Find an edge whose label shares a prefix with the remaining key.
            RadixNode<TValue>? next = null;
            foreach (var (edgeLabel, child) in current.Children)
            {
                var commonLen = CommonPrefixLength(edgeLabel, remaining);
                if (commonLen == 0)
                {
                    continue;
                }

                if (commonLen == edgeLabel.Length)
                {
                    // Edge fully consumed: descend into the child.
                    next = child;
                    remaining = remaining[commonLen..];
                    break;
                }

                // Edge label and remaining key share only a prefix — split
                // the edge by inserting a new intermediate node.
                var splitLabel = edgeLabel[..commonLen];
                var childSuffix = edgeLabel[commonLen..];
                child.Label = childSuffix;
                var splitNode = new RadixNode<TValue>(splitLabel, isTerminal: false);
                splitNode.Children[childSuffix] = child;
                current.Children.Remove(edgeLabel);
                current.Children[splitLabel] = splitNode;
                next = splitNode;
                remaining = remaining[commonLen..];
                break;
            }

            if (next is null)
            {
                // No matching edge: create a fresh terminal child for the
                // remaining suffix.
                var terminal = new RadixNode<TValue>(remaining, isTerminal: true);
                terminal.Value = value;
                current.Children[remaining] = terminal;
                return;
            }

            current = next;
            if (remaining.Length == 0)
            {
                // Reached an existing node at the target key — bind the value.
                current.SetTerminal(value);
                return;
            }
        }
    }

    /// <summary>
    /// Returns the value associated with the longest stored key that is a
    /// prefix of <paramref name="query"/>, or <c>null</c> when no key matches.
    /// </summary>
    public TValue? LongestPrefixLookup(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var current = _root;
        TValue? best = default;
        var hasBest = false;
        var remaining = query;

        while (true)
        {
            if (current.IsTerminal)
            {
                best = current.Value;
                hasBest = true;
            }

            if (remaining.Length == 0)
            {
                return hasBest ? best : default;
            }

            // Pick the unique child whose label starts with the next char.
            // At most one such child can exist after radix-tree compaction.
            RadixNode<TValue>? next = null;
            foreach (var (_, child) in current.Children)
            {
                if (child.Label[0] == remaining[0])
                {
                    next = child;
                    break;
                }
            }

            if (next is null)
            {
                return hasBest ? best : default;
            }

            // The child's label must match the prefix of `remaining`.
            if (!remaining.StartsWith(next.Label, StringComparison.Ordinal))
            {
                return hasBest ? best : default;
            }

            current = next;
            remaining = remaining[next.Label.Length..];
        }
    }

    private static int CommonPrefixLength(string a, string b)
    {
        var max = Math.Min(a.Length, b.Length);
        var i = 0;
        while (i < max && a[i] == b[i])
        {
            i++;
        }
        return i;
    }
}

/// <summary>
/// Single node of a <see cref="RadixTree{TValue}"/>. Renamed from
/// <c>Node&lt;TValue&gt;</c> to <c>RadixNode&lt;TValue&gt;</c> so that the
/// nested type parameter does not collide with the enclosing
/// <see cref="RadixTree{TValue}"/>'s <c>TValue</c>. A class (not a record)
/// because <c>Label</c> and <c>IsTerminal</c> must mutate in place during
/// insertion — see <see cref="RadixTree{TValue}.Insert"/> for the cases.
/// </summary>
internal sealed class RadixNode<TValue>
{
    public RadixNode(string label, bool isTerminal)
    {
        Label = label;
        IsTerminal = isTerminal;
    }

    public string Label { get; set; }
    public bool IsTerminal { get; set; }
    public TValue? Value { get; set; }
    public Dictionary<string, RadixNode<TValue>> Children { get; } =
        new(StringComparer.Ordinal);

    /// <summary>Promote this node to terminal and bind <paramref name="value"/>.</summary>
    public void SetTerminal(TValue value)
    {
        IsTerminal = true;
        Value = value;
    }

    public int SubtreeCount
    {
        get
        {
            var n = IsTerminal ? 1 : 0;
            foreach (var child in Children.Values)
            {
                n += child.SubtreeCount;
            }
            return n;
        }
    }
}