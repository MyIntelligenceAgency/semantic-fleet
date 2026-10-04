// SPDX-License-Identifier: MIT
// Console front-end for Connectors.AI.MultiConnector (Axis 6, epic #1210).
//
// Routing contract (organ semantics, SimpleMatchPromptSettings): a prompt is
// routed by its SIGNATURE PREFIX — the first PromptMultiConnectorSettings
// whose Signature.PromptStart is a prefix of the prompt wins (FirstOrDefault,
// order-sensitive). This tool declares three types:
//
//   [simple]       -> gpt-4o-mini          (OpenAI direct, cheap and fast)
//   [analytique]   -> gemini-2.5-flash      (OpenRouter, mid-tier)
//   [raisonnement] -> claude-sonnet-4.5     (OpenRouter, top-tier, fallback chain)
//
// Usage:
//   SemanticFleet.MultiRoute --demo                 # the three canonical prompts
//   SemanticFleet.MultiRoute --prompt "[simple] ..."
//   SemanticFleet.MultiRoute --prompt "..." --json  # machine-readable line
//
// Keys are read exclusively from the environment (never inline literals):
//   OPENAI_API_KEY      — OpenAI direct
//   OPENROUTER_API_KEY  — OpenRouter (OpenAI-compatible endpoint)
using System.ClientModel;
using System.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using OpenAI;

namespace SemanticFleet.MultiRoute;

internal static class Program
{
    private const string RouterEndpoint = "https://openrouter.ai/api/v1";

    private static int Main(string[] args)
    {
        bool demo = args.Contains("--demo");
        bool json = args.Contains("--json");
        int promptIdx = Array.IndexOf(args, "--prompt");
        if (!demo && promptIdx == -1)
        {
            Console.Error.WriteLine("Usage: SemanticFleet.MultiRoute --demo | --prompt \"<text with [signature] prefix>\" [--json]");
            return 2;
        }

        string? openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string? routerKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrEmpty(openAiKey) || string.IsNullOrEmpty(routerKey))
        {
            Console.Error.WriteLine("OPENAI_API_KEY and OPENROUTER_API_KEY must be set (environment only — never literals).");
            return 2;
        }

        string[] prompts = demo
            ? new[]
            {
                "[simple] Traduis en une phrase : 'la nuit tous les chats sont gris'.",
                "[analytique] Compare en trois puces les compromis cout/latence d'un cache LRU face a un cache LFU.",
                "[raisonnement] Un fermier doit traverser une riviere avec un loup, une chevre et un chou. Donne le plan minimal etape par etape, puis prouve qu'il est minimal.",
            }
            : new[] { args[promptIdx + 1] };

        var (kernel, settings) = BuildKernel(openAiKey, routerKey);
        var service = kernel.GetRequiredService<ITextGenerationService>();

        int rc = 0;
        foreach (string prompt in prompts)
        {
            // Route prediction: the organ's own matching rule (first matching
            // signature prefix, order-sensitive) — printed so the measured
            // latency/response can be read against the table.
            string type = prompt.Split(']')[0].TrimStart('[');
            var matched = settings.PromptMultiConnectorSettings
                .FirstOrDefault(p => prompt.StartsWith(p.PromptType.Signature.PromptStart, StringComparison.Ordinal));
            string route = matched?.PromptType.PromptName ?? "(no match — main connector serves)";
            if (matched is null && prompt.StartsWith('['))
            {
                Console.Error.WriteLine($"Unknown signature prefix '[{type}]' — expected [simple], [analytique] or [raisonnement].");
                rc = 2;
                continue;
            }

            var sw = Stopwatch.StartNew();
            string text;
            try
            {
                var response = service.GetTextContentAsync(prompt, new PromptExecutionSettings()).GetAwaiter().GetResult();
                text = response.Text ?? "(vide)";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{type}] provider call failed: {ex.GetType().Name}: {ex.Message}");
                rc = 1;
                continue;
            }
            finally
            {
                sw.Stop();
            }

            if (json)
            {
                string escaped = System.Text.Json.JsonSerializer.Serialize(text);
                Console.WriteLine($"{{\"type\": \"{type}\", \"route\": \"{route}\", \"latency_ms\": {sw.ElapsedMilliseconds}, \"text\": {escaped}}}");
            }
            else
            {
                string preview = text.Length > 160 ? text[..160] + "..." : text;
                Console.WriteLine($"[{type,-12}] route {route,-18} {sw.ElapsedMilliseconds,5} ms | {preview}");
            }
        }

        return rc;
    }

    /// <summary>
    /// Canonical SK 1.78 wiring (MultiConnectorTests.InitializeKernel): one
    /// main connector + secondaries behind MultiTextCompletion, registered as
    /// the default ITextGenerationService. OpenRouter providers ride the
    /// OpenAI-compatible endpoint via a custom OpenAIClient.
    /// </summary>
    private static (Kernel, MultiTextCompletionSettings) BuildKernel(string openAiKey, string routerKey)
    {
        var cheap = new NamedTextCompletion("gpt-4o-mini", new OpenAIChatCompletionService("gpt-4o-mini", openAiKey))
        {
            CostPer1000Token = 0.00015m,
            MaxDegreeOfParallelism = 5,
        };
        var routerOptions = new OpenAIClientOptions { Endpoint = new Uri(RouterEndpoint) };
        var mid = new NamedTextCompletion(
            "gemini-2.5-flash",
            new OpenAIChatCompletionService("google/gemini-2.5-flash", new OpenAIClient(new ApiKeyCredential(routerKey), routerOptions)))
        {
            CostPer1000Token = 0.0003m,
            MaxDegreeOfParallelism = 5,
        };
        var top = new NamedTextCompletion(
            "claude-sonnet-4.5",
            new OpenAIChatCompletionService("anthropic/claude-sonnet-4.5", new OpenAIClient(new ApiKeyCredential(routerKey), routerOptions)))
        {
            CostPer1000Token = 0.003m,
            MaxDegreeOfParallelism = 5,
        };

        var settings = new MultiTextCompletionSettings
        {
            // Weighted arbiter: 1 latency, 1 cost (organ default).
            ConnectorComparer = MultiTextCompletionSettings.GetWeightedConnectorComparer(1, 1),
            PromptMultiConnectorSettings =
            [
                PromptType("[simple] ", "simple", cheap.Name, mid.Name),
                PromptType("[analytique] ", "analytique", mid.Name, top.Name),
                PromptType("[raisonnement] ", "raisonnement", top.Name, mid.Name, cheap.Name),
            ],
        };

        var builder = Kernel.CreateBuilder();
        builder.WithMultiConnectorCompletionService(
            serviceId: null,
            settings: settings,
            mainTextCompletion: top,
            setAsDefault: true,
            otherCompletions: [cheap, mid]);
        return (builder.Build(), settings);
    }

    /// <summary>One routing-table entry: signature prefix, type name, then the
    /// preference-ordered connectors with declared cost/duration for the arbiter.</summary>
    private static PromptMultiConnectorSettings PromptType(string prefix, string name, params string[] connectors)
    {
        (decimal cost, TimeSpan duration)[] profile =
        [
            (0.00015m, TimeSpan.FromMilliseconds(800)),
            (0.0003m, TimeSpan.FromMilliseconds(1500)),
            (0.003m, TimeSpan.FromMilliseconds(6000)),
        ];
        var entry = new PromptMultiConnectorSettings
        {
            PromptType = new PromptType { PromptName = name, Signature = new PromptSignature { PromptStart = prefix } },
        };
        for (int i = 0; i < connectors.Length; i++)
        {
            (decimal cost, TimeSpan duration) = profile[Math.Min(i, profile.Length - 1)];
            entry.ConnectorSettingsDictionary[connectors[i]] = new PromptConnectorSettings
            {
                AverageCost = cost,
                AverageDuration = duration,
            };
        }
        return entry;
    }
}
