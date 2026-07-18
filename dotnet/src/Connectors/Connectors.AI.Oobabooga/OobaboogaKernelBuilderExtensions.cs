// Copyright (c) MyIA. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.ChatCompletion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga;

/// <summary>
/// Provides extension methods for the <see cref="IKernelBuilder"/> to configure Oobabooga connector completion services.
/// </summary>
public static class OobaboogaKernelBuilderExtensions
{
    /// <summary>
    /// Adds an Oobabooga Text completion service to a Kernel.
    /// </summary>
    /// <param name="builder">The <see cref="IKernelBuilder"/> instance</param>
    /// <param name="settings">An instance of the settings to configure the Oobabooga Text completion.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="setAsDefault">Whether the service should be the default for its type.</param>
    /// <returns>Self instance</returns>
    public static IKernelBuilder AddOobaboogaTextGeneration(this IKernelBuilder builder,
        OobaboogaTextCompletionSettings settings,
        string? serviceId = null,
        bool setAsDefault = false)
    {
        builder.Services.AddKeyedSingleton<ITextGenerationService>(serviceId, (_, _) => new OobaboogaTextCompletion(settings));
        if (setAsDefault)
        {
            builder.Services.AddKeyedSingleton<ITextGenerationService>(null, (_, _) => new OobaboogaTextCompletion(settings));
        }

        return builder;
    }

    /// <summary>
    /// Adds an Oobabooga Chat completion service to a Kernel.
    /// </summary>
    /// <param name="builder">The <see cref="IKernelBuilder"/> instance</param>
    /// <param name="settings">An instance of the settings to configure the Oobabooga Chat completion.</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="setAsDefault">Whether the service should be the default for its type.</param>
    /// <returns>Self instance</returns>
    public static IKernelBuilder AddOobaboogaChatCompletion(this IKernelBuilder builder,
        OobaboogaChatCompletionSettings settings,
        string? serviceId = null,
        bool setAsDefault = false)
    {
        builder.Services.AddKeyedSingleton<IChatCompletionService>(serviceId, (_, _) => new OobaboogaChatCompletion(settings));
        if (setAsDefault)
        {
            builder.Services.AddKeyedSingleton<IChatCompletionService>(null, (_, _) => new OobaboogaChatCompletion(settings));
        }

        return builder;
    }
}
