// SPDX-License-Identifier: MIT
// Standalone POCO extracted from semantic-fleet v0.34.3
// (dotnet/src/Connectors/Connectors.AI.MultiConnector/MultiTextCompletionSettings.cs).
// Only the signature-prefix surface needed to wire a PromptMatcher is preserved.

namespace SemanticFleet.Radix;

/// <summary>
/// Minimal view of a prompt-settings entry used by the radix-tree matcher.
/// The full semantic-fleet record carries connector routing, retry policy, and cost
/// metadata — those are out of scope for the matching module and remain on the
/// consumer side.
/// </summary>
public sealed record PromptMultiConnectorSettings(SignaturePrefix Signature);

/// <summary>
/// Lightweight projection of <c>PromptType.Signature</c> (semantic-fleet
/// <c>MultiTextCompletionSettings.cs</c> L120-L150, v0.34.3). The matcher only
/// inspects <see cref="PromptStart"/>; the full signature also encodes
/// variable span and stop sequences, which are deliberately out of scope.
/// </summary>
public sealed record SignaturePrefix(string PromptStart);