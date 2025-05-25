// Copyright (c) MyIA. All rights reserved.

using System;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// Event arguments for the SuggestionCompleted event.
/// </summary>
public class SuggestionCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the suggested <see cref="MultiCompletionAnalysis"/> data for this analysis run.
    /// </summary>
    public MultiCompletionAnalysis CompletionAnalysis { get; }

    /// <summary>
    /// Gets the suggested <see cref="MultiTextCompletionSettings"/> settings for this analysis run.
    /// </summary>
    public MultiTextCompletionSettings SuggestedSettings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionCompletedEventArgs"/> class.
    /// </summary>
    public SuggestionCompletedEventArgs(MultiCompletionAnalysis completionAnalysis, MultiTextCompletionSettings suggestedSettings)
    {
        this.CompletionAnalysis = completionAnalysis;
        this.SuggestedSettings = suggestedSettings;
    }
}
