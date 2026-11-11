// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Anthropic;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using Xunit;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion;

public class AnthropicConnectorConfigurationTests
{
    private const string TestApiKey = "test-key-not-a-real-secret";

    private const string CompletionResponseJson = @"
        {
          ""id"": ""msg_01"",
          ""type"": ""message"",
          ""role"": ""assistant"",
          ""model"": ""claude-sonnet-4-5"",
          ""content"": [
            { ""type"": ""thinking"", ""thinking"": ""the answer is mechanical"" },
            { ""type"": ""text"", ""text"": ""Fitness beats truth."" }
          ],
          ""stop_reason"": ""end_turn"",
          ""usage"": { ""input_tokens"": 12, ""output_tokens"": 5 }
        }";

    private const string StreamingResponseBody = @"
event: message_start
data: {""type"":""message_start"",""message"":{""id"":""msg_01"",""role"":""assistant""}}

event: content_block_start
data: {""type"":""content_block_start"",""index"":0,""content_block"":{""type"":""text"",""text"":""""}}

event: ping
data: {""type"":""ping""}

event: content_block_delta
data: {""type"":""content_block_delta"",""index"":0,""delta"":{""type"":""text_delta"",""text"":""Fitness ""}}

event: content_block_delta
data: {""type"":""content_block_delta"",""index"":0,""delta"":{""type"":""text_delta"",""text"":""beats truth.""}}

event: content_block_stop
data: {""type"":""content_block_stop"",""index"":0}

event: message_stop
data: {""type"":""message_stop""}";

    private static AnthropicConnectorConfiguration NewConnector(
        string name,
        string modelId,
        string? endPoint = null,
        string? apiKey = TestApiKey)
        => new()
        {
            Name = name,
            ModelId = modelId,
            EndPoint = endPoint,
            ApiKey = apiKey,
        };

    [Fact]
    public void CreateCompletionWithoutApiKeyThrowsNamingConnector()
    {
        var connector = NewConnector("claude-seat", "claude-sonnet-4-5", apiKey: null);
        var savedKey = Environment.GetEnvironmentVariable(AnthropicConnectorConfiguration.ApiKeyEnvironmentVariable);
        Environment.SetEnvironmentVariable(AnthropicConnectorConfiguration.ApiKeyEnvironmentVariable, null);
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => connector.CreateCompletion());
            Assert.Contains("claude-seat", exception.Message);
            Assert.Contains("ANTHROPIC_API_KEY", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AnthropicConnectorConfiguration.ApiKeyEnvironmentVariable, savedKey);
        }
    }

    [Fact]
    public void CreateCompletionFallsBackToEnvironmentVariable()
    {
        var connector = NewConnector("claude-seat", "claude-sonnet-4-5", apiKey: null);
        var savedKey = Environment.GetEnvironmentVariable(AnthropicConnectorConfiguration.ApiKeyEnvironmentVariable);
        Environment.SetEnvironmentVariable(AnthropicConnectorConfiguration.ApiKeyEnvironmentVariable, TestApiKey);
        try
        {
            Assert.NotNull(connector.CreateCompletion());
        }
        finally
        {
            Environment.SetEnvironmentVariable(AnthropicConnectorConfiguration.ApiKeyEnvironmentVariable, savedKey);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://anthropic-proxy.internal")]
    public void CreateCompletionReturnsTextGenerationServiceWithOrWithoutEndpointOverride(string? endPoint)
    {
        var connector = NewConnector("claude-seat", "claude-sonnet-4-5", endPoint: endPoint);

        var service = connector.CreateCompletion();

        Assert.IsAssignableFrom<ITextGenerationService>(service);
    }

    [Fact]
    public void CreateNamedCompletionsFiltersOnIncludedConnectors()
    {
        var configuration = new MultiAnthropicConnectorConfiguration
        {
            IncludedConnectors = new List<string> { "claude-seat", "claude-fast" },
            AnthropicCompletions = new List<AnthropicConnectorConfiguration>
            {
                NewConnector("claude-seat", "claude-sonnet-4-5"),
                NewConnector("claude-fast", "claude-haiku-4-5"),
                NewConnector("excluded-seat", "claude-opus-4-5"),
            },
        };

        var completions = configuration.CreateNamedCompletions();

        Assert.Equal(2, completions.Count);
        Assert.Equal(new[] { "claude-fast", "claude-seat" }, completions.Select(c => c.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void CreateNamedCompletionsModelNamesFilterFurther()
    {
        var configuration = new MultiAnthropicConnectorConfiguration
        {
            IncludedConnectors = new List<string> { "claude-seat", "claude-fast" },
            AnthropicCompletions = new List<AnthropicConnectorConfiguration>
            {
                NewConnector("claude-seat", "claude-sonnet-4-5"),
                NewConnector("claude-fast", "claude-haiku-4-5"),
            },
        };

        var completions = configuration.CreateNamedCompletions(new List<string> { "claude-fast" });

        var completion = Assert.Single(completions);
        Assert.Equal("claude-fast", completion.Name);
    }

    [Fact]
    public void CreateNamedCompletionsCarriesCostsAndTransforms()
    {
        var promptTransform = new PromptTransform();
        var connector = NewConnector("claude-seat", "claude-sonnet-4-5");
        connector.CostPerRequest = 0.03m;
        connector.CostPer1000Token = 0.015m;
        connector.PromptTransform = promptTransform;
        var configuration = new MultiAnthropicConnectorConfiguration
        {
            IncludedConnectors = new List<string> { "claude-seat" },
            AnthropicCompletions = new List<AnthropicConnectorConfiguration> { connector },
        };

        var completion = Assert.Single(configuration.CreateNamedCompletions());

        Assert.Equal(0.03m, completion.CostPerRequest);
        Assert.Equal(0.015m, completion.CostPer1000Token);
        Assert.NotNull(completion.TokenCountFunc);
        Assert.True(completion.TokenCountFunc("a prompt long enough to span several tokens") > 1);
        Assert.Same(promptTransform, completion.PromptTransform);
        // No temperature transform on this transport: the Messages API accepts temperature 0
        // (deterministic decoding), unlike the Oobabooga WebSocket connector that needed the 0 -> 0.01
        // remap (review #100).
        Assert.Null(completion.TemperatureTransform);
        Assert.IsAssignableFrom<ITextGenerationService>(completion.TextCompletion);
    }

    [Fact]
    public async Task GetTextContentsAsyncPostsMessagesApiPayloadAndParsesTextBlocks()
    {
        using var handler = new RecordingHandler(_ => JsonResponse(CompletionResponseJson));
        using var httpClient = new HttpClient(handler);
        using var service = new AnthropicTextGenerationService("claude-sonnet-4-5", TestApiKey, httpClient: httpClient);

        var contents = await service.GetTextContentsAsync("Does fitness beat truth?");

        Assert.Equal("https://api.anthropic.com/v1/messages", handler.RequestUri!.ToString());
        Assert.Equal(TestApiKey, handler.ApiKeyHeader);
        Assert.Equal(AnthropicTextGenerationService.DefaultAnthropicVersion, handler.AnthropicVersionHeader);
        Assert.Equal("POST", handler.Method);

        using var payload = System.Text.Json.JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("claude-sonnet-4-5", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal(AnthropicTextGenerationService.DefaultMaxTokens, payload.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.False(payload.RootElement.TryGetProperty("stream", out _));
        var message = Assert.Single(payload.RootElement.GetProperty("messages").EnumerateArray());
        Assert.Equal("user", message.GetProperty("role").GetString());
        Assert.Equal("Does fitness beat truth?", message.GetProperty("content").GetString());

        // The thinking block is not text: only the text block reaches the caller.
        Assert.Equal("Fitness beats truth.", Assert.Single(contents).Text);
    }

    [Fact]
    public async Task GetTextContentsAsyncHonorsEndpointVersionAndExecutionSettings()
    {
        using var handler = new RecordingHandler(_ => JsonResponse(CompletionResponseJson));
        using var httpClient = new HttpClient(handler);
        using var service = new AnthropicTextGenerationService(
            "claude-haiku-4-5",
            TestApiKey,
            endPoint: "https://anthropic-proxy.internal/",
            anthropicVersion: "2023-01-01",
            httpClient: httpClient);

        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["max_tokens"] = 64,
                ["temperature"] = 0.0,
            },
        };

        await service.GetTextContentsAsync("prompt", settings);

        Assert.Equal("https://anthropic-proxy.internal/v1/messages", handler.RequestUri!.ToString());
        Assert.Equal("2023-01-01", handler.AnthropicVersionHeader);

        using var payload = System.Text.Json.JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(64, payload.RootElement.GetProperty("max_tokens").GetInt32());
        // temperature 0 is a real request value, not an absent one: it must be sent.
        Assert.Equal(0, payload.RootElement.GetProperty("temperature").GetDouble());
    }

    [Fact]
    public async Task GetStreamingTextContentsAsyncConcatenatesTextDeltas()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(StreamingResponseBody, Encoding.UTF8, "text/event-stream"),
        });
        using var httpClient = new HttpClient(handler);
        using var service = new AnthropicTextGenerationService("claude-sonnet-4-5", TestApiKey, httpClient: httpClient);

        var builder = new StringBuilder();
        await foreach (var chunk in service.GetStreamingTextContentsAsync("Does fitness beat truth?"))
        {
            builder.Append(chunk.Text);
        }

        Assert.Equal("Fitness beats truth.", builder.ToString());

        using var payload = System.Text.Json.JsonDocument.Parse(handler.RequestBody!);
        Assert.True(payload.RootElement.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task GetTextContentsAsyncSurfacesTheApiErrorBody()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(@"{""type"":""error"",""error"":{""type"":""rate_limit_error"",""message"":""slow down""}}", Encoding.UTF8, "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        using var service = new AnthropicTextGenerationService("claude-sonnet-4-5", TestApiKey, httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetTextContentsAsync("prompt"));

        Assert.Contains("429", exception.Message);
        Assert.Contains("rate_limit_error", exception.Message);
    }

    [Fact]
    public void ExtractTextThrowsWhenTheBodyCarriesNoContentArray()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => AnthropicTextGenerationService.ExtractText(@"{""type"":""error""}"));

        Assert.Contains("content", exception.Message);
    }

    [Theory]
    [InlineData(@"{""type"":""ping""}", null)]
    [InlineData(@"{""type"":""message_stop""}", null)]
    [InlineData(@"{""type"":""content_block_start"",""index"":0}", null)]
    [InlineData(@"{""type"":""content_block_delta"",""delta"":{""type"":""text_delta"",""text"":""abc""}}", "abc")]
    [InlineData("not json at all", null)]
    public void ExtractStreamingDeltaOnlyYieldsTextDeltas(string payload, string? expected)
    {
        Assert.Equal(expected, AnthropicTextGenerationService.ExtractStreamingDelta(payload));
    }

    private static HttpResponseMessage JsonResponse(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            this._responder = responder;
        }

        public Uri? RequestUri { get; private set; }

        public string? RequestBody { get; private set; }

        public string? Method { get; private set; }

        public string? ApiKeyHeader { get; private set; }

        public string? AnthropicVersionHeader { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.RequestUri = request.RequestUri;
            this.Method = request.Method.Method;
            this.RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            this.ApiKeyHeader = request.Headers.TryGetValues("x-api-key", out var keys) ? keys.FirstOrDefault() : null;
            this.AnthropicVersionHeader = request.Headers.TryGetValues("anthropic-version", out var versions) ? versions.FirstOrDefault() : null;

            return this._responder(request);
        }
    }
}
