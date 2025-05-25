// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// This interface allows for the use of a contravariant generic type parameter to allow for the use of a custom <see cref="OobaboogaCompletionRequestSettings"/> type controlling the specific completion parameters, which differ between standard text completion and chat completion.
/// </summary>
public interface IContravariantOobaboogaCompletionSettings<in TParameters> where TParameters : OobaboogaCompletionRequestSettings
{
    // setter only is required for contravariant generic type parameters
#pragma warning disable CA1044

    /// <summary>
    /// Provides the parameters controlling the specific completion request parameters, which differ between standard text completion and chat completion.
    /// </summary>
    TParameters OobaboogaParameters { set; }

#pragma warning restore CA1044
}
