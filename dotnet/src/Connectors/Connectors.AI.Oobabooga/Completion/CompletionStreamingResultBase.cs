// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using Microsoft.SemanticKernel.Orchestration;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// This class is used to return the result of a streaming completion or chat request.
/// </summary>
public abstract class CompletionStreamingResultBase
{
    internal readonly List<CompletionStreamingResponseBase> ModelResponses = new();

    /// <summary>
    /// Represents a result from a model execution.
    /// </summary>
    public ModelResult ModelResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompletionStreamingResultBase"/> class.
    /// </summary>
    protected CompletionStreamingResultBase()
    {
        this.ModelResult = new ModelResult(this.ModelResponses);
    }

    /// <summary>
    /// Appends a streaming chunk response to the streaming result.
    /// </summary>
    public abstract void AppendResponse(CompletionStreamingResponseBase response);

    /// <summary>
    /// Sends a signal to the streaming result that the response stream has ended.
    /// </summary>
    public abstract void SignalStreamEnd();
}
