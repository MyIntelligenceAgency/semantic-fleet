// Copyright (c) MyIA. All rights reserved.

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using Xunit;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector;

/// <summary>
/// Unit tests for <see cref="OpenRouterConnector"/>: the services it builds must call the OpenRouter endpoint,
/// with the configured key and the OpenRouter attribution headers.
/// </summary>
public sealed class OpenRouterConnectorTests : IDisposable
{
    private const string BaseUrl = "https://openrouter.example/api/v1";

    private const string ChatResponse = @"{""id"":""gen-1"",""object"":""chat.completion"",""created"":1700000000,""model"":""qwen/qwen-chat"",
""choices"":[{""index"":0,""message"":{""role"":""assistant"",""content"":""pong""},""finish_reason"":""stop""}],
""usage"":{""prompt_tokens"":1,""completion_tokens"":1,""total_tokens"":2}}";

    private readonly HttpMessageHandlerStub _handler;
    private readonly HttpClient _httpClient;
    private readonly OpenRouterConnector _connector;

    public OpenRouterConnectorTests()
    {
        this._handler = new HttpMessageHandlerStub();
        this._handler.ResponseToReturn.Content = new StringContent(ChatResponse, Encoding.UTF8, "application/json");
        this._httpClient = new HttpClient(this._handler, false);
        this._connector = new OpenRouterConnector(new OpenRouterConfiguration { ApiKey = "test-key", BaseUrl = BaseUrl }, this._httpClient);
    }

    [Fact]
    public async Task ChatCompletionIsSentToOpenRouterWithKeyAndHeadersAsync()
    {
        // Arrange
        var chat = this._connector.GetChatCompletion(new ModelConfig { ModelId = "qwen/qwen-chat", ChatModelId = "qwen/qwen-chat" });
        var history = new ChatHistory();
        history.AddUserMessage("ping");

        // Act
        var result = await chat.GetChatMessageContentAsync(history);

        // Assert
        Assert.Equal("pong", result.Content);
        Assert.Equal(new Uri(BaseUrl + "/chat/completions"), this._handler.RequestUri);
        Assert.Equal("Bearer test-key", this._handler.RequestHeaders?.Authorization?.ToString());
        Assert.Equal("https://semantic-fleet.myia.io", this._handler.RequestHeaders?.GetValues("HTTP-Referer").Single());
        Assert.Equal("Semantic Fleet", this._handler.RequestHeaders?.GetValues("X-Title").Single());
        Assert.Contains("\"model\":\"qwen/qwen-chat\"", Encoding.UTF8.GetString(this._handler.RequestContent!), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TextCompletionUsesTheTextModelIdAsync()
    {
        // Arrange
        var service = this._connector.GetTextCompletion(new ModelConfig { ModelId = "qwen/qwen-72b", ChatModelId = "qwen/qwen-chat" });

        // Act
        var result = await service.GetTextContentAsync("ping");

        // Assert
        Assert.Equal("pong", result.Text);
        Assert.Equal("qwen/qwen-72b", service.GetModelId());
        Assert.Contains("\"model\":\"qwen/qwen-72b\"", Encoding.UTF8.GetString(this._handler.RequestContent!), StringComparison.Ordinal);
    }

    [Fact]
    public void NamedTextCompletionCarriesTheModelSettings()
    {
        // Act
        var named = this._connector.GetNamedTextCompletion("Qwen72B");

        // Assert
        Assert.Equal("qwen/qwen-72b", named.Name);
        Assert.Equal(1, named.MaxDegreeOfParallelism);
        Assert.NotNull(named.TokenCountFunc);
    }

    [Fact]
    public void UnknownModelNameIsRejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => this._connector.GetNamedTextCompletion("NoSuchModel"));
    }

    public void Dispose()
    {
        this._httpClient.Dispose();
        this._handler.Dispose();
    }
}
