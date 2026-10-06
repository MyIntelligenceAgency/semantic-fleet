# Semantic-Fleet 🚀

[![Oobabooga Connector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.Oobabooga?label=nuget%20Oobabooga%20Connector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)
[![Multiconnector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.MultiConnector?label=nuget%20MultiConnector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.MultiConnector/)

## Vue d'ensemble

Semantic-Fleet est un dépôt conçu pour étendre les capacités de [Semantic Kernel](https://github.com/microsoft/semantic-kernel). Il se concentre sur la fourniture de connecteurs pour les petits modèles de langage (par exemple, Llamas) et d'outils pour distribuer le travail à une flotte de modèles, avec ChatGPT servant de capitaine de la flotte. Ce dépôt est plus qu'une simple collection de connecteurs existants ; c'est une plateforme pour les innovations futures dans l'écosystème .NET pour l'IA.

**État actuel.** La version en préparation (0.35.0) cible Semantic Kernel 1.78. Les paquets ciblent `netstandard2.0` ; la CI les construit et les teste avec les SDK .NET 8, 9 et 10. La dernière version publiée sur NuGet est la 0.34.3 (2023), écrite pour une version bêta de Semantic Kernel : les exemples ci-dessous correspondent à la 0.35.0.

### 🚨 Important : le connecteur Oobabooga cible l'ancienne API

Le 13/11/2023, text-generation-webui (Oobabooga) a remplacé son API historique par une API modelée sur celle d'OpenAI (voir le [commit 454fcf3](https://github.com/oobabooga/text-generation-webui/commit/454fcf39a95691f5e375c48fbc6fe6aa96f0c738)). **Le connecteur Oobabooga de ce dépôt ne parle que l'ancienne API** : il fonctionne avec les versions de text-generation-webui antérieures à ce commit, pas avec les suivantes.

Pour une version récente de text-generation-webui, ou tout autre serveur compatible OpenAI (vLLM, llama.cpp, etc.), utilisez le connecteur OpenAI de Semantic Kernel en le pointant sur le serveur local. Le MultiConnector accepte n'importe quel `ITextGenerationService` : un tel serveur peut y servir de connecteur secondaire.

## Composants principaux

### 🤖 Connecteur Oobabooga

Un connecteur robuste qui couvre actuellement les principales API de complétion et de chat spécifiques à Oobabooga, en mode bloquant et streaming.

📖 **En savoir plus** : 
- [Installation d'Oobabooga et configuration des scripts Multi-Start](./docs/OOBABOOGA.md)
- [Guide du connecteur Oobabooga](./dotnet/src/Connectors/Connectors.AI.Oobabooga/README.md)
- N'oubliez pas de consulter les [notebooks](./dotnet/notebooks/README.md). Ils fournissent un excellent aperçu de ce qui est possible avec nos connecteurs publiés.

#### Installation

Installez le package via NuGet :

```bash
dotnet add package MyIA.SemanticKernel.Connectors.AI.Oobabooga
```

Dans .Net interactive :

```csharp
#r "nuget: MyIA.SemanticKernel.Connectors.AI.Oobabooga"
```

#### Démarrage rapide

Des paramètres différents sont utilisés pour la complétion de texte et de chat, à la fois en mode bloquant et en streaming. Voici un exemple rapide pour la complétion de texte :

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

var settings = new OobaboogaTextCompletionSettings(endpoint: new Uri("http://localhost/"), blockingPort: 5000, streamingPort: 5005);
var oobabooga = new OobaboogaTextCompletion(settings);

// Complétion directe
var completion = await oobabooga.GetTextContentAsync("Hello, world!", new OobaboogaCompletionRequestSettings());
Console.WriteLine(completion.Text);

// Ou via un Kernel
var kernel = Kernel.CreateBuilder()
    .AddOobaboogaTextGeneration(settings, setAsDefault: true)
    .Build();
```

`OobaboogaChatCompletion` et `AddOobaboogaChatCompletion` fournissent l'équivalent pour le chat (`IChatCompletionService`).

### 🌐 MultiConnector
 
Pourquoi se limiter à un seul modèle quand on peut en avoir plusieurs ? MultiConnector vous permet d'intégrer plusieurs LLMs de manière transparente, en optimisant la vitesse et le coût. Il décharge intelligemment les tâches d'un connecteur principal, plus coûteux, vers un connecteur secondaire, plus économique, sans sacrifier la fiabilité ni les performances.

📖 **En savoir plus** : 
- [Guide du MultiConnector](./dotnet/src/Connectors/Connectors.AI.MultiConnector/README.md)
- [Cartographie des fonctionnalités](./docs/MultiConnector_Cartographie.md)
- [Optimisations récentes](./docs/MultiConnector_Optimizations.md)
- [Guide d'intégration des petits modèles](./docs/SMALL_MODELS_INTEGRATION.md)
- [Tests d'intégration](./dotnet/src/IntegrationTests/Connectors/MultiConnector/README.md)

#### Documentation des composants du MultiConnector

Le MultiConnector est composé de plusieurs sous-systèmes, chacun documenté en détail :

- [Système d'analyse](./dotnet/src/Connectors/Connectors.AI.MultiConnector/Analysis/README.md) - Évaluation automatique des performances des modèles
- [Système de gestion des prompts](./dotnet/src/Connectors/Connectors.AI.MultiConnector/PromptSettings/README.md) - Transformation et adaptation des prompts
- [Système de détection de signatures des prompts](./docs/systeme_detection_signatures_prompts.md) - Identification efficace des patterns dans les prompts ; des matchers optionnels (`UseRadixTreePromptMatcher`, dossier `PromptMatching/`) indexent les débuts de prompts dans un arbre radix ; le module autonome [`tools/radix`](./tools/radix) en est une seconde implémentation
- [Mocks arithmétiques](./dotnet/src/Connectors/Connectors.AI.MultiConnector/ArithmeticMocks/README.md) - Simulations pour les tests
- [Configuration](./dotnet/src/Connectors/Connectors.AI.MultiConnector/Configuration/README.md) - Gestion des paramètres des connecteurs

#### Installation

Installez le package via NuGet :

```bash
dotnet add package MyIA.SemanticKernel.Connectors.AI.MultiConnector
```

Dans .Net interactive :

```csharp
#r "nuget: MyIA.SemanticKernel.Connectors.AI.MultiConnector"
```

#### Démarrage rapide

Le MultiConnector dispose de nombreux paramètres contrôlant la façon de router les appels de complétion de texte, et comment échantillonner automatiquement les complétions d'un connecteur principal, tester, évaluer et mettre à jour les paramètres de routage pour utiliser des connecteurs secondaires.

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion.TextCompletion;

using var cleanupToken = new CancellationTokenSource();
var settings = new MultiTextCompletionSettings();

// Connecteur principal : un modèle OpenAI ; connecteurs secondaires : des modèles locaux
var primary = new NamedTextCompletion("gpt-4o-mini", new OpenAIChatCompletionService("gpt-4o-mini", apiKey));
var secondaries = new[]
{
    new NamedTextCompletion("local-llama", new OobaboogaTextCompletion(oobaboogaSettings)),
};

var kernel = Kernel.CreateBuilder()
    .WithMultiConnectorCompletionService(
        settings: settings,
        mainTextCompletion: primary,
        analysisTaskCancellationToken: cleanupToken.Token,
        setAsDefault: true,
        otherCompletions: secondaries)
    .Build();

// Les premiers appels passent par le connecteur principal. L'analyse, automatique ou déclenchée
// à la main selon les paramètres, teste et évalue les secondaires, puis met à jour le routage :
// les appels suivants partent vers un secondaire quand il a été validé pour ce type de prompt.
var result = await kernel.InvokePromptAsync("Combien font 2 + 3 ?", cancellationToken: cleanupToken.Token);
```

Pour un aperçu détaillé de la façon de combler les lacunes, veuillez vous référer aux notebooks et aux tests d'intégration.

## 📚 Notebooks

Vous voulez un aperçu de ce qui est possible avec nos connecteurs publiés ? 
Nos notebooks .Net interactive sont un excellent point de départ.

📖 **En savoir plus** : [Guide des notebooks](./dotnet/notebooks/README.md)

## 🧪 Tests et évaluation

Le projet comprend plusieurs outils pour tester et évaluer les performances des modèles :

- [Tests comparatifs des modèles](./model_tester/README.md) - Scripts pour comparer les performances des différents modèles
- [Campagne de tests avancés](./campaign_tests/README.md) - Outils pour exécuter des campagnes de tests complètes

## Développement

```bash
dotnet build dotnet/Semantic-Fleet-dotnet.sln
dotnet test dotnet/src/Connectors/Connectors.UnitTests/Connectors.UnitTests.csproj
dotnet test dotnet/src/IntegrationTests/IntegrationTests.csproj
```

Les tests d'intégration appellent de vrais services (instances Oobabooga locales, API distantes) : ceux dont le service ou la clé manque sont ignorés. Les clés passent exclusivement par des variables d'environnement (voir `.env.example`), jamais par un littéral dans le code ou la documentation.

## Orientations futures

- **API Open AI** : Oobabooga offre une extension dédiée imitant l'API Open AI. Elle étend le support aux modèles d'embeddings et de génération d'images. Cela sera disponible en tant que package séparé.
- **MultiConnector probabiliste** : Nous ajouterons de la magie Infer.Net pour rendre MultiConnector encore plus intelligent. Plus précisément, les exemples suivants seront fusionnés et intégrés dans le processus de validation des modèles.
   - [Student Skills](https://dotnet.github.io/infer/userguide/Student%20skills.html)
   - [Assessing People's Skills](https://mbmlbook.com/LearningSkills.html)
   - [Difficulty vs Ability](https://dotnet.github.io/infer/userguide/Difficulty%20versus%20ability.html)
   - [Calibrating reviews](https://dotnet.github.io/infer/userguide/Calibrating%20reviews%20of%20conference%20submissions.html)  
- **Intégration Spark.Net** : Préparez-vous à héberger un cluster de mini LLMs locaux.
- **Microsoft Agent Framework** : Microsoft fusionne Semantic Kernel et AutoGen dans Agent Framework, bâti sur les abstractions `IChatClient` de Microsoft.Extensions.AI, que Semantic Kernel 1.x prend déjà en charge. La piste étudiée est de reconstruire le cœur du MultiConnector sur `IChatClient` (un client délégant qui route et valide), en gardant une façade Semantic Kernel pour les utilisateurs actuels.

## Packages NuGet 

Nous fournissons des packages NuGet pour le connecteur Oobabooga et le MultiConnector pour une intégration plus facile dans vos projets.

Voici le [package Nuget pour le connecteur Oobabooga](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)

Et voici le [package Nuget pour le Multiconnector](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Multiconnector/)

## Historique du dépôt

En mai 2025, une opération de nettoyage menée par un agent a endommagé ce dépôt. La branche `main` a alors été reconstituée à partir des traces de session et du paquet source de la version précédente, sans ancêtre commun avec l'historique d'origine. L'historique d'origine n'a pas été perdu : le tag `v0.34.3`, la branche `stable-from-v0343` (migration vers Semantic Kernel 1.78) et la branche `cleanup-orphaned-files` (travaux de mai 2025) conservent leurs commits. La version 0.35.0 réunit ces lignées ; l'ancienne `main` reste archivée.

## 🤝 Contribuer

Vous avez quelque chose à ajouter ? Nous serions ravis de le voir. Consultez nos [directives de contribution](./CONTRIBUTING.md).

Vous avez quelque chose que vous aimeriez voir ajouté ? Vous voulez déjà ces fonctionnalités futures ? Nous serions ravis que vous [nous contactiez](https://github.com/MyIntelligenceAgency) !
