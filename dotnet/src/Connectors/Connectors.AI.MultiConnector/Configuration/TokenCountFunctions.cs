// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using SharpToken;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Transport-neutral map of <see cref="TokenCountFunction"/> to the corresponding token count function.
/// Shared by every MultiConnector configuration so cost accounting keeps the same shape whatever the
/// secondary connectors' transport (OpenAI-compatible today, future transports tomorrow).
/// </summary>
public static class TokenCountFunctions
{
    private static readonly GptEncoding s_gptEncoding = GptEncoding.GetEncoding("cl100k_base");
    private static readonly Func<string, int> s_gp3TokenCounter = s => s_gptEncoding.Encode(s).Count;
    private static readonly Func<string, int> s_wordCounter = s => s.Split(' ').Length;

    static TokenCountFunctions() => Map = new Dictionary<TokenCountFunction, Func<string, int>>
    {
        { TokenCountFunction.Gpt3Tokenizer, s_gp3TokenCounter },
        { TokenCountFunction.WordCount, s_wordCounter }
    };

    /// <summary>
    /// Map of <see cref="TokenCountFunction"/> to the corresponding token count function.
    /// </summary>
    public static Dictionary<TokenCountFunction, Func<string, int>> Map { get; }
}
