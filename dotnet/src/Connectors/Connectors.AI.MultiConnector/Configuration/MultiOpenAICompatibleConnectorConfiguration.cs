// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.TextGeneration;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Represents the main configuration for a MultiConnector whose secondary connectors are OpenAI-compatible
/// providers. Settings files, prompt analysis and cost accounting keep the same shape as the legacy
/// Oobabooga configuration the fleet ran before the transport was retired.
/// </summary>
public class MultiOpenAICompatibleConnectorConfiguration
{
    /// <summary>
    /// Global parameters applied for all connector transforms.
    /// </summary>
    public Dictionary<string, string> GlobalParameters { get; set; } = new();

    /// <summary>
    /// Names of secondary connectors to include in the MultiConnector, among those configured.
    /// </summary>
    public List<string> IncludedConnectors { get; set; } = new();

    /// <summary>
    /// List of OpenAI-compatible connectors available to include as secondary connectors in the MultiConnector.
    /// </summary>
    public List<OpenAICompatibleConnectorConfiguration> OpenAICompatibleCompletions { get; set; } = new();

    /// <summary>
    /// Creates a list of <see cref="NamedTextCompletion"/> from <see cref="OpenAICompatibleCompletions"/>, using
    /// <see cref="IncludedConnectors"/> to filter the connectors to include, or filtering from an optional list
    /// of model names to include.
    /// </summary>
    public List<NamedTextCompletion> CreateNamedCompletions(List<string>? modelNames = null, ILoggerFactory? loggerFactory = null)
    {
        var completions = new List<NamedTextCompletion>();

        var includedCompletions = this.OpenAICompatibleCompletions.Where(configuration =>
            this.IncludedConnectors.Contains(configuration.Name));

        foreach (var connector in includedCompletions)
        {
            if (modelNames != null && !modelNames.Contains(connector.Name))
            {
                continue;
            }

            ITextGenerationService completion = connector.CreateCompletion(loggerFactory);

            Func<string, int> tokenCountFunc = TokenCountFunctions.Map[connector.TokenCountFunction];

            var namedCompletion = new NamedTextCompletion(connector.Name, completion)
            {
                CostPerRequest = connector.CostPerRequest,
                CostPer1000Token = connector.CostPer1000Token,
                TokenCountFunc = tokenCountFunc,
                PromptTransform = connector.PromptTransform
            };

            completions.Add(namedCompletion);
        }

        return completions;
    }
}
