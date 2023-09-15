// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// This serves the event triggered when the analysis settings instance receives new samples.
/// </summary>
public class SamplesReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SamplesReceivedEventArgs"/> class.
    /// </summary>
    public SamplesReceivedEventArgs(List<ConnectorTest> newSamples, AnalysisJob analysisJob)
    {
        this.NewSamples = newSamples;
        this.AnalysisJob = analysisJob;
    }

    /// <summary>
    /// Gets the new samples received.
    /// </summary>
    public List<ConnectorTest> NewSamples { get; set; }

    /// <summary>
    /// Gets the analysis job the sampling was part of.
    /// </summary>
    public AnalysisJob AnalysisJob { get; set; }
}
