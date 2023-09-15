﻿// Copyright (c) MyIA. All rights reserved.

using System.Threading;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using NS of KernelConfig
namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;
#pragma warning restore IDE0130

/// <summary>
/// Provides extension methods for the <see cref="KernelBuilder"/> class to configure a Multi connector completion.
/// </summary>
public static class MultiConnectorKernelBuilderExtensions
{
    #region Text Completion

    /// <summary>
    /// Adds an MultiConnector completion service to the list.
    /// </summary>
    /// <param name="builder">The <see cref="KernelBuilder"/> instance</param>
    /// <param name="settings">An instance of the <see cref="MultiTextCompletionSettings"/> to configure the multi Text completion.</param>
    /// <param name="mainTextCompletion">The primary text completion to used by default for completion calls and vetting other completion providers.</param>
    /// <param name="analysisTaskCancellationToken">The cancellation token to use for the completion manager.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="setAsDefault">Whether the service should be the default for its type.</param>
    /// <param name="otherCompletions">The secondary text completions that need vetting to be used for completion calls.</param>
    /// <returns>Self instance</returns>
    public static KernelBuilder WithMultiConnectorCompletionService(this KernelBuilder builder,
        MultiTextCompletionSettings settings,
        NamedTextCompletion mainTextCompletion,
        CancellationToken? analysisTaskCancellationToken = null,
        string? serviceId = null,
        bool setAsDefault = false,
        params NamedTextCompletion[]? otherCompletions)
    {
        builder.WithAIService<ITextCompletion>(serviceId, (loggerFactory, config) => new MultiTextCompletion(
            settings,
            mainTextCompletion,
            analysisTaskCancellationToken,
            loggerFactory: loggerFactory,
            otherCompletions: otherCompletions), setAsDefault);
        return builder;
    }

    #endregion
}
