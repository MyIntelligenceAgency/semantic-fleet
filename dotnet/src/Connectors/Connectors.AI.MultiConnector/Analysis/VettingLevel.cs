// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

/// <summary>
/// Represents the vetting level of a connector against a prompt type.
/// </summary>
public enum VettingLevel
{
    // TODO: Decouple Vetting Func or/and allow for more nuanced Vetting levels introducing quantitative evaluations (percentage of chance to succeed / to recover the correct result / entropy / results comparisons etc.)
    /// <summary>
    /// Model was not vetted upon evaluation.
    /// </summary>
    Invalid = -1,

    /// <summary>
    /// Vetting was not performed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Vetting was performed by a primary model against test results obtained from a single or several distinct prompts.
    /// </summary>
    Oracle = 1,

    /// <summary>
    /// Vetting was performed by a primary model against test results obtained from distinct prompts.
    /// </summary>
    OracleVaried = 2 // Oracle varied means distinct prompts were used for vetting tests.
}
