// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Class used to configure OpenAI completions.
/// </summary>
public class OpenAIConfiguration
{
    /// <summary>
    /// Optional key to identify your connector among others.
    /// </summary>
    public string ServiceId { get; set; }

    /// <summary>
    /// Name of the text completion model to use when no chat completion model is specified
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// Name of the chat completion model to use
    /// </summary>
    public string? ChatModelId { get; set; }

    /// <summary>
    /// API key to access the OpenAI services
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="OpenAIConfiguration"/>.
    /// </summary>
    public OpenAIConfiguration(string serviceId, string modelId, string apiKey, string? chatModelId = null)
    {
        this.ServiceId = serviceId;
        this.ModelId = modelId;
        this.ChatModelId = chatModelId;
        this.ApiKey = apiKey;
    }
}
