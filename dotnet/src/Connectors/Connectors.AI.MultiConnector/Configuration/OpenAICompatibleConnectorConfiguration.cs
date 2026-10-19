// Copyright (c) MyIA. All rights reserved.

using System;
using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using OpenAI;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Configuration for an OpenAI-compatible text generation provider (OpenAI, OpenRouter, vLLM, or any proxy exposing
/// the OpenAI API shape). This is the transport replacement for the legacy Oobabooga WebSocket connector: the
/// underlying <see cref="ITextGenerationService"/> is the native SK OpenAI client, so both the streaming and
/// non-streaming paths come with it, with no adapter to write.
/// </summary>
public class OpenAICompatibleConnectorConfiguration : ConnectorConfigurationBase
{
    /// <summary>
    /// OpenAI-compatible base endpoint for this provider (e.g. "https://openrouter.ai/api/v1").
    /// When null, the client targets the official OpenAI endpoint.
    /// </summary>
    public string? EndPoint { get; set; }

    /// <summary>
    /// Model id as exposed by the provider (e.g. "anthropic/claude-sonnet-4.5" on OpenRouter).
    /// </summary>
    public string ModelId { get; set; } = "";

    /// <summary>
    /// Provider API key. Falls back to the OPENAI_API_KEY environment variable when left null here.
    /// Never hardcode a literal key in a configuration file committed to the repository.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional OpenAI organization id passed along with each request.
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// An optional transformation to apply to the prompt before sending it to the connector.
    /// </summary>
    public PromptTransform? PromptTransform { get; set; }

    /// <summary>
    /// Creates the native SK <see cref="ITextGenerationService"/> for this provider, endpoint override included.
    /// SK 1.78 removed the separate legacy text-completion service (<c>OpenAITextCompletion</c>):
    /// <see cref="OpenAIChatCompletionService"/> implements both <c>IChatCompletionService</c> and
    /// <c>ITextGenerationService</c>, streaming included, so the inherited <see cref="ConnectorConfigurationBase.UseChatCompletion"/>
    /// toggle no longer selects the transport — both shapes are served by the chat-completions endpoint.
    /// </summary>
    public ITextGenerationService CreateCompletion(ILoggerFactory? loggerFactory = null)
    {
        var apiKey = this.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException(
                $"No API key for connector '{this.Name}': set {nameof(this.ApiKey)} or the OPENAI_API_KEY environment variable.");

        var clientOptions = new OpenAIClientOptions();

        if (this.EndPoint is not null)
        {
            clientOptions.Endpoint = new Uri(this.EndPoint);
        }

        if (this.OrganizationId is not null)
        {
            clientOptions.OrganizationId = this.OrganizationId;
        }

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);

        return new OpenAIChatCompletionService(this.ModelId, client, loggerFactory: loggerFactory);
    }
}
