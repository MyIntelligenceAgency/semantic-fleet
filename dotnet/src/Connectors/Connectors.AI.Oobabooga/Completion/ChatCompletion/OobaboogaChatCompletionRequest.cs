// Copyright (c) MyIA. All rights reserved.

using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;

/// <summary>
/// HTTP schema to perform oobabooga chat completion request.
/// </summary>
public sealed class OobaboogaChatCompletionRequest : OobaboogaChatCompletionRequestSettings
{
    /// <summary>
    /// The user input for the chat completion.
    /// </summary>
    [JsonPropertyName("user_input")]
    public string UserInput { get; set; } = string.Empty;

    /// <summary>
    /// The chat history.
    /// </summary>
    [JsonPropertyName("history")]
    public OobaboogaChatHistory History { get; set; } = new();

    /// <summary>
    /// Creates a new ChatCompletionRequest with the given Chat history, oobabooga settings and semantic-kernel settings.
    /// </summary>
    public static OobaboogaChatCompletionRequest Create(ChatHistory chat, OobaboogaCompletionSettings<OobaboogaChatCompletionRequestSettings> settings, PromptExecutionSettings executionSettings)
    {
        var chatMessages = chat.Take(chat.Count - 1).Select(message => message.Content).ToList();
        var toReturn = new OobaboogaChatCompletionRequest()
        {
            UserInput = chat.Last().Content,
            History = new OobaboogaChatHistory()
            {
                Internal = chatMessages.Count > 1 ? new() { chatMessages } : new(),
                Visible = chatMessages.Count > 1 ? new() { chatMessages } : new(),
            },
        };
        toReturn.Apply(settings.OobaboogaParameters);
        if (!settings.OverrideRequestSettings)
        {
            var tempSettings = OobaboogaCompletionRequestSettings.FromPromptExecutionSettings(executionSettings, toReturn.MaxNewTokens);
            toReturn.Apply(tempSettings);
        }

        return toReturn;
    }
}
