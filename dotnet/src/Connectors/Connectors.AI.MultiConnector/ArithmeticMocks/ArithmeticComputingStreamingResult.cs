﻿// Copyright (c) MyIA. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Orchestration;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.ArithmeticMocks;

/// <summary>
/// Class representing an arithmetic streaming result for an arithmetic computation that implements the ITextStreamingResult interface.
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
    protected override Task<ModelResult> GenerateModelResult()
    {
        var result = this._engine.Run(this._prompt);
        return Task.FromResult(new ModelResult(result));
    }
}
