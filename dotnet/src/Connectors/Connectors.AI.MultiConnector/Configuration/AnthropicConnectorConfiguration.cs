// Copyright (c) MyIA. All rights reserved.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Anthropic;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Configuration for a native Anthropic text generation provider, seated as a secondary connector of a
/// MultiConnector alongside OpenAI-compatible ones.
/// </summary>
/// <remarks>
/// Semantic Kernel 1.78 has no Anthropic connector, so unlike <see cref="OpenAICompatibleConnectorConfiguration"/>
/// — which delegates to the SK OpenAI client — this one carries its own Messages API transport
/// (<see cref="AnthropicTextGenerationService"/>). It mirrors its sibling's shape on purpose: same
/// <c>EndPoint</c>/<c>ModelId</c>/<c>ApiKey</c> triple, same environment fallback, same optional
/// <see cref="PromptTransform"/>, so a settings file can list both kinds of connector without a special case.
/// </remarks>
public class AnthropicConnectorConfiguration : ConnectorConfigurationBase
{
    /// <summary>
    /// Environment variable consulted when <see cref="ApiKey"/> is left null.
    /// </summary>
    public const string ApiKeyEnvironmentVariable = "ANTHROPIC_API_KEY";

    /// <summary>
    /// Anthropic API root for this provider (e.g. "https://api.anthropic.com", or a proxy in front of it).
    /// When null, the client targets the official endpoint.
    /// </summary>
    public string? EndPoint { get; set; }

    /// <summary>
    /// Anthropic model id (e.g. "claude-sonnet-4-5").
    /// </summary>
    public string ModelId { get; set; } = "";

    /// <summary>
    /// Anthropic API key. Falls back to the ANTHROPIC_API_KEY environment variable when left null here.
    /// Never hardcode a literal key in a configuration file committed to the repository.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Value of the <c>anthropic-version</c> header. When null, <see cref="AnthropicTextGenerationService.DefaultAnthropicVersion"/>
    /// is sent.
    /// </summary>
    public string? AnthropicVersion { get; set; }

    /// <summary>
    /// An optional transformation to apply to the prompt before sending it to the connector.
    /// </summary>
    public PromptTransform? PromptTransform { get; set; }

    /// <summary>
    /// Creates the <see cref="ITextGenerationService"/> for this provider, endpoint override included.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory propagated to the transport.</param>
    /// <returns>The Anthropic Messages API text generation service.</returns>
    public ITextGenerationService CreateCompletion(ILoggerFactory? loggerFactory = null)
    {
        var apiKey = this.ApiKey ?? Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"No API key for connector '{this.Name}': set {nameof(this.ApiKey)} or the {ApiKeyEnvironmentVariable} environment variable.");

        var logger = loggerFactory?.CreateLogger<AnthropicTextGenerationService>();

        return new AnthropicTextGenerationService(
            this.ModelId,
            apiKey,
            this.EndPoint,
            this.AnthropicVersion,
            httpClient: null,
            logger: logger);
    }
}
