# multiroute — console front-end for Connectors.AI.MultiConnector

Console wrapper exposing the MultiConnector organ (Axis 6 of the #1210 epic)
to notebook consumers via `Process.Start`. The organ itself cannot load inside
.NET Interactive: its dependency closure (SK 1.78 + OpenAI SDK +
Logging.Abstractions) is NuGet-resolved, and the ALC isolation of a
locally-`#r`'d assembly cannot see it (`FileNotFoundException` on
Logging.Abstractions 10.0.0.0, measured with exact pins). A console app
resolves the whole closure in one ALC — this tool is the organ's invocable
surface.

## Routing contract

Organ semantics (`SimpleMatchPromptSettings`): a prompt is routed by its
**signature prefix** — the first routing-table entry whose
`Signature.PromptStart` is a prefix of the prompt wins. Three types:

| Signature | Route | Provider |
|---|---|---|
| `[simple] ` | gpt-4o-mini | OpenAI direct |
| `[analytique] ` | gemini-2.5-flash | OpenRouter (OpenAI-compatible endpoint) |
| `[raisonnement] ` | claude-sonnet-4.5 | OpenRouter (OpenAI-compatible endpoint) |

The weighted arbiter (`GetWeightedConnectorComparer(1, 1)`) orders each type's
preference chain by declared cost/duration; the main connector
(claude-sonnet-4.5) also serves unmatched prompts.

## Usage

```bash
export OPENAI_API_KEY=...      # OpenAI direct — environment only, never literals
export OPENROUTER_API_KEY=...  # OpenRouter — same rule

dotnet run --project tools/multiroute/src -- --demo              # 3 canonical prompts
dotnet run --project tools/multiroute/src -- --prompt "[simple] ..." --json
```

Output (human): `[type] route <name> <latency> ms | <preview>`.
Output (`--json`): one line per prompt — `{"type", "route", "latency_ms", "text"}`.

Exit codes: `0` success · `1` provider call failed (message on stderr) ·
`2` usage error (missing/unknown signature prefix, missing keys).

## Measured (2026-10-03, real API calls)

```
[simple      ] route simple         2799 ms | # Traduction — "At night, all cats are grey."
[analytique  ] route analytique     8758 ms | # Cache LRU vs LFU : Compromis coût/latence ...
[raisonnement] route raisonnement  14721 ms | # Le problème du fermier, du loup, de la chevre et du chou
```

Latency follows the requested capability — the routing table's declared
duration ordering (800 ms / 1500 ms / 6000 ms profiles) is respected by the
three real providers, with the reasoning route carrying the top-tier model.
