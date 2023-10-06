// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel.Orchestration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// This is a simple class that holds the result of a multi-text completion.
/// </summary>
public class MultiTextCompletionResult
{
    /// <summary>
    /// The result of running a function or a plan
    /// </summary>
    public SKContext Result { get; set; }

    /// <summary>
    /// The effective cost of the completion according to parameters
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// The list of samples received from the connectors if enabled
    /// </summary>
    public List<SamplesReceivedEventArgs>? SampleBatches { get; set; }

    /// <summary>
    /// The duration of the completion call
    /// </summary>
    public TimeSpan Duration { get; internal set; }
}
