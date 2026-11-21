// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Describes one model alias exposed by the claudish proxy: the alias is the value sent as the OpenAI-compatible
/// model id, and the optional costs feed the MultiConnector cost accounting (they are not exposed by the proxy
/// configuration, so they are supplied by the caller).
/// </summary>
public class ClaudishModelRoute
{
    /// <summary>
    /// Model alias as routed by claudish (e.g. "glm-5.2", "qwen3.6-35b-a3b").
    /// </summary>
    public string Alias { get; set; } = "";

    /// <summary>
    /// Cost per 1000 tokens for this alias, when known. Left null when the caller does not price the model.
    /// </summary>
    public decimal? CostPer1000Token { get; set; }

    /// <summary>
    /// Cost per request for this alias, when known.
    /// </summary>
    public decimal? CostPerRequest { get; set; }

    /// <summary>
    /// Maximum number of tokens to generate for this alias.
    /// </summary>
    public int? MaxTokens { get; set; } = 2048;
}

/// <summary>
/// Builds a <see cref="MultiOpenAICompatibleConnectorConfiguration"/> whose connectors all target the claudish proxy
/// (a single OpenAI-compatible endpoint in front of several providers), one connector per routed model alias.
/// This is what lets the MultiConnector prompt-signature routing and cost accounting run across providers reached
/// through the proxy: the proxy owns provider credentials, the configuration only carries the proxy endpoint,
/// the alias and the pricing known to the caller.
/// </summary>
public static class ClaudishConnectorConfiguration
{
    /// <summary>
    /// Environment variable holding the claudish proxy base URL (e.g. "http://localhost:8082/v1").
    /// </summary>
    public const string BaseUrlVariable = "CLAUDISH_BASE_URL";

    /// <summary>
    /// Environment variable holding the API key expected by the claudish proxy. Many claudish deployments accept any
    /// non-empty key and authenticate upstream; leave it unset only when the proxy accepts unauthenticated calls.
    /// </summary>
    public const string ApiKeyVariable = "CLAUDISH_API_KEY";

    /// <summary>
    /// Builds the connector set from an explicit base URL and route list. Every connector shares the proxy endpoint;
    /// only <see cref="OpenAICompatibleConnectorConfiguration.ModelId"/> differs.
    /// </summary>
    /// <param name="baseUrl">Claudish proxy base URL, OpenAI-compatible (ends with the API version segment).</param>
    /// <param name="apiKey">Proxy API key. Falls back to <see cref="ApiKeyVariable"/>; throws when both are absent.</param>
    /// <param name="routes">Aliases to expose as connectors, in order. Duplicate aliases are rejected: two connectors with the same name would be indistinguishable in prompt settings and accounting.</param>
    /// <param name="includedAliases">Optional subset of route aliases to include. Null includes every route.</param>
    public static MultiOpenAICompatibleConnectorConfiguration Create(
        string baseUrl,
        string? apiKey,
        IEnumerable<ClaudishModelRoute> routes,
        IEnumerable<string>? includedAliases = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Claudish base URL must be an absolute URI, got '{baseUrl}'.", nameof(baseUrl));
        }

        var routeList = routes?.ToList() ?? throw new ArgumentNullException(nameof(routes));
        if (routeList.Count == 0)
        {
            throw new ArgumentException("At least one claudish model route is required.", nameof(routes));
        }

        var duplicate = routeList.GroupBy(r => r.Alias, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate claudish model alias '{duplicate.Key}'.", nameof(routes));
        }

        var resolvedKey = apiKey ?? Environment.GetEnvironmentVariable(ApiKeyVariable)
            ?? throw new InvalidOperationException(
                $"No API key for the claudish proxy: pass an explicit key or set the {ApiKeyVariable} environment variable.");

        var included = includedAliases is null ? null : new HashSet<string>(includedAliases, StringComparer.Ordinal);

        var configuration = new MultiOpenAICompatibleConnectorConfiguration();

        foreach (var route in routeList)
        {
            if (included is not null && !included.Contains(route.Alias))
            {
                continue;
            }

            configuration.OpenAICompatibleCompletions.Add(new OpenAICompatibleConnectorConfiguration
            {
                Name = route.Alias,
                ModelId = route.Alias,
                EndPoint = baseUrl,
                ApiKey = resolvedKey,
                MaxTokens = route.MaxTokens,
                CostPer1000Token = route.CostPer1000Token,
                CostPerRequest = route.CostPerRequest,
            });
            configuration.IncludedConnectors.Add(route.Alias);
        }

        if (configuration.OpenAICompatibleCompletions.Count == 0)
        {
            throw new ArgumentException(
                "No claudish route matched the included aliases; the connector set would be empty.",
                nameof(includedAliases));
        }

        return configuration;
    }

    /// <summary>
    /// Builds the connector set from <see cref="BaseUrlVariable"/> and <see cref="ApiKeyVariable"/>.
    /// </summary>
    public static MultiOpenAICompatibleConnectorConfiguration CreateFromEnvironment(
        IEnumerable<ClaudishModelRoute> routes,
        IEnumerable<string>? includedAliases = null)
    {
        var baseUrl = Environment.GetEnvironmentVariable(BaseUrlVariable)
            ?? throw new InvalidOperationException(
                $"No claudish proxy URL: set the {BaseUrlVariable} environment variable.");

        return Create(baseUrl, apiKey: null, routes, includedAliases);
    }

    /// <summary>
    /// Reads the routed model aliases from a claudish configuration file (the "routing" section maps each public
    /// alias to the provider-qualified ids it can serve). Document order is preserved so a caller can use the first
    /// entries as the default connector order.
    /// </summary>
    /// <param name="claudishConfigPath">Path to the claudish config.json.</param>
    /// <param name="costPer1000Token">Optional pricing per alias; aliases absent from the table are left unpriced.</param>
    public static IReadOnlyList<ClaudishModelRoute> ReadRoutes(
        string claudishConfigPath,
        IReadOnlyDictionary<string, decimal>? costPer1000Token = null)
    {
        if (!File.Exists(claudishConfigPath))
        {
            throw new FileNotFoundException($"Claudish configuration not found: {claudishConfigPath}", claudishConfigPath);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(claudishConfigPath));
        if (!document.RootElement.TryGetProperty("routing", out var routing) || routing.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Claudish configuration has no 'routing' object: {claudishConfigPath}");
        }

        var routes = new List<ClaudishModelRoute>();
        foreach (var entry in routing.EnumerateObject())
        {
            decimal? cost = costPer1000Token is not null && costPer1000Token.TryGetValue(entry.Name, out var priced)
                ? priced
                : null;

            routes.Add(new ClaudishModelRoute
            {
                Alias = entry.Name,
                CostPer1000Token = cost,
            });
        }

        if (routes.Count == 0)
        {
            throw new InvalidOperationException($"Claudish configuration routes no model alias: {claudishConfigPath}");
        }

        return routes;
    }
}
