using System.Collections.Generic;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.SemanticKernel;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace SemanticFleet.Benchmarks;

/// <summary>
/// Échelle de comptage d'invocations : le même appel, appelé N fois dans une même région
/// mesurée, pour N = 1 / 100 / 10 000.
///
/// <para><b>Ce que cet instrument est, et ce qu'il n'est pas.</b> Il est <b>complémentaire</b>
/// des bancs « per-op » introduits en amont (PR #108) : ceux-ci utilisent
/// <c>OperationsPerInvoke</c> — N opérations exécutées <i>à l'intérieur</i> d'un seul appel
/// de banc, sur arguments tournants consommés. Le présent instrument utilise
/// <c>InvocationCount</c> — N <i>appels</i> de banc distincts, faits par BDN dans une même
/// région mesurée, dont le temps total est divisé par N. Les deux ne mesurent pas la même
/// chose : le premier amortit sur N opérations <i>internes</i> à un appel, le second sur N
/// appels <i>externes</i> — et le second fait apparaître le coût fixe de la région mesurée
/// (le harnais) sous la forme d'un terme en c/N.</para>
///
/// <para><b>Pourquoi cette échelle.</b> Le document central
/// <c>docs/dotnet/net11-runtime-deltas.md</c> (section « Le régime à N croissant ») nomme un
/// test qui départagerait « coût fixe par itération » de « coût réel », et le donnait comme
/// non fait. Une session du 2026-11-10 l'a joué sur l'appel de ce banc, avec un contrôle de
/// mécanisme par <c>DOTNET_TieredCompilation=0</c> ; le résultat est qu'<b>il n'y a pas
/// d'alternative</b> : le régime N = 1 mesure un coût réel w augmenté d'un coût fixe c/N
/// (le harnais), et, aux petits N, d'une seconde sur-coût due au tiering du JIT. Voir
/// <c>benchmarks/N-LADDER-2026-11-10.md</c>.</para>
///
/// <para><b>Fidélité.</b> Le <c>Setup</c>, les payloads et les quatre appels mesurés sont
/// identiques à ceux de <see cref="PromptSettingsBenchmarks"/> ; seul le régime de comptage
/// change. Les noms de classes sont ceux de l'artefact effectivement mesuré
/// (<c>feature/benchmarkdotnet-baseline</c> @ <c>6f96590</c>, BenchmarkDotNet 0.14.0) :
/// ils sont conservés pour que les rapports BDN publiés restent identifiables par leur
/// nom de fichier (<c>SemanticFleet.Benchmarks.PromptSettings_Ops1-report-github.md</c>).</para>
/// </summary>
public abstract class PromptSettingsOpsPerInvokeBase
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
}

/// <summary>Régime de référence : 1 invocation par itération (protocole du dépôt).</summary>
[MemoryDiagnoser]
[Config(typeof(OpsPerInvoke1Config))]
public class PromptSettings_Ops1 : PromptSettingsOpsPerInvokeBase
{
}

/// <summary>Régime intermédiaire : 100 invocations par itération.</summary>
[MemoryDiagnoser]
[Config(typeof(OpsPerInvoke100Config))]
public class PromptSettings_Ops100 : PromptSettingsOpsPerInvokeBase
{
}

/// <summary>Régime long : 10 000 invocations par itération.</summary>
[MemoryDiagnoser]
[Config(typeof(OpsPerInvoke10000Config))]
public class PromptSettings_Ops10000 : PromptSettingsOpsPerInvokeBase
{
}

/// <summary>
/// Second régime de mesure (job long) sur le même appel : 10 warmup / 100 itérations,
/// unroll 1 — le régime qui contredisait le ShortRun apparié sur `Serialize`.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(LongJobConfig))]
public class PromptSettings_LongJob : PromptSettingsOpsPerInvokeBase
{
}

/// <summary>
/// Configuration in-process : ShortRun du dépôt, avec un nombre d'invocations par
/// itération paramétrable. Le réglage UnrollFactor=1 et ServerGarbageCollection
/// restent ceux du protocole de référence (même contrainte antivirus documentée
/// dans InProcessShortConfig).
/// </summary>
public class OpsPerInvokeConfig : ManualConfig
{
    public OpsPerInvokeConfig(int invocationCount)
    {
        AddJob(Job.ShortRun
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithInvocationCount(invocationCount)
            .WithUnrollFactor(1)
            .WithWarmupCount(5)
            .WithIterationCount(15));
    }
}

public class OpsPerInvoke1Config : OpsPerInvokeConfig
{
    public OpsPerInvoke1Config() : base(1) { }
}

public class OpsPerInvoke100Config : OpsPerInvokeConfig
{
    public OpsPerInvoke100Config() : base(100) { }
}

public class OpsPerInvoke10000Config : OpsPerInvokeConfig
{
    public OpsPerInvoke10000Config() : base(10_000) { }
}

/// <summary>Job long : même appel, 100 itérations, unroll 1, 1 invocation par itération.</summary>
public class LongJobConfig : ManualConfig
{
    public LongJobConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithLaunchCount(1)
            .WithWarmupCount(10)
            .WithIterationCount(100)
            .WithInvocationCount(1)
            .WithUnrollFactor(1));
    }
}
