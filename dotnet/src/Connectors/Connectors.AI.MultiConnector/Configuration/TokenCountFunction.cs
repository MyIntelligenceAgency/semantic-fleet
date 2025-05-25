// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Defines the method to use to count tokens in prompts inputs and results, to account for MaxTokens and to compute costs.
/// </summary>
public enum TokenCountFunction
{
    /// <summary>
    /// use the GPT-3 tokenizer to count tokens.
    /// </summary>
    Gpt3Tokenizer,

    /// <summary>
    /// approximate token count by counting words separated by spaces.
    /// </summary>
    WordCount,
}
