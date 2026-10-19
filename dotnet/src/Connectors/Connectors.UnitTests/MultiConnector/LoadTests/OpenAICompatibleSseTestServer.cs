// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.LoadTests;

/// <summary>
/// Minimal local OpenAI-compatible SSE test server for the load bench: answers every POST with a
/// <c>text/event-stream</c> chat-completions stream of <see cref="ChunksPerResponse"/> content chunks
/// (delta content = chunk index) followed by <c>data: [DONE]</c>.
/// This is the SSE counterpart of the retired WebSocket test server that carried the legacy
/// Oobabooga 50k-chunk bench: like it, it measures the router + channel + socket pipeline on a
/// localhost server, never token generation.
/// </summary>
internal sealed class OpenAICompatibleSseTestServer : IDisposable
{
    private readonly HttpListener _httpListener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<Task> _runningTasks = new();
    private readonly object _tasksLock = new();
    private bool _serverIsRunning;

    /// <summary>
    /// Number of content chunks streamed per response.
    /// </summary>
    public int ChunksPerResponse { get; set; } = 500;

    /// <summary>
    /// Optional delay between two consecutive SSE events (latency regime; zero = pure throughput regime).
    /// </summary>
    public TimeSpan SegmentMessageDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Total number of SSE content events written since the server started.
    /// </summary>
    public long ServedChunks => this._servedChunks;

    private long _servedChunks;

    public OpenAICompatibleSseTestServer(string url)
    {
        this._cancellationTokenSource = new();
        this._httpListener = new HttpListener();
        this._httpListener.Prefixes.Add(url);
        this._httpListener.Start();
        this._serverIsRunning = true;

        Task.Run((Func<Task>)this.HandleRequestsAsync, this._cancellationTokenSource.Token);
    }

    private async Task HandleRequestsAsync()
    {
        while (!this._cancellationTokenSource.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await this._httpListener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (!this._serverIsRunning)
            {
                // Listener was stopped during shutdown (Dispose stops it before awaiting the
                // running tasks; GetContextAsync has no CancellationToken overload). See #7270.
                break;
            }
            catch (ObjectDisposedException) when (!this._serverIsRunning)
            {
                break;
            }

            if (!this._serverIsRunning)
            {
                context.Response.StatusCode = 409;
                context.Response.StatusDescription = "Server is shutting down";
                context.Response.Close();
                return;
            }

            lock (this._tasksLock)
            {
                this._runningTasks.Add(this.HandleSingleSseRequestAsync(context));
            }
        }

        Task[] tasks;
        lock (this._tasksLock)
        {
            tasks = this._runningTasks.ToArray();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task HandleSingleSseRequestAsync(HttpListenerContext context)
    {
        try
        {
            // Drain the request body (chat-completions JSON) before answering.
            using (context.Request.InputStream)
            {
                await context.Request.InputStream.CopyToAsync(Stream.Null, 4096, this._cancellationTokenSource.Token).ConfigureAwait(false);
            }

            var response = context.Response;
            response.StatusCode = 200;
            response.ContentType = "text/event-stream";
            response.SendChunked = true;

            await using var writer = new StreamWriter(response.OutputStream, new UTF8Encoding(false));

            for (int i = 0; i < this.ChunksPerResponse; i++)
            {
                if (this.SegmentMessageDelay.Ticks > 0)
                {
                    await Task.Delay(this.SegmentMessageDelay, this._cancellationTokenSource.Token).ConfigureAwait(false);
                }

                await writer.WriteAsync(this.BuildChunkEvent(i)).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                Interlocked.Increment(ref this._servedChunks);
            }

            await writer.WriteAsync("data: [DONE]\n\n").ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is HttpListenerException or OperationCanceledException or ObjectDisposedException)
        {
            // Client disconnects and shutdown races are expected on a load bench; drop the connection.
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private string BuildChunkEvent(int index)
    {
        var chunk = new
        {
            id = "chatcmpl-load-bench",
            @object = "chat.completion.chunk",
            created = 1_700_000_000,
            model = "bench-model",
            choices = new[]
            {
                new
                {
                    index = 0,
                    delta = new { content = index.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    finish_reason = (string?)null
                }
            }
        };

        return $"data: {JsonSerializer.Serialize(chunk)}\n\n";
    }

    public void Dispose()
    {
        this._serverIsRunning = false;

        // Stop the listener BEFORE awaiting the running tasks: GetContextAsync only unblocks when
        // the listener is stopped, and awaiting the tasks first would deadlock the accept loop
        // while holding the HttpListener prefix (same failure mode as #7270).
        this._httpListener.Stop();
        this._cancellationTokenSource.Cancel();

        Task[] tasks;
        lock (this._tasksLock)
        {
            tasks = this._runningTasks.ToArray();
        }

        try
        {
            Task.WhenAll(tasks).GetAwaiter().GetResult();
        }
        catch (AggregateException)
        {
            // Individual request tasks report client disconnects through their own catch; nothing to do.
        }
        finally
        {
            this._httpListener.Close();
            this._cancellationTokenSource.Dispose();
        }
    }
}
