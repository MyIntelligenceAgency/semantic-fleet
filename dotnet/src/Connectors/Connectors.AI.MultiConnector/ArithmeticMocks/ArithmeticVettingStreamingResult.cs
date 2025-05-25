// Copyright (c) MyIA. All rights reserved.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Orchestration;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.ArithmeticMocks;

/// <summary>
/// Class representing the result of a vetting operation.
/// </summary>
public class ArithmeticVettingStreamingResult : ArithmeticStreamingResultBase
{
    private readonly string _prompt;
    private ArithmeticEngine _engine;
    private readonly MultiTextCompletionSettings _settings;

    /// <summary>
    /// Constructor for the ArithmeticVettingStreamingResult class.
    /// </summary>
    public ArithmeticVettingStreamingResult(MultiTextCompletionSettings settings, string prompt, ArithmeticEngine engine, TimeSpan callTime) : base()
    {
        this._settings = settings;
        this._prompt = prompt;
        this._engine = engine;
    }

    /// <summary>
    /// Generates the model result of the vetting operation. Performs the real operation and compares the result with the connector result.
    /// </summary>
    protected override Task<ModelResult> GenerateModelResult()
    {
        try
        {
            var analysisComponents = this._settings.AnalysisSettings.CaptureVettingPromptComponents(this._prompt, this._settings);

            var operation = ArithmeticEngine.ParsePrompt(analysisComponents.prompt);
            var correctResult = ArithmeticEngine.Compute(operation.operation, operation.operand1, operation.operand2);
            var connectorResult = int.Parse(analysisComponents.response, CultureInfo.InvariantCulture);
            var isCorrect = correctResult == connectorResult;
            var result = isCorrect.ToString(CultureInfo.InvariantCulture);

            return Task.FromResult(new ModelResult(result));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
