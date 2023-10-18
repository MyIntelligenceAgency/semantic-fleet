// Copyright (c) MyIA. All rights reserved.

using System;
using System.Globalization;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;
using System.Text.Json;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Represents general settings for a <see cref="MultiTextCompletion"/> request, adapted from specific completion or prompt config settings.
/// </summary>
public class MultiCompletionRequestSettings : AIRequestSettings
{
    /// <summary>
    /// Modulates the next token probabilities. A value of 0 implies deterministic output (only the most likely token is used). Higher values increase randomness.
    /// </summary>
    public double? Temperature
    {
        get => this.ExtensionData.TryGetValue("TEMPERATURE", out object? value) ? ((IConvertible)value).ToDouble(CultureInfo.InvariantCulture) : null;
        set => this.ExtensionData["TEMPERATURE"] = value!;
    }

    /// <summary>
    /// The maximum number of tokens to generate, ignoring the number of tokens in the prompt.
    /// </summary>
    public int? MaxTokens
    {
        get => this.ExtensionData.TryGetValue("MAXTOKENS", out object? value) ? ((IConvertible)value).ToInt32(CultureInfo.InvariantCulture) : null;
        set => this.ExtensionData["MAXTOKENS"] = value!;
    }

    /// <summary>
    /// Create a new settings object with the values from another settings object.
    /// </summary>
    /// <param name="requestSettings">generic request settings</param>
    /// <param name="defaultMaxTokens">Default max tokens</param>
    /// <returns>An instance of <see cref="OobaboogaCompletionRequestSettings"/></returns>
    public static MultiCompletionRequestSettings FromRequestSettings(AIRequestSettings? requestSettings, int? defaultMaxTokens = null)
    {
        //Request settings are MultiCompletionRequestSettings
        if (requestSettings != null && requestSettings is MultiCompletionRequestSettings requestSettingsMultiCompletionRequestSettings)
        {
            return requestSettingsMultiCompletionRequestSettings;
        }

        var newSettings = new MultiCompletionRequestSettings();
        if (defaultMaxTokens != null)
        {
            newSettings.ExtensionData["MAXTOKENS"] = defaultMaxTokens;
        }

        if (requestSettings != null)
        {
            newSettings.ModelId = requestSettings.ModelId;
            newSettings.ServiceId = requestSettings.ServiceId;

            var json = JsonSerializer.Serialize(requestSettings);
            var deserialized = JsonSerializer.Deserialize<AIRequestSettings>(json);

            if (deserialized != null)
            {
                foreach (var pair in deserialized.ExtensionData)
                {
                    var upperKey = pair.Key.ToUpperInvariant();
                    newSettings.ExtensionData[upperKey] = pair.Value;
                    switch (upperKey)
                    {
                        case "MAXNEWTOKENS":
                            newSettings.ExtensionData["MAXTOKENS"] = pair.Value;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        return newSettings;
    }

    /// <summary>
    /// Static function that clones a <see cref="MultiCompletionRequestSettings"/> object.
    /// </summary>
    public static MultiCompletionRequestSettings CloneRequestSettings(MultiCompletionRequestSettings requestSettings)
    {
        var toReturn = new MultiCompletionRequestSettings();
        toReturn.ModelId = requestSettings.ModelId;
        toReturn.ServiceId = requestSettings.ServiceId;
        foreach (var pair in requestSettings.ExtensionData)
        {
            toReturn.ExtensionData[pair.Key] = pair.Value;
        }

        return toReturn;
    }
}
