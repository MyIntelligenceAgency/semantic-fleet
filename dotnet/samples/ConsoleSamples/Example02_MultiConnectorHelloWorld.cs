// Copyright (c) MyIA. All rights reserved.

// ReSharper disable once InconsistentNaming

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

namespace MyIA.SemanticFleet.ConsoleSamples;

// ReSharper disable once InconsistentNaming
/// <summary>
/// This class provides an example of how to use the MultiConnector with OpenAI-compatible secondary connectors.
/// </summary>
#pragma warning disable CA1707
public static class Example02_MultiConnectorHelloWorld
#pragma warning restore CA1707
{
    /// <summary>
    /// This method runs the MultiConnector HelloWorld program asynchronously.
    /// </summary>
    /// <returns>
    /// A Task representing the asynchronous operation.
    /// </returns>
    public static async Task RunAsync()
    {
        Console.WriteLine("======== MultiConnector HelloWorld ========");

        var creditor = new CallRequestCostCreditor();

        // The most common settings for a MultiTextCompletion are illustrated below, most of them have default values and are optional
        var settings = new MultiTextCompletionSettings()
        {
            Creditor = creditor,
            // We set connectors comparer to only attend to completion cost
            ConnectorComparer = MultiTextCompletionSettings.GetWeightedConnectorComparer(0, 1),
            AnalysisSettings = new()
            {
                // We set the maximum number of tests to perform to 1.
                // Alternatively, we could define a temperature transformation to set a positive temperature and gather distinct test results from our single prompt
                NbPromptTests = 1
            }
        };

        //string jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions() { WriteIndented = true });
        //Console.WriteLine($"Multicompletion settings: {jsonString}");

        // Creating a cancellation token source to be able to cancel the request
        using CancellationTokenSource cleanupToken = new();

        //Creating the primary connector. SK 1.78: OpenAIChatCompletionService implements both
        //IChatCompletionService and ITextGenerationService, so it slots directly into the
        //MultiConnector pipeline whatever the configured model flavour.
        string testOrChatModelId = TestConfiguration.OpenAI.ChatModelId ?? TestConfiguration.OpenAI.ModelId;
        ITextGenerationService openAiConnector = new OpenAIChatCompletionService(testOrChatModelId, TestConfiguration.OpenAI.ApiKey);

        // Creating the corresponding named completion
        var openAiNamedCompletion = new NamedTextCompletion(testOrChatModelId, openAiConnector)
        {
            MaxTokens = TestConfiguration.OpenAI.MaxTokens,
            CostPer1000Token = TestConfiguration.OpenAI.CostPer1000Token,
            TokenCountFunc = TokenCountFunctions.Map[TestConfiguration.OpenAI.TokenCountFunction],
            //We did not observe any limit on Open AI concurrent calls
            MaxDegreeOfParallelism = 5,
        };

        Console.WriteLine($"Name of Primary Completion: {openAiNamedCompletion.Name}");

        // Creating the secondary connectors. We use a dedicated helper, but you can create them manually if you want.
        // Any OpenAI-compatible provider works: leave EndPoint null for the official OpenAI endpoint,
        // or point it at a router / local server exposing the OpenAI chat-completions API.
        var multiConnectorConfiguration = new MultiOpenAICompatibleConnectorConfiguration();
        multiConnectorConfiguration.OpenAICompatibleCompletions.Add(new OpenAICompatibleConnectorConfiguration()
        {
            Name = "OpenAICompatible1",
            ModelId = TestConfiguration.OpenAI.ModelId,
            ApiKey = TestConfiguration.OpenAI.ApiKey,
            UseChatCompletion = true,
            CostPer1000Token = 0.0005m,
        });
        multiConnectorConfiguration.IncludedConnectors.Add("OpenAICompatible1");
        var secondaryCompletions = multiConnectorConfiguration.CreateNamedCompletions();

        for (int i = 0; i < secondaryCompletions.Count; i++)
        {
            Console.WriteLine($"Name of Secondary Completion #{i}: {secondaryCompletions[i].Name}");
        }

        var builder = Kernel.CreateBuilder();

        builder.WithMultiConnectorCompletionService(
            serviceId: null,
            settings: settings,
            mainTextCompletion: openAiNamedCompletion,
            setAsDefault: true,
            analysisTaskCancellationToken: cleanupToken.Token,
            otherCompletions: secondaryCompletions.ToArray());

        var kernel = builder.Build();

        var text = @"A long time ago, people wanted to tell others their stories. First, they wrote letters with their hands. They would send these letters to friends far away. Sometimes, people waited a lot of days to get a letter.

After that, a big machine called the printing press was made. It could make many copies of a story quickly. More people could read the same thing without waiting.

Next, there was a telephone. With it, people could talk and listen to friends who were far. They didn’t have to wait for letters anymore.

Then, there was a thing called television. People could watch stories on it, like a play. They didn’t need to go outside.

Lastly, came mobile phones and computers. People could send messages fast. With the internet, they could also use something called social media to share stories with many people at once.";

        var prompt = $"Summarize the following text in one sentence:\n{text}\n\nSummary:";

        // SK 1.78: CreateSemanticFunction was replaced by CreateFunctionFromPrompt; the
        // MultiCompletionRequestSettings keeps driving the cost-offload pipeline.
        var simpleSemanticFunction = kernel.CreateFunctionFromPrompt(prompt, new MultiCompletionRequestSettings() { MaxTokensMulti = 100 });

        // We enable prompt sampling and analysis so that the multiconnector tests our prompt on our secondary connectors after it is run on the primary connector
        settings.EnablePromptSampling = true;
        settings.AnalysisSettings.EnableAnalysis = true;

        // Subscribe to the Evaluation completed event
        TaskCompletionSource<EvaluationCompletedEventArgs> evaluationCompletedTaskSource = new();
        settings.AnalysisSettings.EvaluationCompleted += (sender, args) =>
        {
            evaluationCompletedTaskSource.SetResult(args);
        };

        // Subscribe to the SuggestionCompleted event
        TaskCompletionSource<SuggestionCompletedEventArgs> suggestionCompletedTaskSource = new();
        settings.AnalysisSettings.SuggestionCompleted += (sender, args) =>
        {
            suggestionCompletedTaskSource.SetResult(args);
        };

        // Run the semantic function with our primary connector
        var result = await kernel.InvokeAsync(simpleSemanticFunction, cancellationToken: cleanupToken.Token).ConfigureAwait(false);
        Console.WriteLine($"Result from primary connector: {result}");

        Console.WriteLine($"Cost from running primary connector's completion: {creditor.OngoingCost}");

        // Wait for the evaluation completed event to be raised
        var analysisResults = await evaluationCompletedTaskSource.Task.ConfigureAwait(false);
        Console.WriteLine("Evaluation for secondary connectors finished");

        // Wait for the suggestion completed event to be raised
        var optimizationResults = await suggestionCompletedTaskSource.Task.ConfigureAwait(false);
        Console.WriteLine("Optimization task finished");

        //var strAnalysisResults = JsonSerializer.Serialize(analysisResults.CompletionAnalysis, new JsonSerializerOptions() { WriteIndented = true });
        //Console.WriteLine($"Analysis results: {strAnalysisResults}");

        //var strSuggestedSettings = JsonSerializer.Serialize(optimizationResults.SuggestedSettings, new JsonSerializerOptions() { WriteIndented = true });
        //Console.WriteLine($"Updated settings: {strSuggestedSettings}");

        // By disabling prompt sampling and automatic analysis, we freeze the settings to the ones suggested by the optimization task 
        settings.EnablePromptSampling = false;
        settings.AnalysisSettings.EnableAnalysis = false;

        creditor.Reset();

        // Run the semantic function with our primary connector
        var secondaryResult = await kernel.InvokeAsync(simpleSemanticFunction, cancellationToken: cleanupToken.Token).ConfigureAwait(false);

        Console.WriteLine($"Result from optimized connector: {secondaryResult}");

        Console.WriteLine($"Cost from running secondary connector's completion: {creditor.OngoingCost}");
    }
}
