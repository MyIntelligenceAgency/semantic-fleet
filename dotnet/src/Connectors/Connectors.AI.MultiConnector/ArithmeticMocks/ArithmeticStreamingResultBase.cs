// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.ArithmeticMocks;

/// <summary>
/// Base class for arithmetic mock results. The legacy version wrapped a Semantic Kernel
/// <c>ModelResult</c>/<c>ITextResult</c>/<c>ITextStreamingResult</c> (all removed in 1.78); the
/// functional logic is preserved here as a plain string producer so that
/// <see cref="ArithmeticCompletionService"/> can materialize it into
/// <c>TextContent</c>/<c>StreamingTextContent</c> directly.
/// </summary>
public abstract class ArithmeticStreamingResultBase
{
    private string? _result;

    /// <summary>
    /// Property returning the computed result text (arithmetic operation result or vetting verdict),
    /// computing it lazily on first access.
    /// </summary>
    public string Result => this._result ?? this.GenerateResultAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Abstract method computing the result text (arithmetic operation result or vetting verdict).
    /// </summary>
    protected abstract Task<string> GenerateResultAsync();

    /// <summary>
    /// Computes (and caches) the result text. Replaces the legacy <c>GetCompletionAsync</c>.
    /// </summary>
    public async Task<string> GetResultAsync(CancellationToken cancellationToken = default)
    {
        this._result = await this.GenerateResultAsync().ConfigureAwait(false);
        return this._result;
    }

    /// <summary>
    /// Streams the result text word by word, simulating token-by-token model output.
    /// Replaces the legacy <c>GetCompletionStreamingAsync</c>.
    /// </summary>
    public async IAsyncEnumerable<string> GetStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this._result = await this.GenerateResultAsync().ConfigureAwait(false);

        string resultText = this._result;
        // Your model logic here
        var streamedOutput = resultText.Split(' ');
        foreach (string word in streamedOutput)
        {
            yield return $"{word} ";
        }
    }
}
