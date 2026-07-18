// Copyright (c) MyIA. All rights reserved.

using System;
using System.Threading.Tasks;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.ArithmeticMocks;

/// <summary>
/// Class representing an arithmetic result for an arithmetic computation.
/// </summary>
public class ArithmeticComputingStreamingResult : ArithmeticStreamingResultBase
{
    private readonly string _prompt;
    private readonly ArithmeticEngine _engine;

    /// <summary>
    /// constructor for the <see cref="ArithmeticComputingStreamingResult"/> class
    /// </summary>
    public ArithmeticComputingStreamingResult(string prompt, ArithmeticEngine engine, TimeSpan callTime) : base()
    {
        this._prompt = prompt;
        this._engine = engine;
    }

    /// <inheritdoc />
    protected override Task<string> GenerateResultAsync()
    {
        var result = this._engine.Run(this._prompt);
        return Task.FromResult(result);
    }
}
