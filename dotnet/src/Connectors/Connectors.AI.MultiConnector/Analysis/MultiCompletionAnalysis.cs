// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// Represents the collection of evaluations of connectors against prompt types, to be saved and analyzed.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
public class MultiCompletionAnalysis : TestEvent
{
    /// <summary>
    /// Gets a string representation of the object for debugging purposes.
    /// </summary>
    /// <returns>A string representation of the object for debugging purposes.</returns>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override string DebuggerDisplay => $"{base.DebuggerDisplay}, {this.Samples.Count} samples, {this.Tests.Count} tests, {this.Evaluations.Count} evaluations";

    /// <summary>
    /// Gets or sets the list of Test samples with prompt and answer collected from a primary connector.
    /// </summary>
    public List<ConnectorTest> Samples { get; set; } = new();

    /// <summary>
    /// Test run timestamp is used to determine if new tests should be run.
    /// </summary>
    public DateTime TestTimestamp { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the list of Connector Tests. Those are run against the samples to determine how each connector performs.
    /// </summary>
    /// <returns>The list of ConnectorTests.</returns>
    public List<ConnectorTest> Tests { get; set; } = new();

    /// <summary>
    /// Evaluation timestamp is used to determine if new evaluations should be performed
    /// </summary>
    public DateTime EvaluationTimestamp { get; set; } = DateTime.MinValue;

    /// <summary>
    /// list of connector tests evaluations performed by primary models on the tests run with secondary models.
    /// </summary>
    public List<ConnectorPromptEvaluation> Evaluations { get; set; } = new();

    /// <summary>
    /// Suggestion timestamp is used to determine if new suggestions should be performed
    /// </summary>
    public DateTime SuggestionTimestamp { get; set; } = DateTime.MinValue;
}
