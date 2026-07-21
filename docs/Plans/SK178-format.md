# Plan JSON format (SK 1.78) — Samples/Plans/SK178/*.json

This document defines the intermediate plan JSON format consumed by `MultiConnectorTests.cs` after the SK 1.78 migration. The legacy `Plan.FromJson(...)` shape (state/steps/parameters/outputs/next_step_index) was REMOVED in Semantic Kernel 1.78 — the `Microsoft.SemanticKernel.Planners.*` NuGet packages were deleted and a `Plan` is now a `KernelFunction`.

To keep the cost-offload integration tests exercisable without depending on a planner, this repo ships a minimal JSON format that maps 1-to-1 to a `ChatHistory` consumable by `FunctionChoiceBehavior.Auto()` (the canonical replacement per the [official MS Learn migration guide](https://learn.microsoft.com/en-us/semantic-kernel/support/migration/stepwise-planner-migration-guide)).

## Why an intermediate format (not raw ChatHistory)?

`ChatHistory` is a runtime type with non-trivial serialization (author roles, metadata, content items). Shipping a hand-written `ChatHistory` JSON would be brittle and tightly coupled to SK internals. The intermediate format below captures the **authoring intent** of a plan (goal + sequence of function invocations) and lets `PlanJsonHelpers.BuildChatHistoryFromPlan` materialize it deterministically.

## Schema

```json
{
  "$comment": "...optional, ignored by the parser...",
  "name": "human-readable plan name",
  "description": "Goal statement; emitted as a system message in the ChatHistory.",
  "input_variable": "INPUT",
  "user_input_template": "{{$INPUT}}",
  "invocations": [
    {
      "plugin_name": "SummarizeSkill",
      "name": "Summarize",
      "description": "Summarize given text or any text document",
      "arguments": {
        "INPUT": "$INPUT"
      },
      "output_variable": "RESULT__SUMMARY"
    }
  ]
}
```

### Field reference

| Field | Type | Required | Purpose |
|-------|------|----------|---------|
| `name` | string | no | Plan display name (used in diagnostics only). |
| `description` | string | recommended | The goal statement; emitted as a `system` message in the ChatHistory so the LLM knows what the sequence is trying to achieve. |
| `input_variable` | string | no (defaults to `"INPUT"`) | The variable name used in `$VARIABLE` references to bind the input text. |
| `user_input_template` | string | no | Optional template for the initial `user` message. The placeholder `{{$INPUT}}` is expanded with the input text at runtime; if absent, the helper writes the raw input text as the user message. |
| `invocations` | array | yes (empty array is allowed for degenerate plans) | Ordered list of `KernelFunction` calls to issue via Auto Function Calling. See below. |

### Invocation fields

| Field | Type | Required | Purpose |
|-------|------|----------|---------|
| `plugin_name` | string | recommended | Plugin that owns the function (e.g. `"SummarizeSkill"`). Empty string means the function is registered on the bare `Kernel`. |
| `name` | string | yes | Function name (e.g. `"Summarize"`, `"Topics"`, `"ElementAtIndex"`). |
| `description` | string | recommended | Human description passed through to `FunctionCallContent` and exposed to the LLM. |
| `arguments` | object<string, string> | yes (can be empty) | Map of argument name to value. Values starting with `$` are interpreted as variable references; the helper substitutes them with the corresponding `output_variable` from a previous invocation (or the `input_variable`). Non-string values are serialized as raw JSON text so numerics survive. |
| `output_variable` | string | no | If set, the result of this invocation can be referenced by later invocations as `$<output_variable>`. The helper substitutes with a placeholder sentinel; the actual value is populated by the SK 1.78 Auto Function Calling loop at runtime. |

### Variable substitution

`$VARIABLE_NAME` is a reference to either:
- the initial input, when `VARIABLE_NAME == input_variable` (defaults to `"INPUT"`), or
- a prior invocation's `output_variable`.

Example chaining (from `Summarize_Topics_ElementAt.json`):

```json
{
  "invocations": [
    { "name": "Summarize",   "arguments": { "INPUT": "$INPUT" }, "output_variable": "RESULT__SUMMARY" },
    { "name": "Topics",      "arguments": { "input": "$INPUT" }, "output_variable": "RESULT__TOPICS" },
    { "name": "ElementAtIndex", "arguments": { "index": "2", "INPUT": "$RESULT__TOPICS", "count": "1" }, "output_variable": "RESULT__THIRD_TOPIC" }
  ]
}
```

## Materialization (BuildChatHistoryFromPlan)

The helper produces a `ChatHistory` containing:
1. **One system message** carrying the plan `description` (the goal).
2. **One user message** carrying the input text (via `user_input_template`, or raw if no template is given).
3. **One assistant message per invocation**, each declaring a `FunctionCallContent` so that the SK 1.78 chat completion service (in Auto Function Calling mode) knows which function to invoke and with which arguments.

Auto Function Calling then drives the actual function invocations and accumulates tool results naturally.

## Compatibility with the existing test scenarios

The two pre-existing scenario files are mirrored in this format as:

| Legacy file | SK 1.78 file |
|-------------|--------------|
| `Samples/Plans/Summarize.json` | `Samples/Plans/SK178/Summarize.json` |
| `Samples/Plans/Summarize_Topics_ElementAt.json` | `Samples/Plans/SK178/Summarize_Topics_ElementAt.json` |

The legacy files are kept on disk so that anyone debugging the migration can diff the two shapes side-by-side. They are **not** consumed by the tests anymore — once `MultiConnectorTests.cs` is rewritten (see Epic #6853, follow-up #7225), the legacy `Plan.FromJson` calls become `PlanJsonHelpers.BuildChatHistoryFromPlanJsonAsync(...)`.

## Source of truth

- Decision: MS Learn stepwise-planner-migration-guide (`FunctionChoiceBehavior.Auto`).
- Tracking: Epic #6853, sub-task #7225 (MultiConnectorTests rewrite).
- Implementation: `dotnet/src/IntegrationTests/Connectors/MultiConnector/PlanJsonHelpers.cs`.
