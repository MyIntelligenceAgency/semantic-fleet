// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// This interface allows for the use of a covariant generic type parameter to allow for the use of a custom <see cref="OobaboogaCompletionRequestSettings"/> type controlling the specific completion parameters, which differ between standard text completion and chat completion.
/// </summary>
public interface ICovariantOobaboogaCompletionSettings<out TParameters> where TParameters : OobaboogaCompletionRequestSettings
{
    /// <summary>
    /// Provides the parameters controlling the specific completion request parameters, which differ between standard text completion and chat completion.
    /// </summary>
    TParameters OobaboogaParameters { get; }
}
