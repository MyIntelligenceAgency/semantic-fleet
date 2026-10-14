// Copyright (c) MyIA. All rights reserved.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.LoadTests;

/// <summary>
/// Load bench for the MultiConnector router on the OpenAI-compatible transport, re-instrumented from the
/// legacy Oobabooga 50k-chunk bench (<c>OobaboogaCompletionTests.ShouldHandleConcurrentEnumerationOfLongStreamingServiceResponseAsync</c>).
/// Two levels, per the fleet bounds set with ai-01 (DM 2026-10-14):
/// <list type="bullet">
/// <item><see cref="ShouldStreamSampleThroughRouterOnOpenAICompatibleTransportAsync"/> — always-on CI sample:
/// a few hundred chunks against a local SSE test server, wall-clock ratchet armed, external hang timeout.</item>
/// <item><see cref="ReferenceBenchAgainstLiveEndpointRunsOnlyWhenOptedIn"/> — opt-in reference run (env var
/// <c>SEMANTICFLEET_LOAD_BENCH=1</c>, never in CI): a few hundred chunks by default against the live fleet endpoint
/// (default <c>https://models.myia.io/v1</c>, shared vLLM — concurrency capped at 2, key from <c>OPENAI_API_KEY</c>,
/// never inline), full 50k load only with <c>SEMANTICFLEET_LOAD_BENCH_FULL=1</c> (announce on the dashboard before and
/// after a full run; no full run 07:00-19:00Z while the fleet loads the server).</item>
/// </list>
/// Honesty bound: the always-on sample measures the router + HTTP/SSE pipeline on a localhost server, never token
/// generation — same bound as the legacy WebSocket bench it replaces. The reference run measures the live endpoint.
/// </summary>
public sealed class OpenAICompatibleLoadTests : IDisposable
{
    private const int SsePort = 3456;

    // Always-on CI sample: 2 calls x 250 chunks = 500 chunks through the router.
    private const int SampleCalls = 2;
    private const int SampleChunksPerCall = 250;
    private const long SampleExpectedChunks = (long)SampleCalls * SampleChunksPerCall;

    // Opt-in reference run against the live fleet endpoint: concurrency is capped at 2 there.
    private const int ReferenceMaxConcurrency = 2;
    private const int FullLoadCalls = 100;
    private const int FullLoadMaxTokensPerCall = 500;
    private const int ReferenceSampleMaxTokens = 300;

    /// <summary>
    /// Wall-clock budget for the always-on sample load, in milliseconds. The local 50k measurement pinned ~39k
    /// chunks/s (see PR body); the 500-chunk sample holds wide margin for shared CI runners while catching
    /// transport regressions of an order of magnitude or more.
    /// </summary>
    private const long SampleMaxDurationMs = 30_000;

    /// <summary>External hang timeout for the always-on sample: the test fails fast instead of hanging the suite.</summary>
    private static readonly TimeSpan SampleExternalTimeout = TimeSpan.FromSeconds(60);

    // 22 chars: the router's PromptSignature extraction requires prompts at least PromptTruncationLength (20) long.
    private const string BenchPrompt = "Hello, my name is John";
    private const string TestApiKey = "test-key-not-a-real-secret";

    private readonly ITestOutputHelper _output;
    private readonly CancellationTokenSource _cleanupToken = new();

    public OpenAICompatibleLoadTests(ITestOutputHelper output)
    {
        this._output = output;
    }

    [Fact]
    public async Task ShouldStreamSampleThroughRouterOnOpenAICompatibleTransportAsync()
    {
        // Arrange: local OpenAI-compatible SSE server answering with the sample chunks per call.
        using var server = new OpenAICompatibleSseTestServer($"http://localhost:{SsePort}/")
        {
            ChunksPerResponse = SampleChunksPerCall,
        };

        var router = this.BuildRouter($"http://localhost:{SsePort}/v1", "bench-model", TestApiKey, out _);

        var expectedText = string.Concat(Enumerable.Range(0, SampleChunksPerCall).Select(i => i.ToString(CultureInfo.InvariantCulture)));

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(this._cleanupToken.Token);
        timeoutCts.CancelAfter(SampleExternalTimeout);

        // Act: sample concurrent streaming calls through the router.
        var stopwatch = Stopwatch.StartNew();
        var results = await this.RunConcurrentCallsAsync(router, SampleCalls, maxConcurrency: SampleCalls, maxTokens: null, timeoutCts.Token).ConfigureAwait(false);
        stopwatch.Stop();

        // Assert: full delivery through the router...
        Assert.All(results, r => Assert.Equal(SampleChunksPerCall, r.Received));
        Assert.All(results, r => Assert.Equal(expectedText, r.Text));
        Assert.Equal(SampleExpectedChunks, results.Sum(r => (long)r.Received));
        // ...and on the wire: the server wrote every chunk of every response.
        Assert.Equal(SampleExpectedChunks, server.ServedChunks);

        // Report the measurements (the bench's deliverable, visible in test output).
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var chunksPerSecond = elapsedMs > 0 ? SampleExpectedChunks * 1000 / elapsedMs : 0;
        this._output.WriteLine($"[load-bench] openai-compatible transport (CI sample): {SampleExpectedChunks} chunks in {elapsedMs} ms");
        this._output.WriteLine($"[load-bench] throughput: {chunksPerSecond} chunks/s aggregate, {elapsedMs / (double)SampleCalls:F1} ms/call average");
        this._output.WriteLine("[load-bench] references: legacy WebSocket bench pinned ~20k chunks/s in 2023; local 50k SSE measurement 2026-10-14: 39494 chunks/s (PR body)");

        // The ratchet the legacy bench never armed.
        Assert.InRange(elapsedMs, 0, SampleMaxDurationMs);
    }

    [Fact]
    public async Task ReferenceBenchAgainstLiveEndpointRunsOnlyWhenOptedIn()
    {
        if (Environment.GetEnvironmentVariable("SEMANTICFLEET_LOAD_BENCH") != "1")
        {
            this._output.WriteLine("[reference-bench] skipped: opt-in only (set SEMANTICFLEET_LOAD_BENCH=1, never in CI). Default = sample (a few hundred chunks, concurrency <= 2); SEMANTICFLEET_LOAD_BENCH_FULL=1 adds the 50k load.");
            return;
        }

        var endpoint = Environment.GetEnvironmentVariable("SEMANTICFLEET_LOAD_BENCH_ENDPOINT") ?? "https://models.myia.io/v1";
        var model = Environment.GetEnvironmentVariable("SEMANTICFLEET_LOAD_BENCH_MODEL")
            ?? throw new InvalidOperationException(
                "SEMANTICFLEET_LOAD_BENCH_MODEL is required for the reference run: the model name goes in the PR body and the measures stay comparable across runs.");
        bool fullLoad = Environment.GetEnvironmentVariable("SEMANTICFLEET_LOAD_BENCH_FULL") == "1";

        int calls = fullLoad ? FullLoadCalls : 1;
        int maxTokens = fullLoad ? FullLoadMaxTokensPerCall : ReferenceSampleMaxTokens;
        var runTimeout = fullLoad ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(5);

        // The connector falls back to OPENAI_API_KEY; the key never lives in a committed file.
        var router = this.BuildRouter(endpoint, model, apiKey: null, out var connectorName);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(this._cleanupToken.Token);
        timeoutCts.CancelAfter(runTimeout);

        var runStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        this._output.WriteLine($"[reference-bench] run {runStamp} endpoint={endpoint} model={model} connector={connectorName} mode={(fullLoad ? "full-50k" : "sample")} concurrency<={ReferenceMaxConcurrency} maxTokens/call={maxTokens}");

        var stopwatch = Stopwatch.StartNew();
        var results = await this.RunConcurrentCallsAsync(router, calls, maxConcurrency: ReferenceMaxConcurrency, maxTokens: maxTokens, timeoutCts.Token).ConfigureAwait(false);
        stopwatch.Stop();

        var totalChunks = results.Sum(r => (long)r.Received);
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var chunksPerSecond = elapsedMs > 0 ? totalChunks * 1000 / elapsedMs : 0;
        this._output.WriteLine($"[reference-bench] run {runStamp} model={model}: {totalChunks} chunks in {elapsedMs} ms = {chunksPerSecond} chunks/s (concurrency <= {ReferenceMaxConcurrency})");

        // Assert delivery only: the throughput of a shared live endpoint belongs to the report, not to a ratchet.
        Assert.All(results, r => Assert.True(r.Received > 0, "every reference call must stream at least one chunk"));
        Assert.DoesNotContain(results, r => r.Text.Length == 0);
    }

    private MultiTextCompletion BuildRouter(string endpoint, string modelId, string? apiKey, out string connectorName)
    {
        connectorName = "sse-bench";
        var configuration = new MultiOpenAICompatibleConnectorConfiguration
        {
            IncludedConnectors = new System.Collections.Generic.List<string> { connectorName },
            OpenAICompatibleCompletions = new System.Collections.Generic.List<OpenAICompatibleConnectorConfiguration>
            {
                new()
                {
                    Name = connectorName,
                    ModelId = modelId,
                    EndPoint = endpoint,
                    ApiKey = apiKey,
                }
            },
        };

        var single = Assert.Single(configuration.CreateNamedCompletions());

        var settings = new MultiTextCompletionSettings
        {
            EnablePromptSampling = false,
            LogCallResult = false,
        };

        return new MultiTextCompletion(settings, single, this._cleanupToken.Token);
    }

    private async Task<CallResult[]> RunConcurrentCallsAsync(MultiTextCompletion router, int calls, int maxConcurrency, int? maxTokens, CancellationToken cancellationToken)
    {
        using var concurrencyGate = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = Enumerable.Range(0, calls).Select(_ => Task.Run(async () =>
        {
            await concurrencyGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                PromptExecutionSettings settings = maxTokens is null
                    ? new PromptExecutionSettings()
                    : new OpenAIPromptExecutionSettings { MaxTokens = maxTokens };

                var received = 0;
                var text = new StringBuilder();
                await foreach (var chunk in router.GetStreamingTextContentsAsync(
                    BenchPrompt, settings, null, cancellationToken).ConfigureAwait(false))
                {
                    received++;
                    text.Append(chunk.Text);
                }

                return new CallResult(received, text.ToString());
            }
            finally
            {
                concurrencyGate.Release();
            }
        }, cancellationToken)).ToList();

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private readonly record struct CallResult(int Received, string Text);

    public void Dispose()
    {
        this._cleanupToken.Cancel();
        this._cleanupToken.Dispose();
    }
}
