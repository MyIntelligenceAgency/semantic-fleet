# SemanticFleet.Radix

Self-contained .NET module implementing **Axis 4 (radix-tree signature
matching)** of [Epic #1210](https://github.com/jsboige/CoursIA/issues/1210).

It is deliberately kept outside `dotnet/` — it carries no dependency on the
`MultiConnector` assembly and mirrors the few surface types it needs, so it
builds and tests on its own. See
[Where this module lives](#where-this-module-lives).

## Why this module exists

`MultiTextCompletionSettings.MatchPromptSettings` ships a TODO
(`MultiTextCompletionSettings.cs:266-270`) describing a planned radix-tree /
trie matcher to replace the linear scan in `SimpleMatchPromptSettings`
(same file, line 281):

```csharp
//TODO: Optionally allow for faster prompt matching with a pre-computation cost
//when prefix settings were tested and configured.
//   * build a RadixTree<string, char, TValue> --> Trie<string, char, TValue> --> ...
```

```csharp
public static PromptMultiConnectorSettings? SimpleMatchPromptSettings(
    CompletionJob completionJob,
    IEnumerable<PromptMultiConnectorSettings> promptMultiConnectorSettings)
{
    return promptMultiConnectorSettings.FirstOrDefault(
        s => s.PromptType.Signature.Matches(completionJob));
}
```

That scan is `O(n)` in the number of registered signatures and is fine for a
handful of them, but becomes the dominant cost as the catalogue of
`PromptMultiConnectorSettings` grows. This module supplies a **radix-tree**
implementation behind the same delegate signature.

Note that `Signature.Matches` does strictly more than prefix comparison — see
[Honest gap vs the current upstream baseline](#honest-gap-vs-the-current-upstream-baseline)
before swapping the matcher wholesale.

## How it plugs in

`MultiTextCompletionSettings.PromptMatcher` is a delegate hook:

```csharp
public Func<CompletionJob, IEnumerable<PromptMultiConnectorSettings>, PromptMultiConnectorSettings?>
    PromptMatcher { get; set; } = SimpleMatchPromptSettings;
```

To use the radix-tree matcher:

```csharp
using SemanticFleet.Radix;

settings.PromptMatcher = RadixTreePromptMatcher.Match;
```

The hook is `{ get; set; }`, so opting in requires no change to the
`MultiConnector` core.

## API

| Entry point | Purpose |
|-------------|---------|
| `RadixTreePromptMatcher.Match(job, settings)` | Build-then-lookup, single call. Convenience for small catalogues. |
| `RadixTreePromptMatcher.BuildTree(settings)` | Build a reusable tree once. |
| `RadixTreePromptMatcher.MatchWithTree(job, tree)` | Lookup against a pre-built tree. Recommended for hot paths. |
| `RadixTree<TValue>` | Generic radix tree — usable beyond `PromptMatcher` for any prefix-lookup workload. |

## Complexity

| Matcher | Build | Lookup |
|---------|-------|--------|
| `SimpleMatchPromptSettings` (linear) | O(1) | O(n × average-prefix-length) |
| `RadixTreePromptMatcher.Match` | O(total signature length) | O(matched-prefix-length) |
| `RadixTreePromptMatcher.MatchWithTree` | O(total signature length) once | O(matched-prefix-length) |

`n` = number of registered signatures, `matched-prefix-length` ≤ `query.Length`.

For the typical semantic-fleet workload (a few hundred to a few thousand
signatures, dominated by repeated lookups on the same catalogue), prefer the
**two-step `BuildTree` + `MatchWithTree`** pattern.

## Behavioural parity

Tests in
[`tests/RadixTreePromptMatcherTests.cs`](tests/RadixTreePromptMatcherTests.cs)
exercise parity with `SimpleMatchPromptSettings` on disjoint signature sets
where both algorithms pick the same entry. Where prefixes overlap, the
radix-tree matcher returns the entry with the **longest matching prefix** —
matching the literal TODO's "longest prefix" language.

When multiple settings share the same `SignaturePrefix.PromptStart`, the
radix-tree matcher keeps the **most recently inserted** value (last-writer
wins, analogous to `Dictionary<string, TValue>`). The linear baseline returns
the **first** matching entry (`FirstOrDefault`). The test suite avoids this
ambiguity by exercising disjoint signature sets.

Empty `PromptStart` values are **skipped silently** at `BuildTree` time: registering
an empty prefix is a degenerate case the baseline never refuses, so we narrow
the matcher contract rather than introducing a hard failure for behaviour nobody
should rely on. At the lower level, `RadixTree<TValue>.Insert` itself refuses
empty keys outright via `ArgumentException.ThrowIfNullOrEmpty` — the skip is
the matcher's accommodation of the baseline's lax behaviour.

## Building & testing

```bash
dotnet build tools/radix/src
dotnet test  tools/radix/tests
```

The `BenchmarkRadixVsLinear` xUnit fact is marked `[Fact(Skip = ...)]` — it
is a micro-benchmark (not a unit test) and is excluded from `dotnet test`.
Run it manually with:

```bash
dotnet test tools/radix/tests \
    --filter "FullyQualifiedName~BenchmarkRadixVsLinear" \
    --logger "console;verbosity=detailed"
```

## Where this module lives

This module now lives **inside `semantic-fleet` itself**, at `tools/radix/`.

It was originally authored as a standalone module in a downstream consumer
(`CoursIA`, at `tools/semantic-fleet-radix/`) because contributing here
required a fork at the time. That constraint is gone — `semantic-fleet` is
consumed as a submodule and committed to directly — so the module was moved
to its natural home. The sources are unchanged by the move; only this README
was adapted (paths, this section, and the gap section below).

It remains **Axis 4 only**. Axes 1-3 of
[Epic #1210](https://github.com/jsboige/CoursIA/issues/1210) (signature
sampling, hybrid dictionary, wiring it in as the default) stay out of scope.
It is **not** wired in: `MultiTextCompletionSettings.PromptMatcher`
(`dotnet/src/Connectors/Connectors.AI.MultiConnector/MultiTextCompletionSettings.cs:131`)
still defaults to `SimpleMatchPromptSettings`. Opting in is one assignment,
subject to the gap below.

## Honest gap vs the current upstream baseline

**This matcher is a faster front-filter, not a drop-in replacement.**

The baseline it was written against has since moved. Today
`SimpleMatchPromptSettings`
(`MultiTextCompletionSettings.cs:281`) reads:

```csharp
promptMultiConnectorSettings.FirstOrDefault(s => s.PromptType.Signature.Matches(completionJob))
```

and `PromptSignature.Matches`
(`PromptSettings/PromptSignature.cs:168`) is a **conjunction of three checks**:

```csharp
this.MatchSettings(completionJob.RequestSettings)          // ModelId + ServiceId
&& (this.CompiledRegex?.IsMatch(completionJob.Prompt)      // regex when configured
    ?? completionJob.Prompt.StartsWith(this.PromptStart))  // prefix fallback
```

A radix tree keyed on prompt prefixes can reproduce **only the third leg**. It
cannot evaluate `CompiledRegex`, and it cannot check request settings
(`ModelId` / `ServiceId`) — those are not prefix-decidable. Consequences:

| Signature shape | Radix matcher verdict |
|---|---|
| `PromptStart` only, distinct `RequestSettings` irrelevant | equivalent to the baseline |
| `CompiledRegex` configured | **may return a signature the baseline would reject** |
| Same prefix, different `ModelId`/`ServiceId` | **may return the wrong connector** |

Relatedly, this module's `PromptMultiConnectorSettings(SignaturePrefix Signature)`
is a **simplified surrogate** of the upstream record: upstream reaches the
signature through `s.PromptType.Signature` and carries connector routing, retry
policy, and cost data this module does not model.

So the safe use today is a **hybrid**: radix lookup to narrow the candidate set
by prefix, then `Signature.Matches` on the survivors to preserve exact
semantics. That hybrid is not implemented here — it is the natural follow-up,
and until it exists, swapping `PromptMatcher` wholesale is only sound for
catalogues that use prefixes alone.

## License

MIT — extracted verbatim surface types from semantic-fleet v0.34.3 are
referenced for compatibility only; all new code (the radix tree and matcher)
is original to this module.
