using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.SemanticKernel;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace SemanticFleet.Benchmarks;

/// <summary>
/// CoursIA #25635 — the N-scaling regime test that <c>docs/dotnet/net11-runtime-deltas.md</c>
/// names as the missing measurement: <c>OperationsPerInvoke</c> fixed and growing on the SAME
/// call (N = 1 / 100 / 10 000) over the two sub-100 us hot cells, <c>PromptSignature.Matches</c>
/// and <c>PromptTransform.InterpolateKeys</c>.
///
/// Design: three twin benchmarks per cell (one per N), one class, one dataset. BDN divides the
/// reported Mean and Allocated by N, so each row is a per-op figure. The reading criterion:
/// if the per-op Mean DECREASES as N grows, the "fixed cost per iteration" hypothesis is
/// supported (a fixed per-invocation overhead is being amortized); if it stays STABLE across N,
/// the "real cost" hypothesis is. A verdict is SEPARATED when the [min, max] of the 5-pass
/// means of two disjoint N do not overlap; otherwise it is "non etabli", which is a publishable
/// result in its own right. No mechanism is asserted without the measurement.
///
/// The rotation over 8 distinct payloads plus consumption into a static sink (the same
/// anti-cache design as <see cref="PromptSettingsBenchmarks"/> per-op instrument) keeps every
/// entry a distinct string instance so the compiler cannot fold the loop; each variant marker
/// sits past the 100-char PromptStart so signature matching keeps the same StartsWith path.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessShortConfig))]
public class OperationsPerInvokeRegimeBenchmarks
{
    private const int RotationSize = 8;
    private const int RotationMask = RotationSize - 1;

    private const string EscapeHeavyPrompt =
        "Analyse le corpus suivant (extrait « brutes » du client) :\n"
        + "\t- demande : « résume l\\'accord de service, sigle \\\"SLA\\\", en français »\n"
        + "\t- contraintes : réponse JSON stricte, accents éàçüÿ préservés, pas d'anglicisme\n"
        + "\t- historique : 42 échanges antérieurs, délai contractuel <= 5 secondes\n"
        + "\t- contexte métier : secteur bancaire, réglementation européenne, clause de réversibilité\n";

    private static bool s_boolSink;
    private static long s_longSink;

    private PromptSignature _signature = null!;
    private PromptTransform _transform = null!;
    private Dictionary<string, object> _context = null!;

    private readonly CompletionJob[] _rotatingJobs = new CompletionJob[RotationSize];
    private readonly Dictionary<string, object>[] _rotatingContexts = new Dictionary<string, object>[RotationSize];
    private int _rotationIndex;

    [GlobalSetup]
    public void Setup()
    {
        // Signature matching path (per completion call): signature without regex, StartsWith path.
        _signature = new PromptSignature
        {
            PromptStart = EscapeHeavyPrompt.Substring(0, 100),
            RequestSettings = new PromptExecutionSettings
            {
                ModelId = "test-model",
                ServiceId = "multi",
            },
        };

        // Key interpolation path: regex-based {key} replacement with escape-heavy values.
        _transform = new PromptTransform
        {
            Template = "Vous êtes {role}, expert en {domain}.\nStyle attendu : {style}\nInstructions : \"réponse structurée, accents éàç préservées\".\nCorpus :\n{corpus}",
        };
        _context = new Dictionary<string, object>
        {
            ["role"] = "analyste senior",
            ["domain"] = "accords de service (SLA)",
            ["style"] = EscapeHeavyPrompt,
            ["corpus"] = string.Concat(System.Linq.Enumerable.Repeat(EscapeHeavyPrompt, 4)),
        };

        // Rotating payloads: the variant marker is appended after the 100-char PromptStart, so
        // signature matching keeps the same StartsWith path while every entry is a distinct
        // string instance (no loop folding).
        for (int i = 0; i < RotationSize; i++)
        {
            string variant = EscapeHeavyPrompt + $" [variant {i}]";
            _rotatingJobs[i] = new CompletionJob(variant, new PromptExecutionSettings
            {
                ModelId = "test-model",
                ServiceId = "multi",
            });
            _rotatingContexts[i] = new Dictionary<string, object>(_context)
            {
                ["corpus"] = string.Concat(System.Linq.Enumerable.Repeat(variant, 4)),
            };
        }
    }

    // --- N = 1 : a single invocation. Any fixed per-iteration overhead IS the whole cost here. ---

    [Benchmark(Description = "Signature.Matches N=1 (regime)", OperationsPerInvoke = 1)]
    public bool SigMatch_N001()
    {
        bool acc = false;
        for (int i = 0; i < 1; i++)
            acc ^= _signature.Matches(_rotatingJobs[_rotationIndex++ & RotationMask]);
        s_boolSink ^= acc;
        return acc;
    }

    [Benchmark(Description = "InterpolateKeys N=1 (regime)", OperationsPerInvoke = 1)]
    public long InterpKeys_N001()
    {
        long len = 0;
        for (int i = 0; i < 1; i++)
            len += _transform.InterpolateKeys(_transform.Template, _rotatingContexts[_rotationIndex++ & RotationMask]).Length;
        s_longSink += len;
        return len;
    }

    // --- N = 100 : a fixed per-invocation overhead would be amortized 100x and start to show. ---

    [Benchmark(Description = "Signature.Matches N=100 (regime)", OperationsPerInvoke = 100)]
    public bool SigMatch_N100()
    {
        bool acc = false;
        for (int i = 0; i < 100; i++)
            acc ^= _signature.Matches(_rotatingJobs[_rotationIndex++ & RotationMask]);
        s_boolSink ^= acc;
        return acc;
    }

    [Benchmark(Description = "InterpolateKeys N=100 (regime)", OperationsPerInvoke = 100)]
    public long InterpKeys_N100()
    {
        long len = 0;
        for (int i = 0; i < 100; i++)
            len += _transform.InterpolateKeys(_transform.Template, _rotatingContexts[_rotationIndex++ & RotationMask]).Length;
        s_longSink += len;
        return len;
    }

    // --- N = 10 000 : the amortization limit; a fixed overhead is fully spread, real cost remains. ---

    [Benchmark(Description = "Signature.Matches N=10000 (regime)", OperationsPerInvoke = 10_000)]
    public bool SigMatch_N10000()
    {
        bool acc = false;
        for (int i = 0; i < 10_000; i++)
            acc ^= _signature.Matches(_rotatingJobs[_rotationIndex++ & RotationMask]);
        s_boolSink ^= acc;
        return acc;
    }

    [Benchmark(Description = "InterpolateKeys N=10000 (regime)", OperationsPerInvoke = 10_000)]
    public long InterpKeys_N10000()
    {
        long len = 0;
        for (int i = 0; i < 10_000; i++)
            len += _transform.InterpolateKeys(_transform.Template, _rotatingContexts[_rotationIndex++ & RotationMask]).Length;
        s_longSink += len;
        return len;
    }
}
