﻿// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Text;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Represents the settings used to configure the multi-text completion process.
/// </summary>
/// <remarks>
///  Prompts with variable content at the start are currently not accounted for automatically and need attending by either providing a manual regex to avoid creating increasing prompt types indefinitely, or using the FreezePromptTypes setting to prevent new creation but the first alternative is preferred because unmatched prompts will go through the entire settings unless a regex matches them.
/// </remarks>
public class MultiTextCompletionSettings
{
    private ConcurrentBag<PromptMultiConnectorSettings> _promptMultiConnectorSettingsInternal = new();

    private readonly object _promptTypeLock = new();

    /// <summary>
    /// Loads suggested settings from an analysis.
    /// If the file doesn't exist, it returns the current settings.
    /// </summary>
    public MultiTextCompletionSettings LoadSuggestedSettingsFromAnalysis()
    {
        string analysisSettingsMultiCompletionSettingsFilePath = this.AnalysisSettings.MultiCompletionSettingsFilePath;
        if (!File.Exists(analysisSettingsMultiCompletionSettingsFilePath))
        {
            return this;
        }

        string readAllText = File.ReadAllText(analysisSettingsMultiCompletionSettingsFilePath);
        MultiTextCompletionSettings? loadSuggestedAnalysisSettings = Json.Deserialize<MultiTextCompletionSettings>(readAllText);
        return loadSuggestedAnalysisSettings ?? this;
    }

    /// <summary>
    /// If true, the prompt types, no new prompt types are discovered automatically and standard prompt settings will be associated with unrecognized prompts.
    /// </summary>
    public bool FreezePromptTypes { get; set; } = false;

    /// <summary>
    /// Represents the length to which prompts will be truncated for signature extraction.
    /// </summary>
    /// <remarks>
    /// Prompts with variable content at the start are currently not accounted for automatically though, and need either a manual regex to avoid creating increasing prompt types, or using the FreezePromptTypes setting but the first alternative is preferred because unmatched prompts will go through the entire settings unless a regex matches them.
    /// </remarks>
    public int PromptTruncationLength { get; set; } = 20;

    //TODO: Investigate the false positive issue (see remarks)
    /// <summary>
    /// If true, prompt signature starts are adjusted to the actual template starter when differing samples are witnessed.
    /// </summary>
    /// <remarks>
    /// This optional feature adjusts prompt start in matching signature to the true static prefix identified from comparing distinct instances. This is useful for scenarios where prompt starts may overlap but may induce higher computational cost unless a fast precomputed matching function is offered.
    /// Beware of false positives though, as when the evaluation prompt is evaluated several times with the same template and different data.
    /// </remarks>
    public bool AdjustPromptStarts { get; set; } = false;

    /// <summary>
    /// Determines if call prompts are considered for sampling to run tests and evaluations on connectors (enabled by default)
    /// </summary>
    public bool EnablePromptSampling { get; set; } = true;

    /// <summary>
    /// The number of sample to collect for each prompt type
    /// </summary>
    public int MaxInstanceNb { get; set; } = 10;

    /// <summary>
    /// Holds the settings for completion analysis process.
    /// </summary>
    public MultiCompletionAnalysisSettings AnalysisSettings { get; set; } = new();

    /// <summary>
    /// By default, connectors instrumentation server side and client side avoids to trigger result evaluation for display. This is mostly harmless and this outputs the corresponding log for more comfort.
    /// </summary>
    public bool LogCallResult { get; set; } = false;

    /// <summary>
    /// When enabled, a log event is generated when new tests are collected for analysis
    /// </summary>
    public bool LogTestCollection { get; set; } = false;

    /// <summary>
    /// By default, prompt logs are json encoded to maintain global readability of log traces, but you might remove that option to get more readable prompt logs.
    /// </summary>
    public bool PromptLogsJsonEncoded { get; set; } = true;

    /// <summary>
    /// Represents the max length of prompt and responses in chars to be logged.
    /// </summary>
    public int PromptLogTruncationLength { get; set; } = 2000;

    /// <summary>
    /// Optionally change the format of logged prompts for enhanced readability in very large execution traces.
    /// </summary>
    public string PromptLogTruncationFormat { get; set; } = Defaults.TruncatedLogFormat;

    /// <summary>
    /// List of settings for multiple connectors associated with each prompt type.
    /// </summary>
    public List<PromptMultiConnectorSettings> PromptMultiConnectorSettings
    {
        get => this._promptMultiConnectorSettingsInternal.ToList();
        set => this._promptMultiConnectorSettingsInternal = new ConcurrentBag<PromptMultiConnectorSettings>(value);
    }

    /// <summary>
    /// Comparer used to choose among vetted connectors.
    /// </summary>
    [JsonIgnore]
    public Func<CompletionJob, PromptConnectorSettings, PromptConnectorSettings, int> ConnectorComparer { get; set; } = GetWeightedConnectorComparer(1, 1);

    /// <summary>
    /// Custom matcher to find the prompt settings from prompt. Default enumerates prefixes and uses StartsWith. More efficient precomputed matchers can be used.
    /// </summary>
    [JsonIgnore]
    public Func<CompletionJob, IEnumerable<PromptMultiConnectorSettings>, PromptMultiConnectorSettings?> PromptMatcher { get; set; } = SimpleMatchPromptSettings;

    /// <summary>
    /// Global parameters allow injecting dynamic blocks consistently into the specific templates associated with prompt types, named connectors or the evaluation process. Use them in your templates with token formatted like {ParamName}.
    /// </summary>
    public Dictionary<string, string> GlobalParameters { get; set; } = new()
    {
        { nameof(Defaults.SystemSupplement), Defaults.SystemSupplement },
        { nameof(Defaults.UserPreamble), Defaults.UserPreamble },
        { nameof(Defaults.SemanticRemarks), Defaults.SemanticRemarks }
    };

    /// <summary>
    /// Optionally provide a context provider to inject dynamic blocks consistently into the specific templates associated with prompt types, named connectors or the evaluation process. Use them in your templates with token formatted like {ParamName}.
    /// </summary>
    [JsonIgnore]
    public Func<MultiCompletionSession, Dictionary<string, object>>? ContextProvider { get; set; }

    /// <summary>
    /// Optionally transform the input prompts globally
    /// </summary>
    public PromptTransform? GlobalPromptTransform { get; set; }

    /// <summary>
    /// An optional creditor that will compute compound costs from the connectors settings and usage.
    /// </summary>
    public CallRequestCostCreditor? Creditor { get; set; }

    /// <summary>
    /// Each time a new sample is generated, a delay is applied to collect more before saving them in a batch and triggering analysis
    /// </summary>
    public TimeSpan SampleCollectionDelay { get; set; } = TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// By default, samples are collected according to the configured MaxNumber. If disabled, samples will not be collected on prompt type where all known connectors were already evaluated for validity
    /// </summary>
    public bool SampleVettedConnectors { get; set; } = true;

    /// <summary>
    /// Returns settings for a given prompt.
    /// If settings for the prompt do not exist, new settings are created, added to the list, and returned.
    /// </summary>
    public PromptMultiConnectorSettings GetPromptSettings(CompletionJob completionJob, out bool isNew)
    {
        isNew = false;
        PromptMultiConnectorSettings? toReturn = this.MatchPromptSettings(completionJob);
        if (toReturn == null)
        {
            if (this.FreezePromptTypes)
            {
                toReturn = new PromptMultiConnectorSettings()
                {
                    PromptType = new PromptType()
                    {
                        Instances = { "" },
                        Signature = new PromptSignature(completionJob.RequestSettings, ""),
                        PromptName = "default",
                        SignatureNeedsAdjusting = false,
                    },
                };
            }
            else
            {
                lock (this._promptTypeLock)
                {
                    toReturn = this.MatchPromptSettings(completionJob);
                    if (toReturn == null)
                    {
                        var newSignature = PromptSignature.ExtractFromPrompt(completionJob, this._promptMultiConnectorSettingsInternal, this.PromptTruncationLength);
                        toReturn = new PromptMultiConnectorSettings()
                        {
                            PromptType = new PromptType()
                            {
                                Instances = { completionJob.Prompt },
                                Signature = newSignature,
                                PromptName = newSignature.PromptStart.Replace(" ", "_"),
                                SignatureNeedsAdjusting = this.AdjustPromptStarts,
                            },
                        };

                        this._promptMultiConnectorSettingsInternal.Add(toReturn);
                        isNew = true;
                    }
                }
            }
        }
        else
        {
            if (toReturn.PromptType.SignatureNeedsAdjusting && completionJob.Prompt != toReturn.PromptType.Instances[0])
            {
                lock (toReturn.PromptType.Signature)
                {
                    toReturn.PromptType.Signature = PromptSignature.ExtractFrom2Instances(completionJob.Prompt, toReturn.PromptType.Instances[0], completionJob.RequestSettings);
                }
            }
        }

        return toReturn;
    }

    /// <summary>
    /// This method is responsible for loading the appropriate settings in order to initiate the session state
    /// </summary>
    public MultiCompletionSession GetMultiCompletionSession(CompletionJob completionJob, IReadOnlyList<NamedTextCompletion> textCompletions, ILogger? logger)
    {
        var promptSettings = this.GetPromptSettings(completionJob, out var isNewPrompt);
        logger?.LogTrace("Retrieved prompt type and settings for connector, prompt signature:{0}", Json.Encode(promptSettings.PromptType.Signature.PromptStart, true));
        var textCompletionAndSettings = promptSettings.SelectAppropriateTextCompletion(completionJob, textCompletions, this.ConnectorComparer);
        logger?.LogTrace("Selected connector for prompt type: {0}", textCompletionAndSettings.namedTextCompletion.Name);

        var session = new MultiCompletionSession(completionJob,
            promptSettings,
            isNewPrompt,
            textCompletionAndSettings.namedTextCompletion,
            textCompletions,
            textCompletionAndSettings.promptConnectorSettings,
            this,
            logger);

        session.AdjustPromptAndRequestSettings();

        return session;
    }

    /// <summary>
    /// Generates a log for a prompt that is truncated at the beginning and the end.
    /// </summary>
    public string GeneratePromptLog(string prompt)
    {
        var promptLog = PromptSignature.GeneratePromptLog(prompt, this.PromptLogTruncationLength, this.PromptLogTruncationFormat, this.PromptLogsJsonEncoded);
        return promptLog;
    }

    private PromptMultiConnectorSettings? MatchPromptSettings(CompletionJob completionJob)
    {
        //TODO: Optionally allow for faster prompt matching with a pre-computation cost when prefix settings were tested and configured.
        //Make the matching method based on a setting property. options of increasing complexity:
        //   * Make a random char sampler to extract a unique sub-signature from the whole list of Signature.
        //   * build a RadixTree<string, char, TValue> --> Trie<string, char, TValue> --> HybridDictionary<string, TValue> or a Dictionary<string, TValue> with a custom comparer.
        //   * Use Infer.net string capabilities (see DNA samples) to build a probabilistic model of the signature and use it for matching.
        var toReturn = this.PromptMatcher(completionJob, this._promptMultiConnectorSettingsInternal);
        return toReturn;
    }

    /// <summary>
    /// Finds the first PromptMultiConnectorSettings that matches the given CompletionJob.
    /// </summary>
    /// <param name="completionJob">The CompletionJob to match.</param>
    /// <param name="promptMultiConnectorSettings">The collection of PromptMultiConnectorSettings to search.</param>
    /// <returns>The first PromptMultiConnectorSettings that matches the given CompletionJob, or null if none is found.</returns>
    public static PromptMultiConnectorSettings? SimpleMatchPromptSettings(CompletionJob completionJob, IEnumerable<PromptMultiConnectorSettings> promptMultiConnectorSettings)
    {
        var toReturn = promptMultiConnectorSettings.FirstOrDefault(s => s.PromptType.Signature.Matches(completionJob));
        return toReturn;
    }

    /// <summary>
    /// this will reset the vetting level of all connectors for all prompt types, and empty recorded instances, such that test collection and vetting analysis will be triggered if enabled.
    /// </summary>
    public void ResetPromptVetting()
    {
        foreach (var promptMultiConnectorSetting in this._promptMultiConnectorSettingsInternal)
        {
            promptMultiConnectorSetting.PromptType.Instances.Clear();
            foreach (var pairConnectorSetting
                     in promptMultiConnectorSetting.ConnectorSettingsDictionary)
            {
                pairConnectorSetting.Value.VettingLevel = VettingLevel.None;
            }
        }
    }

    /// <summary>
    /// Builds a comparer to compare two connectors given the respective weight attached to to their average duration and average cost fractions.
    /// </summary>
    /// <param name="durationWeight">the weight of the duration gains in proportion</param>
    /// <param name="costWeight">the weight of the cost gains in proportion</param>
    public static Func<CompletionJob, PromptConnectorSettings, PromptConnectorSettings, int> GetWeightedConnectorComparer(double durationWeight, double costWeight)
    {
        //TODO: Optionally allow for more elaborate connector comparison
        //Possible implementation: Build an Infer.probabilistic model with latent variables for general LLM capabilities, prompt type skill capability, prompt difficulty, etc. and observed variables tests, evaluations etc., precompute the posterior distribution of the latent variables given the observed variables, and choose the connector with the highest expected gain.

        return (completionJob, promptConnectorSettings1, promptConnectorSettings2) =>
        {
            double? durationCoefficient = null;
            if (promptConnectorSettings1.AverageDuration > TimeSpan.Zero && promptConnectorSettings2.AverageDuration > TimeSpan.Zero)
            {
                durationCoefficient = promptConnectorSettings2.AverageDuration.Ticks / (double)promptConnectorSettings1.AverageDuration.Ticks;
            }

            double? costCoefficient = null;
            if (promptConnectorSettings1.AverageCost > 0 && promptConnectorSettings2.AverageCost > 0)
            {
                costCoefficient = (double)promptConnectorSettings2.AverageCost / (double)promptConnectorSettings1.AverageCost;
            }

            var doubledResult = (costWeight * costCoefficient ?? 1 + durationWeight * durationCoefficient ?? 1) / (costWeight + durationWeight);
            var intResult = doubledResult <= 1 ? Math.Abs(doubledResult - 1) < 0.01 ? 0 : 1 : -1;
            return intResult;
        };
    }

    /// <summary>
    /// Static function that clones a <see cref="CompleteRequestSettings"/> object.
    /// </summary>
    public static CompleteRequestSettings CloneRequestSettings(CompleteRequestSettings requestSettings)
    {
        return new CompleteRequestSettings()
        {
            MaxTokens = requestSettings.MaxTokens,
            ResultsPerPrompt = requestSettings.ResultsPerPrompt,
            ChatSystemPrompt = requestSettings.ChatSystemPrompt,
            FrequencyPenalty = requestSettings.FrequencyPenalty,
            PresencePenalty = requestSettings.PresencePenalty,
            StopSequences = requestSettings.StopSequences,
            Temperature = requestSettings.Temperature,
            TokenSelectionBiases = requestSettings.TokenSelectionBiases,
            TopP = requestSettings.TopP,
        };
    }

    /// <summary>
    /// Helper method to run a function or plan and return the result and optionally the cost and samples.
    /// </summary>
    public async Task<MultiTextCompletionResult> ExecuteAsync(
        ISKFunction planOrFunction,
        IKernel kernel,
        ContextVariables? variables = null,
        CancellationToken? cancellationToken = default,
        bool computeCost = false,
        bool collectSamples = false)
    {
        var initialCost = 0m;
        CallRequestCostCreditor? localCreditor = null;
        if (computeCost)
        {
            if (this.Creditor == null)
            {
                localCreditor = new CallRequestCostCreditor();
                this.Creditor = localCreditor;
            }
            else
            {
                initialCost = this.Creditor.OngoingCost;
            }
        }

        var originalPromptSampling = this.EnablePromptSampling;

        var receivedTaskSources = new List<TaskCompletionSource<SamplesReceivedEventArgs>>();
        if (collectSamples)
        {
            this.EnablePromptSampling = true;
            TaskCompletionSource<SamplesReceivedEventArgs> samplesReceivedTaskSource = new();

            receivedTaskSources.Add(samplesReceivedTaskSource);
            // Subscribe to the OptimizationCompleted event
            this.AnalysisSettings.SamplesReceived += (sender, args) =>
            {
                // Signal the completion of the optimization
                samplesReceivedTaskSource.SetResult(args);

                samplesReceivedTaskSource = new();
                receivedTaskSources.Add(samplesReceivedTaskSource);
            };
        }

        variables = variables ?? kernel.CreateNewContext().Variables;
        var sw = Stopwatch.StartNew();
        var result = await kernel.RunAsync(planOrFunction, variables, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
        var duration = sw.Elapsed;
        var toReturn = new MultiTextCompletionResult(result)
        {
            Duration = duration,
        };
        if (computeCost)
        {
            var ongoingCost = this.Creditor!.OngoingCost;
            toReturn.Cost = ongoingCost - initialCost;
            if (localCreditor != null)
            {
                this.Creditor = null;
            }
        }

        if (collectSamples)
        {
            var validationTestBatches = new List<SamplesReceivedEventArgs>();
            foreach (var receivedTaskSource in receivedTaskSources)
            {
                var sampleReceived = await receivedTaskSource.Task.ConfigureAwait(false);
                validationTestBatches.Add(sampleReceived);
            }

            toReturn.SampleBatches = validationTestBatches;
            if (this.EnablePromptSampling != originalPromptSampling)
            {
                this.EnablePromptSampling = originalPromptSampling;
            }
        }

        return toReturn;
    }

    /// <summary>
    /// Runs a series of tests, evaluations on the connectors, optimize settings and returns the results. Assumes a series of samples were collected and the optimization task is locked, waiting for release.
    /// </summary>
    public async Task<SuggestionCompletedEventArgs> OptimizeAsync(ILogger? logger = null)
    {
        // We disable prompt sampling to ensure no other tests are generated
        this.EnablePromptSampling = false;

        // Create a task completion source to signal the completion of the optimization

        TaskCompletionSource<SuggestionCompletedEventArgs> suggestionCompletedTaskSource = new();

        // Subscribe to the OptimizationCompleted event

        this.AnalysisSettings.SuggestionCompleted += (sender, args) =>
        {
            // Signal the completion of the optimization
            suggestionCompletedTaskSource.SetResult(args);
        };

        // Subscribe to the OptimizationCompleted event
        this.AnalysisSettings.AnalysisTaskCrashed += (sender, args) =>
        {
            // Signal the completion of the optimization
            suggestionCompletedTaskSource.SetException(args.CrashEvent.Exception);
        };

        logger?.LogTrace("\n# Releasing analysis task, waiting for suggestion completed event\n");

        this.AnalysisSettings.AnalysisAwaitsManualTrigger = false;
        this.AnalysisSettings.ReleaseAnalysisTasks();

        // Get the optimization results
        var optimizationResults = await suggestionCompletedTaskSource.Task.ConfigureAwait(false);
        return optimizationResults;
    }

    /// <summary>
    /// Validates a set of collected samples using the primary connector and return the evaluation results.
    /// </summary>
    public async Task<List<(ConnectorPromptEvaluation, AnalysisJob)>> ValidateAsync(List<SamplesReceivedEventArgs> validationTestBatches)
    {
        var evaluations = new List<(ConnectorPromptEvaluation, AnalysisJob)>();
        foreach (SamplesReceivedEventArgs validationTestBatch in validationTestBatches)
        {
            foreach (var sample in validationTestBatch.NewSamples)
            {
                var evaluation = await this.AnalysisSettings.EvaluateConnectorTestAsync(sample, validationTestBatch.AnalysisJob).ConfigureAwait(false);
                if (evaluation == null)
                {
                    throw new SKException("Validation of MultiCompletion failed to complete");
                }

                evaluations.Add((evaluation, validationTestBatch.AnalysisJob));
            }
        }

        return evaluations;
    }
}
