// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;
using Xunit;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion;

public class ClaudishConnectorConfigurationTests
{
    private const string TestApiKey = "test-key-not-a-real-secret";
    private const string TestBaseUrl = "http://localhost:8082/v1";

    private static List<ClaudishModelRoute> NewRoutes()
        => new()
        {
            new ClaudishModelRoute { Alias = "glm-5.2", CostPer1000Token = 0.0006m },
            new ClaudishModelRoute { Alias = "qwen3.6-35b-a3b", CostPer1000Token = 0m, MaxTokens = 4096 },
            new ClaudishModelRoute { Alias = "MiniMax-M3.1-Flash", CostPerRequest = 0.001m },
        };

    [Fact]
    public void CreateBuildsOneConnectorPerAliasSharingTheProxyEndpoint()
    {
        var configuration = ClaudishConnectorConfiguration.Create(TestBaseUrl, TestApiKey, NewRoutes());

        Assert.Equal(3, configuration.OpenAICompatibleCompletions.Count);
        Assert.Equal(new[] { "glm-5.2", "qwen3.6-35b-a3b", "MiniMax-M3.1-Flash" }, configuration.IncludedConnectors);
        Assert.All(configuration.OpenAICompatibleCompletions, connector =>
        {
            Assert.Equal(TestBaseUrl, connector.EndPoint);
            Assert.Equal(connector.Name, connector.ModelId);
            Assert.Equal(TestApiKey, connector.ApiKey);
        });

        var qwen = configuration.OpenAICompatibleCompletions.Single(c => c.Name == "qwen3.6-35b-a3b");
        Assert.Equal(0m, qwen.CostPer1000Token);
        Assert.Equal(4096, qwen.MaxTokens);

        var miniMax = configuration.OpenAICompatibleCompletions.Single(c => c.Name == "MiniMax-M3.1-Flash");
        Assert.Equal(0.001m, miniMax.CostPerRequest);
    }

    [Fact]
    public void CreateFiltersToIncludedAliasesAndRejectsEmptySelection()
    {
        var configuration = ClaudishConnectorConfiguration.Create(
            TestBaseUrl, TestApiKey, NewRoutes(), includedAliases: new[] { "glm-5.2" });

        Assert.Single(configuration.OpenAICompatibleCompletions);
        Assert.Equal("glm-5.2", configuration.IncludedConnectors.Single());

        Assert.Throws<ArgumentException>(() => ClaudishConnectorConfiguration.Create(
            TestBaseUrl, TestApiKey, NewRoutes(), includedAliases: new[] { "not-routed" }));
    }

    [Fact]
    public void CreateRejectsDuplicateAliases()
    {
        var routes = NewRoutes();
        routes.Add(new ClaudishModelRoute { Alias = "glm-5.2" });

        var exception = Assert.Throws<ArgumentException>(() => ClaudishConnectorConfiguration.Create(TestBaseUrl, TestApiKey, routes));
        Assert.Contains("glm-5.2", exception.Message);
    }

    [Fact]
    public void CreateWithoutApiKeyThrowsNamingTheEnvironmentVariable()
    {
        var savedKey = Environment.GetEnvironmentVariable(ClaudishConnectorConfiguration.ApiKeyVariable);
        Environment.SetEnvironmentVariable(ClaudishConnectorConfiguration.ApiKeyVariable, null);
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => ClaudishConnectorConfiguration.Create(TestBaseUrl, apiKey: null, NewRoutes()));
            Assert.Contains(ClaudishConnectorConfiguration.ApiKeyVariable, exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ClaudishConnectorConfiguration.ApiKeyVariable, savedKey);
        }
    }

    [Fact]
    public void CreateFromEnvironmentRequiresTheBaseUrlVariable()
    {
        var savedUrl = Environment.GetEnvironmentVariable(ClaudishConnectorConfiguration.BaseUrlVariable);
        Environment.SetEnvironmentVariable(ClaudishConnectorConfiguration.BaseUrlVariable, null);
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => ClaudishConnectorConfiguration.CreateFromEnvironment(NewRoutes()));
            Assert.Contains(ClaudishConnectorConfiguration.BaseUrlVariable, exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ClaudishConnectorConfiguration.BaseUrlVariable, savedUrl);
        }
    }

    [Fact]
    public void ReadRoutesPreservesDocumentOrderAndAppliesTheOptionalPriceTable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"claudish-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, @"{
  ""version"": ""1.0.0"",
  ""routing"": {
    ""glm-5.2"": [""gc@glm-5.2""],
    ""qwen3.6-35b-a3b"": [""vllm-myia@qwen3.6-35b-a3b""],
    ""MiniMax-M3.1-Flash"": [""mmc@MiniMax-M3.1-Flash""]
  },
  ""customEndpoints"": {
    ""vllm-myia"": { ""kind"": ""simple"", ""url"": ""https://example.invalid/v1"", ""format"": ""openai"" }
  }
}");
        try
        {
            var routes = ClaudishConnectorConfiguration.ReadRoutes(
                path,
                new Dictionary<string, decimal> { ["glm-5.2"] = 0.0006m });

            Assert.Equal(
                new[] { "glm-5.2", "qwen3.6-35b-a3b", "MiniMax-M3.1-Flash" },
                routes.Select(r => r.Alias));
            Assert.Equal(0.0006m, routes.Single(r => r.Alias == "glm-5.2").CostPer1000Token);
            Assert.Null(routes.Single(r => r.Alias == "qwen3.6-35b-a3b").CostPer1000Token);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadRoutesThrowsWhenTheConfigurationHasNoRoutingSection()
    {
        var path = Path.Combine(Path.GetTempPath(), $"claudish-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, @"{ ""version"": ""1.0.0"" }");
        try
        {
            Assert.Throws<InvalidOperationException>(() => ClaudishConnectorConfiguration.ReadRoutes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
