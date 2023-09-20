// Copyright (c) MyIA. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Http;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga;

/// <summary>
/// Provides extension methods for the <see cref="KernelBuilder"/> class to configure a Oobabooga connector completion.
/// </summary>
public static class OobaboogaKernelBuilderExtensions
{
    /// <summary>
    /// Adds an Oobabooga Text completion service to a Kernel.
    /// </summary>
    /// <param name="builder">The <see cref="KernelBuilder"/> instance</param>
    /// <param name="settings">An instance of the <see cref="OobaboogaTextCompletionSettings"/> to configure the Oobabooga Text completion.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="setAsDefault">Whether the service should be the default for its type.</param>
    /// <returns>Self instance</returns>
    public static KernelBuilder WithOobaboogaTextCompletionService(this KernelBuilder builder,
        OobaboogaTextCompletionSettings settings,
        string? serviceId = null,
        bool setAsDefault = false)
    {
        builder.WithAIService<ITextCompletion>(serviceId, (loggerFactory, config) => new OobaboogaTextCompletion(
            settings), setAsDefault);
        return builder;
    }

    /// <summary>
    /// Adds an Oobabooga Text completion service to a Kernel.
    /// </summary>
    /// <param name="builder">The <see cref="KernelBuilder"/> instance</param>
    /// <param name="settings">An instance of the <see cref="OobaboogaChatCompletionSettings"/> to configure the Oobabooga Chat completion.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="alsoAsTextCompletion">Whether to use the service also for text completion</param>
    /// <param name="setAsDefault">Whether the service should be the default for its type.</param>
    /// <returns>Self instance</returns>
    public static KernelBuilder WithOobaboogaChatCompletionService(this KernelBuilder builder,
        OobaboogaChatCompletionSettings settings,
        string? serviceId = null,
        bool alsoAsTextCompletion = true,
        bool setAsDefault = false)
    {
        OobaboogaChatCompletion Factory(ILoggerFactory loggerFactory, IDelegatingHandlerFactory httpHandlerFactory) => new(
            settings);

        builder.WithAIService<IChatCompletion>(serviceId, Factory, setAsDefault);

        // If enabled, allow to use it also for semantic functions
        if (alsoAsTextCompletion)
        {
            builder.WithAIService<ITextCompletion>(serviceId, Factory, setAsDefault);
        }

        return builder;
    }
}
