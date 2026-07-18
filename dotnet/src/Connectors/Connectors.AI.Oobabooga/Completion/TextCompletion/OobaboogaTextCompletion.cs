// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

/// <summary>
/// Oobabooga text completion service API.
/// Adapted from <see href="https://github.com/oobabooga/text-generation-webui/tree/main/api-examples"/>
/// </summary>
public sealed class OobaboogaTextCompletion : OobaboogaCompletionBase<string, OobaboogaCompletionRequestSettings, OobaboogaCompletionRequest, TextCompletionResponse>, ITextGenerationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OobaboogaTextCompletion"/> class.
    /// </summary>
    /// <param name="completionRequestSettings">An instance of <see cref="OobaboogaTextCompletionSettings"/>, which are text completion settings specific to Oobabooga api</param>
    public OobaboogaTextCompletion(OobaboogaTextCompletionSettings completionRequestSettings) : base(completionRequestSettings)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken)
    {
        this.LogActionDetails();
        var texts = await this.GetCompletionsBaseAsync(prompt, executionSettings, cancellationToken).ConfigureAwait(false);
        return texts.Select(t => new TextContent(t, modelId: null)).ToList();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        this.LogActionDetails();
        await foreach (var text in this.GetStreamingCompletionsBaseAsync(prompt, executionSettings, cancellationToken))
        {
            yield return new StreamingTextContent(text);
        }
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExtractCompletionTexts(TextCompletionResponse completionResponse)
    {
        return completionResponse.Results.ConvertAll(result => result.Text ?? string.Empty);
    }

    /// <inheritdoc/>
    protected override string? ExtractStreamText(CompletionStreamingResponseBase response)
    {
        return ((TextCompletionStreamingResponse)response).Text;
    }

    /// <inheritdoc/>
    protected override CompletionStreamingResponseBase? GetResponseObject(string messageText)
    {
        return JsonSerializer.Deserialize<TextCompletionStreamingResponse>(messageText);
    }

    /// <summary>
    /// Creates an Oobabooga request, mapping the execution settings fields to their Oobabooga API counter parts.
    /// </summary>
    /// <param name="input">The text to complete.</param>
    /// <param name="executionSettings">The execution settings.</param>
    /// <returns>An Oobabooga CompletionRequest object with the text and completion parameters.</returns>
    protected override OobaboogaCompletionRequest CreateCompletionRequest(string input, PromptExecutionSettings? executionSettings)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentNullException(nameof(input));
        }

        executionSettings ??= new();

        // Prepare the request using the provided parameters.
        var toReturn = OobaboogaCompletionRequest.Create(input, (OobaboogaCompletionSettings<OobaboogaCompletionRequestSettings>)this.OobaboogaSettings, executionSettings);
        return toReturn;
    }
}
