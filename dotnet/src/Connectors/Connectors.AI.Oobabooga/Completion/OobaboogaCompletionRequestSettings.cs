// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// HTTP schema to perform oobabooga completion request, without the user input.
/// </summary>
public class OobaboogaCompletionRequestSettings : AIRequestSettings
{
    /// <summary>
    /// The maximum number of tokens to generate, ignoring the number of tokens in the prompt.
    /// </summary>
    [JsonPropertyName("max_new_tokens")]
    public int? MaxNewTokens { get; set; }

    /// <summary>
    /// If true, the model will automatically determine the maximum number of tokens to generate according to its own limits.
    /// </summary>
    [JsonPropertyName("auto_max_new_tokens")]
    public int? AutoMaxNewTokens { get; set; }

    /// <summary>
    /// Determines whether to use a specific named Oobabooga preset with all generation parameters predefined. <see href="https://github.com/oobabooga/text-generation-webui/tree/main/presets">default Oobabooga presets</see> were crafted after the result of a <see href="https://github.com/oobabooga/oobabooga.github.io/blob/main/arena/results.md"> documented contest</see>
    /// </summary>
    [JsonPropertyName("preset")]
    public string Preset { get; set; } = "None";

    /// <summary>
    /// Determines whether or not to use sampling; use greedy decoding if false.
    /// </summary>
    [JsonPropertyName("do_sample")]
    public bool DoSample { get; set; } = true;

    /// <summary>
    /// Modulates the next token probabilities. A value of 0 implies deterministic output (only the most likely token is used). Higher values increase randomness.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    /// <summary>
    /// If set to a value less than 1, only the most probable tokens with cumulative probability less than this value are kept for generation.
    /// </summary>
    [JsonPropertyName("top_p")]
    public double TopP { get; set; }

    /// <summary>
    /// Measures how similar the conditional probability of predicting a target token is to the expected conditional probability of predicting a random token, given the generated text.
    /// </summary>
    [JsonPropertyName("typical_p")]
    public double TypicalP { get; set; } = 1;

    /// <summary>
    /// Sets a probability floor below which tokens are excluded from being sampled, in units of 1e-4.
    /// </summary>
    [JsonPropertyName("epsilon_cutoff")]
    public double EpsilonCutoff { get; set; }

    /// <summary>
    /// Used with top_p, top_k, and epsilon_cutoff set to 0. This parameter hybridizes locally typical sampling and epsilon sampling, in units of 1e-4.
    /// </summary>
    [JsonPropertyName("eta_cutoff")]
    public double EtaCutoff { get; set; }

    /// <summary>
    /// Controls Tail Free Sampling (value between 0 and 1)
    /// </summary>
    [JsonPropertyName("tfs")]
    public double Tfs { get; set; } = 1;

    /// <summary>
    /// Top A Sampling is a way to pick the next word in a sentence based on how important it is in the context. Top-A considers the probability of the most likely token, and sets a limit based on its percentage. After this, remaining tokens are compared to this limit. If their probability is too low, they are removed from the pool​.
    /// </summary>
    [JsonPropertyName("top_a")]
    public double TopA { get; set; }

    /// <summary>
    /// Exponential penalty factor for repeating prior tokens. 1 means no penalty, higher value = less repetition.
    /// </summary>
    [JsonPropertyName("repetition_penalty")]
    public double RepetitionPenalty { get; set; } = 1.18;

    /// <summary>
    ///When using "top k", you select the top k most likely words to come next based on their probability of occurring, where k is a fixed number that you specify. You can use Top_K to control the amount of diversity in the model output​
    /// </summary>
    [JsonPropertyName("top_k")]
    public int TopK { get; set; } = 20;

    /// <summary>
    /// Minimum length of the sequence to be generated.
    /// </summary>
    [JsonPropertyName("min_length")]
    public int MinLength { get; set; }

    /// <summary>
    /// If set to a value greater than 0, each ngram of that size can only occur once.
    /// </summary>
    [JsonPropertyName("no_repeat_ngram_size")]
    public int NoRepeatNgramSize { get; set; }

    /// <summary>
    /// Number of beams for beam search. 1 means no beam search.
    /// </summary>
    [JsonPropertyName("num_beams")]
    public int NumBeams { get; set; } = 1;

    /// <summary>
    /// The values balance the model confidence and the degeneration penalty in contrastive search decoding.
    /// </summary>
    [JsonPropertyName("penalty_alpha")]
    public int PenaltyAlpha { get; set; }

    /// <summary>
    /// Exponential penalty to the length that is used with beam-based generation
    /// </summary>
    [JsonPropertyName("length_penalty")]
    public double LengthPenalty { get; set; } = 1;

    /// <summary>
    ///  Controls the stopping condition for beam-based methods, like beam-search. It accepts the following values: True, where the generation stops as soon as there are num_beams complete candidates; False, where an heuristic is applied and the generation stops when is it very unlikely to find better candidates.
    /// </summary>
    [JsonPropertyName("early_stopping")]
    public bool EarlyStopping { get; set; }

    /// <summary>
    /// Parameter used for mirostat sampling in Llama.cpp, controlling perplexity during text (default: 0, 0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0)
    /// </summary>
    [JsonPropertyName("mirostat_mode")]
    public int MirostatMode { get; set; }

    /// <summary>
    /// Set the Mirostat target entropy, parameter tau (default: 5.0)
    /// </summary>
    [JsonPropertyName("mirostat_tau")]
    public int MirostatTau { get; set; } = 5;

    /// <summary>
    /// Set the Mirostat learning rate, parameter eta (default: 0.1)
    /// </summary>
    [JsonPropertyName("mirostat_eta")]
    public double MirostatEta { get; set; } = 0.1;

    /// <summary>
    /// Classifier-Free Guidance Scale, equivalent to the parameter commonly used in image generation diffusion models.
    /// </summary>
    [JsonPropertyName("guidance_scale")]
    public double GuidanceScale { get; set; } = 1;

    /// <summary>
    /// Tokens to avoid during generation
    /// </summary>
    [JsonPropertyName("negative_prompt")]
    public string NegativePrompt { get; set; } = "";

    /// <summary>
    /// Random seed to control sampling, used when DoSample is True.
    /// </summary>
    [JsonPropertyName("seed")]
    public int Seed { get; set; } = -1;

    /// <summary>
    /// Controls whether to add beginning of a sentence token
    /// </summary>
    [JsonPropertyName("add_bos_token")]
    public bool AddBosToken { get; set; } = true;

    /// <summary>
    /// The leftmost tokens are removed if the prompt exceeds this length. Most models require this to be at most 2048.
    /// </summary>
    [JsonPropertyName("truncation_length")]
    public int TruncationLength { get; set; } = 2048;

    /// <summary>
    /// Forces the model to never end the generation prematurely.
    /// </summary>
    [JsonPropertyName("ban_eos_token")]
    public bool BanEosToken { get; set; } = false;

    /// <summary>
    /// Some specific models need this unset.
    /// </summary>
    [JsonPropertyName("skip_special_tokens")]
    public bool SkipSpecialTokens { get; set; } = true;

    /// <summary>
    /// In addition to the defaults. Written between "" and separated by commas. For instance: "\nYour Assistant:", "\nThe assistant:"
    /// </summary>
    [JsonPropertyName("stopping_strings")]
    public List<string> StoppingStrings { get; set; } = new();

    /// <summary>
    /// The system prompt to use when generating text completions using a chat model.
    /// Defaults to "Assistant is a large language model."
    /// </summary>
    [JsonPropertyName("chat_system_prompt")]
    public string ChatSystemPrompt { get; set; } = "Assistant is a large language model.";

    /// <summary>
    /// Imports the settings from the given <see cref="OobaboogaCompletionSettings"/> object.
    /// </summary>
    public void Apply(OobaboogaCompletionRequestSettings requestSettings)
    {
        this.AddBosToken = requestSettings.AddBosToken;
        this.AutoMaxNewTokens = requestSettings.AutoMaxNewTokens;
        this.BanEosToken = requestSettings.BanEosToken;
        this.DoSample = requestSettings.DoSample;
        this.EarlyStopping = requestSettings.EarlyStopping;
        this.EpsilonCutoff = requestSettings.EpsilonCutoff;
        this.EtaCutoff = requestSettings.EtaCutoff;
        this.GuidanceScale = requestSettings.GuidanceScale;
        this.LengthPenalty = requestSettings.LengthPenalty;
        this.MaxNewTokens = requestSettings.MaxNewTokens;
        this.MinLength = requestSettings.MinLength;
        this.MirostatEta = requestSettings.MirostatEta;
        this.MirostatMode = requestSettings.MirostatMode;
        this.MirostatTau = requestSettings.MirostatTau;
        this.NegativePrompt = requestSettings.NegativePrompt;
        this.NoRepeatNgramSize = requestSettings.NoRepeatNgramSize;
        this.NumBeams = requestSettings.NumBeams;
        this.PenaltyAlpha = requestSettings.PenaltyAlpha;
        this.Preset = requestSettings.Preset;
        this.RepetitionPenalty = requestSettings.RepetitionPenalty;
        this.Seed = requestSettings.Seed;
        this.SkipSpecialTokens = requestSettings.SkipSpecialTokens;
        this.StoppingStrings = requestSettings.StoppingStrings;
        this.Temperature = requestSettings.Temperature;
        this.Tfs = requestSettings.Tfs;
        this.TopA = requestSettings.TopA;
        this.TopK = requestSettings.TopK;
        this.TopP = requestSettings.TopP;
        this.TruncationLength = requestSettings.TruncationLength;
        this.TypicalP = requestSettings.TypicalP;
        this.ChatSystemPrompt = requestSettings.ChatSystemPrompt;
    }

    /// <summary>
    /// Converts the semantic-kernel presence penalty, scaled -2:+2 with default 0 for no penalty to the Oobabooga repetition penalty, strictly positive with default 1 for no penalty. See <see href="https://github.com/oobabooga/text-generation-webui/blob/main/docs/Generation-parameters.md"/>  and subsequent links for more details.
    /// </summary>
    public static double GetRepetitionPenalty(double presencePenalty)
    {
        return 1 + presencePenalty / 2;
    }

    /// <summary>
    /// Create a new settings object with the values from another settings object.
    /// </summary>
    /// <param name="requestSettings">generic request settings</param>
    /// <param name="defaultMaxTokens">Default max tokens</param>
    /// <returns>An instance of <see cref="OobaboogaCompletionRequestSettings"/></returns>
    public static OobaboogaCompletionRequestSettings FromRequestSettings(AIRequestSettings? requestSettings, int? defaultMaxTokens = null)
    {
        // No request settings provided
        if (requestSettings is null)
        {
            var newSettings = new OobaboogaCompletionRequestSettings();
            if (defaultMaxTokens != null)
            {
                newSettings.MaxNewTokens = defaultMaxTokens;
            }

            return newSettings;
        }

        //Request settings are Oobabooga Completion or ChatCompletion parameters
        if (requestSettings is OobaboogaCompletionRequestSettings requestSettingsOobaboogaCompletionParameters)
        {
            return requestSettingsOobaboogaCompletionParameters;
        }

        //Request settings are OpenAI request settings
        if (requestSettings is OpenAIRequestSettings requestSettingsOpenAIRequestSettings)
        {
            var fromOpenAI = new OobaboogaCompletionRequestSettings();
            fromOpenAI.MaxNewTokens = requestSettingsOpenAIRequestSettings.MaxTokens;
            fromOpenAI.Temperature = requestSettingsOpenAIRequestSettings.Temperature;
            fromOpenAI.TopP = requestSettingsOpenAIRequestSettings.TopP;
            fromOpenAI.RepetitionPenalty = GetRepetitionPenalty(requestSettingsOpenAIRequestSettings.PresencePenalty);
            fromOpenAI.StoppingStrings = requestSettingsOpenAIRequestSettings.StopSequences.ToList();
            return fromOpenAI;
        }

        //Request settings are an unknown format. Trying to leverage ExtensionData property 

        var toReturn = new OobaboogaCompletionRequestSettings
        {
            ServiceId = requestSettings.ServiceId,
            ModelId = requestSettings.ModelId
        };

        foreach (KeyValuePair<string, object> extendedProperty in requestSettings.ExtensionData)
        {
            if (extendedProperty.Value != null)
            {
                switch (extendedProperty.Key.ToUpperInvariant())
                {
                    case "TEMPERATURE":
                        toReturn.Temperature = extendedProperty.Value is JsonElement jsonElementTemperature ? jsonElementTemperature.GetDouble() : ((IConvertible)extendedProperty.Value).ToDouble(CultureInfo.InvariantCulture);
                        break;
                    case "TOPP":
                    case "TOP_P":
                        toReturn.TopP = extendedProperty.Value is JsonElement jsonElementTopP ? jsonElementTopP.GetDouble() : ((IConvertible)extendedProperty.Value).ToDouble(CultureInfo.InvariantCulture);
                        break;
                    case "PRESENCEPENALTY":
                    case "PRESENCE_PENALTY":
                        toReturn.RepetitionPenalty = GetRepetitionPenalty(extendedProperty.Value is JsonElement jsonElementPresencePenalty ? jsonElementPresencePenalty.GetDouble() : ((IConvertible)extendedProperty.Value).ToDouble(CultureInfo.InvariantCulture));
                        break;
                    case "MAXTOKENS":
                    case "MAX_TOKENS":
                    case "MAXNEWTOKENS":
                        toReturn.MaxNewTokens = extendedProperty.Value is JsonElement jsonElementMaxTokens ? jsonElementMaxTokens.GetInt32() : ((IConvertible)extendedProperty.Value).ToInt32(CultureInfo.InvariantCulture);
                        break;
                    case "STOPSEQUENCES":
                    case "STOP_SEQUENCES":
                        string strValue;
                        if (extendedProperty.Value is JsonElement jsonElementStopSequences)
                        {
                            strValue = jsonElementStopSequences.GetRawText();
                            toReturn.StoppingStrings = JsonSerializer.Deserialize<List<string>>(strValue) ?? new();
                        }
                        else if (extendedProperty.Value is IEnumerable enumerationStopSequences)
                        {
                            toReturn.StoppingStrings = new(((IEnumerable)extendedProperty.Value).Cast<string>());
                        }
                        else
                        {
                            throw new ArgumentException($"Unexpected type for {extendedProperty.Key} property: {extendedProperty.Value.GetType()}");
                        }

                        break;
                    case "CHATSYSTEMPROMPT":
                    case "CHAT_SYSTEM_PROMPT":
                        toReturn.ChatSystemPrompt = (extendedProperty.Value is JsonElement jsonElementChatSystemPrompt ? jsonElementChatSystemPrompt.GetString() : ((IConvertible)extendedProperty.Value).ToString(CultureInfo.InvariantCulture))!;
                        break;
                    default:
                        break;
                }
            }
        }

        return toReturn;
    }
}
