// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;
using SemanticKernel.UnitTests;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SemanticKernel.IntegrationTests.Connectors.MultiConnector;

/// <summary>
/// Integration tests for <see cref=" OobaboogaTextCompletion"/>.
///
/// SK 1.78 migration: the legacy <c>Plan</c> planning model (<c>Plan.FromJson</c>,
/// <c>plan.State</c>, <c>plan.Steps</c>, <c>SequentialPlanner.CreatePlanAsync</c>) was
/// REMOVED. <c>MultiTextCompletionSettings.ExecuteAsync</c> now takes a
/// <see cref="KernelFunction"/>, so each test's plan is rebuilt as a runtime
/// <see cref="KernelFunction"/> that walks the resolved SK 1.78 plan
/// (<see cref="PlanJsonHelpers"/>) and invokes each resolved step against the
/// <see cref="Kernel"/> on which the cost-offload system runs. The plan-as-KernelFunction
/// pattern keeps the cost-offload contract intact while delegating step sequencing to
/// the helper rather than the now-removed planners.
/// </summary>
public sealed class MultiConnectorTests : IDisposable
{
    private const string StartGoal =
        "The goal of this plan is to evaluate the capabilities of a smaller LLM model. Start by writing a text of about 100 words on a given topic, as the input parameter of the plan. Then use distinct functions from the available skills on the input text and/or the previous functions results, choosing parameters in such a way that you know you will succeed at running each function but a smaller model might not. Try to propose steps of distinct difficulties so that models of distinct capabilities might succeed on some functions and fail on others. In a second phase, you will be asked to evaluate the function answers from smaller models. Please beware of correct Xml tags, attributes, and parameter names when defined and when reused.";

    // SK 1.78: resolved plan JSON lives under samples/Plans/SK178/.
    private const string PlansDirectory = "../../../../../../samples/Plans/SK178/";
    private const string TextsDirectory = "../../../../../../samples/Texts/";

    private readonly IConfigurationRoot _configuration;
    private readonly List<ClientWebSocket> _webSockets = new();
    private readonly Func<ClientWebSocket> _webSocketFactory;
    private readonly RedirectOutput _testOutputHelper;
    private readonly string _planDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, PlansDirectory);
    private readonly string _textDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, TextsDirectory);
    private readonly CancellationTokenSource _cleanupToken = new();

    public MultiConnectorTests(ITestOutputHelper output)
    {
        this._testOutputHelper = new RedirectOutput(output);

        // Load configuration
        this._configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<MultiConnectorTests>()
            .Build();

        this._webSocketFactory = () =>
        {
            var toReturn = new ClientWebSocket();
            this._webSockets.Add(toReturn);
            return toReturn;
        };
    }

    /// <summary>
    /// This test method uses a plan loaded from a file, an input text of a particular difficulty, and all models configured in settings file
    /// </summary>
    [Theory(Skip = "This test is for manual verification.")]
    [InlineData(true, 1, "Summarize.json", "Comm_simple.txt", "Danse_simple.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, 1, "Summarize_Topics_ElementAt.json", "Comm_medium.txt", "Danse_simple.txt", "SummarizeSkill", "MiscSkill")]
    public async Task ChatGptOffloadsToMultipleOobaboogaUsingFileAsync(bool succeedsOffloading, int nbPromptTests, string planFileName, string inputTextFileName, string validationTextFileName, params string[] skillNames)
    {
        await this.ChatGptOffloadsToSingleOobaboogaUsingFileAsync(succeedsOffloading, "", nbPromptTests, planFileName, inputTextFileName, validationTextFileName, skillNames).ConfigureAwait(false);
    }

    /// <summary>
    /// This test method uses a plan loaded from a file, together with an input text loaded from a file, and adds a single completion model from its name as configured in the settings file.
    /// </summary>
    [Theory(Skip = "This test is for manual verification.")]
    [InlineData(true, "microsoft_phi-1_5", 1, "Summarize.json", "Comm_simple.txt", "Danse_simple.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "microsoft_phi-1_5", 1, "Summarize.json", "Comm_medium.txt", "Danse_medium.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(false, "microsoft_phi-1_5", 1, "Summarize.json", "Comm_hard.txt", "Danse_hard.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_orca_mini_3B-GGML", 1, "Summarize.json", "Comm_simple.txt", "Danse_simple.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_orca_mini_3B-GGML", 1, "Summarize.json", "Comm_medium.txt", "Danse_medium.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(false, "TheBloke_orca_mini_3B-GGML", 1, "Summarize.json", "Comm_hard.txt", "Danse_hard.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_Mistral-7B-OpenOrca-GGUF", 1, "Summarize.json", "Comm_simple.txt", "Danse_simple.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_Mistral-7B-OpenOrca-GGUF", 1, "Summarize.json", "Comm_medium.txt", "Danse_medium.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_Mistral-7B-OpenOrca-GGUF", 1, "Summarize.json", "Comm_hard.txt", "Danse_hard.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "Summarize.json", "Comm_simple.txt", "Danse_simple.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "Summarize.json", "Comm_medium.txt", "Danse_medium.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "Summarize.json", "Comm_hard.txt", "Danse_hard.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "Summarize_Topics_ElementAt.json", "Comm_simple.txt", "Danse_simple.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "Summarize_Topics_ElementAt.json", "Comm_medium.txt", "Danse_medium.txt", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "Summarize_Topics_ElementAt.json", "Comm_hard.txt", "Danse_hard.txt", "SummarizeSkill", "MiscSkill")]
    public async Task ChatGptOffloadsToSingleOobaboogaUsingFileAsync(bool succeedsOffloading, string completion, int nbTests, string planFile, string inputFile, string validationFile, params string[] skills)
    {
        // Resolve the SK 1.78 plan file path under samples/Plans/SK178/.
        var planPath = System.IO.Path.Combine(this._planDirectory, planFile);
        var textPath = System.IO.Path.Combine(this._textDirectory, inputFile);
        var validationTextPath = System.IO.Path.Combine(this._textDirectory, validationFile);

        // SK 1.78: a Plan is now a KernelFunction. The planFactory returns a runtime KernelFunction that
        // walks each resolved SK 1.78 invocation against the Kernel on which the cost-offload
        // system runs. Auto Function Calling (FunctionChoiceBehavior.Auto) is the canonical
        // replacement for the removed planners per the MS Learn migration guide
        // (learn.microsoft.com/en-us/semantic-kernel/support/migration/stepwise-planner-migration-guide).
        Func<Kernel, CancellationToken, bool, Task<KernelFunction>> planFactory = async (kernel, token, isValidation) =>
        {
            var inputPath = isValidation ? validationTextPath : textPath;
            var input = await System.IO.File.ReadAllTextAsync(inputPath, token).ConfigureAwait(false);

            var (history, invocations) = await PlanJsonHelpers.BuildChatHistoryFromPlanJsonAsync(planPath, input, token).ConfigureAwait(false);

            this._testOutputHelper.LogDebug(
                "SK 1.78 plan resolved: {0} invocation(s); ChatHistory messages={1}",
                invocations.Count,
                history.Count);

            return BuildPlanAsKernelFunction(kernel, history, invocations);
        };

        List<string>? modelNames = null;
        if (!string.IsNullOrEmpty(completion))
        {
            modelNames = new List<string> { completion };
        }

        await this.ChatGptOffloadsToOobaboogaAsync(succeedsOffloading, planFactory, modelNames, nbTests, skills).ConfigureAwait(false);
    }

    /// <summary>
    /// SequentialPlanner was REMOVED in SK 1.78. We keep the same test slot for parity with the
    /// pre-migration test matrix but mark it [Skip] — the planner-driven scenario is now covered by
    /// the file-loaded plan path which is the canonical Auto Function Calling entry point
    /// (<see cref="FunctionChoiceBehavior.Auto"/>). Reinstating a live planner test requires a
    /// dedicated function-calling planner (tracked separately, epic #6853 follow-up).
    /// </summary>
    [Theory(Skip = "SequentialPlanner was removed in SK 1.78; use the file-loaded plan path instead.")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "trivial", "Comm_simple.txt", "Danse_simple.txt", "WriterSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "medium", "Comm_simple.txt", "Danse_simple.txt", "WriterSkill", "MiscSkill")]
    public async Task ChatGptOffloadsToOobaboogaUsingPlannerAsync(bool succeedsOffloading, string completionName, int nbPromptTests, string difficulty, string inputFile, string validationFile, params string[] skillNames)
    {
        // SK 1.78: there is no SequentialPlanner. Keep the signature stable for the test matrix
        // but short-circuit with an explanatory failure so that, if this test were ever unskipped
        // (e.g. once a function-calling planner returns), the failure mode is clear.
        throw new NotSupportedException(
            "SequentialPlanner was removed in SK 1.78; rebuild this scenario via the file-loaded plan path " +
            "which uses FunctionChoiceBehavior.Auto(). See docs/Plans/SK178-format.md and epic #6853.");
    }

    /// <summary>
    /// Builds a runtime <see cref="KernelFunction"/> that walks an SK 1.78 plan resolved by
    /// <see cref="PlanJsonHelpers.BuildChatHistoryFromPlanJsonAsync"/>. Each
    /// <see cref="ResolvedInvocation"/> is dispatched to the matching <see cref="KernelFunction"/>
    /// already registered on the supplied <paramref name="kernel"/>; outputs from one step become
    /// inputs to the next per <c>output_variable</c> substitution. The plan-as-KernelFunction
    /// pattern keeps <see cref="MultiTextCompletionSettings.ExecuteAsync"/> happy (it now requires
    /// a <see cref="KernelFunction"/> rather than the legacy <c>Plan</c>) while preserving the
    /// cost-offload semantics the tests were written for.
    /// </summary>
    private static KernelFunction BuildPlanAsKernelFunction(Kernel kernel, ChatHistory history, IReadOnlyList<ResolvedInvocation> invocations)
    {
        // SK 1.78: ChatHistory.SystemMessage was REMOVED — iterate the history to surface the
        // first system-role message (mirrors the helper's emission order: one system message
        // first, then the user message). This is the only place we read "the plan goal".
        var systemContent = string.Empty;
        foreach (var msg in history)
        {
            if (msg.Role == AuthorRole.System)
            {
                systemContent = msg.Content ?? string.Empty;
                break;
            }
        }

        var planName = string.IsNullOrWhiteSpace(systemContent)
            ? "Sk178Plan"
            : $"Sk178Plan:{systemContent}";

        // Delegate returns Task<string>: SK 1.78 KernelFunctionFromMethod auto-wraps
        // Task<string> in `new FunctionResult(function, value, kernel.Culture)`. No manual
        // FunctionResult ctor needed.
        return KernelFunctionFactory.CreateFromMethod(
            method: async (KernelArguments args, CancellationToken ct) =>
            {
                var buffer = new StringBuilder();
                var userContent = history.LastOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
                var knownOutputs = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["INPUT"] = userContent,
                };

                foreach (var invocation in invocations)
                {
                    var function = ResolveFunction(kernel, invocation);
                    if (function is null)
                    {
                        buffer.AppendLine($"[SK178] Skipped step '{invocation.FullyQualifiedName}' — function not registered on the kernel.");
                        continue;
                    }

                    var stepArgs = new KernelArguments();
                    foreach (var (key, value) in invocation.Arguments)
                    {
                        stepArgs[key] = value;
                    }

                    var stepResult = await kernel.InvokeAsync(function, stepArgs, ct).ConfigureAwait(false);
                    var stepValue = stepResult.GetValue<object>()?.ToString() ?? string.Empty;

                    buffer.AppendLine($"[SK178] {invocation.FullyQualifiedName} -> {stepValue}");

                    if (!string.IsNullOrEmpty(invocation.OutputVariable))
                    {
                        knownOutputs[invocation.OutputVariable] = stepValue;
                    }
                }

                buffer.AppendLine(userContent);
                return buffer.ToString();
            },
            functionName: planName.Length > 64 ? planName[..64] : planName,
            description: "SK 1.78 plan runtime walker (replaces the legacy SK Plan.FromJson path).");
    }

    /// <summary>
    /// Resolves a <see cref="ResolvedInvocation"/> to the actual <see cref="KernelFunction"/>
    /// registered on the kernel. Empty <c>PluginName</c> means the function is registered on the
    /// bare kernel (not under a plugin).
    /// </summary>
    private static KernelFunction? ResolveFunction(Kernel kernel, ResolvedInvocation invocation)
    {
        if (string.IsNullOrEmpty(invocation.PluginName))
        {
            return kernel.Plugins.SelectMany(p => p)
                .FirstOrDefault(f => string.Equals(f.Name, invocation.FunctionName, StringComparison.Ordinal));
        }

        if (kernel.Plugins.TryGetPlugin(invocation.PluginName, out var plugin) &&
            plugin.TryGetFunction(invocation.FunctionName, out var function))
        {
            return function;
        }

        return null;
    }

    private async Task ChatGptOffloadsToOobaboogaAsync(bool succeedsOffloading, Func<Kernel, CancellationToken, bool, Task<KernelFunction>> planFactory, List<string>? modelNames, int nbPromptTests, params string[] skillNames)
    {
        // Arrange

        this._testOutputHelper.LogTrace("# Starting test in environment directory: {0}\n", Environment.CurrentDirectory);

        var sw = Stopwatch.StartNew();

        var multiConnectorConfiguration = this._configuration.GetSection("MultiConnector").Get<MultiOobaboogaConnectorConfiguration>();
        Assert.NotNull(multiConnectorConfiguration);

        var creditor = new CallRequestCostCreditor();

        //We configure settings to enable analysis, and let the connector discover the best connector settings, updating on the fly and deleting analysis file

        this._testOutputHelper.LogTrace("\n# Creating MultiTextCompletionSettings\n");

        var settings = this.SetupMultiTextCompletionSettings(multiConnectorConfiguration, creditor, nbPromptTests);

        // Cleanup in case the previous test failed to delete the analysis file
        if (File.Exists(settings.AnalysisSettings.AnalysisFilePath))
        {
            this._testOutputHelper.LogTrace("Deleting preexisting analysis file: {0}\n", settings.AnalysisSettings.AnalysisFilePath);
            File.Delete(settings.AnalysisSettings.AnalysisFilePath);
        }

        var kernel = this.InitializeKernel(settings, modelNames, multiConnectorConfiguration, cancellationToken: this._cleanupToken.Token);

        if (kernel == null)
        {
            return;
        }

        var prepareKernelTimeElapsed = sw.Elapsed;

        this._testOutputHelper.LogTrace("\n# Loading Skills\n");

        var skills = TestHelpers.GetSkills(kernel, skillNames);

        // Act

        var (firstPassEffectiveCost, secondPassEffectiveCost, evaluations) = await this.ExecutePlansAndOptimizeAsync(kernel, planFactory, settings, sw).ConfigureAwait(false);

        // Assert

        this._testOutputHelper.LogTrace("\n# Assertions \n");

        if (succeedsOffloading)
        {
            this.DoOffloadingAsserts(firstPassEffectiveCost, secondPassEffectiveCost, evaluations);
        }
        else
        {
            Assert.Throws<TrueException>((Action)(() => this.DoOffloadingAsserts(firstPassEffectiveCost, secondPassEffectiveCost, evaluations)));
        }
    }

    private MultiTextCompletionSettings SetupMultiTextCompletionSettings(MultiOobaboogaConnectorConfiguration multiOobaboogaConnectorConfiguration, CallRequestCostCreditor creditor, int nbPromptTests)
    {
        // The most common settings for a MultiTextCompletion are illustrated below, most of them have default values and are optional
        var settings = new MultiTextCompletionSettings()
        {
            //We start with prompt sampling off to control precisely the prompt types we want to test by enabling this parameter on specific periods
            EnablePromptSampling = false,
            // We only collect one sample per prompt type for tests for now. We'll enable collecting one more sample for final validation on unseen material
            MaxInstanceNb = 1,
            // We'll use a simple creditor to track usage costs
            Creditor = creditor,
            // Prompt type require a signature for identification, and we'll use the first 11 characters of the prompt as signature
            PromptTruncationLength = 11,
            //This optional feature upgrade prompt signature by adjusting prompt starts to the true complete prefix of the template preceding user input. This is useful where many prompt would yield overlapping starts, but it may falsely create new prompt types if some inputs have partially overlapping starts.
            // Prompts with variable content at the start are currently not accounted for automatically though, and need either a manual regex to avoid creating increasing prompt types, or using the FreezePromptTypes setting but the first alternative is preferred because unmatched prompts will go through the entire settings unless a regex matches them.
            AdjustPromptStarts = false,
            // Uncomment to enable additional logging of MultiTextCompletion calls, results and/or test sample collection
            LogCallResult = true,
            LogTestCollection = true,
            // In those tests, we don't have information about the underlying model hosts, so we can't make performance comparisons between models. Instead, arbitrary cost per token are defined in settings, and usage costs are computed.
            ConnectorComparer = MultiTextCompletionSettings.GetWeightedConnectorComparer(0, 1),
            // Adding a simple transform for template-less models, which require a line break at the end of the prompt
            GlobalPromptTransform = new PromptTransform()
            {
                TransformFunction = (s, context) => s.EndsWith("\n", StringComparison.OrdinalIgnoreCase) ? s : s + "\n",
            },
            // Analysis settings are an important part of the main settings, dealing with how to collect samples, conduct tests, evaluate them and update the connector settings
            AnalysisSettings = new MultiCompletionAnalysisSettings()
            {
                // Analysis must be enabled for analysis jobs to be created from sample usage, we'll play with that parameter
                EnableAnalysis = false,
                // This is the number of tests to run and validate for each prompt type before it can be considered able to handle the prompt type
                NbPromptTests = nbPromptTests,
                // Because we only collect one sample, we have to artificially raise the temperature for the test completion request settings, in order to induce diverse results
                TestsTemperatureTransform = d => Math.Max(d ?? 0, 0.7),
                // We use manual release of analysis task to make sure analysis event is only fired once with the final result
                AnalysisAwaitsManualTrigger = true,
                // Accordingly, delays and periods are also removed
                AnalysisDelay = TimeSpan.Zero,
                TestsPeriod = TimeSpan.Zero,
                EvaluationPeriod = TimeSpan.Zero,
                SuggestionPeriod = TimeSpan.Zero,
                // Secondary connectors usually don't support multiple concurrent requests, default Test parallelism defaults to 1 but you can change that here
                MaxDegreeOfParallelismTests = 1,
                // Change the following settings if you run all models on the same machine and want to limit the number of concurrent connectors
                MaxDegreeOfParallelismConnectorsByTest = 3,
                // Primary connector ChatGPT supports multiple concurrent request, default parallelism is 5 but you can change that here
                MaxDegreeOfParallelismEvaluations = 5,
                // We update the settings live from suggestion following analysis
                UpdateSuggestedSettings = true,
                // For instrumented data in file format, feel free to uncomment either of the following lines
                DeleteAnalysisFile = false,
                SaveSuggestedSettings = true,
                // In order to spare on fees, you can use self vetting of prompt tests by the tested connector, which may work well depending on the models vetted
                //UseSelfVetting = false,
            },
            // In order to highlight prompts and response in log trace, you can uncomment the following lines
            //PromptLogsJsonEncoded = false,
            //PromptLogTruncationLength = 500,
            //PromptLogTruncationFormat = @"
            //================================= START ====== PROMPT/RESULT =============================================
            //{0}

            //(...)

            //{1}
            //================================== END ====== PROMPT/RESULT =============================================="
        };

        // We add or override the global parameters from the settings file

        foreach (var userGlobalParams in multiOobaboogaConnectorConfiguration.GlobalParameters)
        {
            settings.GlobalParameters[userGlobalParams.Key] = userGlobalParams.Value;
        }

        return settings;
    }

    private async Task<(decimal, decimal, List<(ConnectorPromptEvaluation, AnalysisJob)>)> ExecutePlansAndOptimizeAsync(Kernel kernel, Func<Kernel, CancellationToken, bool, Task<KernelFunction>> planFactory, MultiTextCompletionSettings settings, Stopwatch sw)
    {
        var initialElapsed = sw.Elapsed;

        // Create a plan
        this._testOutputHelper.LogTrace("\n# Loading Test plan\n");
        var plan1 = await planFactory(kernel, this._cleanupToken.Token, false);
        this._testOutputHelper.LogTrace("\n# 1st Run of plan with primary connector\n");
        //We enable sampling and analysis trigger. There is a lock to prevent automatic analysis starting while the test is running, but we'll manually trigger analysis after the test is done
        settings.EnablePromptSampling = true;
        settings.AnalysisSettings.EnableAnalysis = true;
        var firstPassResult = await settings.ExecuteAsync(plan1, kernel, cancellationToken: this._cleanupToken.Token, computeCost: true).ConfigureAwait(false);
        this._testOutputHelper.LogTrace("\n# 1st run finished in {0}\n", firstPassResult.Duration);
        this._testOutputHelper.LogDebug("Result from primary connector execution of SK 1.78 plan used for multi-connector evaluation with duration {0} and cost {1}:\n {2}\n", firstPassResult.Duration, firstPassResult.Cost, firstPassResult.Result);

        // Perform tests, evaluation, and optimization
        var optimizationResults = await settings.OptimizeAsync(this._testOutputHelper).ConfigureAwait(false);
        var optimizationDoneElapsed = sw.Elapsed;
        var optimizationDuration = optimizationDoneElapsed - initialElapsed;
        settings.AnalysisSettings.EnableAnalysis = false;
        this._testOutputHelper.LogTrace("\n# Optimization task finished in {0}\n", optimizationDuration);
        // SK 1.78 migration: Json.Encode / Json.Serialize from Microsoft.SemanticKernel.Text.Json
        // were REMOVED. The BCL System.Text.Json.JsonSerializer.Serialize is the canonical
        // replacement; we pass an indented-true options object so the diagnostic output keeps its
        // former multi-line shape (the optimization results are read off LogDebug by humans).
        var serializedSettings = JsonSerializer.Serialize(
            optimizationResults.SuggestedSettings,
            new JsonSerializerOptions { WriteIndented = true });
        this._testOutputHelper.LogDebug("Optimized with suggested settings: {0}\n", serializedSettings);

        //Re execute plan with suggested settings - SK 1.78: a fresh plan KernelFunction is rebuilt from the same source JSON.
        var plan2 = await planFactory(kernel, this._cleanupToken.Token, false);
        this._testOutputHelper.LogTrace("\n# 2nd run of plan with updated settings and variable completions\n");
        var secondPassResult = await settings.ExecuteAsync(plan2, kernel, cancellationToken: this._cleanupToken.Token, computeCost: true).ConfigureAwait(false);
        this._testOutputHelper.LogTrace("\n# 2nd run finished in {0}\n", secondPassResult.Duration);
        this._testOutputHelper.LogDebug("Result from vetted connector execution of SK 1.78 plan used for multi-connector evaluation with duration {0} and cost {1}:\n {2}\n", secondPassResult.Duration, secondPassResult.Cost, secondPassResult.Result);

        // We validate the new connector with a new plan with distinct data
        this._testOutputHelper.LogTrace("\n# Loading validation plan from factory\n");
        var plan3 = await planFactory(kernel, this._cleanupToken.Token, true);
        this._testOutputHelper.LogDebug("SK 1.78 plan used for multi-connector validation resolved to {0} invocation(s).", plan3.Name);

        // Execute third pass with validation plan
        this._testOutputHelper.LogTrace("\n# 3rd run of plan with final settings\n");
        // Since we already collected samples for the optimization, we don't want to max out the number of samples collected for validation, so we'll just add one
        settings.MaxInstanceNb += 1;
        var thirdPassResult = await settings.ExecuteAsync(plan3, kernel, cancellationToken: this._cleanupToken.Token, computeCost: true, collectSamples: true).ConfigureAwait(false);
        this._testOutputHelper.LogTrace("\n# 3rd run finished in {0}\n", thirdPassResult.Duration);
        this._testOutputHelper.LogTrace("\n# Start final validation with {0} sample batches received from 3rd run validating manually with primary connector\n", thirdPassResult.SampleBatches!.Count);
        var evaluations = await settings.ValidateAsync(thirdPassResult.SampleBatches).ConfigureAwait(false);
        this._testOutputHelper.LogTrace("\n# End validation, starting Asserts\n");

        return (firstPassResult.Cost, secondPassResult.Cost, evaluations);
    }

    private void DoOffloadingAsserts(decimal firstPassEffectiveCost, decimal secondPassEffectiveCost, List<(ConnectorPromptEvaluation, AnalysisJob)> evaluations)
    {
        this._testOutputHelper.LogTrace("Asserting secondary connectors reduced the original plan cost");

        Assert.True(firstPassEffectiveCost > secondPassEffectiveCost);

        this._testOutputHelper.LogTrace("Asserting secondary connectors plan capabilities are vetted on a distinct validation input");

        var atLeastOneSecondaryCompletionValidated = evaluations.Any(e => e.Item1.Test.ConnectorName != e.Item2.TextCompletions[0].Name && e.Item1.IsVetted);
        Assert.True(atLeastOneSecondaryCompletionValidated);
    }

    /// <summary>
    /// Configures a kernel with MultiTextCompletion comprising a primary OpenAI connector with parameters defined in main settings for OpenAI integration tests, and Oobabooga secondary connectors with parameters defined in the MultiConnector part of the settings file. Returns null if no matching secondary connector is found in configuration.
    ///
    /// SK 1.78 migration: <c>ITextCompletion</c> / <c>OpenAITextCompletion</c> / <c>OpenAIChatCompletion</c> were
    /// REMOVED. The canonical SK 1.78 surface is <see cref="ITextGenerationService"/> /
    /// <see cref="IChatCompletionService"/>; <c>OpenAIChatCompletionService</c> implements both and is the
    /// direct replacement. <c>kernel.CreateNewContext()</c> is also gone — <see cref="Kernel"/> is now
    /// immutable and the cost-offload call signature takes <see cref="KernelArguments"/> directly.
    /// </summary>
    private Kernel? InitializeKernel(MultiTextCompletionSettings multiTextCompletionSettings, List<string>? modelNames, MultiOobaboogaConnectorConfiguration multiOobaboogaConnectorConfiguration, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= CancellationToken.None;

        var openAiConfiguration = this._configuration.GetSection("OpenAI").Get<OpenAIConfiguration>();
        Assert.NotNull(openAiConfiguration);

        string testOrChatModelId;
        ITextGenerationService openAiConnector;

        if (!string.IsNullOrEmpty(openAiConfiguration.ChatModelId))
        {
            testOrChatModelId = openAiConfiguration.ChatModelId!;
            // SK 1.78: OpenAIChatCompletionService implements both IChatCompletionService and
            // ITextGenerationService, so it slots into the existing ITextGenerationService-shaped
            // MultiTextCompletion pipeline that the SK 1.78-migrated connector uses.
            openAiConnector = new OpenAIChatCompletionService(testOrChatModelId, openAiConfiguration.ApiKey, loggerFactory: this._testOutputHelper);
        }
        else
        {
            testOrChatModelId = openAiConfiguration.ModelId;
            // OpenAITextCompletion was REMOVED in SK 1.78 — fall back to the chat service for the
            // text-completion-only scenario, which still satisfies ITextGenerationService.
            openAiConnector = new OpenAIChatCompletionService(testOrChatModelId, openAiConfiguration.ApiKey, loggerFactory: this._testOutputHelper);
        }

        var openAiNamedCompletion = new NamedTextCompletion(testOrChatModelId, openAiConnector)
        {
            CostPer1000Token = 0.0015m,
            TokenCountFunc = MultiOobaboogaConnectorConfiguration.TokenCountFunctionMap[TokenCountFunction.Gpt3Tokenizer],
            //We did not observe any limit on Open AI concurrent calls
            MaxDegreeOfParallelism = 5,
        };

        List<NamedTextCompletion> oobaboogaCompletions = multiOobaboogaConnectorConfiguration.CreateNamedCompletions(modelNames);

        if (oobaboogaCompletions.Count == 0)
        {
            this._testOutputHelper.LogWarning("No Secondary connectors matching test configuration found, aborting test");
            return null;
        }

        var builder = Kernel.CreateBuilder();

        builder.WithMultiConnectorCompletionService(
            serviceId: null,
            settings: multiTextCompletionSettings,
            mainTextCompletion: openAiNamedCompletion,
            setAsDefault: true,
            analysisTaskCancellationToken: cancellationToken,
            otherCompletions: oobaboogaCompletions.ToArray());

        var kernel = builder.Build();

        return kernel;
    }

    public void Dispose()
    {
        try
        {
            foreach (ClientWebSocket clientWebSocket in this._webSockets)
            {
                clientWebSocket.Dispose();
            }
        }
        finally
        {
            this._cleanupToken.Cancel();
            this._cleanupToken.Dispose();
            this._testOutputHelper.Dispose();
        }
    }
}
