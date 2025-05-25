// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

/// <summary>
/// Represents an enumeration of how max tokens are adjusted from the request settings to account for the connector's max tokens.
/// </summary>
public enum MaxTokensAdjustment
{
    /// <summary>
    ///No adjustment
    ///</summary>
    None,

    /// <summary>
    /// Adjust the max tokens to a percentage of the connector's max tokens
    /// </summary>
    Percentage,

    /// <summary>
    /// Count the input prompt tokens using given tokenizer and subtract to the connector's max tokens
    /// </summary>
    CountInputTokens
}
