# Baseline .NET 9 — bancs BenchmarkDotNet SemanticFleet

Baseline de référence pour la mesure des gains runtime .NET 10/11 (issue CoursIA #18770, fille de l'EPIC #18695). Banc : `benchmarks/SemanticFleet.Benchmarks` (BenchmarkDotNet 0.14.0), exerçant l'**API publique** du connecteur MultiConnector sur les hot paths identifiés par le digest .NET 11.

## Environnement de mesure

| Élément | Valeur |
| --- | --- |
| Commit mesuré | `9df3603` (pointe de `stable-from-v0343`, ligne suivie par le submodule CoursIA) |
| Runtime | .NET 9.0.20 (9.0.2026.41315), X64 RyuJIT AVX2 |
| SDK | 10.0.204 |
| BenchmarkDotNet | 0.14.0, InProcessEmitToolchain, Job.ShortRun 5 warmup / 15 itérations |
| Machine | Windows 11 Pro 10.0.26300 (poste de travail, GC client) |

## Résultats (net9.0, lib netstandard2.0 consommée depuis l'exécutable net9.0)

| Benchmark | Mean | Error | Allocated |
| --- | ---: | ---: | ---: |
| `PromptSignature.Matches` (chemin StartsWith, par appel de completion) | 3,500 us | 0,834 us (23,8 %) | 2,72 KB |
| `JsonSerializer.Serialize(PromptMultiConnectorSettings)` (écriture de persistance) | 90,577 us | 11,996 us (13,2 %) | 10,62 KB |
| `FromRequestSettings` (round-trip Serialize + Deserialize, par appel) | 95,433 us | 11,277 us (11,8 %) | 10,54 KB |
| `PromptTransform.InterpolateKeys` (remplacement regex `{key}`) | 21,287 us | 4,721 us (22,2 %) | 13,49 KB |

Charges « escape-heavy » : texte français avec guillemets, backslashes, tabulations, sauts de ligne et accents — l'encodeur JavaScript par défaut de System.Text.Json échappe le non-ASCII, ce qui correspond au scénario du digest (writer ×3,9 attendu sur payload escape-heavy).

## Lecture des hot paths

- **Sérialisation JSON des DTO** (les deux chemins à ~90 us) : c'est la cible première des gains runtime .NET 10/11 côté `System.Text.Json` writer. Le round-trip `FromRequestSettings` paie la désérialisation en miroir (`AIRequestSettings`/`PromptExecutionSettings` via `ExtensionData` en dictionnaire, donc boxing `JsonElement`).
- **`InterpolateKeys`** : regex compilée + `string.Format` par token ; 13,5 KB allouées par appel sur un template de ~200 caractères — la marge runtime est surtout ici une question d'allocations (réduites par les interpolations de chaînes natives et les spans).
- **`Matches`** : chemin le moins coûteux (~3,5 us) mais appelé à chaque completion ; les 2,72 KB proviennent de l'accès `ExtensionData` (boxing) dans `MatchSettings`.

## Contexte de fidélité pin/pointe

Le submodule CoursIA pointe `cac5abc` (14/07), 18 commits derrière la pointe `9df3603` de sa branche de suivi `stable-from-v0343`. Ces 18 commits incluent le port Semantic Kernel 1.78 qui déplace `AIRequestSettings` → `PromptExecutionSettings` : le banc suit donc l'API de la pointe (là où la PR merge), et la mesure est reproductible au commit `9df3603` exact. Une mesure au pin serait un état d'API supersédé — au prochain bump du submodule, la pointe rejoint le pin et la baseline s'applique telle quelle.

## Protocole de reproduction

```bash
dotnet build benchmarks/SemanticFleet.Benchmarks -c Release
cd benchmarks/SemanticFleet.Benchmarks/bin/Release/net9.0
./SemanticFleet.Benchmarks.exe -f "*"
```

La config `InProcessShortConfig` (in-process, 5 warmup / 15 itérations) est attachée par attribut — elle évite le blocage du processus enfant par Defender/antivirus sur Windows (exit -2147450730) tout en maintenant l'erreur relative dans la fourchette 12-24 %.

## Limites

- Poste de travail unique (pas de multi-machine) ; ordres de grandeur et ratios allocation/temps, pas des valeurs absolues de référence hardware.
- `MinIterationTime` : `Matches` observe un minimum d'itération à 3,6 us (très court) — l'erreur relative 23,8 % en découle ; itérations plus nombreuses si ce point devient discriminant.
- Les charges sont des constantes déterministes (pas de graine aléatoire nécessaire) ; `VettingLevel.OracleVaried`, 5 instances de prompt, 4 clés d'interpolation.
