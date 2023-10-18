// Copyright (c) MyIA. All rights reserved.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Represents general settings for a <see cref="MultiTextCompletion"/> request, adapted from specific completion or prompt config settings.
/// </summary>
public class MultiCompletionRequestSettings : AIRequestSettings
{
    /// <summary>
    /// Modulates the next token probabilities. A value of 0 implies deterministic output (only the most likely token is used). Higher values increase randomness.
    /// </summary>
    [JsonIgnore]
    public double? Temperature
    {
        get
        {
            if (this.ExtensionData.TryGetValue("TEMPERATURE", out object? value))
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.GetDouble();
                }

                return ((IConvertible)value).ToDouble(CultureInfo.InvariantCulture);
            }

            return null;
        }
        set => this.ExtensionData["TEMPERATURE"] = value!;
    }

    /// <summary>
    /// The maximum number of tokens to generate, ignoring the number of tokens in the prompt.
    /// </summary>
    [JsonIgnore]
    public int? MaxTokens
    {
        get
        {
            if (this.ExtensionData.TryGetValue("MAXTOKENS", out object? value))
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.GetInt32();
                }

                return ((IConvertible)value).ToInt32(CultureInfo.InvariantCulture);
            }

            return null;
        }
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
                    var pairValue = pair.Value;
                    newSettings.ExtensionData[upperKey] = pairValue;
                    switch (upperKey)
                    {
                        case "MAXNEWTOKENS":
                            newSettings.ExtensionData["MAXTOKENS"] = pairValue;
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
