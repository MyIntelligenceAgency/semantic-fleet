// SPDX-License-Identifier: MIT
// Micro-benchmark comparing the radix-tree matcher against the linear O(n)
// baseline. Not a substitute for BenchmarkDotNet, but a quick sanity check
// that confirms the asymptotic improvement holds in practice.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace SemanticFleet.Radix.Tests;

public class BenchmarkRadixVsLinear
{
    private readonly ITestOutputHelper _output;

    public BenchmarkRadixVsLinear(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Micro-benchmark, run manually via dotnet run --project tests --filter BenchmarkRadixVsLinear")]
    public void CompareLookupLatency_AcrossCatalogSizes()
    {
        // Synthesise catalogue sizes from 100 to 50_000 entries.
        int[] sizes = { 100, 1_000, 10_000, 50_000 };
        int iterations = 10_000;

        foreach (var n in sizes)
        {
            var (settings, queries) = SynthesiseCatalog(n);

            // Warm up both paths.
            for (int i = 0; i < 100; i++)
            {
                SimpleMatchPromptSettings.Match(new CompletionJob(queries[0]), settings);
                RadixTreePromptMatcher.Match(new CompletionJob(queries[0]), settings);
            }

            var linearSw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                SimpleMatchPromptSettings.Match(new CompletionJob(queries[i % queries.Length]), settings);
            }
            linearSw.Stop();

            var radixSw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                RadixTreePromptMatcher.Match(new CompletionJob(queries[i % queries.Length]), settings);
            }
            radixSw.Stop();

            double linearMs = linearSw.Elapsed.TotalMilliseconds;
            double radixMs = radixSw.Elapsed.TotalMilliseconds;
            _output.WriteLine(
                $"n={n,6} | linear {linearMs,8:0.00} ms | radix {radixMs,8:0.00} ms | speedup x{linearMs / Math.Max(radixMs, 0.001),5:0.0}");
        }
    }

    private static (PromptMultiConnectorSettings[] settings, string[] queries) SynthesiseCatalog(int n)
    {
        // Deterministic pseudo-random prefixes so re-runs are comparable.
        var rng = new Random(42);
        var prefixes = new string[n];
        for (int i = 0; i < n; i++)
        {
            var len = 8 + rng.Next(24); // 8..31 char prefixes
            var buf = new char[len];
            for (int j = 0; j < len; j++)
            {
                buf[j] = (char)('a' + rng.Next(26));
            }
            prefixes[i] = new string(buf);
        }

        var settings = prefixes
            .Select(p => new PromptMultiConnectorSettings(new SignaturePrefix(p)))
            .ToArray();

        // Each query appends a few extra characters to one of the prefixes.
        var queries = new string[n];
        for (int i = 0; i < n; i++)
        {
            queries[i] = prefixes[i] + "_suffix_" + i;
        }

        return (settings, queries);
    }
}