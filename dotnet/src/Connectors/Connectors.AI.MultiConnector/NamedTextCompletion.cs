﻿// Copyright (c) MyIA. All rights reserved.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.AI.TextCompletion;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Represents a text completion provider instance with the corresponding given name.
/// </summary>
[DebuggerDisplay("{Name}")]
public class NamedTextCompletion
{
    /// <summary>
    /// Gets or sets the name of the text completion provider.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// text completion provider instance, to be used for prompt answering and testing.
    /// </summary>
    public ITextCompletion TextCompletion { get; set; }

    /// <summary>
    /// The maximum number of tokens to generate in the completion.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Optionally transform the input prompt specifically for the model
    /// </summary>
    public PromptTransform? PromptTransform { get; set; }

    /// <summary>
    /// The model might support a different range of temperature than SK (is 0 legal?) This optional function can help keep the temperature in the model's range.
    /// </summary>
    [JsonIgnore]
    public Func<double, double>? TemperatureTransform { get; set; }

    /// <summary>
    /// The model might support a different range of settings than SK. This optional function can help keep the settings in the model's range.
    /// </summary>
    [JsonIgnore]
    public Func<CompleteRequestSettings, CompleteRequestSettings>? RequestSettingsTransform { get; set; }

    /// <summary>
    /// The strategy to ensure request settings max token don't exceed the model's total max token.
    /// </summary>
    public MaxTokensAdjustment MaxTokensAdjustment { get; set; } = MaxTokensAdjustment.Percentage;

    /// <summary>
    /// When <see cref="MaxTokensAdjustment"/> is set to <see cref="MaxTokensAdjustment.Percentage"/>, this is the percentage of the model's max tokens available for completion settings.
    /// </summary>
    public int MaxTokensReservePercentage { get; set; } = 80;

    /// <summary>
    /// Cost per completion request.
    /// </summary>
    public decimal? CostPerRequest { get; set; }

    /// <summary>
    /// Cost for 1000 completion token from request + result text.
    /// </summary>
    public decimal? CostPer1000Token { get; set; }

    /// <summary>
    /// Gets or sets the function to count the number of tokens in a string.
    /// </summary>
    [JsonIgnore]
    public Func<string, int>? TokenCountFunc { get; set; }

    /// <summary>
    /// The maximum number of parallel requests to send to the model.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedTextCompletion"/> class.
    /// </summary>
    /// <param name="name">The name of the text completion provider.</param>
    /// <param name="textCompletion">The text completion provider.</param>
    public NamedTextCompletion(string name, ITextCompletion textCompletion)
    {
        this.Name = name;
        this.TextCompletion = textCompletion;
    }

    /// <summary>
    /// Calculates the cost of a text completion request.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="result">The result text.</param>
    /// <returns>The calculated cost.</returns>
    public decimal GetCost(string text, string result)
    {
        var tokenCount = (this.TokenCountFunc ?? (s => 0))(text + result);
        decimal tokenCost = (this.CostPer1000Token ?? 0) * tokenCount / 1000;
        var toReturn = this.CostPerRequest ?? 0 + tokenCost;
        return toReturn;
    }
}
