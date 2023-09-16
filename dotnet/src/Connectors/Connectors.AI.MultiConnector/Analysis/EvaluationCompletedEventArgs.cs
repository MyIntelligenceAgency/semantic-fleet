// Copyright (c) MyIA. All rights reserved.

using System;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// Event arguments for the EvaluationCompleted event.
/// </summary>
public class EvaluationCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the completion analysis data.
    /// </summary>
    public MultiCompletionAnalysis CompletionAnalysis { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationCompletedEventArgs"/> class.
    /// </summary>
    /// <param name="completionAnalysis">The completion analysis data.</param>
    public EvaluationCompletedEventArgs(MultiCompletionAnalysis completionAnalysis)
    {
        this.CompletionAnalysis = completionAnalysis;
    }
}
