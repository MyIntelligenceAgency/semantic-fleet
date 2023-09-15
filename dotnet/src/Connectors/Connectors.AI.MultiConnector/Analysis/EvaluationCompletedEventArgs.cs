// Copyright (c) MyIA. All rights reserved.

using System;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// Event arguments for the EvaluationCompleted event.
/// </summary>
public class EvaluationCompletedEventArgs : EventArgs
{
    public MultiCompletionAnalysis CompletionAnalysis { get; set; }

    public EvaluationCompletedEventArgs(MultiCompletionAnalysis completionAnalysis)
    {
        this.CompletionAnalysis = completionAnalysis;
    }
}
