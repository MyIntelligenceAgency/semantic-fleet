// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Text;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;

/// <summary>
/// Oobabooga chat completion service API.
/// Adapted from <see href="https://github.com/oobabooga/text-generation-webui/tree/main/api-examples"/>
/// </summary>
public sealed class OobaboogaChatCompletion : OobaboogaCompletionBase<ChatHistory, OobaboogaChatCompletionRequestSettings, OobaboogaChatCompletionRequest, ChatCompletionResponse, ChatCompletionResult, ChatCompletionStreamingResult>, IChatCompletion, ITextCompletion
{
    private const string ChatHistoryMustContainAtLeastOneUserMessage = "Chat history must contain at least one User message with instructions.";

    /// <summary>
    /// Initializes a new instance of the <see cref="OobaboogaChatCompletion"/> class.
    /// </summary>
    /// <param name="chatCompletionRequestSettings">An instance of <see cref="OobaboogaChatCompletionRequestSettings"/>, which are chat completion settings specific to Oobabooga api</param>
    public OobaboogaChatCompletion(OobaboogaChatCompletionSettings? chatCompletionRequestSettings) : base(chatCompletionRequestSettings)
    {
    }

    /// <inheritdoc/>
    public ChatHistory CreateNewChat(string? instructions = null)
    {
        this.LogActionDetails();
        var toReturn = new ChatHistory();
        if (!string.IsNullOrWhiteSpace(instructions))
        {
            toReturn.AddSystemMessage(instructions!);
        }

        return toReturn;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IChatResult>> GetChatCompletionsAsync(
        ChatHistory chat,
        AIRequestSettings? requestSettings = null,
        CancellationToken cancellationToken = default)
    {
        this.LogActionDetails();
        return await this.InternalGetChatCompletionsAsync(chat, requestSettings, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<IChatStreamingResult> GetStreamingChatCompletionsAsync(
        ChatHistory chat,
        AIRequestSettings? requestSettings = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this.LogActionDetails();
        await foreach (var chatCompletionStreamingResult in this.InternalGetStreamingChatCompletionsAsync(chat, requestSettings, cancellationToken))
        {
            yield return chatCompletionStreamingResult;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ITextResult>> GetCompletionsAsync(string text, AIRequestSettings? requestSettings, CancellationToken cancellationToken = default)
    {
        this.LogActionDetails();
        ChatHistory chat = this.PrepareChatHistory(text, requestSettings, out AIRequestSettings chatSettings);
        return (await this.InternalGetChatCompletionsAsync(chat, chatSettings, cancellationToken).ConfigureAwait(false))
            .OfType<ITextResult>()
            .ToList();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ITextStreamingResult> GetStreamingCompletionsAsync(string text, AIRequestSettings? requestSettings, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this.LogActionDetails();
        ChatHistory chat = this.PrepareChatHistory(text, requestSettings, out AIRequestSettings chatSettings);

        await foreach (var chatCompletionStreamingResult in this.InternalGetStreamingChatCompletionsAsync(chat, chatSettings, cancellationToken))
        {
            yield return (ITextStreamingResult)chatCompletionStreamingResult;
        }
    }

    #region private ================================================================================

    /// <inheritdoc/>
    protected override CompletionStreamingResponseBase? GetResponseObject(string messageText)
    {
        return Json.Deserialize<ChatCompletionStreamingResponse>(messageText);
    }

    private async Task<IReadOnlyList<IChatResult>> InternalGetChatCompletionsAsync(
        ChatHistory chat,
        AIRequestSettings? requestSettings = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotEmptyList(chat, ChatHistoryMustContainAtLeastOneUserMessage, nameof(chat));
        return await this.GetCompletionsBaseAsync(chat, requestSettings, cancellationToken).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<IChatStreamingResult> InternalGetStreamingChatCompletionsAsync(
        ChatHistory chat,
        AIRequestSettings? requestSettings = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotEmptyList(chat, ChatHistoryMustContainAtLeastOneUserMessage, nameof(chat));

        await foreach (var chatCompletionStreamingResult in this.GetStreamingCompletionsBaseAsync(chat, requestSettings, cancellationToken))
        {
            yield return chatCompletionStreamingResult;
        }
    }

    private ChatHistory PrepareChatHistory(string text, AIRequestSettings? requestSettings, out AIRequestSettings settings)
    {
        if (requestSettings is OobaboogaChatCompletionRequestSettings requestSettingsChatCompletionParameters)
        {
            settings = requestSettingsChatCompletionParameters;
        }
        else
        {
            var oobaboogaParams = OobaboogaCompletionRequestSettings.FromRequestSettings(requestSettings, null);
            var chatSettings = new OobaboogaChatCompletionRequestSettings();
            chatSettings.Apply(oobaboogaParams);
            settings = chatSettings;
        }

        var chat = this.CreateNewChat(((OobaboogaChatCompletionRequestSettings)settings).ChatSystemPrompt);
        chat.AddUserMessage(text);
        return chat;
    }

    #endregion

    /// <inheritdoc/>
    protected override IReadOnlyList<ChatCompletionResult> GetCompletionResults(ChatCompletionResponse completionResponse)
    {
        return completionResponse.Results.ConvertAll(result => new ChatCompletionResult(result));
    }

    /// <inheritdoc/>
    protected override OobaboogaChatCompletionRequest CreateCompletionRequest(ChatHistory input, AIRequestSettings? requestSettings)
    {
        requestSettings ??= new();

        var completionRequest = OobaboogaChatCompletionRequest.Create(input, (OobaboogaCompletionSettings<OobaboogaChatCompletionRequestSettings>)this.OobaboogaSettings, requestSettings);
        return completionRequest;
    }
}
