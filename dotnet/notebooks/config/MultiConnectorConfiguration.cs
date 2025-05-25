// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;


/// <summary>
/// Represents the main settings part of parameters used for MultiConnector integration tests.
/// </summary>
[SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
    Justification = "Configuration classes are instantiated through IConfiguration.")]
internal sealed class MultiConnectorConfiguration
{
    public string OobaboogaEndPoint { get; set; } = "http://localhost";

    public Dictionary<string, string> GlobalParameters { get; set; } = new();

    public List<string> IncludedConnectors { get; set; } = new();

    public List<string> IncludedConnectorsDev { get; set; } = new();

    public List<OobaboogaConnectorConfiguration> OobaboogaCompletions { get; set; } = new();
}


/// <summary>
/// Represents a text completion provider instance with the corresponding given name and settings.
/// </summary>
public class OobaboogaMultiConnectorConfiguration : OobaboogaConnectorConfiguration
{

    public string Name { get; set; } = "";

    /// <summary>
    /// The maximum number of tokens to generate in the completion.
    /// </summary>
    public int? MaxTokens { get; set; }

    public TokenCountFunction TokenCountFunction { get; set; } = TokenCountFunction.Gpt3Tokenizer;

    public decimal CostPerRequest { get; set; }

    public decimal? CostPer1000Token { get; set; }

    public PromptTransform? PromptTransform { get; set; }
}

/// <summary>
/// Defines the method to use to count tokens in prompts inputs and results, to account for MaxTokens and to compute costs.
/// </summary>
public enum TokenCountFunction
{
    Gpt3Tokenizer,
    WordCount,
}
