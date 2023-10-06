// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Base class for connector configuration.
/// </summary>
public class ConnectorConfigurationBase
{
    /// <summary>
    /// Name for your Oobabooga connector. Useful in case you have multiple connectors.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Oobabooga has both a text completion and a chat completion api. This setting determines which one to use.
    /// </summary>
    public bool UseChatCompletion { get; set; }

    /// <summary>
    /// The maximum number of tokens to generate in the completion.
    /// </summary>
    public int? MaxTokens { get; set; } = 2048;

    /// <summary>
    /// The function used to count token and compute costs and remaining tokens
    /// </summary>
    public TokenCountFunction TokenCountFunction { get; set; } = TokenCountFunction.Gpt3Tokenizer;

    /// <summary>
    /// The cost per request for this connector
    /// </summary>
    public decimal? CostPerRequest { get; set; }

    /// <summary>
    /// The cost per 1000 tokens for this connector, using the <see cref="TokenCountFunction"/> to compute the number of tokens.
    /// </summary>
    public decimal? CostPer1000Token { get; set; } = 0.0015m;
}
