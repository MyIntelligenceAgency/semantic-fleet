// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Text;
using Microsoft.SemanticKernel.TextGeneration;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;

/// <summary>
/// Oobabooga chat completion service API.
/// Adapted from <see href="https://github.com/oobabooga/text-generation-webui/tree/main/api-examples"/>
/// </summary>
public sealed class OobaboogaChatCompletion : OobaboogaCompletionBase<ChatHistory, OobaboogaChatCompletionRequestSettings, OobaboogaChatCompletionRequest, ChatCompletionResponse>, IChatCompletionService, ITextGenerationService
{
    private const string ChatHistoryMustContainAtLeastOneUserMessage = "Chat history must contain at least one User message with instructions.";

    // Tracks the cumulative streamed message so that only the delta chunk is yielded.
    // Reset on the first chunk (MessageNum == 0) of every stream, so the service is safe to reuse across calls.
    private string _lastSentMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="OobaboogaChatCompletion"/> class.
    /// </summary>
    /// <param name="chatCompletionRequestSettings">An instance of <see cref="OobaboogaChatCompletionRequestSettings"/>, which are chat completion settings specific to Oobabooga api</param>
    public OobaboogaChatCompletion(OobaboogaChatCompletionSettings? chatCompletionRequestSettings) : base(chatCompletionRequestSettings)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ChatHistory"/> optionally seeded with a system instruction.
    /// </summary>
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
    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken)
    {
        Verify.NotEmptyList(chatHistory, ChatHistoryMustContainAtLeastOneUserMessage, nameof(chatHistory));

        this.LogActionDetails();
        var texts = await this.GetCompletionsBaseAsync(chatHistory, executionSettings, cancellationToken).ConfigureAwait(false);
        return texts.Select(t => new ChatMessageContent(AuthorRole.Assistant, t)).ToList();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Verify.NotEmptyList(chatHistory, ChatHistoryMustContainAtLeastOneUserMessage, nameof(chatHistory));

        this.LogActionDetails();
        await foreach (var text in this.GetStreamingCompletionsBaseAsync(chatHistory, executionSettings, cancellationToken))
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, text);
        }
    }

    /// <summary>
    /// Text-generation facade over the chat completion API: wraps the prompt as a single user chat
    /// message and projects the chat result back to plain text. Restores the legacy Semantic Kernel
    /// 0.x behavior where the chat connector was also usable as a plain text completion (e.g. inside
    /// the MultiConnector which only routes text-generation services).
    /// </summary>
    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken)
    {
        var chatHistory = this.CreateNewChat();
        chatHistory.AddUserMessage(prompt);
        var chatContents = await this.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken).ConfigureAwait(false);
        return chatContents.Select(c => new TextContent(c.Content, modelId: null)).ToList();
    }

    /// <summary>
    /// Streaming text-generation facade over the chat completion API. See
    /// <see cref="GetTextContentsAsync"/> for the rationale.
    /// </summary>
    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chatHistory = this.CreateNewChat();
        chatHistory.AddUserMessage(prompt);
        await foreach (var chatContent in this.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken).ConfigureAwait(false))
        {
            yield return new StreamingTextContent(chatContent.Content ?? string.Empty);
        }
    }

    #region private ================================================================================

    /// <inheritdoc/>
    protected override CompletionStreamingResponseBase? GetResponseObject(string messageText)
    {
        return Json.Deserialize<ChatCompletionStreamingResponse>(messageText);
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExtractCompletionTexts(ChatCompletionResponse completionResponse)
    {
        return completionResponse.Results.ConvertAll(result =>
            result.History.Visible.Count > 0 ? result.History.Visible.Last().Last() : string.Empty);
    }

    /// <inheritdoc/>
    protected override string? ExtractStreamText(CompletionStreamingResponseBase response)
    {
        var chatResponse = (ChatCompletionStreamingResponse)response;
        // Reset delta state at the start of every stream (service instances may be reused).
        if (response.MessageNum == 0)
        {
            this._lastSentMessage = string.Empty;
        }

        if (chatResponse.History.Visible.Count == 0)
        {
            return null;
        }

        var newMessage = chatResponse.History.Visible.Last().Last();
        var newChunk = newMessage.Substring(this._lastSentMessage.Length);
        this._lastSentMessage = newMessage;
        return newChunk;
    }

    /// <inheritdoc/>
    protected override OobaboogaChatCompletionRequest CreateCompletionRequest(ChatHistory input, PromptExecutionSettings? executionSettings)
    {
        executionSettings ??= new();

        var completionRequest = OobaboogaChatCompletionRequest.Create(input, (OobaboogaCompletionSettings<OobaboogaChatCompletionRequestSettings>)this.OobaboogaSettings, executionSettings);
        return completionRequest;
    }

    #endregion
}
