// Copyright (c) MyIA. All rights reserved.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.LoadTests;

/// <summary>
/// Load bench for the MultiConnector router on the OpenAI-compatible transport, re-instrumented from the
/// legacy Oobabooga 50k-chunk bench (<c>OobaboogaCompletionTests.ShouldHandleConcurrentEnumerationOfLongStreamingServiceResponseAsync</c>).
/// Same load shape: 500 streamed chunks per call, 100 concurrent calls = 50 000 chunks through the router
/// (session routing + costing + analysis channel + stream tee/replay), with the wall-clock budget
/// <strong>armed</strong> — the legacy bench left <c>maxTestDuration</c> at zero on every line, so the
/// 2023 figure of ~20k chunks/s was measured but never pinned.
/// Honesty bound: the server is a localhost SSE test server. This measures the router + HTTP/SSE pipeline,
/// never token generation — same bound as the legacy WebSocket bench it replaces.
/// </summary>
public sealed class OpenAICompatibleLoadTests : IDisposable
{
    private const int SsePort = 3456;
    private const int ChunksPerCall = 500;
    private const int ConcurrentCalls = 100;
    private const long TotalExpectedChunks = (long)ChunksPerCall * ConcurrentCalls;
    // 22 chars: the router's PromptSignature extraction requires prompts at least PromptTruncationLength (20) long.
    private const string BenchPrompt = "Hello, my name is John";
    private const string TestApiKey = "test-key-not-a-real-secret";

    /// <summary>
    /// Wall-clock budget for the 50k-chunk load, in milliseconds. Pinned at ~25x the local measurement
    /// (50k chunks in ~1.3 s = ~39k chunks/s on the development seat, see PR body) so shared CI runners
    /// stay green while a transport regression of an order of magnitude or more turns the ratchet red.
    /// </summary>
    private const long MaxTestDurationMs = 30_000;

    private readonly ITestOutputHelper _output;
    private readonly CancellationTokenSource _cleanupToken = new();

    public OpenAICompatibleLoadTests(ITestOutputHelper output)
    {
        this._output = output;
    }

    [Fact]
    public async Task ShouldStream50kChunksThroughRouterOnOpenAICompatibleTransportAsync()
    {
        // Arrange: local OpenAI-compatible SSE server answering with 500 content chunks per call.
        using var server = new OpenAICompatibleSseTestServer($"http://localhost:{SsePort}/")
        {
            ChunksPerResponse = ChunksPerCall,
        };

        var configuration = new MultiOpenAICompatibleConnectorConfiguration
        {
            IncludedConnectors = new System.Collections.Generic.List<string> { "sse-bench" },
            OpenAICompatibleCompletions = new System.Collections.Generic.List<OpenAICompatibleConnectorConfiguration>
            {
                new()
                {
                    Name = "sse-bench",
                    ModelId = "bench-model",
                    EndPoint = $"http://localhost:{SsePort}/v1",
                    ApiKey = TestApiKey,
                }
            },
        };

        var completions = configuration.CreateNamedCompletions();
        var single = Assert.Single(completions);

        var settings = new MultiTextCompletionSettings
        {
            EnablePromptSampling = false,
            LogCallResult = false,
        };

        var router = new MultiTextCompletion(settings, single, this._cleanupToken.Token);

        var expectedText = string.Concat(Enumerable.Range(0, ChunksPerCall).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        // Act: 100 concurrent streaming calls through the router, 500 chunks each.
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, ConcurrentCalls).Select(_ => Task.Run(async () =>
        {
            var received = 0;
            var text = new StringBuilder();
            await foreach (var chunk in router.GetStreamingTextContentsAsync(
                BenchPrompt, new PromptExecutionSettings(), null, this._cleanupToken.Token).ConfigureAwait(false))
            {
                received++;
                text.Append(chunk.Text);
            }

            return (received, text.ToString());
        })).ToList();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert: full delivery through the router...
        Assert.All(results, r => Assert.Equal(ChunksPerCall, r.received));
        Assert.All(results, r => Assert.Equal(expectedText, r.Item2));
        Assert.Equal(TotalExpectedChunks, results.Sum(r => (long)r.received));
        // ...and on the wire: the server wrote every chunk of every response.
        Assert.Equal(TotalExpectedChunks, server.ServedChunks);

        // Report the measurements (the bench's deliverable, visible in test output).
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var chunksPerSecond = elapsedMs > 0 ? TotalExpectedChunks * 1000 / elapsedMs : 0;
        this._output.WriteLine($"[load-bench] openai-compatible transport: {TotalExpectedChunks} chunks in {elapsedMs} ms");
        this._output.WriteLine($"[load-bench] throughput: {chunksPerSecond} chunks/s aggregate, {elapsedMs / (double)ConcurrentCalls:F1} ms/call average");
        this._output.WriteLine($"[load-bench] reference: legacy WebSocket bench pinned ~20k chunks/s in 2023 (router pipeline, not token generation)");

        // The ratchet the legacy bench never armed.
        Assert.InRange(elapsedMs, 0, MaxTestDurationMs);
    }

    public void Dispose()
    {
        this._cleanupToken.Cancel();
        this._cleanupToken.Dispose();
    }
}
