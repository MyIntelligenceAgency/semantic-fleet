// Copyright (c) MyIA. All rights reserved.

using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Provides extension methods for the <see cref="IKernelBuilder"/> to configure a Multi connector completion.
/// </summary>
public static class MultiConnectorKernelBuilderExtensions
{
    #region Text Completion

    /// <summary>
    /// Adds an MultiConnector completion service to the list.
    /// </summary>
    /// <param name="builder">The <see cref="IKernelBuilder"/> instance</param>
    /// <param name="settings">An instance of the <see cref="MultiTextCompletionSettings"/> to configure the multi Text completion.</param>
    /// <param name="mainTextCompletion">The primary text completion to used by default for completion calls and vetting other completion providers.</param>
    /// <param name="analysisTaskCancellationToken">The cancellation token to use for the completion manager.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="setAsDefault">Whether the service should be the default for its type.</param>
    /// <param name="otherCompletions">The secondary text completions that need vetting to be used for completion calls.</param>
    /// <returns>Self instance</returns>
    public static IKernelBuilder WithMultiConnectorCompletionService(this IKernelBuilder builder,
        MultiTextCompletionSettings settings,
        NamedTextCompletion mainTextCompletion,
        CancellationToken? analysisTaskCancellationToken = null,
        string? serviceId = null,
        bool setAsDefault = false,
        params NamedTextCompletion[]? otherCompletions)
    {
        builder.Services.AddKeyedSingleton<ITextGenerationService>(serviceId, (sp, _) => new MultiTextCompletion(
            settings,
            mainTextCompletion,
            analysisTaskCancellationToken,
            loggerFactory: sp.GetService<ILoggerFactory>(),
            otherCompletions: otherCompletions));
        if (setAsDefault)
        {
            builder.Services.AddKeyedSingleton<ITextGenerationService>(null, (sp, _) => new MultiTextCompletion(
                settings,
                mainTextCompletion,
                analysisTaskCancellationToken,
                loggerFactory: sp.GetService<ILoggerFactory>(),
                otherCompletions: otherCompletions));
        }

        return builder;
    }

    #endregion
}
