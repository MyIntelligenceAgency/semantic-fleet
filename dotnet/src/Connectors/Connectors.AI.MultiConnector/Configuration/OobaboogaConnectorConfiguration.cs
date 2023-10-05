// Copyright (c) MyIA. All rights reserved.

using System;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Represents a text completion provider instance with the corresponding given name and settings.
/// </summary>
public class OobaboogaConnectorConfiguration
{
    /// <summary>
    /// Name for your Oobabooga connector. Useful in case you have multiple connectors.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Endpoint to access the Oobabooga services
    /// </summary>
    public string? EndPoint { get; set; }

    /// <summary>
    /// Port to access the non-streaming Oobabooga services
    /// </summary>
    public int BlockingPort { get; set; }

    /// <summary>
    /// Port to access the streaming Oobabooga services
    /// </summary>
    public int StreamingPort { get; set; }

    /// <summary>
    /// Oobabooga has both a text completion and a chat completion api. This setting determines which one to use.
    /// </summary>
    public bool UseChatCompletion { get; set; }

    /// <summary>
    /// The maximum number of tokens to generate in the completion.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// The function used to count token and compute costs and remaining tokens
    /// </summary>
    public TokenCountFunction TokenCountFunction { get; set; } = TokenCountFunction.Gpt3Tokenizer;

    /// <summary>
    /// The cost per request for this connector
    /// </summary>
    public decimal CostPerRequest { get; set; }

    /// <summary>
    /// The cost per 1000 tokens for this connector, using the <see cref="TokenCountFunction"/> to compute the number of tokens.
    /// </summary>
    public decimal? CostPer1000Token { get; set; }

    /// <summary>
    /// The optional name of a oobabooga generation preset to use with this completion model.
    /// </summary>
    public string? Preset { get; set; }

    /// <summary>
    /// The optional name of a oobabooga instruction template to use in instruct mode with this completion model.
    /// </summary>
    public string? InstructionTemplate { get; set; }

    /// <summary>
    /// An optional transformation to apply to the prompt before sending it to the connector.
    /// </summary>
    public PromptTransform? PromptTransform { get; set; }

    /// <summary>
    /// Creates Oobabooga Text or Chat completion settings according to this configuration.
    /// </summary>
    public ICovariantOobaboogaCompletionSettings<OobaboogaCompletionParameters> CreateSettings(string defaultEndpoint, Func<ClientWebSocket>? webSocketFactory = null, ILoggerFactory? loggerFactory = null)
    {
        Uri endpoint = new(this.EndPoint ?? defaultEndpoint);
        ICovariantOobaboogaCompletionSettings<OobaboogaCompletionParameters> settings;

        if (this.UseChatCompletion)
        {
            var chatSettings = new OobaboogaChatCompletionSettings(endpoint, this.BlockingPort, this.StreamingPort, webSocketFactory: webSocketFactory, loggerFactory: loggerFactory);
            if (this.InstructionTemplate != null)
            {
                chatSettings.OobaboogaParameters.InstructionTemplate = this.InstructionTemplate;
            }

            settings = chatSettings;
        }
        else
        {
            settings = new OobaboogaTextCompletionSettings(endpoint, this.BlockingPort, this.StreamingPort, webSocketFactory: webSocketFactory, loggerFactory: loggerFactory);
        }

        if (this.Preset != null)
        {
            settings.OobaboogaParameters.Preset = this.Preset;
        }

        return settings;
    }
}
