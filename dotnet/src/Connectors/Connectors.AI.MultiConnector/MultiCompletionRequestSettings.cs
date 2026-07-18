// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Represents general settings for a <see cref="MultiTextCompletion"/> request, adapted from specific completion or prompt config settings.
/// </summary>
public class MultiCompletionRequestSettings : PromptExecutionSettings
{
    /// <summary>
    /// Initializes a new instance. SK 1.78: <see cref="PromptExecutionSettings.ExtensionData"/> is no longer
    /// lazy-initialized at construction (it is null on a <c>new</c>-constructed object, populated by the JSON
    /// deserializer instead). We ensure a non-null dictionary here. Without this, the
    /// <see cref="TemperatureMulti"/>/<see cref="MaxTokensMulti"/> setters and the
    /// <see cref="FromRequestSettings"/>/<see cref="CloneRequestSettings"/> factories (which both do
    /// <c>new MultiCompletionRequestSettings()</c>) throw <see cref="NullReferenceException"/>. Idempotent
    /// (<c>??=</c> preserves any data a deserializer may have already populated).
    /// </summary>
    public MultiCompletionRequestSettings()
    {
        this.ExtensionData ??= new Dictionary<string, object?>();
    }

    /// <summary>
    /// Modulates the next token probabilities. A value of 0 implies deterministic output (only the most likely token is used). Higher values increase randomness.
    /// </summary>
    [JsonIgnore]
    public double? TemperatureMulti
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
    public int? MaxTokensMulti
    {
        get
        {
            if (this.ExtensionData.TryGetValue("MAX_TOKENS", out object? value))
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.GetInt32();
                }

                return ((IConvertible)value).ToInt32(CultureInfo.InvariantCulture);
            }

            return null;
        }
        set => this.ExtensionData["MAX_TOKENS"] = value!;
    }

    /// <summary>
    /// Create a new settings object with the values from another settings object.
    /// </summary>
    /// <param name="requestSettings">generic request settings</param>
    /// <param name="defaultMaxTokens">Default max tokens</param>
    /// <returns>An instance of <see cref="MultiCompletionRequestSettings"/></returns>
    public static MultiCompletionRequestSettings FromRequestSettings(PromptExecutionSettings? requestSettings, int? defaultMaxTokens = null)
    {
        //Request settings are MultiCompletionRequestSettings
        if (requestSettings != null && requestSettings is MultiCompletionRequestSettings requestSettingsMultiCompletionRequestSettings)
        {
            return requestSettingsMultiCompletionRequestSettings;
        }

        var newSettings = new MultiCompletionRequestSettings();
        if (defaultMaxTokens != null)
        {
            newSettings.ExtensionData["MAX_TOKENS"] = defaultMaxTokens;
        }

        if (requestSettings != null)
        {
            newSettings.ModelId = requestSettings.ModelId;
            newSettings.ServiceId = requestSettings.ServiceId;

            var json = JsonSerializer.Serialize(requestSettings);
            var deserialized = JsonSerializer.Deserialize<PromptExecutionSettings>(json);

            if (deserialized != null)
            {
                foreach (var pair in deserialized.ExtensionData ?? new Dictionary<string, object?>())
                {
                    var upperKey = pair.Key.ToUpperInvariant();
                    var pairValue = pair.Value;
                    newSettings.ExtensionData[upperKey] = pairValue;
                    switch (upperKey)
                    {
                        case "MAXNEWTOKENS":
                            newSettings.ExtensionData["MAX_TOKENS"] = pairValue;
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
        foreach (var pair in requestSettings.ExtensionData ?? new Dictionary<string, object?>())
        {
            toReturn.ExtensionData[pair.Key] = pair.Value;
        }

        return toReturn;
    }
}
