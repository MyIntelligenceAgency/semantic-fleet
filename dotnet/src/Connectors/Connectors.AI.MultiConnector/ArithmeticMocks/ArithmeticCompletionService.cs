// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.ArithmeticMocks;

/// <summary>
///Arithmetic computation completion model that implements the ITextCompletion interface. It supports several or all the 4 basic arithmetic operations, and can vet the result of another connector.
/// </summary>
public class ArithmeticCompletionService : ITextCompletion
{
    /// <summary>
    /// Initializes a new instance of the ArithmeticCompletionService class with the specified settings and parameters.
    /// </summary>
    /// <param name="multiTextCompletionSettings">The settings for multi-text completion.</param>
    /// <param name="supportedOperations">The list of supported arithmetic operations.</param>
    /// <param name="engine">The arithmetic engine to use for calculations.</param>
    /// <param name="callTime">The time it takes to make a call to the service.</param>
    /// <param name="costPerRequest">The cost per request made to the service.</param>
    /// <param name="creditor">The creditor for call request costs.</param>
    /// <returns>
    /// An instance of the ArithmeticCompletionService class.
    /// </returns>
    public ArithmeticCompletionService(MultiTextCompletionSettings multiTextCompletionSettings, List<ArithmeticOperation> supportedOperations, ArithmeticEngine engine, TimeSpan callTime, decimal costPerRequest, CallRequestCostCreditor? creditor)
    {
        this.MultiTextCompletionSettings = multiTextCompletionSettings;
        this.SupportedOperations = supportedOperations;
        this.Engine = engine;
        this.CallTime = callTime;
        this.CostPerRequest = costPerRequest;
        this.Creditor = creditor;
        this.VettingPromptSettings = this.GenerateVettingSignature();
    }

    private PromptMultiConnectorSettings GenerateVettingSignature()
    {
        var tempOperation = ArithmeticEngine.GeneratePrompt(ArithmeticOperation.Add, 1, 1);
        var tempResult = "2";
        var vettingSession = this.MultiTextCompletionSettings.AnalysisSettings.GetVettingJob(tempOperation, tempResult, this.MultiTextCompletionSettings);
        return this.MultiTextCompletionSettings.GetPromptSettings(vettingSession, out _);
    }

    /// <summary>
    /// The settings for vetting prompt completion.
    /// </summary>
    public PromptMultiConnectorSettings VettingPromptSettings { get; set; }

    /// <summary>
    /// The settings for multi-text completion.
    /// </summary>
    public MultiTextCompletionSettings MultiTextCompletionSettings { get; set; }

    /// <summary>
    /// The list of supported arithmetic operations.
    /// </summary>
    public List<ArithmeticOperation> SupportedOperations { get; set; }

    /// <summary>
    /// The arithmetic engine to use for calculations.
    /// </summary>
    public ArithmeticEngine Engine { get; set; }

    /// <summary>
    /// The time it takes to make a call to the service.
    /// </summary>
    public TimeSpan CallTime { get; set; }

    /// <summary>
    /// The cost per request made to the service.
    /// </summary>
    public decimal CostPerRequest { get; set; }

    /// <summary>
    /// The creditor for call request costs.
    /// </summary>
    public CallRequestCostCreditor? Creditor { get; set; }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ITextResult>> GetCompletionsAsync(string text, AIRequestSettings? requestSettings, CancellationToken cancellationToken = default)
    {
        var job = new CompletionJob(text, requestSettings);
        ArithmeticStreamingResultBase streamingResult = await this.ComputeResultAsync(job, cancellationToken).ConfigureAwait(false);
        return new List<ITextResult>
        {
            streamingResult
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ITextStreamingResult> GetStreamingCompletionsAsync(string text, AIRequestSettings? requestSettings, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var job = new CompletionJob(text, requestSettings);
        ArithmeticStreamingResultBase streamingResult = await this.ComputeResultAsync(job, cancellationToken).ConfigureAwait(false);
        yield return streamingResult;
    }

    private async Task<ArithmeticStreamingResultBase> ComputeResultAsync(CompletionJob job, CancellationToken cancellationToken = default)
    {
        await Task.Delay(this.CallTime, cancellationToken).ConfigureAwait(false);
        var isVetting = this.VettingPromptSettings.PromptType.Signature.Matches(job);
        ArithmeticStreamingResultBase streamingResult;
        if (isVetting)
        {
            streamingResult = new ArithmeticVettingStreamingResult(this.MultiTextCompletionSettings, job.Prompt, this.Engine, this.CallTime);
        }
        else
        {
            this.Creditor?.Credit(this.CostPerRequest);
            streamingResult = new ArithmeticComputingStreamingResult(job.Prompt, this.Engine, this.CallTime);
        }

        return streamingResult;
    }
}
