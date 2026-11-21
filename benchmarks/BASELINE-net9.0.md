# Baseline .NET 9 — bancs BenchmarkDotNet SemanticFleet

> **Régénérée le 2026-10-29 sous l'instrument corrigé** (PR #108 : invocation count auto + `IterationTime(100 ms)`, `UnrollFactor(1)` conservé, + 4 variantes « per-op instrument » `OperationsPerInvoke` sur arguments tournants consommés). Les valeurs de la baseline initiale (2026-07, mesurées sous l'ancien pin `WithInvocationCount(1)`) sont **supersédées** — elles restent dans l'historique git. La campagne N-scaling sur jsboige/CoursIA#18695 avait établi que le pin sur-estimait le coût/op d'un facteur ~8-19 (harnais ShortRun dominant) et produisait une dispersion intra-version de 16-40 %.

Baseline de référence pour la mesure des gains runtime .NET 10/11 (issue CoursIA #18770, fille de l'EPIC #18695). Banc : `benchmarks/SemanticFleet.Benchmarks` (BenchmarkDotNet 0.15.8), exerçant l'**API publique** du connecteur MultiConnector sur les hot paths identifiés par le digest .NET 11.

## Environnement de mesure

| Élément | Valeur |
| --- | --- |
| Commit mesuré | `d53f533` (pointe de `stable-from-v0343`, inclut le correctif d'instrument #108) |
| Runtime | .NET 9.0.22 (9.0.2226.52218), X64 RyuJIT x86-64-v3 |
| SDK | 10.0.114 |
| BenchmarkDotNet | 0.15.8, InProcessEmitToolchain, Job.ShortRun 5 warmup / 15 itérations, `IterationTime(100 ms)`, `UnrollFactor(1)`, invocation count auto |
| Machine | Windows 11 Pro 10.0.26300.9711, i7-12700H (poste de travail, GC client) |
| Durée de run | 23,93 s pour les 8 bancs |

## Résultats (net9.0, lib netstandard2.0 consommée depuis l'exécutable net9.0)

Les 4 bancs originaux (1 opération par appel) et leurs 4 variantes per-op (`OperationsPerInvoke` sur arguments tournants consommés — le coût/op de référence est celui des lignes per-op, stable de N=1 à N=10 000) :

| Benchmark | Mean | Error (99,9 % CI) | Allocated |
| --- | ---: | ---: | ---: |
| `PromptSignature.Matches` (chemin StartsWith, par appel de completion) | 24,27 ns | 3,31 ns (13,6 %) | 80 B |
| `JsonSerializer.Serialize(PromptMultiConnectorSettings)` (écriture de persistance) | 4,02 µs | 260 ns (6,5 %) | 6,99 KB |
| `FromRequestSettings` (round-trip Serialize + Deserialize, par appel) | 6,61 µs | 403 ns (6,1 %) | 6,91 KB |
| `PromptTransform.InterpolateKeys` (remplacement regex `{key}`) | 1,62 µs | 348 ns (21,5 %) | 9,54 KB |
| `Matches` ×10 000 (per-op instrument, payload tournant) | 22,97 ns | 2,07 ns (9,0 %) | 80 B |
| `Serialize` ×1 000 (per-op instrument) | 3,90 µs | 95,5 ns (2,5 %) | 6,99 KB |
| `FromRequestSettings` ×1 000 (per-op instrument, payload tournant) | 6,70 µs | 359 ns (5,4 %) | 7,01 KB |
| `InterpolateKeys` ×10 000 (per-op instrument, contexte tournant) | 1,93 µs | 431 ns (22,3 %) | 9,73 KB |

Charges « escape-heavy » : texte français avec guillemets, backslashes, tabulations, sauts de ligne et accents — l'encodeur JavaScript par défaut de System.Text.Json échappe le non-ASCII, ce qui correspond au scénario du digest (writer ×3,9 attendu sur payload escape-heavy).

## Facteurs de sur-estimation de l'ancienne baseline (mesurés)

| Hot path | Ancien (pin, 2026-07) | Nouveau (instrument corrigé) | Facteur |
| --- | ---: | ---: | ---: |
| `Matches` | 3,50 µs | 22,97-24,27 ns | ×144-×152 |
| `Serialize` | 90,58 µs | 3,90-4,02 µs | ×22-×23 |
| `FromRequestSettings` | 95,43 µs | 6,61-6,70 µs | ×14-×15 |
| `InterpolateKeys` | 21,29 µs | 1,62-1,93 µs | ×11-×13 |

Les `Allocated` de l'ancienne baseline l'étaient aussi (2,72 KB/op pour `Matches` contre 80 B réels) : la normalisation par une itération de 3,6-90 µs faisait porter le bruit GC du harnais sur l'opération.

## Lecture des hot paths

- **Sérialisation JSON des DTO** (les deux chemins à ~4-6,7 µs/op) : c'est la cible première des gains runtime .NET 10/11 côté `System.Text.Json` writer. Le round-trip `FromRequestSettings` paie la désérialisation en miroir (`AIRequestSettings`/`PromptExecutionSettings` via `ExtensionData` en dictionnaire, donc boxing `JsonElement`).
- **`InterpolateKeys`** : regex compilée + `string.Format` par token ; ~9,5-9,7 KB allouées par appel sur un template de ~200 caractères — la marge runtime est surtout ici une question d'allocations (réduites par les interpolations de chaînes natives et les spans).
- **`Matches`** : chemin le moins coûteux (~23-24 ns/op, 80 B alloués) mais appelé à chaque completion.

## Contexte de fidélité pin/pointe

Mesuré à la pointe `d53f533` de `stable-from-v0343` (lancé depuis un worktree propre de ce commit). Le bump du gitlink du submodule CoursIA suit le merge du présent PR, porté par le coordinateur.

## Protocole de reproduction

```bash
dotnet run -c Release --path benchmarks/SemanticFleet.Benchmarks --filter "*"
```

`--filter "*"` est **obligatoire** avec BDN 0.15 : sans lui, le banc entre en sélection interactive et s'arrête sans exécuter (mesuré le 2026-10-29). La config `InProcessShortConfig` (in-process, 5 warmup / 15 itérations, `IterationTime(100 ms)`) est attachée par attribut — elle évite le blocage du processus enfant par Defender/antivirus sur Windows (exit -2147450730) tout en maintenant l'erreur relative dans la fourchette 2,5-22 %.

## Limites

- Poste de travail unique (pas de multi-machine) ; ordres de grandeur et ratios allocation/temps, pas des valeurs absolues de référence hardware.
- Avertissements `MinIterationTime` résiduels sur les 8 bancs (itérations minimales observées 57-97 ms, juste sous le plancher de 100 ms) et `MultimodalDistribution` sur `FromRequestSettings` : bimodalité GC (mode rapide vs mode GC) inhérente à la sérialisation escape-heavy sur un poste GC client — documentée dans le body de #108. Ce n'est pas un sous-remplissage : l'ancien pin remplissait à 3,6-90 µs, l'instrument corrigé à 57-97 ms.
- Les charges sont des constantes déterministes (pas de graine aléatoire nécessaire) ; `VettingLevel.OracleVaried`, 5 instances de prompt, 4 clés d'interpolation.
