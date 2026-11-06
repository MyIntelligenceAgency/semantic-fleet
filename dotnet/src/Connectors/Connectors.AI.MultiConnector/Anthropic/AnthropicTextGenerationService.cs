// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Anthropic;

/// <summary>
/// Text generation service speaking the Anthropic Messages API (<c>POST /v1/messages</c>).
/// </summary>
/// <remarks>
/// Semantic Kernel 1.78 ships no Anthropic text generation connector, so a MultiConnector that wants a
/// native Claude seat has to carry its own transport. This class is that transport and nothing more: it
/// maps the SK <see cref="ITextGenerationService"/> contract onto the Messages API, both for the buffered
/// and the streaming path, and stays on netstandard2.0 like the rest of the package.
/// <para>
/// The HTTP client is injectable so the unit tests can drive it with a stub handler: no test in this
/// repository reaches the network.
/// </para>
/// </remarks>
public sealed class AnthropicTextGenerationService : ITextGenerationService, IDisposable
{
    /// <summary>Default Anthropic API root.</summary>
    public const string DefaultEndPoint = "https://api.anthropic.com";

    /// <summary>Default value of the <c>anthropic-version</c> header.</summary>
    public const string DefaultAnthropicVersion = "2023-06-01";

    /// <summary>Maximum number of tokens generated when the caller does not ask for another value.</summary>
    public const int DefaultMaxTokens = 2048;

    private const string MessagesPath = "/v1/messages";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicTextGenerationService"/> class.
    /// </summary>
    /// <param name="modelId">Anthropic model id, e.g. "claude-sonnet-4-5".</param>
    /// <param name="apiKey">Anthropic API key, sent as the <c>x-api-key</c> header.</param>
    /// <param name="endPoint">API root override; defaults to <see cref="DefaultEndPoint"/>.</param>
    /// <param name="anthropicVersion">Value of the <c>anthropic-version</c> header.</param>
    /// <param name="httpClient">Optional client to send the requests (tests pass a stub-backed one).</param>
    /// <param name="logger">Optional logger.</param>
    public AnthropicTextGenerationService(
        string modelId,
        string apiKey,
        string? endPoint = null,
        string? anthropicVersion = null,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("An Anthropic connector needs a model id.", nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("An Anthropic connector needs an API key.", nameof(apiKey));
        }

        this.ModelId = modelId;
        this.ApiKey = apiKey;
        this.EndPoint = string.IsNullOrWhiteSpace(endPoint) ? DefaultEndPoint : endPoint!.TrimEnd('/');
        this.AnthropicVersion = string.IsNullOrWhiteSpace(anthropicVersion) ? DefaultAnthropicVersion : anthropicVersion!;
        this._logger = logger;
        this._ownsHttpClient = httpClient is null;
        this._httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>Gets the Anthropic model id this service generates with.</summary>
    public string ModelId { get; }

    /// <summary>Gets the API root this service posts to.</summary>
    public string EndPoint { get; }

    /// <summary>Gets the value sent in the <c>anthropic-version</c> header.</summary>
    public string AnthropicVersion { get; }

    private string ApiKey { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var request = this.CreateRequest(prompt, executionSettings, stream: false);
        using (request)
        {
            using var response = await this._httpClient
                .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            EnsureSuccess(response, body);

            var text = ExtractText(body);
            this._logger?.LogDebug("Anthropic completion on {Model} returned {Length} characters.", this.ModelId, text.Length);

            return new List<TextContent> { new(text, this.ModelId) };
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = this.CreateRequest(prompt, executionSettings, stream: true);
        using (request)
        {
            using var response = await this._httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                EnsureSuccess(response, errorBody);
            }

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Server-sent events: only "data:" lines carry a payload; comments, "event:" and blank
                // separators are skipped, and the terminal [DONE] marker is not JSON.
                if (!line.StartsWith("data:", StringComparison.Ordinal))
                {
                    continue;
                }

                var payload = line.Substring("data:".Length).Trim();
                if (payload.Length == 0 || payload == "[DONE]")
                {
                    continue;
                }

                var delta = ExtractStreamingDelta(payload);
                if (!string.IsNullOrEmpty(delta))
                {
                    yield return new StreamingTextContent(delta);
                }
            }
        }
    }

    /// <summary>Extracts the concatenated text blocks of a Messages API response body.</summary>
    /// <param name="body">The JSON response body.</param>
    /// <returns>The concatenated text of every <c>text</c> content block.</returns>
    public static string ExtractText(string body)
    {
        using var document = JsonDocument.Parse(body);

        if (!document.RootElement.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Anthropic response carried no 'content' array: {Truncate(body)}");
        }

        var builder = new StringBuilder();
        foreach (var block in content.EnumerateArray())
        {
            if (block.ValueKind == JsonValueKind.Object
                && block.TryGetProperty("type", out var type)
                && type.GetString() == "text"
                && block.TryGetProperty("text", out var text))
            {
                builder.Append(text.GetString());
            }
        }

        return builder.ToString();
    }

    /// <summary>Extracts the text delta carried by one server-sent event payload, if any.</summary>
    /// <param name="payload">The JSON payload of a <c>data:</c> line.</param>
    /// <returns>The delta text, or null when the event carries none (message_start, ping, message_delta, message_stop).</returns>
    public static string? ExtractStreamingDelta(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.TryGetProperty("type", out var type))
            {
                var eventType = type.GetString();
                if (eventType == "error")
                {
                    throw new InvalidOperationException($"Anthropic stream reported an error: {Truncate(payload)}");
                }

                if (eventType != "content_block_delta")
                {
                    return null;
                }
            }

            if (root.TryGetProperty("delta", out var delta)
                && delta.ValueKind == JsonValueKind.Object
                && delta.TryGetProperty("text", out var text))
            {
                return text.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            // A comment or a keep-alive line that is not JSON: nothing to yield.
            return null;
        }
    }

    /// <summary>Releases the HTTP client when this instance created it.</summary>
    public void Dispose()
    {
        if (this._ownsHttpClient)
        {
            this._httpClient.Dispose();
        }
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Anthropic Messages API returned {(int)response.StatusCode} ({response.StatusCode}): {Truncate(body)}");
    }

    private static string Truncate(string value, int maxLength = 512)
        => value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";

    private static int? ReadInt(PromptExecutionSettings? settings, string key)
    {
        var value = ReadRaw(settings, key);
        switch (value)
        {
            case null:
                return null;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number):
                return number;
            case int number:
                return number;
            case long number:
                return (int)number;
            default:
                return null;
        }
    }

    private static double? ReadDouble(PromptExecutionSettings? settings, string key)
    {
        var value = ReadRaw(settings, key);
        switch (value)
        {
            case null:
                return null;
            case JsonElement element when element.ValueKind == JsonValueKind.Number:
                return element.GetDouble();
            case double number:
                return number;
            case int number:
                return number;
            case long number:
                return number;
            default:
                return null;
        }
    }

    private static object? ReadRaw(PromptExecutionSettings? settings, string key)
    {
        if (settings is null)
        {
            return null;
        }

        if (settings.ExtensionData is not null
            && settings.ExtensionData.TryGetValue(key, out var fromExtension)
            && fromExtension is not null)
        {
            return fromExtension;
        }

        return null;
    }

    private HttpRequestMessage CreateRequest(string prompt, PromptExecutionSettings? settings, bool stream)
    {
        var payload = BuildRequestJson(prompt, settings, stream);

        var request = new HttpRequestMessage(HttpMethod.Post, this.EndPoint + MessagesPath)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        request.Headers.TryAddWithoutValidation("x-api-key", this.ApiKey);
        request.Headers.TryAddWithoutValidation("anthropic-version", this.AnthropicVersion);

        return request;
    }

    private string BuildRequestJson(string prompt, PromptExecutionSettings? settings, bool stream)
    {
        var maxTokens = ReadInt(settings, "max_tokens") ?? DefaultMaxTokens;
        var temperature = ReadDouble(settings, "temperature");

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("model", this.ModelId);
            writer.WriteNumber("max_tokens", maxTokens);

            if (temperature.HasValue)
            {
                writer.WriteNumber("temperature", temperature.Value);
            }

            if (stream)
            {
                writer.WriteBoolean("stream", true);
            }

            // The Messages API takes a conversation; a bare text prompt is a single user turn.
            writer.WritePropertyName("messages");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("role", "user");
            writer.WriteString("content", prompt);
            writer.WriteEndObject();
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
