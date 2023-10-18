﻿// Copyright (c) MyIA. All rights reserved.

using System.Text.Json.Serialization;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;

/// <summary>
/// Settings to perform oobabooga chat completion request.
/// </summary>
public class OobaboogaChatCompletionRequestSettings : OobaboogaCompletionRequestSettings
{
    /// <summary>
    /// The mode of chat completion. Valid options: 'chat', 'chat-instruct', 'instruct'.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "chat";

    /// <summary>
    /// The character name for the chat completion.
    /// </summary>
    [JsonPropertyName("character")]
    public string Character { get; set; } = "Example";

    /// <summary>
    /// The instruction template for instruct mode chat completion. Instruction following models usually require a specific format that they were specifically trained or fine-tuned to follow. You can find default templates in the <see href="https://github.com/oobabooga/text-generation-webui/tree/main/instruction-templates">corresponding directory</see> and default mappings between models and templates in the <see href="https://github.com/oobabooga/text-generation-webui/blob/main/models/config.yaml">config.yaml file</see>.
    /// </summary>
    [JsonPropertyName("instruction_template")]
    public string? InstructionTemplate { get; set; }

    /// <summary>
    /// The name to use for the user in the chat completion.
    /// </summary>
    [JsonPropertyName("your_name")]
    public string YourName { get; set; } = "You";

    /// <summary>
    /// Determines whether to regenerate the chat completion.
    /// </summary>
    [JsonPropertyName("regenerate")]
    public bool Regenerate { get; set; }

    /// <summary>
    /// Determines whether to continue the chat completion.
    /// </summary>
    [JsonPropertyName("_continue")]
    public bool Continue { get; set; }

    /// <summary>
    /// Determines whether to stop at newline in the chat completion.
    /// </summary>
    [JsonPropertyName("stop_at_newline")]
    public bool StopAtNewline { get; set; }

    /// <summary>
    /// The number of chat generation attempts.
    /// </summary>
    [JsonPropertyName("chat_generation_attempts")]
    public int ChatGenerationAttempts { get; set; } = 1;

    /// <summary>
    /// The chat-instruct command for the chat completion when corresponding mode is used.
    /// </summary>
    [JsonPropertyName("chat-instruct_command")]
    public string ChatInstructCommand { get; set; } = "Continue the chat dialogue below. Write a single reply for the character \"<|character|>\".\n\n<|prompt|>";

    /// <summary>
    /// The instruction context for the chat-instruct / instruct completion.
    /// </summary>
    [JsonPropertyName("context_instruct")]
    public string ContextInstruct { get; set; } = "";

    /// <summary>
    /// Imports the settings from the given <see cref="OobaboogaChatCompletionRequestSettings"/> object.
    /// </summary>
    public void Apply(OobaboogaChatCompletionRequestSettings requestSettings)
    {
        base.Apply(requestSettings);
        this.ChatInstructCommand = requestSettings.ChatInstructCommand;
        this.Character = requestSettings.Character;
        this.ChatGenerationAttempts = requestSettings.ChatGenerationAttempts;
        this.ContextInstruct = requestSettings.ContextInstruct;
        this.Continue = requestSettings.Continue;
        this.InstructionTemplate = requestSettings.InstructionTemplate;
        this.Mode = requestSettings.Mode;
        this.Regenerate = requestSettings.Regenerate;
        this.StopAtNewline = requestSettings.StopAtNewline;
        this.YourName = requestSettings.YourName;
    }
}
