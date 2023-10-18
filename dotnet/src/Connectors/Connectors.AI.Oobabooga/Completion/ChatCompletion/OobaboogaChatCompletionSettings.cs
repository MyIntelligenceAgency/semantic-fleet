﻿// Copyright (c) MyIA. All rights reserved.

using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;

/// <summary>
/// Settings for <see cref="OobaboogaChatCompletion"/>. It controls how oobabooga completion requests are made. Some parameters control the endpoint to which requests are sent, others control the behavior of the requests themselves. In particular, oobabooga offers a streaming API through websockets, and this class controls how websockets are managed for optimal resources management.
/// </summary>
public class OobaboogaChatCompletionSettings : OobaboogaCompletionSettings<OobaboogaChatCompletionRequestSettings>
{
    private const string ChatBlockingUriPath = "/api/v1/chat";
    private const string ChatStreamingUriPath = "/api/v1/chat-stream";

    /// <summary>
    /// This is the default constructor for deserialization purposes. It is not meant to be used directly.
    /// </summary>
    [JsonConstructor]
    public OobaboogaChatCompletionSettings() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OobaboogaChatCompletionSettings"/> class, which controls how oobabooga chat completion requests are made and concurrent access usage.
    /// </summary>
    /// <param name="endpoint">The service API endpoint to which requests should be sent.</param>
    /// <param name="blockingPort">The port used for handling blocking requests. Default value is 5000</param>
    /// <param name="streamingPort">The port used for handling streaming requests. Default value is 5005</param>
    /// <param name="concurrentSemaphore">You can optionally set a hard limit on the max number of concurrent calls to the either of the completion methods by providing a <see cref="SemaphoreSlim"/>. Calls in excess will wait for existing consumers to release the semaphore</param>
    /// <param name="useWebSocketsPooling">If true, websocket clients will be recycled in a reusable pool as long as concurrent calls are detected</param>
    /// <param name="webSocketsCleanUpCancellationToken">if websocket pooling is enabled, you can provide an optional CancellationToken to properly dispose of the clean up tasks when disposing of the connector</param>
    /// <param name="keepAliveWebSocketsDuration">When pooling is enabled, pooled websockets are flushed on a regular basis when no more connections are made. This is the time to keep them in pool before flushing</param>
    /// <param name="webSocketFactory">The WebSocket factory used for making streaming API requests. Note that only when pooling is enabled will websocket be recycled and reused for the specified duration. Otherwise, a new websocket is created for each call and closed and disposed afterwards, to prevent data corruption from concurrent calls.</param>
    /// <param name="httpClient">Optional. The HTTP client used for making blocking API requests. If not specified, a default client will be used.</param>
    /// <param name="loggerFactory">Application logger</param>
    public OobaboogaChatCompletionSettings(Uri? endpoint = default,
        int blockingPort = 5000,
        int streamingPort = 5005,
        SemaphoreSlim? concurrentSemaphore = null,
        bool useWebSocketsPooling = true,
        CancellationToken? webSocketsCleanUpCancellationToken = default,
        int keepAliveWebSocketsDuration = 100,
        Func<ClientWebSocket>? webSocketFactory = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null) : base(endpoint, blockingPort, streamingPort, concurrentSemaphore, useWebSocketsPooling, webSocketsCleanUpCancellationToken, keepAliveWebSocketsDuration, webSocketFactory, httpClient, loggerFactory, ChatBlockingUriPath, ChatStreamingUriPath)
    {
    }
}
