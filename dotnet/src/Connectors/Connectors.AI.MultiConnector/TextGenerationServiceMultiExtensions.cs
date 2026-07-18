// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Bridges the Semantic Kernel 1.78 <see cref="ITextGenerationService"/> API to the legacy
/// Complete/GetCompletions call shape still used by the multi-connector orchestration and analysis
/// code. Result-wrapper types (<c>ITextResult</c>/<c>ITextStreamingResult</c>/<c>ModelResult</c>)
/// were removed in 1.78: services return <see cref="TextContent"/> directly, so these helpers
/// project that materialized content back to the plain strings the legacy logic reasons in.
/// </summary>
public static class TextGenerationServiceMultiExtensions
{
    /// <summary>
    /// Requests completions and projects every result to its text content.
    /// Replaces the legacy <c>ITextCompletion.GetCompletionsAsync</c> that returned
    /// <c>IReadOnlyList&lt;ITextResult&gt;</c> requiring a subsequent <c>GetCompletionAsync</c> call.
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetCompletionsAsync(
        this ITextGenerationService service,
        string prompt,
        PromptExecutionSettings? requestSettings,
        CancellationToken cancellationToken = default)
    {
        var contents = await service.GetTextContentsAsync(prompt, requestSettings, kernel: null, cancellationToken).ConfigureAwait(false);
        var results = new List<string>(contents.Count);
        foreach (var content in contents)
        {
            results.Add(content.Text ?? string.Empty);
        }

        return results;
    }

    /// <summary>
    /// Requests a single completion and returns its text content, or <c>null</c> when the model
    /// produced no output. Replaces the legacy <c>CompleteAsync</c> extension on <c>ITextCompletion</c>.
    /// </summary>
    public static async Task<string?> CompleteAsync(
        this ITextGenerationService service,
        string prompt,
        PromptExecutionSettings? requestSettings,
        CancellationToken cancellationToken = default)
    {
        var contents = await service.GetTextContentsAsync(prompt, requestSettings, kernel: null, cancellationToken).ConfigureAwait(false);
        return contents.Count > 0 ? contents[0].Text : null;
    }

    /// <summary>
    /// Streams completion chunks projected to their text content. Replaces the legacy
    /// <c>CompleteStreamAsync</c> extension on <c>ITextCompletion</c> that returned
    /// <c>IAsyncEnumerable&lt;string&gt;</c>.
    /// </summary>
    public static async IAsyncEnumerable<string> CompleteStreamAsync(
        this ITextGenerationService service,
        string prompt,
        PromptExecutionSettings? requestSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in service.GetStreamingTextContentsAsync(prompt, requestSettings, kernel: null, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk.Text ?? string.Empty;
        }
    }
}
