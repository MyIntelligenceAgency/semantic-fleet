// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// Base class for Oobabooga completion with common scaffolding shared between Text and Chat completions and generic parameters corresponding to the various types used in Text and Chat completions.
/// </summary>
public abstract class OobaboogaCompletionBase<TCompletionInput, TOobaboogaParameters, TCompletionRequest, TCompletionResponse> : OobaboogaCompletionBase
    where TOobaboogaParameters : OobaboogaCompletionRequestSettings, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OobaboogaCompletionBase"/> class.
    /// </summary>
    /// <param name="oobaboogaSettings">The settings controlling how calls to the Oobabooga server are made</param>
    protected OobaboogaCompletionBase(OobaboogaCompletionSettings<TOobaboogaParameters>? oobaboogaSettings = default) : base(oobaboogaSettings ?? new())
    {
    }

    /// <summary>
    /// This method contains the logic to deal with performing HTTP calls to the Oobabooga API. It is used by both Chat and Text completion.
    /// Returns the extracted completion texts (the connector no longer wraps them in a result-wrapper; the caller maps them to ChatMessageContent / TextContent).
    /// </summary>
    protected async Task<IReadOnlyList<string>> GetCompletionsBaseAsync(
        TCompletionInput input,
        PromptExecutionSettings? executionSettings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await this.OobaboogaSettings.StartConcurrentCallAsync(cancellationToken).ConfigureAwait(false);

            var completionRequest = this.CreateCompletionRequest(input, executionSettings);

            var requestJson = JsonSerializer.Serialize(completionRequest);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, this.OobaboogaSettings.BlockingUri!)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            httpRequestMessage.Headers.Add("User-Agent", "Semantic-Kernel");

            using var response = await this.OobaboogaSettings.HttpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            TCompletionResponse? completionResponse = JsonSerializer.Deserialize<TCompletionResponse>(body);

            if (completionResponse is null)
            {
                throw new KernelException($"Unexpected response from Oobabooga API:\n{body}");
            }

            return this.ExtractCompletionTexts(completionResponse);
        }
        catch (Exception e) when (e is not KernelException && !e.IsCriticalException())
        {
            throw new KernelException($"Something went wrong with Oobabooga Completion: {e.Message}", e);
        }
        finally
        {
            this.OobaboogaSettings.FinishConcurrentCall();
        }
    }

    /// <summary>
    /// This method contains the logic to deal with performing websocket calls to the Oobabooga API. It is used by both Chat and Text completion.
    /// Yields the streaming text chunks (the connector no longer wraps them in a result-wrapper; the caller maps them to StreamingChatMessageContent / StreamingTextContent).
    /// </summary>
    protected async IAsyncEnumerable<string> GetStreamingCompletionsBaseAsync(
        TCompletionInput input,
        PromptExecutionSettings? executionSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await this.OobaboogaSettings.StartConcurrentCallAsync(cancellationToken).ConfigureAwait(false);

        var completionRequest = this.CreateCompletionRequest(input, executionSettings);

        var requestJson = JsonSerializer.Serialize(completionRequest);

        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        ClientWebSocket? clientWebSocket = null;
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });

        try
        {
            // if pooling is enabled, web socket is going to be recycled for reuse, if not it will be properly disposed of after the call
#pragma warning disable CA2000 // Dispose objects before losing scope
            if (!this.OobaboogaSettings.UseWebSocketsPooling || !this.OobaboogaSettings.WebSocketPool.TryTake(out clientWebSocket))
            {
                clientWebSocket = this.OobaboogaSettings.WebSocketFactory();
            }
#pragma warning restore CA2000 // Dispose objects before losing scope
            if (clientWebSocket.State == WebSocketState.None)
            {
                await clientWebSocket.ConnectAsync(this.OobaboogaSettings.StreamingUri, cancellationToken).ConfigureAwait(false);
            }

            var sendSegment = new ArraySegment<byte>(requestBytes);
            await clientWebSocket.SendAsync(sendSegment, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);

            var processingTask = this.ProcessWebSocketMessagesAsync(clientWebSocket, channel.Writer, cancellationToken);

            // Yield streaming text chunks as they are produced by the websocket processing task
            await foreach (var text in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return text;
            }

            // Await the processing task to make sure it's finished before continuing
            await processingTask.ConfigureAwait(false);
        }
        finally
        {
            if (clientWebSocket != null)
            {
                if (this.OobaboogaSettings.UseWebSocketsPooling && clientWebSocket.State == WebSocketState.Open)
                {
                    this.OobaboogaSettings.WebSocketPool.Add(clientWebSocket);
                }
                else
                {
                    await this.OobaboogaSettings.DisposeClientGracefullyAsync(clientWebSocket).ConfigureAwait(false);
                }
            }

            this.OobaboogaSettings.FinishConcurrentCall();
        }
    }

    /// <summary>
    /// This method contains the logic to extract completion texts from a blocking completion call response.
    /// </summary>
    protected abstract IReadOnlyList<string> ExtractCompletionTexts([DisallowNull] TCompletionResponse completionResponse);

    /// <summary>
    /// This method contains the logic to extract a single streaming text chunk (delta for chat, direct for text) from a websocket streaming response.
    /// Returns null when the message is not a text-stream event (e.g. stream_end).
    /// </summary>
    protected abstract string? ExtractStreamText(CompletionStreamingResponseBase response);

    /// <summary>
    /// This method contains the logic to build the Oobabooga request object. It is used by both Text and Chat completion, the latter extending the former with additional parameters.
    /// </summary>
    protected abstract TCompletionRequest CreateCompletionRequest(TCompletionInput input, PromptExecutionSettings? executionSettings);

    /// <summary>
    /// That method is responsible for processing the websocket messages that build a streaming response. It writes the extracted text chunks into the provided channel. It is crucial that it is run asynchronously to prevent a deadlock with results iteration.
    /// </summary>
    protected async Task ProcessWebSocketMessagesAsync(ClientWebSocket clientWebSocket, ChannelWriter<string> writer, CancellationToken cancellationToken)
    {
        var buffer = new byte[this.OobaboogaSettings.WebSocketBufferSize];
        var finishedProcessing = false;
        while (!finishedProcessing && !cancellationToken.IsCancellationRequested)
        {
            MemoryStream messageStream = new();
            WebSocketReceiveResult result;
            do
            {
                var segment = new ArraySegment<byte>(buffer);
                result = await clientWebSocket.ReceiveAsync(segment, cancellationToken).ConfigureAwait(false);
                await messageStream.WriteAsync(buffer, 0, result.Count, cancellationToken).ConfigureAwait(false);
            } while (!result.EndOfMessage);

            messageStream.Seek(0, SeekOrigin.Begin);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string messageText;
                using (var reader = new StreamReader(messageStream, Encoding.UTF8))
                {
                    messageText = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                var responseObject = this.GetResponseObject(messageText);

                if (responseObject is null)
                {
                    throw new KernelException($"Unexpected response from Oobabooga API: {messageText}");
                }

                switch (responseObject.Event)
                {
                    case CompletionStreamingResponseBase.ResponseObjectTextStreamEvent:
                        var chunk = this.ExtractStreamText(responseObject);
                        if (chunk is not null)
                        {
                            await writer.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
                        }
                        break;
                    case CompletionStreamingResponseBase.ResponseObjectStreamEndEvent:
                        writer.TryComplete();
                        if (!this.OobaboogaSettings.UseWebSocketsPooling)
                        {
                            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge stream-end oobabooga message", CancellationToken.None).ConfigureAwait(false);
                        }

                        finishedProcessing = true;
                        break;
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None).ConfigureAwait(false);
                writer.TryComplete();
                finishedProcessing = true;
            }

            if (clientWebSocket.State != WebSocketState.Open)
            {
                writer.TryComplete();
                finishedProcessing = true;
            }
        }
    }
}

/// <summary>
/// Base class for Oobabooga completion with common scaffolding shared between Text and Chat completion
/// </summary>
public abstract class OobaboogaCompletionBase
{
    private protected readonly OobaboogaCompletionSettings OobaboogaSettings;
    private ILogger? Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OobaboogaCompletionBase"/> class.
    /// </summary>
    /// <param name="oobaboogaSettings">The settings controlling how calls to the Oobabooga server are made</param>
    protected OobaboogaCompletionBase(OobaboogaCompletionSettings oobaboogaSettings)
    {
        this.OobaboogaSettings = oobaboogaSettings;
        this.Logger = oobaboogaSettings.LoggerFactory is not null ? oobaboogaSettings.LoggerFactory.CreateLogger(this.GetType()) : NullLogger.Instance;
    }

    /// <summary>
    /// This method contains the logic to extract results from streaming completion call web socket messages.
    /// </summary>
    protected abstract CompletionStreamingResponseBase? GetResponseObject(string messageText);

    /// <summary>
    /// Metadata attributes for the AI service (satisfies <see cref="global::Microsoft.SemanticKernel.Services.IAIService.Attributes"/>).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    #region private ================================================================================

    /// <summary>
    /// Logs Oobabooga action details.
    /// </summary>
    /// <param name="callerMemberName">Caller member name. Populated automatically by runtime.</param>
    private protected void LogActionDetails([CallerMemberName] string? callerMemberName = default)
    {
        this.Logger?.LogInformation("Oobabooga Action: {Action}.", callerMemberName);
    }

    #endregion
}
