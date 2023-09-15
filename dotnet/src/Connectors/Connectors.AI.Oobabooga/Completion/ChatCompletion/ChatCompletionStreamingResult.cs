// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;

/// <summary>
/// Oobabooga chat completion service API streaming implementation using Channels for asynchronous enumeration.
/// </summary>
public sealed class ChatCompletionStreamingResult : CompletionStreamingResultBase, IChatStreamingResult, ITextStreamingResult
{
    private readonly Channel<ChatMessageBase> _chatMessageChannel = Channel.CreateUnbounded<ChatMessageBase>(new UnboundedChannelOptions()
    {
        SingleReader = true,
        SingleWriter = true,
        AllowSynchronousContinuations = false
    });

    private void AppendResponse(ChatCompletionStreamingResponse response)
    {
        this.ModelResponses.Add(response);
        if (response.History.Visible.Count > 0)
        {
            this._chatMessageChannel.Writer.TryWrite(new SKChatMessage(response.History.Visible.Last()));
        }
    }

    /// <inheritdoc/>
    public override void AppendResponse(CompletionStreamingResponseBase response)
    {
        this.AppendResponse((ChatCompletionStreamingResponse)response);
    }

    /// <inheritdoc/>
    public override void SignalStreamEnd()
    {
        this._chatMessageChannel.Writer.Complete();
    }

    /// <inheritdoc/>
    public async Task<ChatMessageBase> GetChatMessageAsync(CancellationToken cancellationToken = default)
    {
        return await this._chatMessageChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatMessageBase> GetStreamingChatMessageAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await this._chatMessageChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (this._chatMessageChannel.Reader.TryRead(out ChatMessageBase? chunk))
            {
                yield return chunk;
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GetCompletionStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var result in this.GetStreamingChatMessageAsync(cancellationToken))
        {
            yield return result.Content;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        var message = await this.GetChatMessageAsync(cancellationToken).ConfigureAwait(false);

        return message.Content;
    }
}
