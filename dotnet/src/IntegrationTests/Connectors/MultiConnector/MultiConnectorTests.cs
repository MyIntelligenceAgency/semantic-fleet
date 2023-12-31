﻿// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextCompletion;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Text;
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
/// </summary>
public sealed class MultiConnectorTests : IDisposable
{
    private const string StartGoal =
        "The goal of this plan is to evaluate the capabilities of a smaller LLM model. Start by writing a text of about 100 words on a given topic, as the input parameter of the plan. Then use distinct functions from the available skills on the input text and/or the previous functions results, choosing parameters in such a way that you know you will succeed at running each function but a smaller model might not. Try to propose steps of distinct difficulties so that models of distinct capabilities might succeed on some functions and fail on others. In a second phase, you will be asked to evaluate the function answers from smaller models. Please beware of correct Xml tags, attributes, and parameter names when defined and when reused.";

    private const string PlansDirectory = "../../../../../../samples/Plans/";
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
        // Load the plan from the provided file path
        var planPath = System.IO.Path.Combine(this._planDirectory, planFile);
        var textPath = System.IO.Path.Combine(this._textDirectory, inputFile);
        var validationTextPath = System.IO.Path.Combine(this._textDirectory, validationFile);

        Func<IKernel, CancellationToken, bool, Task<Plan>> planFactory = async (kernel, token, isValidation) =>
        {
            var planJson = await System.IO.File.ReadAllTextAsync(planPath, token);
            var ctx = kernel.CreateNewContext();
            var plan = Plan.FromJson(planJson, ctx.Functions, true);
            var inputPath = textPath;
            if (isValidation)
            {
                inputPath = validationTextPath;
            }

            var input = await System.IO.File.ReadAllTextAsync(inputPath, token);
            plan.State.Update(input);
            return plan;
        };

        List<string>? modelNames = null;
        if (!string.IsNullOrEmpty(completion))
        {
            modelNames = new List<string> { completion };
        }

        await this.ChatGptOffloadsToOobaboogaAsync(succeedsOffloading, planFactory, modelNames, nbTests, skills).ConfigureAwait(false);
    }

    // This test method uses the SequentialPlanner to create a plan based on difficulty
    //[Theory(Skip = "This test is for manual verification.")]
    [Theory(Skip = "This test is for manual verification.")]
    //[InlineData("",  1, "medium", "SummarizeSkill", "MiscSkill")]
    //[InlineData("TheBloke_StableBeluga-13B-GGML", 1, "medium", "SummarizeSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "trivial", "Comm_simple.txt", "Danse_simple.txt", "WriterSkill", "MiscSkill")]
    [InlineData(true, "TheBloke_LLaMA2-13B-Tiefighter-GGUF", 1, "medium", "Comm_simple.txt", "Danse_simple.txt", "WriterSkill", "MiscSkill")]
    public async Task ChatGptOffloadsToOobaboogaUsingPlannerAsync(bool succeedsOffloading, string completionName, int nbPromptTests, string difficulty, string inputFile, string validationFile, params string[] skillNames)
    {
        // Create a plan using SequentialPlanner based on difficulty
        var modifiedStartGoal = StartGoal.Replace("distinct difficulties", $"{difficulty} difficulties", StringComparison.OrdinalIgnoreCase);
        var textPath = System.IO.Path.Combine(this._textDirectory, inputFile);
        var validationTextPath = System.IO.Path.Combine(this._textDirectory, validationFile);

        Func<IKernel, CancellationToken, bool, Task<Plan>> planFactory = async (kernel, token, isValidation) =>
        {
            var planner = new SequentialPlanner(kernel);
            var inputPath = textPath;
            if (isValidation)
            {
                inputPath = validationTextPath;
            }

            var input = await System.IO.File.ReadAllTextAsync(inputPath, token);
            var plan = await planner.CreatePlanAsync(modifiedStartGoal, token);

            this._testOutputHelper.LogDebug("Plan proposed by primary connector:\n {0}\n", plan.ToJson(true));

            plan.State.Update(input);
            var inputKey = "INPUT";
            var inputToken = "$INPUT";
            if (plan.Steps[0].Parameters[inputKey] != inputToken)
            {
                this._testOutputHelper.LogTrace("Fixed generated plan first param to variable text");
                plan.Steps[0].Parameters.Set(inputKey, inputToken);
            }

            this._testOutputHelper.LogDebug("Plan After update:\n {0}\n", plan.ToJson(true));
            return plan;
        };

        List<string>? modelNames = null;
        if (!string.IsNullOrEmpty(completionName))
        {
            modelNames = new List<string> { completionName };
        }

        await this.ChatGptOffloadsToOobaboogaAsync(succeedsOffloading, planFactory, modelNames, nbPromptTests, skillNames).ConfigureAwait(false);
    }

    private async Task ChatGptOffloadsToOobaboogaAsync(bool succeedsOffloading, Func<IKernel, CancellationToken, bool, Task<Plan>> planFactory, List<string>? modelNames, int nbPromptTests, params string[] skillNames)
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

    private async Task<(decimal, decimal, List<(ConnectorPromptEvaluation, AnalysisJob)>)> ExecutePlansAndOptimizeAsync(IKernel kernel, Func<IKernel, CancellationToken, bool, Task<Plan>> planFactory, MultiTextCompletionSettings settings, Stopwatch sw)
    {
        var initialElapsed = sw.Elapsed;

        // Create a plan
        this._testOutputHelper.LogTrace("\n# Loading Test plan\n");
        var plan1 = await planFactory(kernel, this._cleanupToken.Token, false);
        var plan1Json = plan1.ToJson();
        this._testOutputHelper.LogDebug("Plan used for multi-connector first pass: {0}", plan1Json);
        var planBuildingTimeElapsed = sw.Elapsed;

        // Execute the plan once with primary connector
        this._testOutputHelper.LogTrace("\n# 1st Run of plan with primary connector\n");
        //We enable sampling and analysis trigger. There is a lock to prevent automatic analysis starting while the test is running, but we'll manually trigger analysis after the test is done
        settings.EnablePromptSampling = true;
        settings.AnalysisSettings.EnableAnalysis = true;
        var firstPassResult = await settings.ExecuteAsync(plan1, kernel, cancellationToken: this._cleanupToken.Token, computeCost: true).ConfigureAwait(false);
        this._testOutputHelper.LogTrace("\n# 1st run finished in {0}\n", firstPassResult.Duration);
        this._testOutputHelper.LogDebug("Result from primary connector execution of Plan used for multi-connector evaluation with duration {0} and cost {1}:\n {2}\n", firstPassResult.Duration, firstPassResult.Cost, firstPassResult.Result);

        // Perform tests, evaluation, and optimization
        var optimizationResults = await settings.OptimizeAsync(this._testOutputHelper).ConfigureAwait(false);
        var optimizationDoneElapsed = sw.Elapsed;
        var optimizationDuration = optimizationDoneElapsed - planBuildingTimeElapsed;
        settings.AnalysisSettings.EnableAnalysis = false;
        this._testOutputHelper.LogTrace("\n# Optimization task finished in {0}\n", optimizationDuration);
        this._testOutputHelper.LogDebug("Optimized with suggested settings: {0}\n", Json.Encode(Json.Serialize(optimizationResults.SuggestedSettings), true));

        //Re execute plan with suggested settings
        var ctx = kernel.CreateNewContext();
        var plan2 = Plan.FromJson(plan1Json, ctx.Functions, true);
        this._testOutputHelper.LogTrace("\n# 2nd run of plan with updated settings and variable completions\n");
        var secondPassResult = await settings.ExecuteAsync(plan2, kernel, cancellationToken: this._cleanupToken.Token, computeCost: true).ConfigureAwait(false);
        this._testOutputHelper.LogTrace("\n# 2nd run finished in {0}\n", secondPassResult.Duration);
        this._testOutputHelper.LogDebug("Result from vetted connector execution of Plan used for multi-connector evaluation with duration {0} and cost {1}:\n {2}\n", secondPassResult.Duration, secondPassResult.Cost, secondPassResult.Result);

        // We validate the new connector with a new plan with distinct data
        this._testOutputHelper.LogTrace("\n# Loading validation plan from factory\n");
        var plan3 = await planFactory(kernel, this._cleanupToken.Token, true);
        this._testOutputHelper.LogDebug("Plan used for multi-connector validation: {0}", plan3.ToJson(true));

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
    /// Configures a kernel with MultiTextCompletion comprising a primary OpenAI connector with parameters defined in main settings for OpenAI integration tests, and Oobabooga secondary connectors with parameters defined in the MultiConnector part of the settings file. Returns null if no matching secondary connector is found in configuration
    /// </summary>
    private IKernel? InitializeKernel(MultiTextCompletionSettings multiTextCompletionSettings, List<string>? modelNames, MultiOobaboogaConnectorConfiguration multiOobaboogaConnectorConfiguration, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= CancellationToken.None;

        var openAiConfiguration = this._configuration.GetSection("OpenAI").Get<OpenAIConfiguration>();
        Assert.NotNull(openAiConfiguration);

        ITextCompletion openAiConnector;

        string testOrChatModelId;
        if (openAiConfiguration.ChatModelId != null)
        {
            testOrChatModelId = openAiConfiguration.ChatModelId;
            openAiConnector = new OpenAIChatCompletion(testOrChatModelId, openAiConfiguration.ApiKey, loggerFactory: this._testOutputHelper);
        }
        else
        {
            testOrChatModelId = openAiConfiguration.ModelId;
            openAiConnector = new OpenAITextCompletion(testOrChatModelId, openAiConfiguration.ApiKey, loggerFactory: this._testOutputHelper);
        }

        var openAiNamedCompletion = new NamedTextCompletion(testOrChatModelId, openAiConnector)
        {
            MaxTokens = 4096,
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

        var builder = new KernelBuilder()
            .WithLoggerFactory(this._testOutputHelper);

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
