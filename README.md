# Semantic-Fleet

[![Oobabooga Connector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.Oobabooga?label=nuget%20Oobabooga%20Connector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)
[![Multiconnector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.MultiConnector?label=nuget%20MultiConnector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.MultiConnector/)

## Vue d'ensemble

Semantic-Fleet étend [Semantic Kernel](https://github.com/microsoft/semantic-kernel) (référencé en **1.78.0**, gestion centralisée des packages dans `dotnet/Directory.Packages.props`) pour orchestrer une flotte de modèles de langage : des connecteurs pour les petits modèles auto-hébergés, et le MultiConnector, qui route chaque prompt vers le connecteur le plus adapté en coût et en performance.

## Historique du dépôt (mai 2025 — restauration)

En mai 2025, un incident a coûté à ce dépôt la parenté d'origine de ses commits de développement. La restauration (septembre 2026) croise plusieurs sources vérifiées :

- la branche `stable-from-v0343` — le tag v0.34.3 de 2023, la migration Semantic Kernel 1.78 et le module radix ;
- la branche `cleanup-orphaned-files` — les 17 commits pré-incident les plus complets, fusionnés en retour par la [PR #79](https://github.com/MyIntelligenceAgency/semantic-fleet/pull/79) ;
- le fork [MyIntelligenceAgency/semantic-kernel](https://github.com/MyIntelligenceAgency/semantic-kernel) (branches `feature/multiconnector`, `feature/oobabooga`, `AsynchronousStreaming`) et la PR Semantic Kernel [#2323](https://github.com/microsoft/semantic-kernel/pull/2323), qui portent la documentation la plus détaillée du MultiConnector ;
- les paquets NuGet 0.34.3 (dernière version publiée), qui embarquent leur documentation XML.

Vraiment perdu : la parenté d'origine des commits d'avant l'incident. Le contenu, lui, survit. Une partie du code C# restauré, écrite contre l'API pré-1.0 de Semantic Kernel, n'est **pas** réintégrée dans l'arbre compilé : elle reste intégrale sur `cleanup-orphaned-files` et constitue le matériau du chantier de migration SK 1.78 (`docs/Plans/SK178-format.md`, #7225/#7618/#7621).

## Composants

### MultiConnector

Le composant central : intégration de plusieurs LLM avec routage intelligent — signature du prompt, niveau de validation (vetting) par type de prompt, coût et durée. Il décharge les tâches d'un connecteur principal vers des connecteurs secondaires plus économiques, sans sacrifier la fiabilité.

📖 **Documentation restaurée** :

- [Guide du MultiConnector](./dotnet/src/Connectors/Connectors.AI.MultiConnector/README.md)
- [Cartographie des fonctionnalités](./docs/MultiConnector_Cartographie.md)
- [Optimisations](./docs/MultiConnector_Optimizations.md)
- [Intégration des petits modèles](./docs/SMALL_MODELS_INTEGRATION.md)
- [Configuration des modèles](./docs/MODEL_CONFIG.md)
- [Système de détection de signatures des prompts](./docs/systeme_detection_signatures_prompts.md)
- [Tests d'intégration](./dotnet/src/IntegrationTests/Connectors/MultiConnector/README.md)

Le cœur compile proprement sous SK 1.78 ; l'oracle de test déterministe est en cours (#72/#73).

### PromptMatcher radix

Module autonome [`tools/radix`](./tools/radix/README.md) : matching de signatures de prompts par arbre radix hybride, sans dépendance à l'assemblage MultiConnector, avec sa propre suite de tests. C'est l'implémentation de référence du pattern-matching des prompts.

### Connecteur Oobabooga — statut legacy

Le connecteur couvre les API de complétion et de chat d'Oobabooga (modes bloquant et streaming), **telles qu'elles existaient avant le commit `454fcf3` du 13/11/2023** de text-generation-webui, qui a remplacé l'API traditionnelle par une API modelée sur celle d'OpenAI. Les versions d'Oobabooga postérieures à ce commit ne sont **pas supportées** ; le connecteur n'est pas maintenu activement. La voie de modernisation prévue est un client générique compatible OpenAI, dans la continuité du re-basing sur `IChatClient`.

📖 [Installation d'Oobabooga et scripts Multi-Start](./docs/OOBABOOGA.md) · [Guide du connecteur](./dotnet/src/Connectors/Connectors.AI.Oobabooga/README.md)

## Notebooks

Les notebooks .NET Interactive restent le meilleur aperçu des connecteurs :
[guide des notebooks](./dotnet/notebooks/README.md).

## Tests et évaluation

- [Tests d'intégration](./dotnet/src/IntegrationTests/) — état courant : 4 réussis, 7 ignorés (besoin d'instances locales), 0 échec
- [Tests unitaires](./dotnet/src/Connectors/Connectors.UnitTests/) — l'oracle déterministe (#72/#73) remplace progressivement les tests dépendants du minutage
- [Comparatif de modèles](./model_tester/README.md) — scripts OpenAI/OpenRouter pilotés par variables d'environnement
- [Campagne de tests](./campaign_tests/README.md) — outils et résultats de campagnes complètes

## Développement

```bash
dotnet build semantic-fleet.sln
dotnet test dotnet/src/IntegrationTests/IntegrationTests.csproj
dotnet test dotnet/src/Connectors/Connectors.UnitTests/Connectors.UnitTests.csproj
```

Les credentials passent exclusivement par des variables d'environnement (voir `.env.example`) — jamais de littéral dans le code ni la documentation.

## Packages NuGet

- [MyIA.SemanticKernel.Connectors.AI.Oobabooga](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)
- [MyIA.SemanticKernel.Connectors.AI.MultiConnector](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.MultiConnector/)

Dernière version publiée : 0.34.3.

## Orientations futures

- **Oracle de test déterministe** (#72/#73) — supprimer la dépendance au minutage des tests MultiConnector
- **Migration SK 1.78 du code pré-1.0 restauré** (#7225/#7618/#7621)
- **Re-basing sur `IChatClient`** (`Microsoft.Extensions.AI`) puis Agent Framework : le MultiConnector comme middleware `DelegatingChatClient`, Oobabooga en client générique compatible OpenAI, façade SK mince conservée

## Contribuer

Consultez nos [directives de contribution](./CONTRIBUTING.md). Vous voulez voir une fonctionnalité atterrir ? [Contactez-nous](https://github.com/MyIntelligenceAgency).
