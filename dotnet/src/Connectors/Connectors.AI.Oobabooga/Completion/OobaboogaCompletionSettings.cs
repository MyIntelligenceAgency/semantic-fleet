﻿// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Diagnostics;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// This is the base class for all Oobabooga completion settings. It controls how oobabooga completion requests are made. Some parameters control the endpoint to which requests are sent, others control the behavior of the requests themselves. In particular, oobabooga offers a streaming API through websockets, and this class controls how websockets are managed for optimal resources management.
/// </summary>
public class OobaboogaCompletionSettings
{
    /// <summary>
    /// This constant relative path for blocking endpoints is according to current oobabooga API implementation
    /// </summary>
    protected const string BlockingUriPath = "/api/v1/generate";

    /// <summary>
    /// This constant relative path for streaming endpoints is according to current oobabooga API implementation
    /// </summary>
    protected const string StreamingUriPath = "/api/v1/stream";

    internal ILoggerFactory? LoggerFactory { get; }
    private ILogger? Logger { get; }

    private readonly int _maxNbConcurrentWebSockets;
    private readonly SemaphoreSlim? _concurrentSemaphore;
    private readonly ConcurrentBag<bool>? _activeConnections;
    internal readonly ConcurrentBag<ClientWebSocket> WebSocketPool = new();
    private readonly int _keepAliveWebSocketsDuration;

    private long _lastCallTicks = long.MaxValue;

    /// <summary>
    /// Controls the size of the buffer used to received websocket packets
    /// </summary>
    public int WebSocketBufferSize { get; set; } = 2048;

    /// <summary>
    /// This is the API endpoint to which non streaming requests are sent, computed from base endpoint, port and relative path.
    /// </summary>
    public Uri? BlockingUri { get; set; }

    /// <summary>
    /// This is the API endpoint to which streaming requests are sent, computed from base endpoint, port and relative path.
    /// </summary>
    public Uri? StreamingUri { get; set; }

    /// <summary>
    /// The HttpClient used for making blocking API requests.
    /// </summary>
    [JsonIgnore]
    public HttpClient HttpClient { get; set; }

    /// <summary>
    /// The factory used to create websockets for making streaming API requests.
    /// </summary>
    [JsonIgnore]
    public Func<ClientWebSocket> WebSocketFactory { get; set; }

    /// <summary>
    /// Determines whether the connector should use websockets pooling to reuse websockets in order to prevent resource exhaustion in case of high load.
    /// </summary>
    public bool UseWebSocketsPooling { get; set; }

    /// <summary>
    /// Default constructor for deserialization
    /// </summary>
    [JsonConstructor]
    public OobaboogaCompletionSettings()
    {
        this.HttpClient = new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false);
        this.WebSocketFactory = () =>
        {
            ClientWebSocket webSocket = new();
            this.SetWebSocketOptions(webSocket);
            return webSocket;
        };
        this._activeConnections = new();
        this._maxNbConcurrentWebSockets = 0;
        this.StartCleanupTask(CancellationToken.None);
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="OobaboogaCompletionSettings"/> class, which controls how oobabooga completion requests are made.
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
    /// <param name="blockingPath">the path for the blocking API relative to the Endpoint base path</param>
    /// <param name="streamingPath">the path for the streaming API relative to the Endpoint base path</param>
    public OobaboogaCompletionSettings(Uri? endpoint = default,
        int blockingPort = 5000,
        int streamingPort = 5005,
        SemaphoreSlim? concurrentSemaphore = null,
        bool useWebSocketsPooling = true,
        CancellationToken? webSocketsCleanUpCancellationToken = default,
        int keepAliveWebSocketsDuration = 100,
        Func<ClientWebSocket>? webSocketFactory = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null,
        string blockingPath = BlockingUriPath,
        string streamingPath = StreamingUriPath)
    {
        endpoint ??= new Uri("http://localhost/");

        this.BlockingUri = new UriBuilder(endpoint)
        {
            Port = blockingPort,
            Path = blockingPath
        }.Uri;
        var streamingUriBuilder = new UriBuilder(endpoint)
        {
            Port = streamingPort,
            Path = streamingPath
        };
        if (streamingUriBuilder.Uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            streamingUriBuilder.Scheme = streamingUriBuilder.Scheme == "https" ? "wss" : "ws";
        }

        this.StreamingUri = streamingUriBuilder.Uri;

        this.HttpClient = httpClient ?? new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false);
        this.LoggerFactory = loggerFactory;
        this.Logger = loggerFactory is not null ? loggerFactory.CreateLogger(this.GetType()) : NullLogger.Instance;

        this.UseWebSocketsPooling = useWebSocketsPooling;
        this._keepAliveWebSocketsDuration = keepAliveWebSocketsDuration;
        if (webSocketFactory != null)
        {
            this.WebSocketFactory = () =>
            {
                var webSocket = webSocketFactory();
                this.SetWebSocketOptions(webSocket);
                return webSocket;
            };
        }
        else
        {
            this.WebSocketFactory = () =>
            {
                ClientWebSocket webSocket = new();
                this.SetWebSocketOptions(webSocket);
                return webSocket;
            };
        }

        // if a hard limit is defined, we use a semaphore to limit the number of concurrent calls, otherwise, we use a stack to track active connections
        if (concurrentSemaphore != null)
        {
            this._concurrentSemaphore = concurrentSemaphore;
            this._maxNbConcurrentWebSockets = concurrentSemaphore.CurrentCount;
        }
        else
        {
            this._activeConnections = new();
            this._maxNbConcurrentWebSockets = 0;
        }

        if (this.UseWebSocketsPooling)
        {
            this.StartCleanupTask(webSocketsCleanUpCancellationToken ?? CancellationToken.None);
        }
    }

    /// <summary>
    /// Sets the options for the <paramref name="clientWebSocket"/>, either persistent and provided by the ctor, or transient if none provided.
    /// </summary>
    private void SetWebSocketOptions(ClientWebSocket clientWebSocket)
    {
        clientWebSocket.Options.SetRequestHeader("User-Agent", Telemetry.HttpUserAgent);
    }

    private void StartCleanupTask(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew<Task>(
            async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await this.FlushWebSocketClientsAsync(cancellationToken).ConfigureAwait(false);
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    /// <summary>
    /// Flushes the web socket clients that have been idle for too long
    /// </summary>
    private async Task FlushWebSocketClientsAsync(CancellationToken cancellationToken)
    {
        // In the cleanup task, make sure you handle OperationCanceledException appropriately
        // and make frequent checks on whether cancellation is requested.
        try
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(this._keepAliveWebSocketsDuration, cancellationToken).ConfigureAwait(false);

                // If another call was made during the delay, do not proceed with flushing
                if (DateTime.UtcNow.Ticks - Interlocked.Read(ref this._lastCallTicks) < TimeSpan.FromMilliseconds(this._keepAliveWebSocketsDuration).Ticks)
                {
                    return;
                }

                while (this.GetCurrentConcurrentCallsNb() == 0 && this.WebSocketPool.TryTake(out ClientWebSocket clientToDispose))
                {
                    await this.DisposeClientGracefullyAsync(clientToDispose).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException exception)
        {
            this.Logger?.LogTrace(message: "FlushWebSocketClientsAsync cleaning task was cancelled", exception: exception);
            while (this.WebSocketPool.TryTake(out ClientWebSocket clientToDispose))
            {
                await this.DisposeClientGracefullyAsync(clientToDispose).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Gets the number of concurrent calls, either by reading the semaphore count or by reading the active connections stack count
    /// </summary>
    private int GetCurrentConcurrentCallsNb()
    {
        if (this._concurrentSemaphore != null)
        {
            return this._maxNbConcurrentWebSockets - this._concurrentSemaphore!.CurrentCount;
        }

        return this._activeConnections!.Count;
    }

    /// <summary>
    /// Starts a concurrent call, either by taking a semaphore slot or by pushing a value on the active connections stack
    /// </summary>
    /// <param name="cancellationToken"></param>
    protected internal async Task StartConcurrentCallAsync(CancellationToken cancellationToken)
    {
        if (this._concurrentSemaphore != null)
        {
            await this._concurrentSemaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            this._activeConnections!.Add(true);
        }
    }

    /// <summary>
    /// Ends a concurrent call, either by releasing a semaphore slot or by popping a value from the active connections stack
    /// </summary>
    protected internal void FinishConcurrentCall()
    {
        if (this._concurrentSemaphore != null)
        {
            this._concurrentSemaphore!.Release();
        }
        else
        {
            this._activeConnections!.TryTake(out _);
        }

        Interlocked.Exchange(ref this._lastCallTicks, DateTime.UtcNow.Ticks);
    }

    /// <summary>
    /// Closes and disposes of a client web socket after use
    /// </summary>
    protected internal async Task DisposeClientGracefullyAsync(ClientWebSocket clientWebSocket)
    {
        try
        {
            if (clientWebSocket.State == WebSocketState.Open)
            {
                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing client before disposal", CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException exception)
        {
            this.Logger?.LogTrace(message: "Closing client web socket before disposal was cancelled", exception: exception);
        }
        catch (WebSocketException exception)
        {
            this.Logger?.LogTrace(message: "Closing client web socket before disposal raised web socket exception", exception: exception);
        }
        finally
        {
            clientWebSocket.Dispose();
        }
    }
}

/// <summary>
/// This class inherits from the base settings controlling the general behavior of Oobabooga completion connectors and introduces a generic type parameter to allow for the use of a custom <see cref="OobaboogaCompletionParameters"/> type controlling the specific completion parameters, which differ between standard text completion and chat completion.
/// </summary>
/// <typeparam name="TParameters"></typeparam>
public class OobaboogaCompletionSettings<TParameters> : OobaboogaCompletionSettings where TParameters : OobaboogaCompletionParameters, new()
{
    [JsonConstructor]
    public OobaboogaCompletionSettings() : base()
    {
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="OobaboogaCompletionSettings"/> class, which controls how oobabooga completion requests are made.
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
    /// <param name="blockingPath">the path for the blocking API relative to the Endpoint base path</param>
    /// <param name="streamingPath">the path for the streaming API relative to the Endpoint base path</param>
    public OobaboogaCompletionSettings(Uri? endpoint = default,
        int blockingPort = 5000,
        int streamingPort = 5005,
        SemaphoreSlim? concurrentSemaphore = null,
        bool useWebSocketsPooling = true,
        CancellationToken? webSocketsCleanUpCancellationToken = default,
        int keepAliveWebSocketsDuration = 100,
        Func<ClientWebSocket>? webSocketFactory = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null,
        string blockingPath = BlockingUriPath,
        string streamingPath = StreamingUriPath) : base(endpoint, blockingPort, streamingPort, concurrentSemaphore, useWebSocketsPooling, webSocketsCleanUpCancellationToken, keepAliveWebSocketsDuration, webSocketFactory, httpClient, loggerFactory, blockingPath, streamingPath)
    {
    }

    /// <summary>
    /// Determines whether or not to use the overlapping Semantic Kernel request settings for the completion request. Prompt is still provided by Semantic Kernel even if request settings are discarded.
    /// </summary>
    public bool OverrideRequestSettings { get; set; }

    /// <summary>
    /// Gets or sets the parameters controlling the specific completion request parameters, which differ between standard text completion and chat completion.
    /// </summary>
    public TParameters OobaboogaParameters { get; set; } = new();
}
