// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;
using Xunit;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace SemanticKernel.IntegrationTests.Connectors.Oobabooga;

/// <summary>
/// Integration tests for <see cref="MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion.OobaboogaChatCompletion"/>.
/// </summary>
public sealed class OobaboogaCompletionTests : IDisposable
{
    private const string Input = " My name is";

    private readonly IConfigurationRoot _configuration;
    private readonly List<ClientWebSocket> _webSockets = new();
    private readonly Func<ClientWebSocket> _webSocketFactory;

    public OobaboogaCompletionTests()
    {
        // Load configuration
        this._configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        this._webSocketFactory = () =>
        {
            var toReturn = new ClientWebSocket();
            this._webSockets.Add(toReturn);
            return toReturn;
        };
    }

    [Fact(Skip = "This test is for manual verification.")]
    public async Task OobaboogaLocalTextCompletionAsync()
    {
        var oobaboogaConfiguration = this._configuration.GetSection("Oobabooga").Get<OobaboogaConnectorConfiguration>();

        Assert.NotNull(oobaboogaConfiguration);

        var oobaboogaSettings = new OobaboogaTextCompletionSettings(endpoint: new Uri(oobaboogaConfiguration!.EndPoint!),
            blockingPort: oobaboogaConfiguration.BlockingPort);

        var oobaboogaLocal = new OobaboogaTextCompletion(oobaboogaSettings);

        // Act
        var localResponse = await oobaboogaLocal.CompleteAsync(Input, new OobaboogaCompletionRequestSettings()
        {
            Temperature = 0.01,
            MaxNewTokens = 7,
            TopP = 0.1,
        });

        AssertAcceptableResponse(localResponse);
    }

    [Fact(Skip = "This test is for manual verification.")]
    public async Task OobaboogaLocalTextCompletionStreamingAsync()
    {
        var oobaboogaConfiguration = this._configuration.GetSection("Oobabooga").Get<OobaboogaConnectorConfiguration>();

        Assert.NotNull(oobaboogaConfiguration);

        var oobaboogaSettings = new OobaboogaTextCompletionSettings(endpoint: new Uri(oobaboogaConfiguration!.EndPoint!),
            streamingPort: oobaboogaConfiguration!.StreamingPort, webSocketFactory: this._webSocketFactory);

        var oobaboogaLocal = new OobaboogaTextCompletion(oobaboogaSettings);

        // Act
        var localResponse = oobaboogaLocal.CompleteStreamAsync(Input, new OobaboogaCompletionRequestSettings()
        {
            Temperature = 0.01,
            MaxNewTokens = 7,
            TopP = 0.1,
        });

        StringBuilder stringBuilder = new();
        await foreach (var result in localResponse)
        {
            stringBuilder.Append(result);
        }

        var resultsMerged = stringBuilder.ToString();
        AssertAcceptableResponse(resultsMerged);
    }

    [Fact(Skip = "This test is for manual verification.")]
    public async Task OobaboogaLocalChatCompletionAsync()
    {
        var oobaboogaConfiguration = this._configuration.GetSection("Oobabooga").Get<OobaboogaConnectorConfiguration>();

        Assert.NotNull(oobaboogaConfiguration);

        var oobaboogaSettings = new OobaboogaChatCompletionSettings(endpoint: new Uri(oobaboogaConfiguration!.EndPoint!),
            blockingPort: oobaboogaConfiguration.BlockingPort);

        var oobaboogaLocal = new OobaboogaChatCompletion(oobaboogaSettings);

        var history = new ChatHistory();
        history.AddUserMessage("What is your name?");
        // Act
        var localResponse = await oobaboogaLocal.GetChatMessageContentsAsync(history, new OobaboogaChatCompletionRequestSettings()
        {
            Temperature = 0.01,
            MaxNewTokens = 20,
            TopP = 0.1,
        });

        var chatMessage = localResponse[^1];
        this.AssertAcceptableChatResponse(chatMessage);
    }

    [Fact(Skip = "This test is for manual verification.")]
    public async Task OobaboogaLocalChatCompletionStreamingAsync()
    {
        var oobaboogaConfiguration = this._configuration.GetSection("Oobabooga").Get<OobaboogaConnectorConfiguration>();

        Assert.NotNull(oobaboogaConfiguration);

        var oobaboogaSettings = new OobaboogaChatCompletionSettings(endpoint: new Uri(oobaboogaConfiguration!.EndPoint!),
            streamingPort: oobaboogaConfiguration.StreamingPort, webSocketFactory: this._webSocketFactory);

        var oobaboogaLocal = new OobaboogaChatCompletion(oobaboogaSettings);

        var history = new ChatHistory();
        history.AddUserMessage("What is your name?");

        // Act
        var localResponse = oobaboogaLocal.GetStreamingChatMessageContentsAsync(history, new OobaboogaChatCompletionRequestSettings()
        {
            Temperature = 0.01,
            MaxNewTokens = 7,
            TopP = 0.1,
        });

        StringBuilder logBuilder = new();
        StringBuilder contentBuilder = new();
        AuthorRole? role = null;
        await foreach (var message in localResponse)
        {
            logBuilder.AppendLine(CultureInfo.InvariantCulture, $"{message.Role}: {message.Content}");
            role ??= message.Role;
            contentBuilder.Append(message.Content ?? string.Empty);
        }

        var chatMessage = new ChatMessageContent(role ?? AuthorRole.Assistant, contentBuilder.ToString());
        this.AssertAcceptableChatResponse(chatMessage);
    }

    private static void AssertAcceptableResponse(string localResponse)
    {
        // Assert
        Assert.NotNull(localResponse);
        // Depends on the target LLM obviously, but most LLMs should propose an arbitrary surname preceded by a white space, including the start prompt or not
        // ie "  My name is" => " John (...)" or "  My name is" => " My name is John (...)".
        // A few will return an empty string, but well those shouldn't be used for integration tests.
        var expectedRegex = new Regex(@"\s\w+.*");
        Assert.Matches(expectedRegex, localResponse);
    }

    private void AssertAcceptableChatResponse(ChatMessageContent? chatMessage)
    {
        Assert.NotNull(chatMessage);
        Assert.NotNull(chatMessage.Content);
        Assert.Equal(chatMessage.Role, AuthorRole.Assistant);
        // Default chat settings use the "Example" character, which depicts an assistant named Chiharu. Any non trivial chat model should return the appropriate name.
        var expectedRegex = new Regex(".*Chiharu.*");
        Assert.Matches(expectedRegex, chatMessage.Content);
    }

    public void Dispose()
    {
        foreach (ClientWebSocket clientWebSocket in this._webSockets)
        {
            clientWebSocket.Dispose();
        }
    }
}
