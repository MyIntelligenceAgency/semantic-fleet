// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.ArithmeticMocks;

/// <summary>
/// Base class for arithmetic streaming results that implements the ITextStreamingResult interface.
/// </summary>
public abstract class ArithmeticStreamingResultBase : ITextStreamingResult
{
    private ModelResult? _modelResult;

    /// <summary>
    /// Property returning the model result, either the result of the arithmetic operation or the result of the vetting operation.
    /// </summary>
    public ModelResult ModelResult => this._modelResult ?? this.GenerateModelResult().Result;

    /// <summary>
    /// Abstract method to build model result, either the result of the arithmetic operation or the result of the vetting operation.
    /// </summary>
    protected abstract Task<ModelResult> GenerateModelResult();

    /// <summary>
    /// Method returning the prompt string result from model result
    /// </summary>
    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        this._modelResult = await this.GenerateModelResult().ConfigureAwait(false);
        return this.ModelResult?.GetResult<string>() ?? string.Empty;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetCompletionStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this._modelResult = await this.GenerateModelResult().ConfigureAwait(false);

        string resultText = this.ModelResult.GetResult<string>();
        // Your model logic here
        var streamedOutput = resultText.Split(' ');
        foreach (string word in streamedOutput)
        {
            yield return $"{word} ";
        }
    }
}
