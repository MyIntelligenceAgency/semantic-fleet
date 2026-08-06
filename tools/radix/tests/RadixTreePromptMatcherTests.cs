// SPDX-License-Identifier: MIT
// Unit tests for RadixTreePromptMatcher + parity checks vs the verbatim
// SimpleMatchPromptSettings baseline (semantic-fleet v0.34.3 L283-292).

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SemanticFleet.Radix.Tests;

public class RadixTreePromptMatcherTests
{
    private static PromptMultiConnectorSettings S(string prefix) =>
        new(new SignaturePrefix(prefix));

    [Fact]
    public void NullJob_Throws()
    {
        var settings = new[] { S("hello") };
        Assert.Throws<ArgumentNullException>(() =>
            RadixTreePromptMatcher.Match(null!, settings));
    }

    [Fact]
    public void NullSettings_Throws()
    {
        var job = new CompletionJob("hello world");
        Assert.Throws<ArgumentNullException>(() =>
            RadixTreePromptMatcher.Match(job, null!));
    }

    [Fact]
    public void EmptyCatalog_ReturnsNull()
    {
        var job = new CompletionJob("anything");
        Assert.Null(RadixTreePromptMatcher.Match(job, Array.Empty<PromptMultiConnectorSettings>()));
    }

    [Fact]
    public void NoMatchingPrefix_ReturnsNull()
    {
        var settings = new[] { S("foo"), S("bar"), S("baz") };
        var job = new CompletionJob("qux quux");
        Assert.Null(RadixTreePromptMatcher.Match(job, settings));
    }

    [Fact]
    public void ExactPrefixMatch_ReturnsEntry()
    {
        var settings = new[] { S("hello") };
        var job = new CompletionJob("hello world");
        var match = RadixTreePromptMatcher.Match(job, settings);
        Assert.NotNull(match);
        Assert.Equal("hello", match!.Signature.PromptStart);
    }

    [Fact]
    public void LongestPrefixWins_DisjointSignatures()
    {
        var settings = new[]
        {
            S("h"),
            S("he"),
            S("hel"),
            S("hell"),
            S("hello"),
        };
        var job = new CompletionJob("hello world");
        var match = RadixTreePromptMatcher.Match(job, settings);
        Assert.NotNull(match);
        Assert.Equal("hello", match!.Signature.PromptStart);
    }

    [Fact]
    public void LongestPrefixWins_BranchingSignatures()
    {
        var settings = new[]
        {
            S("Translate the following to French"),
            S("Translate the following to"),
            S("Summarise the following"),
        };
        var job = new CompletionJob("Translate the following to German: hi");
        var match = RadixTreePromptMatcher.Match(job, settings);
        Assert.NotNull(match);
        Assert.Equal("Translate the following to", match!.Signature.PromptStart);
    }

    [Fact]
    public void EmptyPrefix_SkippedSilently()
    {
        var settings = new[]
        {
            S(""),
            S("foo"),
        };
        var job = new CompletionJob("foobar");
        var match = RadixTreePromptMatcher.Match(job, settings);
        Assert.NotNull(match);
        Assert.Equal("foo", match!.Signature.PromptStart);
    }

    [Fact]
    public void EmptyQuery_NoMatch()
    {
        var settings = new[] { S("a"), S("b") };
        var job = new CompletionJob(string.Empty);
        // Empty query has no characters to descend the tree on — no match.
        Assert.Null(RadixTreePromptMatcher.Match(job, settings));
    }

    [Fact]
    public void EmptyQuery_MatchesEmptyPrefixIfRegistered()
    {
        // The RadixTree public API refuses empty keys at Insert time — see
        // ArgumentException.ThrowIfNullOrEmpty. Document the contract here so
        // a future regression that loosens the guard trips the test.
        var tree = new RadixTree<int>();
        Assert.Throws<ArgumentException>(() => tree.Insert(string.Empty, 42));
    }

    [Fact]
    public void QueryShorterThanLongestPrefix_ReturnsBestTerminal()
    {
        var settings = new[]
        {
            S("hello world"),
            S("hello"),
        };
        var job = new CompletionJob("hello");
        var match = RadixTreePromptMatcher.Match(job, settings);
        Assert.NotNull(match);
        // "hello world" cannot match a shorter query — "hello" is the longest
        // stored prefix that matches.
        Assert.Equal("hello", match!.Signature.PromptStart);
    }

    [Fact]
    public void ParityWithLinearBaseline_DisjointSignatures()
    {
        // Compare the radix-tree result against the verbatim
        // SimpleMatchPromptSettings on a catalogue with no prefix overlaps.
        // When prefixes are disjoint, both algorithms pick the same entry.
        var settings = new[]
        {
            S("Translate to FR:"),
            S("Translate to EN:"),
            S("Summarise:"),
            S("Classify:"),
            S("Extract entities:"),
        };
        var prompts = new[]
        {
            "Translate to FR: bonjour le monde",
            "Translate to EN: hello world",
            "Summarise: long article goes here...",
            "Classify: positive sentiment ahead",
            "Extract entities: Apple, Microsoft, Google",
            "Unrelated prompt",
        };

        foreach (var prompt in prompts)
        {
            var job = new CompletionJob(prompt);
            var baseline = SimpleMatchPromptSettings.Match(job, settings);
            var radix = RadixTreePromptMatcher.Match(job, settings);
            Assert.Equal(baseline?.Signature.PromptStart,
                         radix?.Signature.PromptStart);
        }
    }

    [Fact]
    public void RadixTree_CountReflectsDistinctKeys()
    {
        var tree = new RadixTree<int>();
        Assert.Equal(0, tree.Count);
        tree.Insert("hello", 1);
        Assert.Equal(1, tree.Count);
        tree.Insert("help", 2);
        Assert.Equal(2, tree.Count);
        // Re-inserting the same key overwrites but does not increase Count.
        tree.Insert("hello", 99);
        Assert.Equal(2, tree.Count);
    }

    [Fact]
    public void BuildTreeThenMatch_AmortisesBuild()
    {
        // Demonstrates the recommended pattern: build once, match many.
        var settings = new[]
        {
            S("Translate to FR:"),
            S("Translate to EN:"),
            S("Summarise:"),
        };
        var tree = RadixTreePromptMatcher.BuildTree(settings);

        var cases = new (string prompt, string expected)[]
        {
            ("Translate to FR: salut", "Translate to FR:"),
            ("Translate to EN: hi", "Translate to EN:"),
            ("Summarise: long text", "Summarise:"),
            ("No match here", null!),
        };

        foreach (var (prompt, expected) in cases)
        {
            var job = new CompletionJob(prompt);
            var match = RadixTreePromptMatcher.MatchWithTree(job, tree);
            Assert.Equal(expected, match?.Signature.PromptStart);
        }
    }

    [Fact]
    public void UnicodePromptStart_MatchesExactly()
    {
        var settings = new[] { S("Évaluer la qualité de") };
        var job = new CompletionJob("Évaluer la qualité de la traduction");
        var match = RadixTreePromptMatcher.Match(job, settings);
        Assert.NotNull(match);
        Assert.Equal("Évaluer la qualité de", match!.Signature.PromptStart);
    }

    [Fact]
    public void LookalikeButDifferentCodePoint_NoFalseMatch()
    {
        // Two prompts that share a prefix but differ on a non-ASCII character.
        var settings = new[] { S("café au lait") };
        var job = new CompletionJob("café au chocolates"); // NOT a prefix match.
        var match = RadixTreePromptMatcher.Match(job, settings);
        // "café au lait" is NOT a prefix of "café au chocolates", so no match.
        Assert.Null(match);
    }
}