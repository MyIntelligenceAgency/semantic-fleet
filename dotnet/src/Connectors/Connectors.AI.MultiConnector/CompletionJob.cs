// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SemanticKernel.AI;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Represents a job to be executed by the MultiConnector's completion
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
public readonly struct CompletionJob : System.IEquatable<CompletionJob>
{
    /// <summary>
    /// Initializes a new instance of the CompletionJob struct.
    /// </summary>
    /// <param name="prompt">The input prompt for the job.</param>
    /// <param name="settings">The request settings for the job.</param>
    /// <exception cref="ArgumentNullException">Thrown when prompt or settings are null.</exception>
    public CompletionJob(string prompt, AIRequestSettings? settings)
    {
        this.Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        this.RequestSettings = MultiCompletionRequestSettings.FromRequestSettings(settings ?? throw new ArgumentNullException(nameof(settings)));
    }

    /// <summary>
    /// Gets the Debugger friendly string for the current CompletionJob object.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{this.Prompt.Substring(0, Math.Min(this.Prompt.Length, 10))}(...)";

    /// <summary>
    /// The input prompt that is passed to <see cref="MultiTextCompletion"/>.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    /// The <see cref="MultiCompletionRequestSettings"/> that are passed to <see cref="MultiTextCompletion"/>.
    /// </summary>
    public MultiCompletionRequestSettings RequestSettings { get; }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is CompletionJob job && this.Equals(job);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return System.HashCode.Combine(this.Prompt, this.RequestSettings);
    }

    /// <summary>
    /// Determines whether two specified CompletionJob objects have the same value.
    /// </summary>
    /// <param name="left">The first CompletionJob to compare.</param>
    /// <param name="right">The second CompletionJob to compare.</param>
    /// <returns>true if the value of left is the same as the value of right; otherwise, false.</returns>
    public static bool operator ==(CompletionJob left, CompletionJob right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two specified CompletionJob objects have different values.
    /// </summary>
    /// <param name="left">The first CompletionJob to compare.</param>
    /// <param name="right">The second CompletionJob to compare.</param>
    /// <returns>true if the value of left is different from the value of right; otherwise, false.</returns>
    public static bool operator !=(CompletionJob left, CompletionJob right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Checks if the current CompletionJob object is equal to another CompletionJob object by comparing prompts and request settings
    /// </summary>
    /// <param name="other">A CompletionJob object to compare with this object.</param>
    /// <returns>true if the current CompletionJob object is equal to the other parameter; otherwise, false.</returns>
    public bool Equals(CompletionJob other)
    {
        return this.Prompt == other.Prompt &&
               EqualityComparer<AIRequestSettings>.Default.Equals(this.RequestSettings, other.RequestSettings);
    }
}
