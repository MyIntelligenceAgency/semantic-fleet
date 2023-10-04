// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Class used to configure Azure OpenAI completions.
/// </summary>
public class AzureOpenAIConfiguration
{
    /// <summary>
    /// Optional key to identify your connector among others.
    /// </summary>
    public string ServiceId { get; set; }

    /// <summary>
    /// Name of the text completion model to use when no chat completion model is specified
    /// </summary>
    public string DeploymentName { get; set; }

    /// <summary>
    /// Name of the chat completion model to use
    /// </summary>
    public string? ChatDeploymentName { get; set; }

    /// <summary>
    /// Endpoint to access the Azure OpenAI services
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// API key to access the Azure OpenAI services
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="AzureOpenAIConfiguration"/>.
    /// </summary>
    public AzureOpenAIConfiguration(string serviceId, string deploymentName, string endpoint, string apiKey, string? chatDeploymentName = null)
    {
        this.ServiceId = serviceId;
        this.DeploymentName = deploymentName;
        this.ChatDeploymentName = chatDeploymentName;
        this.Endpoint = endpoint;
        this.ApiKey = apiKey;
    }
}
