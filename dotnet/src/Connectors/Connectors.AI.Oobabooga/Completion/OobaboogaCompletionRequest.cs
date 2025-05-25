// Copyright (c) MyIA. All rights reserved.

using System;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.AI;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// HTTP schema to perform oobabooga completion request. Contains many parameters, some of which are specific to certain kinds of models.
/// See <see href="https://github.com/oobabooga/text-generation-webui/blob/main/docs/Generation-parameters.md"/> and subsequent links for additional information.
/// </summary>
[Serializable]
public class OobaboogaCompletionRequest : OobaboogaCompletionRequestSettings
{
    /// <summary>
    /// The prompt text to complete.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new CompletionRequest with the given prompt, oobabooga settings and semantic-kernel settings.
    /// </summary>
    public static OobaboogaCompletionRequest Create(string prompt, OobaboogaCompletionSettings<OobaboogaCompletionRequestSettings> settings, AIRequestSettings requestSettings)
    {
        var toReturn = new OobaboogaCompletionRequest()
        {
            Prompt = prompt
        };
        toReturn.Apply(settings.OobaboogaParameters);
        if (!settings.OverrideRequestSettings)
        {
            var tempSettings = OobaboogaCompletionRequestSettings.FromRequestSettings(requestSettings, toReturn.MaxNewTokens);
            toReturn.Apply(tempSettings);
        }

        return toReturn;
    }
}
