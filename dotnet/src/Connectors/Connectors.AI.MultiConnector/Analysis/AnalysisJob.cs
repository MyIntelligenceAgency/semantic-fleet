// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// Represents a job to be executed by the MultiConnector's analysis.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
public class AnalysisJob : TestEvent
{
    /// <summary>
    /// Gets the Debugger friendly string for the current AnalysisJob object.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [JsonIgnore]
    public override string DebuggerDisplay => $"{base.DebuggerDisplay}, {this.TextCompletions.Count} Completions to test and analyze";

    /// <summary>
    /// Gets the settings for the MultiTextCompletion.
    /// </summary>
    public MultiTextCompletionSettings Settings { get; }

    /// <summary>
    /// Gets the list of text completions to be tested and analyzed.
    /// </summary>
    public IReadOnlyList<NamedTextCompletion> TextCompletions { get; }

    /// <summary>
    /// Gets the logger for logging analysis events.
    /// </summary>
    [JsonIgnore]
    public ILogger? Logger { get; }

    ///// <summary>
    ///// Gets or sets a value indicating whether to skip periods during analysis.
    ///// </summary>
    //public bool SkipPeriods { get; set; }

    /// <summary>
    /// Gets the cancellation token for the analysis job.
    /// </summary>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Initializes a new instance of the AnalysisJob class.
    /// </summary>
    /// <param name="settings">The settings for the MultiTextCompletion.</param>
    /// <param name="textCompletions">The list of text completions to be tested and analyzed.</param>
    /// <param name="logger">The logger for logging analysis events.</param>
    /// <param name="cancellationToken">The cancellation token for the analysis job.</param>
    public AnalysisJob(MultiTextCompletionSettings settings, IReadOnlyList<NamedTextCompletion> textCompletions, ILogger? logger, CancellationToken cancellationToken)
    {
        this.Settings = settings;
        this.TextCompletions = textCompletions;
        this.Logger = logger;
        this.CancellationToken = cancellationToken;
    }
}
