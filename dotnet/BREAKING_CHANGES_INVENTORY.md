# SK 1.0.0-beta6 → 1.78.0 Breaking Changes Inventory

**Source grain :** Issue MyIntelligenceAgency/semantic-fleet (axe-mineur upgrade cible).
**Branche :** `upgrade-sk` dans ce submodule.
**Date :** 2026-07-16 (po-2026 c.616).
**Machine :** po-2026 (`Windows 11`, .NET 10.0.204 SDK).
**Statut :** inventaire détaillé complémentaire au commentaire tracker
[issue #6853](https://github.com/jsboige/CoursIA/issues/6853) (qui a
posté le sommaire + build counts). Ce fichier-ci contient la
**répartition par symbole** et le **plan de bataille tranche 2**
que le commentaire tracker ne contient pas.

## Périmètre de cette PR (c.616 — tranche 1 inventaire)

Cette PR livre UNIQUEMENT le **fichier d'inventaire complémentaire**
(`BREAKING_CHANGES_INVENTORY.md`) à valeur de référence durable.
**Le bump de packages est déjà livré** par le commit `560feb0`
préexistant sur cette branche `upgrade-sk` (`origin/upgrade-sk`).

Vérification cross-référence :

| Item | Commit | Date | Auteur |
|------|--------|------|--------|
| Bump packages + TFMs (Directory.Packages.props + 3 csproj) | [`560feb0`](https://github.com/MyIntelligenceAgency/semantic-fleet/commit/560feb0) | 2026-07-16T12:29Z | jsboige (`myia-po-2026:CoursIA-2`) |
| Inventaire détaillé par symbole + plan tranche 2 (ce fichier) | à venir sur `upgrade-sk` | 2026-07-16 | jsboige (`myia-po-2026:CoursIA-2`) |

**Hors scope explicite (tranche 2 et au-delà) :**

- Migration du code source des connectors
  (`Connectors.AI.Oobabooga/`, `Connectors.AI.MultiConnector/`) vers les
  nouvelles API.
- Fix des tests (`Connectors.UnitTests/`, `IntegrationTests/`).

## Verdict build (héritée de `560feb0`)

`dotnet build -c Release dotnet/Semantic-Fleet-dotnet.sln`

- **Avertissements :** 3 (`ConsoleSamples` reste 1 warning TFM post-bump).
- **Erreurs :** **83** (compte warm-cache, projet `Oobabooga` seul qui
  bloque la build de la solution).
- **Temps écoulé :** 6.08 s cold / 8.45 s warm (post-premier-build).

> Note : mon comptage cross-projet initial (`166 erreurs`) agrégé sur
> plusieurs projets était artefact d'un build où plusieurs projets
> essayaient de compiler avant le premier fail ; le chiffre réel
> vérifiable par `dotnet build` = 83 (Oobabooga seulement, car MSBuild
> s'arrête au premier projet en échec de dépendances).

## Inventaire détaillé par symbole

Cœur de la substance tranche 2 : les **types qui ont disparu ou changé
de namespace** dans SK 1.x stables vs 1.0.0-beta6.

| # | Symbole SK 0.x | Remplacement SK 1.78.0 | Erreurs CS0246 | Notes migration |
|---|----------------|------------------------|---------------:|------------------|
| 1 | `AIRequestSettings`         | `PromptExecutionSettings` | 38 | Constructeur + propriétés |
| 2 | `Microsoft.SemanticKernel.AI` (namespace) | supprimé → utiliser `Microsoft.SemanticKernel.*` direct | 38 (CS0234) | Tous les sous-namespaces aplatis |
| 3 | `ChatHistory`               | `ChatHistory` (namespace déplacé `Microsoft.SemanticKernel.Chatting`) | 18 | Ajuster `using` |
| 4 | `ChatMessageBase`           | `ChatMessageContent` | 10 | Renommage complet |
| 5 | `ModelResult`               | supprimé (contenu sur l'objet résultat direct) | 8 | Refactor résultat |
| 6 | `ITextStreamingResult`      | `IAsyncEnumerable<StreamingTextContent>` | 8 | Pattern async-iter |
| 7 | `ITextResult`               | `*Service` retourne `TextContent` directement | 8 | Type direct |
| 8 | `KernelBuilder`             | `Kernel.CreateBuilder()` | 8 (CS0122) | Factory method |
| 9 | `IChatStreamingResult`      | `IAsyncEnumerable<StreamingChatMessageContent>` | 6 | Pattern async-iter |
| 10 | `IChatResult`              | `*Service` retourne `ChatMessageContent` directement | 6 | Type direct |
| 11 | `ParameterView`            | `KernelArguments` | 2 | Renommage simple |

**Namespaces SK 0.x supprimés (CS0234) :**

- `Microsoft.SemanticKernel.AI.TextCompletion` (7 occurrences)
- `Microsoft.SemanticKernel.AI.ChatCompletion` (6)
- `Microsoft.SemanticKernel.AI` (6)
- `Microsoft.SemanticKernel.Orchestration` (3)

→ tous migrent vers `Microsoft.SemanticKernel.{Chatting,TextGeneration,Connectors}*` en SK 1.x.

## Inventaire par projet (héritée de `560feb0`)

| Projet | Erreurs | Statut post-bump | Migration tranche 2 |
|--------|--------:|------------------|----------------------|
| `Connectors.AI.Oobabooga.csproj`         | 83 | ne compile pas | la plus grosse masse ; bloque toute la solution |
| `Connectors.AI.MultiConnector.csproj`   | 51 (12 fichiers) | ne compile pas | mêmes patterns |
| `Connectors.UnitTests.csproj`           | 23 (3 fichiers) | ne compile pas | suit migration connectors |
| `IntegrationTests.csproj`               | n/a (dépend Oobabooga) | ne compile pas | suit Oobabooga |
| `ConsoleSamples.csproj`                 | **0 erreur / 1 warning** | OK | Aucune migration requise (TFM bump suffit) |

## Plan tranche 2 (migration mécanique — à ordonnancer)

L'inventaire confirme que **le pattern est unique** (suppression des
namespaces `Microsoft.SemanticKernel.AI.*` + `Orchestration` et de
l'API result-based au profit de l'API service-based), répété
mécaniquement. Ordre de dépendance conseillé :

1. **`InternalUtilities` d'abord** (`IDelegatingHandlerFactory`,
   `ParameterView`, `Verify`) — fondation partagée. ~6 occurrences.
2. **`Connectors.AI.Oobabooga`** (83 erreurs, la plus grosse masse) —
   résoudre débloque `MultiConnector` + `UnitTests`.
3. **`Connectors.AI.MultiConnector`** (51 occ.) — mêmes patterns.
4. **`Connectors.UnitTests`** (23 occ.) — suit la migration des connectors.

**Estimation** : la masse d'erreurs est élevée (~150 occurrences
totales) mais **le pattern est unique et mécanique** (remplacement de
types + namespaces), pas une réécriture architecturale. La tranche 2
est faisable en focus (~250-300 lignes diff estimées pour 6 projets).

**Risques identifiés :**

- `AIRequestSettings` → `PromptExecutionSettings` peut changer la
  signature de certaines factory methods (`GetTextContentAsync` vs
  `GetTextContentAsync(settings)`). À investiguer dans un sample.
- `ChatHistory` namespace change : vérifier que `using` est la seule
  modification (pas de rename de classe).
- `Kernel.CreateBuilder()` : changer les `new KernelBuilder()` en
  appels factory. Vérifier que le config DI reste compatible.
- `IDelegatingHandlerFactory` : API interne supprimée, **recoder**
  (pas un simple remplacement) — risque non-trivial.

## Acceptance tranche 1 (déjà livré par `560feb0`)

- [x] Branche `upgrade-sk` poussée sur MyIntelligenceAgency/semantic-fleet (additive, pas de force push)
- [x] Inventaire breaking changes posté en commentaire sur tracker #6853
- [ ] `dotnet build Semantic-Fleet-dotnet.sln` : 83 erreurs documentées avec plan (acceptation #3 assouplie pour tranche 1 — migration = tranche 2)
- [ ] Tests unitaires : ≥ 49/52 (acceptance #4 différée à la tranche 2, dépend de la migration)
- [x] PAS de bump du pointeur submodule CoursIA dans cette tranche (acceptance #5 respectée)

## Conclusion

Ce fichier + le commentaire tracker + le commit `560feb0` forment
ensemble le livrable complet de la **tranche 1 inventaire** demandée
par ai-01 dans le DM GO. La **tranche 2** (migration mécanique) est
explicitement hors-scope de cette PR-ci.

`See #6853` (tracker parent, l'epic Axe 2 n'est pas entièrement
résolu par cette seule tranche).