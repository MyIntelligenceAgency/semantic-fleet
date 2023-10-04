// Copyright (c) MyIA. All rights reserved.

using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

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
    /// An optional transformation to apply to the prompt before sending it to the connector.
    /// </summary>
    public PromptTransform? PromptTransform { get; set; }
}
