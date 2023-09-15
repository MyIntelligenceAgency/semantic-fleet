// Copyright (c) MyIA. All rights reserved.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;

/// <summary>
/// Oobabooga implementation of <see cref="IChatResult"/> and <see cref="ITextResult"/>. Actual response object is stored in a ModelResult instance, and completion text is simply passed forward.
/// </summary>
public sealed class ChatCompletionResult : IChatResult, ITextResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCompletionResult"/> class from the deserialized result of a request to a Oobabooga chat API.
    /// </summary>
    public ChatCompletionResult(ChatCompletionResponseHistory responseData)
    {
        this._responseHistory = responseData;
        this.ModelResult = new ModelResult(responseData);
    }

    /// <summary>
    /// Gets the <see cref="ModelResult"/> instance containing the actual response object.
    /// </summary>
    public ModelResult ModelResult { get; }

    /// <inheritdoc/>
    public Task<ChatMessageBase> GetChatMessageAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((ChatMessageBase)new SKChatMessage(this._responseHistory.History.Visible.Last()));
    }

    /// <inheritdoc/>
    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        var message = await this.GetChatMessageAsync(cancellationToken).ConfigureAwait(false);

        return message.Content;
    }

    #region private ================================================================================

    private readonly ChatCompletionResponseHistory _responseHistory;

    #endregion
}
