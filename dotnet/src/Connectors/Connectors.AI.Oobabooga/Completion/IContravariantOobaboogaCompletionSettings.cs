// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// This interface allows for the use of a contravariant generic type parameter to allow for the use of a custom <see cref="OobaboogaCompletionParameters"/> type controlling the specific completion parameters, which differ between standard text completion and chat completion.
/// </summary>
public interface IContravariantOobaboogaCompletionSettings<in TParameters> where TParameters : OobaboogaCompletionParameters
{
    /// <summary>
    /// Provides the parameters controlling the specific completion request parameters, which differ between standard text completion and chat completion.
    /// </summary>
    TParameters OobaboogaParameters { set; }
}
