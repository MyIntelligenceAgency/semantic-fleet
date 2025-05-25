// Copyright (c) MyIA. All rights reserved.

// ReSharper disable once InconsistentNaming

using Microsoft.SemanticKernel.AI.TextCompletion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

namespace MyIA.SemanticFleet.ConsoleSamples;

// ReSharper disable once InconsistentNaming
/// <summary>
/// This class provides an example of how to use the OobaboogaTextCompletion class.
/// </summary>
#pragma warning disable CA1707
public static class Example01_OobaboogaHelloWorld
#pragma warning restore CA1707
{
    /// <summary>
    /// This method runs the Oobabooga HelloWorld program asynchronously.
    /// </summary>
    /// <returns>
    /// A Task representing the asynchronous operation.
    /// </returns>
    public static async Task RunAsync()
    {
        Console.WriteLine("======== Oobabooga HelloWorld ========");

        var settings = new OobaboogaTextCompletionSettings(endpoint: new Uri(TestConfiguration.Oobabooga.EndPoint!), blockingPort: TestConfiguration.Oobabooga.BlockingPort, streamingPort: TestConfiguration.Oobabooga.StreamingPort);
        var oobabooga = new OobaboogaTextCompletion(settings);

        var startText = "Hello, world!";
        Console.WriteLine($"Start text: {startText}");
        // Get text completions
        var completions = await oobabooga.CompleteAsync(startText, new OobaboogaCompletionRequestSettings()
        {
            Temperature = 0.01,
            MaxNewTokens = 20,
            TopP = 0.1,
        });

        Console.WriteLine($"Completion: {completions}");

        //return Task.CompletedTask;
    }
}
