// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using Xunit;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion;

public class OpenAICompatibleConnectorConfigurationTests
{
    private const string TestApiKey = "test-key-not-a-real-secret";

    private static OpenAICompatibleConnectorConfiguration NewConnector(
        string name,
        string modelId,
        string? endPoint = null,
        string? apiKey = TestApiKey)
        => new()
        {
            Name = name,
            ModelId = modelId,
            EndPoint = endPoint,
            ApiKey = apiKey,
        };

    [Fact]
    public void CreateCompletionWithoutApiKeyThrowsNamingConnector()
    {
        var connector = NewConnector("router-main", "anthropic/claude-sonnet-4.5", endPoint: "https://openrouter.ai/api/v1", apiKey: null);
        var savedKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => connector.CreateCompletion());
            Assert.Contains("router-main", exception.Message);
            Assert.Contains("OPENAI_API_KEY", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", savedKey);
        }
    }

    [Fact]
    public void CreateCompletionFallsBackToEnvironmentVariable()
    {
        var connector = NewConnector("official-openai", "gpt-4o-mini", endPoint: null, apiKey: null);
        var savedKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", TestApiKey);
        try
        {
            Assert.NotNull(connector.CreateCompletion());
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", savedKey);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://openrouter.ai/api/v1")]
    public void CreateCompletionReturnsTextGenerationServiceWithOrWithoutEndpointOverride(string? endPoint)
    {
        var connector = NewConnector("router-main", "anthropic/claude-sonnet-4.5", endPoint: endPoint);

        var service = connector.CreateCompletion();

        Assert.IsAssignableFrom<ITextGenerationService>(service);
    }

    [Fact]
    public void CreateNamedCompletionsFiltersOnIncludedConnectors()
    {
        var configuration = new MultiOpenAICompatibleConnectorConfiguration
        {
            IncludedConnectors = new List<string> { "router-main", "local-vllm" },
            OpenAICompatibleCompletions = new List<OpenAICompatibleConnectorConfiguration>
            {
                NewConnector("router-main", "anthropic/claude-sonnet-4.5", endPoint: "https://openrouter.ai/api/v1"),
                NewConnector("local-vllm", "qwen/qwen3-14b", endPoint: "http://localhost:8001/v1"),
                NewConnector("excluded-official", "gpt-4o-mini"),
            },
        };

        var completions = configuration.CreateNamedCompletions();

        Assert.Equal(2, completions.Count);
        Assert.Equal(new[] { "local-vllm", "router-main" }, completions.Select(c => c.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void CreateNamedCompletionsModelNamesFilterFurther()
    {
        var configuration = new MultiOpenAICompatibleConnectorConfiguration
        {
            IncludedConnectors = new List<string> { "router-main", "local-vllm" },
            OpenAICompatibleCompletions = new List<OpenAICompatibleConnectorConfiguration>
            {
                NewConnector("router-main", "anthropic/claude-sonnet-4.5", endPoint: "https://openrouter.ai/api/v1"),
                NewConnector("local-vllm", "qwen/qwen3-14b", endPoint: "http://localhost:8001/v1"),
            },
        };

        var completions = configuration.CreateNamedCompletions(new List<string> { "local-vllm" });

        var completion = Assert.Single(completions);
        Assert.Equal("local-vllm", completion.Name);
    }

    [Fact]
    public void CreateNamedCompletionsCarriesCostsAndTransforms()
    {
        var promptTransform = new PromptTransform();
        var connector = NewConnector("router-main", "anthropic/claude-sonnet-4.5", endPoint: "https://openrouter.ai/api/v1");
        connector.CostPerRequest = 0.02m;
        connector.CostPer1000Token = 0.001m;
        connector.PromptTransform = promptTransform;
        var configuration = new MultiOpenAICompatibleConnectorConfiguration
        {
            IncludedConnectors = new List<string> { "router-main" },
            OpenAICompatibleCompletions = new List<OpenAICompatibleConnectorConfiguration> { connector },
        };

        var completion = Assert.Single(configuration.CreateNamedCompletions());

        Assert.Equal(0.02m, completion.CostPerRequest);
        Assert.Equal(0.001m, completion.CostPer1000Token);
        Assert.NotNull(completion.TokenCountFunc);
        Assert.True(completion.TokenCountFunc("a prompt long enough to span several tokens") > 1);
        Assert.Same(promptTransform, completion.PromptTransform);
        Assert.NotNull(completion.TemperatureTransform);
        Assert.Equal(0.01, completion.TemperatureTransform(0));
        Assert.Equal(0.7, completion.TemperatureTransform(0.7));
        Assert.IsAssignableFrom<ITextGenerationService>(completion.TextCompletion);
    }
}
