// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;

/// <summary>
/// Chat message representation from Semantic Kernel ChatMessageBase Abstraction
/// </summary>
public class SKChatMessage : ChatMessageBase
{
    /// <summary>
    /// Create a new instance of a chat message
    /// </summary>
    /// <param name="oobaboogaChatMessages">List of Oobabooga chat messages</param>
    public SKChatMessage(List<string> oobaboogaChatMessages)
        : base(AuthorRole.Assistant, oobaboogaChatMessages.Last())
    {
    }
}
