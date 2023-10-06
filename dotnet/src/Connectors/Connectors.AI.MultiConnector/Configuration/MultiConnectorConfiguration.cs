// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SemanticKernel.AI.TextCompletion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;
using SharpToken;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Represents the main configuration part for a MultiConnector made of Oobabooga secondary connectors.
/// </summary>
public class MultiOobaboogaConnectorConfiguration
{
    private static readonly GptEncoding s_gptEncoding = GptEncoding.GetEncoding("cl100k_base");
    private static readonly Func<string, int> s_gp3TokenCounter = s => s_gptEncoding.Encode(s).Count;
    private static readonly Func<string, int> s_wordCounter = s => s.Split(' ').Length;

    static MultiOobaboogaConnectorConfiguration() => TokenCountFunctionMap = new Dictionary<TokenCountFunction, Func<string, int>>
    {
        { TokenCountFunction.Gpt3Tokenizer, s_gp3TokenCounter },
        { TokenCountFunction.WordCount, s_wordCounter }
    };

    /// <summary>
    /// Default endpoint for secondary Oobabooga connectors. Can be overriden by each connector.
    /// </summary>
    public string OobaboogaEndPoint { get; set; } = "http://localhost";

    /// <summary>
    /// Global parameters applied for all connector transforms.
    /// </summary>
    public Dictionary<string, string> GlobalParameters { get; set; } = new();

    /// <summary>
    /// Names of secondary connectors to include in the MultiConnector, among those configured.
    /// </summary>
    public List<string> IncludedConnectors { get; set; } = new();

    /// <summary>
    /// Overrides to <see cref="IncludedConnectors"/>, meant for development purposes using a second "xxx.development.json" settings file.
    /// </summary>
    public List<string> IncludedConnectorsDev { get; set; } = new();

    /// <summary>
    /// List of Oobabooga connectors available to include as secondary connectors in the MultiConnector.
    /// </summary>
    public List<OobaboogaConnectorConfiguration> OobaboogaCompletions { get; set; } = new();

    /// <summary>
    /// Map of <see cref="TokenCountFunction"/> to the corresponding token count function.
    /// </summary>
    public static Dictionary<TokenCountFunction, Func<string, int>> TokenCountFunctionMap { get; }

    /// <summary>
    /// Creates a list of <see cref="NamedTextCompletion"/> from the <see cref="OobaboogaCompletions"/> list, using the <see cref="IncludedConnectors"/> list to filter the connectors to include, or filtering from an optional list of model names to include.
    /// </summary>
    public List<NamedTextCompletion> CreateNamedCompletions(List<string>? modelNames = null)
    {
        var oobaboogaCompletions = new List<NamedTextCompletion>();

        var includedCompletions = this.OobaboogaCompletions.Where(configuration =>
            this.IncludedConnectorsDev.Count > 0
                ? this.IncludedConnectorsDev.Contains(configuration.Name)
                : this.IncludedConnectors.Contains(configuration.Name));

        foreach (var oobaboogaConnector in includedCompletions)
        {
            if (modelNames != null && !modelNames.Contains(oobaboogaConnector.Name))
            {
                continue;
            }

            var settings = oobaboogaConnector.CreateSettings(this.OobaboogaEndPoint);
            ITextCompletion oobaboogaCompletion = oobaboogaConnector.UseChatCompletion ? new OobaboogaChatCompletion((OobaboogaChatCompletionSettings)settings) : new OobaboogaTextCompletion((OobaboogaTextCompletionSettings)settings);

            Func<string, int> tokenCountFunc = TokenCountFunctionMap[oobaboogaConnector.TokenCountFunction];

            var oobaboogaNamedCompletion = new NamedTextCompletion(oobaboogaConnector.Name, oobaboogaCompletion)
            {
                CostPerRequest = oobaboogaConnector.CostPerRequest,
                CostPer1000Token = oobaboogaConnector.CostPer1000Token,
                TokenCountFunc = tokenCountFunc,
                TemperatureTransform = d => d == 0 ? 0.01 : d,
                PromptTransform = oobaboogaConnector.PromptTransform
                //RequestSettingsTransform = requestSettings =>
                //{
                //    var newRequestSettings = new CompleteRequestSettings()
                //    {
                //        MaxTokens = requestSettings.MaxTokens,
                //        ResultsPerPrompt = requestSettings.ResultsPerPrompt,
                //        ChatSystemPrompt = requestSettings.ChatSystemPrompt,
                //        FrequencyPenalty = requestSettings.FrequencyPenalty,
                //        PresencePenalty = 0.3,
                //        StopSequences = requestSettings.StopSequences,
                //        Temperature = 0.7,
                //        TokenSelectionBiases = requestSettings.TokenSelectionBiases,
                //        TopP = 0.9,
                //    };
                //    return newRequestSettings;
                //}
            };
            oobaboogaCompletions.Add(oobaboogaNamedCompletion);
        }

        return oobaboogaCompletions;
    }
}
