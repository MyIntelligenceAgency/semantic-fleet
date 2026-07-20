// Copyright (c) MyIA. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace SemanticKernel.IntegrationTests.Connectors.MultiConnector;

/// <summary>
/// Acceptance tests for <see cref="PlanJsonHelpers"/> (SK 1.78 plan JSON materialization).
/// These tests don't require a live LLM or Oobabooga server — they exercise the JSON
/// parsing + ChatHistory/ResolvedInvocation materialization pipeline that
/// <see cref="MultiConnectorTests"/> consumes via
/// <see cref="PlanJsonHelpers.BuildChatHistoryFromPlanJsonAsync"/>.
///
/// Reference: docs/Plans/SK178-format.md + issue #7225 (tracker under #1210 Axe 3+).
/// </summary>
public sealed class PlanJsonHelpersTests : IDisposable
{
    private const string Sk178PlansDirectory = "../../../../../../samples/Plans/SK178/";

    private readonly string _sk178PlansDirectory = Path.Combine(Environment.CurrentDirectory, Sk178PlansDirectory);

    public void Dispose()
    {
        // No resources to release. The base class is IDisposable so xUnit
        // gets a per-test instance lifecycle which is what we want here.
    }

    [Fact]
    public async Task LoadPlan_Summarize_ResolvesExpectedShape()
    {
        var planPath = Path.Combine(this._sk178PlansDirectory, "Summarize.json");
        Assert.True(File.Exists(planPath), $"SK 1.78 plan fixture missing: {planPath}");

        var plan = await PlanJsonHelpers.LoadPlanAsync(planPath);

        Assert.Equal("Summarize plan", plan.Name);
        // The plan-level description is a free-form instruction string — verify it
        // begins with the canonical "You must evaluate..." prefix rather than the
        // invocation-level description (which is "Summarize given text...").
        Assert.StartsWith("You must evaluate", plan.Description);
        Assert.Equal("INPUT", plan.InputVariable);
        Assert.Equal("{{$INPUT}}", plan.UserInputTemplate);
        Assert.Single(plan.Invocations);

        // The single invocation carries the operational description that older
        // MultiConnectorTests used to surface via Plan.Description.
        var summarize = plan.Invocations[0];
        Assert.Equal("Summarize", summarize.Name);
        Assert.Equal("SummarizeSkill", summarize.PluginName);
        Assert.Equal("Summarize given text or any text document", summarize.Description);
        Assert.Equal("RESULT__SUMMARY", summarize.OutputVariable);
        Assert.Equal("$INPUT", summarize.Arguments["INPUT"]);
    }

    [Fact]
    public async Task LoadPlan_SummarizeTopicsElementAt_ResolvesChainedInvocations()
    {
        var planPath = Path.Combine(this._sk178PlansDirectory, "Summarize_Topics_ElementAt.json");
        Assert.True(File.Exists(planPath), $"SK 1.78 plan fixture missing: {planPath}");

        var plan = await PlanJsonHelpers.LoadPlanAsync(planPath);

        Assert.Equal("Summarize Topics Elements plan", plan.Name);
        Assert.Equal(3, plan.Invocations.Count);

        // Step 1: Summarize
        var summarize = plan.Invocations[0];
        Assert.Equal("Summarize", summarize.Name);
        Assert.Equal("SummarizeSkill", summarize.PluginName);
        Assert.Equal("RESULT__SUMMARY", summarize.OutputVariable);
        Assert.Equal("$INPUT", summarize.Arguments["INPUT"]);

        // Step 2: Topics — chained off the input
        var topics = plan.Invocations[1];
        Assert.Equal("Topics", topics.Name);
        Assert.Equal("SummarizeSkill", topics.PluginName);
        Assert.Equal("RESULT__TOPICS", topics.OutputVariable);
        Assert.Equal("$INPUT", topics.Arguments["input"]);

        // Step 3: ElementAtIndex — chained off RESULT__TOPICS
        var elementAt = plan.Invocations[2];
        Assert.Equal("ElementAtIndex", elementAt.Name);
        Assert.Equal("MiscSkill", elementAt.PluginName);
        Assert.Equal("RESULT__THIRD_TOPIC", elementAt.OutputVariable);
        Assert.Equal("$RESULT__TOPICS", elementAt.Arguments["INPUT"]);
        Assert.Equal("2", elementAt.Arguments["index"]);
        Assert.Equal("1", elementAt.Arguments["count"]);
    }

    [Fact]
    public async Task BuildChatHistoryFromPlan_EmitsSystemAndUserMessages()
    {
        var planPath = Path.Combine(this._sk178PlansDirectory, "Summarize.json");
        var input = "Hello, world. The quick brown fox jumps over the lazy dog.";

        var (history, _) = await PlanJsonHelpers.BuildChatHistoryFromPlanJsonAsync(planPath, input);

        Assert.Equal(2, history.Count);

        // The plan's description is emitted as a system message. For Summarize.json
        // the plan-level description begins with "You must evaluate" — verify that
        // the helper emits it verbatim (the invocation-level descriptions are
        // surfaced separately through ResolvedInvocation and don't appear here).
        var system = history[0];
        Assert.Equal(AuthorRole.System, system.Role);
        Assert.Contains("You must evaluate", system.Content);

        // The user_input_template expands $INPUT — for Summarize.json the
        // template is "{{$INPUT}}" so the user message equals the raw input.
        var user = history[1];
        Assert.Equal(AuthorRole.User, user.Role);
        Assert.Equal(input, user.Content);
    }

    [Fact]
    public async Task BuildInvocationPlan_ResolvesVariableSubstitutionForChainedPlan()
    {
        var planPath = Path.Combine(this._sk178PlansDirectory, "Summarize_Topics_ElementAt.json");
        var input = "The history of the Roman Empire is long and complex.";

        var (_, invocations) = await PlanJsonHelpers.BuildChatHistoryFromPlanJsonAsync(planPath, input);

        Assert.Equal(3, invocations.Count);

        // Step 1 references the user-supplied INPUT.
        Assert.Equal(input, invocations[0].Arguments["INPUT"]);
        Assert.Equal("SummarizeSkill-Summarize", invocations[0].FullyQualifiedName);

        // Step 2 also references the user-supplied INPUT.
        Assert.Equal(input, invocations[1].Arguments["input"]);

        // Step 3 references RESULT__TOPICS, which is a placeholder sentinel at
        // plan-resolution time (the actual value flows through at runtime via
        // the SK 1.78 Auto Function Calling loop). The helper still substitutes
        // it with a sentinel so the test can assert the binding shape.
        Assert.Equal("<RESULT__TOPICS>", invocations[2].Arguments["INPUT"]);
        Assert.Equal("MiscSkill-ElementAtIndex", invocations[2].FullyQualifiedName);
    }
}
