using System;
using System.Collections.Generic;
using System.Text.Json;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;
using Microsoft.SemanticKernel;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace SemanticFleet.Benchmarks;

/// <summary>
/// Benchmarks of the MultiConnector public API hot paths: prompt signature matching,
/// prompt settings JSON serialization, request settings JSON round-trip and key interpolation.
/// Payloads are escape-heavy French text (quotes, backslashes, newlines, accents) since
/// System.Text.Json escapes non-ASCII by default.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessShortConfig))]
public class PromptSettingsBenchmarks
{
    private const string EscapeHeavyPrompt =
        "Analyse le corpus suivant (extrait « brutes » du client) :\n"
        + "\t- demande : « résume l\\'accord de service, sigle \\\"SLA\\\", en français »\n"
        + "\t- contraintes : réponse JSON stricte, accents éàçüÿ préservés, pas d'anglicisme\n"
        + "\t- historique : 42 échanges antérieurs, délai contractuel <= 5 secondes\n"
        + "\t- contexte métier : secteur bancaire, réglementation européenne, clause de réversibilité\n";

    private PromptSignature _signature = null!;
    private CompletionJob _job = default;
    private PromptMultiConnectorSettings _promptSettings = null!;
    private PromptExecutionSettings _baseSettings = null!;
    private PromptTransform _transform = null!;
    private Dictionary<string, object> _interpolationContext = null!;

    // --- Per-operation instrument (OperationsPerInvoke on rotating, consumed arguments) ---
    // Design validated by the N-scaling campaign on jsboige/CoursIA#18695: per-op cost is
    // stable from N=1 to N=10 000 (ratios 0.92-0.99) and BDN's "Allocated" column is
    // per operation, so a flat allocation across N is the signature of real (not folded)
    // work. Rotation over 8 distinct payloads plus consumption into a static sink prevents
    // result caching; each variant marker sits past the 100-char PromptStart so signature
    // matching keeps exercising the same StartsWith path.
    private const int RotationSize = 8;
    private const int RotationMask = RotationSize - 1;
    private const int SmallOpsPerInvoke = 1_000;
    private const int LargeOpsPerInvoke = 10_000;

    private static bool s_boolSink;
    private static long s_longSink;

    private readonly CompletionJob[] _rotatingJobs = new CompletionJob[RotationSize];
    private readonly PromptExecutionSettings[] _rotatingSettings = new PromptExecutionSettings[RotationSize];
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

        _job = new CompletionJob(EscapeHeavyPrompt, new PromptExecutionSettings
        {
            ModelId = "test-model",
            ServiceId = "multi",
        });

        // Persistence path: PromptMultiConnectorSettings as saved to the settings file.
        _promptSettings = new PromptMultiConnectorSettings
        {
            PromptType = new PromptType
            {
                PromptName = "résumé-accord-SLA",
                Signature = _signature,
            },
        };
        for (int i = 0; i < 5; i++)
        {
            _promptSettings.PromptType.Instances.Add(EscapeHeavyPrompt);
        }

        PromptConnectorSettings connectorSettings = _promptSettings.GetConnectorSettings("oobabooga-local");
        connectorSettings.VettingLevel = VettingLevel.OracleVaried;
        connectorSettings.VettingConnector = "oobabooga-local";
        connectorSettings.AverageCost = 0.00042m;
        connectorSettings.PromptConnectorTypeTransform = new PromptTransform
        {
            Template = "Vous êtes {role}.\n{input}",
        };

        // Request settings conversion path (per completion call): FromRequestSettings on a base
        // PromptExecutionSettings triggers the JsonSerializer.Serialize -> Deserialize round-trip.
        _baseSettings = new PromptExecutionSettings
        {
            ModelId = "test-model",
            ServiceId = "multi",
            ExtensionData = new Dictionary<string, object>
            {
                ["temperature"] = 0.7,
                ["max_tokens"] = 512,
                ["stop"] = EscapeHeavyPrompt,
                ["system_prompt"] = EscapeHeavyPrompt,
            },
        };

        // Key interpolation path: regex-based {key} replacement with escape-heavy values.
        _transform = new PromptTransform
        {
            Template = "Vous êtes {role}, expert en {domain}.\nStyle attendu : {style}\nInstructions : \"réponse structurée, accents éàç préservés\".\nCorpus :\n{corpus}",
        };
        _interpolationContext = new Dictionary<string, object>
        {
            ["role"] = "analyste senior",
            ["domain"] = "accords de service (SLA)",
            ["style"] = EscapeHeavyPrompt,
            ["corpus"] = string.Concat(System.Linq.Enumerable.Repeat(EscapeHeavyPrompt, 4)),
        };

        // Rotating payloads for the per-op instrument. The variant marker is appended after
        // the 100-char PromptStart, so signature matching keeps exercising the same
        // StartsWith path while every entry is a distinct string instance.
        for (int i = 0; i < RotationSize; i++)
        {
            string variantPrompt = EscapeHeavyPrompt + $" [variant {i}]";

            _rotatingJobs[i] = new CompletionJob(variantPrompt, new PromptExecutionSettings
            {
                ModelId = "test-model",
                ServiceId = "multi",
            });

            _rotatingSettings[i] = new PromptExecutionSettings
            {
                ModelId = "test-model",
                ServiceId = "multi",
                ExtensionData = new Dictionary<string, object>
                {
                    ["temperature"] = 0.7,
                    ["max_tokens"] = 512,
                    ["stop"] = variantPrompt,
                    ["system_prompt"] = variantPrompt,
                },
            };

            _rotatingContexts[i] = new Dictionary<string, object>(_interpolationContext)
            {
                ["corpus"] = string.Concat(System.Linq.Enumerable.Repeat(variantPrompt, 4)),
            };
        }
    }

    [Benchmark(Baseline = true, Description = "PromptSignature.Matches (StartsWith, per completion call)")]
    public bool Signature_Matches() => _signature.Matches(_job);

    [Benchmark(Description = "JsonSerializer.Serialize(PromptMultiConnectorSettings) (persistence write)")]
    public string JsonSerialize_PromptSettings() => JsonSerializer.Serialize(_promptSettings);

    [Benchmark(Description = "FromRequestSettings (Serialize + Deserialize round-trip)")]
    public MultiCompletionRequestSettings RequestSettings_RoundTrip() =>
        MultiCompletionRequestSettings.FromRequestSettings(_baseSettings);

    [Benchmark(Description = "PromptTransform.InterpolateKeys (regex {key} replacement)")]
    public string Transform_InterpolateKeys() => _transform.InterpolateKeys(_transform.Template, _interpolationContext);

    // Per-op instrument variants. OperationsPerInvoke divides the reported time and the
    // reported Allocated by N, so the loop overhead (counter + array index) is amortized
    // and an iteration fills BDN's time target even for sub-microsecond operations. The
    // per-op results are comparable with the single-shot benchmarks above: same code
    // paths, same payloads modulo the variant marker.

    [Benchmark(Description = "PromptSignature.Matches x10 000 (per-op instrument, rotating payload)", OperationsPerInvoke = LargeOpsPerInvoke)]
    public bool Signature_Matches_PerOp()
    {
        bool acc = false;
        for (int i = 0; i < LargeOpsPerInvoke; i++)
        {
            acc ^= _signature.Matches(_rotatingJobs[_rotationIndex++ & RotationMask]);
        }
        s_boolSink ^= acc;
        return acc;
    }

    [Benchmark(Description = "JsonSerializer.Serialize(PromptMultiConnectorSettings) x1 000 (per-op instrument)", OperationsPerInvoke = SmallOpsPerInvoke)]
    public long JsonSerialize_PerOp()
    {
        long totalLength = 0;
        for (int i = 0; i < SmallOpsPerInvoke; i++)
        {
            totalLength += JsonSerializer.Serialize(_promptSettings).Length;
        }
        s_longSink += totalLength;
        return totalLength;
    }

    [Benchmark(Description = "FromRequestSettings x1 000 (per-op instrument, rotating payload)", OperationsPerInvoke = SmallOpsPerInvoke)]
    public long RequestSettings_RoundTrip_PerOp()
    {
        long consumed = 0;
        for (int i = 0; i < SmallOpsPerInvoke; i++)
        {
            consumed += MultiCompletionRequestSettings.FromRequestSettings(
                _rotatingSettings[_rotationIndex++ & RotationMask]).GetHashCode();
        }
        s_longSink += consumed;
        return consumed;
    }

    [Benchmark(Description = "PromptTransform.InterpolateKeys x10 000 (per-op instrument, rotating context)", OperationsPerInvoke = LargeOpsPerInvoke)]
    public long Transform_InterpolateKeys_PerOp()
    {
        long totalLength = 0;
        for (int i = 0; i < LargeOpsPerInvoke; i++)
        {
            totalLength += _transform.InterpolateKeys(
                _transform.Template, _rotatingContexts[_rotationIndex++ & RotationMask]).Length;
        }
        s_longSink += totalLength;
        return totalLength;
    }
}

/// <summary>
/// In-process config for Windows machines where the out-of-process benchmark child process
/// gets blocked by antivirus/Defender (exit code -2147450730). ShortRun job with enough
/// iterations to keep the relative error in the 10-20% range.
/// </summary>
public class InProcessShortConfig : ManualConfig
{
    public InProcessShortConfig()
    {
        // Multi-runtime (mesure du delta runtime, CoursIA #18771) : InProcess ne peut
        // cibler qu'un runtime par run -- quand DOTNET_MULTI_RUNTIME=1, on n'ajoute PAS
        // le job InProcess et BDN utilise son job par defaut (process enfant) avec
        // `--runtimes netX.0`. Le protocole de reference (baseline net9) reste
        // le job InProcess ci-dessous.
        if (Environment.GetEnvironmentVariable("DOTNET_MULTI_RUNTIME") == "1")
            return;

        // Invocation count is left on auto so each iteration carries enough invocations
        // to fill BDN's documented minimum iteration time (100 ms). The previous
        // WithInvocationCount(1) pin starved iterations to a single invocation (measured
        // 3.7-90 us per iteration on jsboige/CoursIA#18695), which produced the 16-40 %
        // intra-version spread that made runtime-delta runs INCONCLUSIVE. UnrollFactor(1)
        // stays so per-invocation work is never batched; the per-op benchmarks above
        // carry OperationsPerInvoke on rotating consumed arguments instead.
        AddJob(Job.ShortRun
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithUnrollFactor(1)
            .WithWarmupCount(5)
            .WithIterationCount(15)
            .WithIterationTime(TimeInterval.FromMilliseconds(100)));
    }
}
