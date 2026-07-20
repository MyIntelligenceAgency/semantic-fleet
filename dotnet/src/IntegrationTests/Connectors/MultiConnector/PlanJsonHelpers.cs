// Copyright (c) Microsoft. All rights reserved.
#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernel.IntegrationTests.Connectors.MultiConnector;

/// <summary>
/// Helpers to consume the SK 1.78 plan JSON format (Samples/Plans/SK178/*.json) and produce
/// a ChatHistory suitable for the SK 1.78 Auto Function Calling flow
/// (<see cref="FunctionChoiceBehavior.Auto"/>).
///
/// The legacy format used in this repo before SK 1.78 (Plan.FromJson with
/// state/steps/parameters/outputs) was REMOVED in SK 1.78 - the Planners.* packages were
/// deleted and a Plan is now a KernelFunction. To keep the cost-offload integration tests
/// in MultiConnectorTests.cs exercisable, we ship an intermediate JSON shape and a single
/// helper that materializes it into a ChatHistory that the Auto Function Calling loop
/// (<see cref="FunctionChoiceBehavior.Auto"/>) can drive.
///
/// The actual function CALLS are performed by the SK 1.78 Auto Function Calling loop, so
/// the helper does NOT execute anything itself - it only builds the prompt and exposes the
/// declared sequence of invocations so the caller (the test) can feed the corresponding
/// KernelFunctions as available tools before the chat completion is requested.
/// </summary>
internal static class PlanJsonHelpers
{
    // JSON property names accepted in the SK 1.78 plan format.
    private const string NameKey = "name";
    private const string DescriptionKey = "description";
    private const string InputVariableKey = "input_variable";
    private const string UserInputTemplateKey = "user_input_template";
    private const string InvocationsKey = "invocations";

    private const string InvocationPluginKey = "plugin_name";
    private const string InvocationFunctionKey = "name";
    private const string InvocationDescriptionKey = "description";
    private const string InvocationArgumentsKey = "arguments";
    private const string InvocationOutputKey = "output_variable";

    // Variable interpolation syntax: $INPUT, $RESULT__SUMMARY, ...
    private const char VariablePrefix = '$';

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Reads a plan JSON file and returns the parsed <see cref="Sk178Plan"/>.
    /// </summary>
    public static async Task<Sk178Plan> LoadPlanAsync(string planPath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(planPath);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return ParsePlan(doc.RootElement);
    }

    /// <summary>
    /// Parses a plan JSON byte array into <see cref="Sk178Plan"/>.
    /// </summary>
    public static Sk178Plan ParsePlan(string planJson)
    {
        using var doc = JsonDocument.Parse(planJson);
        return ParsePlan(doc.RootElement);
    }

    private static Sk178Plan ParsePlan(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Plan JSON root must be an object.");
        }

        var plan = new Sk178Plan
        {
            Name = ReadOptionalString(root, NameKey) ?? "(unnamed plan)",
            Description = ReadOptionalString(root, DescriptionKey) ?? string.Empty,
            InputVariable = ReadOptionalString(root, InputVariableKey) ?? "INPUT",
            UserInputTemplate = ReadOptionalString(root, UserInputTemplateKey) ?? string.Empty,
        };

        if (root.TryGetProperty(InvocationsKey, out var invocationsElement) &&
            invocationsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var invocation in invocationsElement.EnumerateArray())
            {
                plan.Invocations.Add(ParseInvocation(invocation));
            }
        }

        return plan;
    }

    private static Sk178Invocation ParseInvocation(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Invocation JSON element must be an object.");
        }

        var invocation = new Sk178Invocation
        {
            PluginName = ReadOptionalString(element, InvocationPluginKey) ?? string.Empty,
            Name = ReadOptionalString(element, InvocationFunctionKey) ?? throw new InvalidDataException("Invocation missing 'name'."),
            Description = ReadOptionalString(element, InvocationDescriptionKey) ?? string.Empty,
            OutputVariable = ReadOptionalString(element, InvocationOutputKey) ?? string.Empty,
        };

        if (element.TryGetProperty(InvocationArgumentsKey, out var argsElement) &&
            argsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var arg in argsElement.EnumerateObject())
            {
                if (arg.Value.ValueKind == JsonValueKind.String)
                {
                    invocation.Arguments[arg.Name] = arg.Value.GetString() ?? string.Empty;
                }
                else
                {
                    // For non-string arguments we serialize back to JSON so the consumer
                    // sees a stable representation - this preserves numerics like the
                    // "index": 2 in Summarize_Topics_ElementAt.json.
                    invocation.Arguments[arg.Name] = arg.Value.GetRawText();
                }
            }
        }

        return invocation;
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Null => null,
            _ => property.GetRawText(),
        };
    }

    /// <summary>
    /// Resolves a single $VARIABLE reference inside an argument value, returning the
    /// (variableName, rawValue) pair or null if the value is not a variable reference.
    /// </summary>
    internal static (string VariableName, string Raw)? TryParseVariableReference(string? argumentValue)
    {
        if (string.IsNullOrEmpty(argumentValue) || argumentValue[0] != VariablePrefix)
        {
            return null;
        }

        return (argumentValue.Substring(1), argumentValue);
    }

    /// <summary>
    /// Materializes the SK 1.78 plan into a <see cref="ChatHistory"/> ready for
    /// <see cref="FunctionChoiceBehavior.Auto"/> execution.
    ///
    /// The resulting history has:
    ///   1. A single "system" message describing the plan goal.
    ///   2. A single "user" message carrying the input text (or the user_input_template
    ///      with {{$INPUT}} expanded).
    ///
    /// The helper does NOT pre-declare the tool-call sequence in the ChatHistory itself:
    /// the SK 1.78 Auto Function Calling loop will discover available KernelFunctions on
    /// the Kernel and decide when/how to invoke them at runtime. The plan's invocation
    /// list is exposed separately via <see cref="BuildInvocationPlan"/> so the caller can
    /// register the corresponding KernelFunctions on the Kernel before requesting chat
    /// completion (and so the caller's diagnostic logs can show the intended sequence).
    /// </summary>
    public static ChatHistory BuildChatHistoryFromPlan(Sk178Plan plan, string inputValue)
    {
        var history = new ChatHistory();

        // 1) system message with the plan goal
        if (!string.IsNullOrEmpty(plan.Description))
        {
            history.AddSystemMessage(plan.Description);
        }

        // 2) user message with the input text (apply the user_input_template if provided)
        var userText = string.IsNullOrEmpty(plan.UserInputTemplate)
            ? inputValue
            : plan.UserInputTemplate.Replace("{{$" + plan.InputVariable + "}}", inputValue);
        history.AddUserMessage(userText);

        return history;
    }

    /// <summary>
    /// Returns the materialized invocation sequence (each invocation with arguments
    /// resolved against the prior outputs) as a flat list of
    /// <see cref="ResolvedInvocation"/> structs. The caller (the integration test)
    /// registers the corresponding KernelFunctions on the Kernel so the Auto Function
    /// Calling loop can pick them up at runtime.
    ///
    /// Note: in a strict pre-declared-sequence planner (legacy Plan.FromJson) the
    /// argument values would come from the tool results of previous invocations. In the
    /// SK 1.78 Auto Function Calling flow the chat completion service handles that
    /// chaining itself - the helper still computes the *initial* values statically so
    /// that the caller can log the intended sequence deterministically.
    /// </summary>
    public static IReadOnlyList<ResolvedInvocation> BuildInvocationPlan(Sk178Plan plan, string inputValue)
    {
        var knownOutputs = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [plan.InputVariable] = inputValue,
        };

        var resolved = new List<ResolvedInvocation>(plan.Invocations.Count);
        foreach (var invocation in plan.Invocations)
        {
            var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, rawValue) in invocation.Arguments)
            {
                var parsed = TryParseVariableReference(rawValue);
                if (parsed is { } pair && knownOutputs.TryGetValue(pair.VariableName, out var outputValue))
                {
                    arguments[key] = outputValue;
                }
                else
                {
                    arguments[key] = rawValue;
                }
            }

            resolved.Add(new ResolvedInvocation
            {
                PluginName = invocation.PluginName,
                FunctionName = invocation.Name,
                Description = invocation.Description,
                OutputVariable = invocation.OutputVariable,
                Arguments = arguments,
            });

            // Declare the output variable name so that downstream invocations referencing
            // $RESULT__XXX in the same plan can be resolved statically. The actual value
            // comes from the tool result at runtime - we use a sentinel here.
            if (!string.IsNullOrEmpty(invocation.OutputVariable))
            {
                knownOutputs[invocation.OutputVariable] = $"<{invocation.OutputVariable}>";
            }
        }

        return resolved;
    }

    /// <summary>
    /// Convenience: read the plan JSON file and build both the ChatHistory and the
    /// invocation plan in one call. The caller registers each ResolvedInvocation's
    /// plugin+function as available on the Kernel before requesting chat completion with
    /// <see cref="FunctionChoiceBehavior.Auto"/>.
    /// </summary>
    public static async Task<(ChatHistory History, IReadOnlyList<ResolvedInvocation> Invocations)> BuildChatHistoryFromPlanJsonAsync(string planPath, string inputValue, CancellationToken cancellationToken = default)
    {
        var plan = await LoadPlanAsync(planPath, cancellationToken).ConfigureAwait(false);
        return (BuildChatHistoryFromPlan(plan, inputValue), BuildInvocationPlan(plan, inputValue));
    }
}

/// <summary>
/// A single invocation with its arguments pre-resolved against the plan's variable
/// substitutions. The caller (the integration test) uses the PluginName/FunctionName to
/// register the corresponding KernelFunction on the Kernel so the SK 1.78 Auto Function
/// Calling loop can pick it up at runtime.
/// </summary>
internal sealed class ResolvedInvocation
{
    public string PluginName { get; init; } = string.Empty;
    public string FunctionName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string OutputVariable { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Arguments { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Returns the SK 1.78 fully-qualified name "PluginName-FunctionName", or just
    /// "FunctionName" when no plugin is set.
    /// </summary>
    public string FullyQualifiedName => string.IsNullOrEmpty(PluginName)
        ? FunctionName
        : $"{PluginName}-{FunctionName}";
}

/// <summary>
/// Parsed SK 1.78 plan. Plain DTO - no behavior. The legacy SK Plan type
/// (Plan.FromJson / plan.State / plan.Steps) was REMOVED in SK 1.78.
/// </summary>
internal sealed class Sk178Plan
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input_variable")]
    public string InputVariable { get; set; } = "INPUT";

    [JsonPropertyName("user_input_template")]
    public string UserInputTemplate { get; set; } = string.Empty;

    [JsonPropertyName("invocations")]
    public List<Sk178Invocation> Invocations { get; } = new();
}

internal sealed class Sk178Invocation
{
    [JsonPropertyName("plugin_name")]
    public string PluginName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, string> Arguments { get; } = new(StringComparer.Ordinal);

    [JsonPropertyName("output_variable")]
    public string OutputVariable { get; set; } = string.Empty;
}
