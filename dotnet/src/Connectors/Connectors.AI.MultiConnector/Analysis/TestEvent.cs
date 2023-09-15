﻿// Copyright (c) MyIA. All rights reserved.

using System;
using System.Diagnostics;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// Represents a common base class to record Timestamps and duration
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
public class TestEvent
{
    /// <summary>
    /// Gets the debugger display.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual string DebuggerDisplay => $"{this.Timestamp}, Duration: {this.Duration}";

    /// <summary>
    /// Gets or sets the timestamp of the test event.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the duration of the test event.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
