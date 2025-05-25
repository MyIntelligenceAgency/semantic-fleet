// Copyright (c) MyIA. All rights reserved.

using System;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// Event arguments for when an analysis task crashes.
/// </summary>
public class AnalysisTaskCrashedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisTaskCrashedEventArgs"/> class.
    /// </summary>
    /// <param name="crashEvent">The crash event details.</param>
    public AnalysisTaskCrashedEventArgs(CrashEvent crashEvent)
    {
        this.CrashEvent = crashEvent;
    }

    /// <summary>
    /// Gets the crash event details.
    /// </summary>
    public CrashEvent CrashEvent { get; }
}

/// <summary>
/// Represents a crash event during a test.
/// </summary>
public class CrashEvent : TestEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrashEvent"/> class.
    /// </summary>
    /// <param name="exception">The exception that caused the crash.</param>
    public CrashEvent(Exception exception)
    {
        this.Exception = exception;
    }

    /// <summary>
    /// Gets the exception that caused the crash.
    /// </summary>
    public Exception Exception { get; }
}
