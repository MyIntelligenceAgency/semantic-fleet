// SPDX-License-Identifier: MIT
// Reference implementation extracted verbatim from semantic-fleet v0.34.3
// (MultiTextCompletionSettings.cs L283-292). Kept here so the new matcher has a
// faithful baseline for both correctness and performance comparison.
//
// Original signature (verbatim, semantic-fleet):
//   public static PromptMultiConnectorSettings? SimpleMatchPromptSettings(
//       CompletionJob completionJob,
//       IEnumerable<PromptMultiConnectorSettings> promptMultiConnectorSettings)
//   {
//       return promptMultiConnectorSettings.FirstOrDefault(p =>
//           completionJob.Prompt.StartsWith(p.Signature.PromptStart));
//   }
//
// The standalone version preserves the predicate semantics (FirstOrDefault on
// StartsWith) so any divergence observed by the tests indicates a real bug in
// the new radix-tree matcher — not a behavioural drift in the baseline.

using System.Linq;

namespace SemanticFleet.Radix;

/// <summary>
/// Linear O(n) baseline matcher. Preserved verbatim from semantic-fleet
/// v0.34.3 for behavioural and performance comparison.
/// </summary>
public static class SimpleMatchPromptSettings
{
    /// <summary>
    /// Returns the first settings entry whose <see cref="SignaturePrefix.PromptStart"/>
    /// is a prefix of <paramref name="completionJob"/>'s prompt, or <c>null</c>
    /// when no entry matches. Order-sensitive (matches <c>FirstOrDefault</c>).
    /// </summary>
    public static PromptMultiConnectorSettings? Match(
        CompletionJob completionJob,
        IEnumerable<PromptMultiConnectorSettings> promptMultiConnectorSettings)
    {
        return promptMultiConnectorSettings.FirstOrDefault(p =>
            completionJob.Prompt.StartsWith(p.Signature.PromptStart));
    }
}