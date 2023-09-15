﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.TextCompletion;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

/// <inheritdoc cref="CompletionStreamingResultBase" />
public sealed class TextCompletionStreamingResult : CompletionStreamingResultBase, ITextStreamingResult
{
    private readonly Channel<string> _responseChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
    {
        SingleReader = true,
        SingleWriter = true,
        AllowSynchronousContinuations = false
    });

    /// <inheritdoc/>
    public override void AppendResponse(CompletionStreamingResponseBase response)
    {
        this.AppendResponse((TextCompletionStreamingResponse)response);
    }

    private void AppendResponse(TextCompletionStreamingResponse response)
    {
        this.ModelResponses.Add(response);
        this._responseChannel.Writer.TryWrite(response.Text);
    }

    /// <inheritdoc/>
    public override void SignalStreamEnd()
    {
        this._responseChannel.Writer.Complete();
    }

    /// <inheritdoc/>
    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        StringBuilder resultBuilder = new();

        await foreach (var chunk in this.GetCompletionStreamingAsync(cancellationToken))
        {
            resultBuilder.Append(chunk);
        }

        return resultBuilder.ToString();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GetCompletionStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await this._responseChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (this._responseChannel.Reader.TryRead(out string? chunk))
            {
                yield return chunk;
            }
        }
    }
}
