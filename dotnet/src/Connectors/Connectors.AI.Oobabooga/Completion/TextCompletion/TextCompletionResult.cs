// Copyright (c) MyIA. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

/// <summary>
/// Oobabooga implementation of <see cref="ITextResult"/>. Actual response object is stored in a ModelResult instance, and completion text is simply passed forward.
/// </summary>
public sealed class TextCompletionResult : ITextResult
{
    private readonly ModelResult _responseData;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextCompletionResult"/> class from the deserialized result of a request to a Oobabooga API.
    /// </summary>
    public TextCompletionResult(TextCompletionResponseText responseData)
    {
        this._responseData = new ModelResult(responseData);
    }

    /// <inheritdoc/>
    public ModelResult ModelResult => this._responseData;

    /// <inheritdoc/>
    public Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this._responseData.GetResult<TextCompletionResponseText>().Text ?? string.Empty);
    }
}
