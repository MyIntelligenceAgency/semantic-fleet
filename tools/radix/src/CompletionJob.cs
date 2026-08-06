// SPDX-License-Identifier: MIT
// Standalone POCO extracted from semantic-fleet v0.34.3
// (dotnet/src/Connectors/Connectors.AI.MultiConnector/CompletionJob.cs).
// Only the surface needed to wire a PromptMatcher delegate is preserved.

namespace SemanticFleet.Radix;

/// <summary>
/// Minimal view of a completion request used to select a prompt configuration.
/// Mirrors the shape consumed by
/// <c>MultiTextCompletionSettings.PromptMatcher</c> in semantic-fleet v0.34.3.
/// </summary>
public sealed record CompletionJob(string Prompt);