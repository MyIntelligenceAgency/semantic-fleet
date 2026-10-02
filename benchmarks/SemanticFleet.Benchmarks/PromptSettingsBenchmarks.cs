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

/// <summary>
/// In-process config for Windows machines where the out-of-process benchmark child process
/// gets blocked by antivirus/Defender (exit code -2147450730). ShortRun job with enough
/// iterations to keep the relative error in the 10-20% range.
/// </summary>
public class InProcessShortConfig : ManualConfig
{
    public InProcessShortConfig()
    {
        AddJob(Job.ShortRun
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithWarmupCount(5)
            .WithIterationCount(15));
    }
}
