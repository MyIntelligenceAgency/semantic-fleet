# Semantic-Fleet 🚀

[![Oobabooga Connector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.Oobabooga?label=nuget%20Oobabooga%20Connector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)
[![Multiconnector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.MultiConnector?label=nuget%20MultiConnector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.MultiConnector/)

## Vue d'ensemble

Semantic-Fleet est un dépôt conçu pour étendre les capacités de [Semantic Kernel](https://github.com/microsoft/semantic-kernel). Il se concentre sur la fourniture de connecteurs pour les petits modèles de langage (par exemple, Llamas) et d'outils pour distribuer le travail à une flotte de modèles, avec ChatGPT servant de capitaine de la flotte. Ce dépôt est plus qu'une simple collection de connecteurs existants ; c'est une plateforme pour les innovations futures dans l'écosystème .NET pour l'IA.

### 🚨 Important : Changement de compatibilité avec Oobabooga

Nous souhaitons informer nos utilisateurs qu'en raison de récents changements dans l'API Oobabooga (voir [commit 454fcf3 du 13/11/2023](https://github.com/oobabooga/text-generation-webui/commit/454fcf39a95691f5e375c48fbc6fe6aa96f0c738)), **toutes les versions d'Oobabooga au-delà de ce commit ne seront plus prises en charge par `semantic-fleet`**.

Le concepteur d'Oobabooga a remplacé l'API traditionnelle par une nouvelle API modelée sur celle d'OpenAI. Malheureusement, nous n'avons pas encore eu l'occasion de mettre à jour notre pont pour être compatible avec ces changements.

Nous travaillons activement pour assurer la compatibilité dans les futures versions, mais pour l'instant, nous recommandons à nos utilisateurs de :

1. **Éviter de mettre à jour Oobabooga au-delà du commit spécifié** si vous souhaitez continuer à utiliser `semantic-fleet` sans interruption.
2. Restez à l'écoute de nos futures mises à jour pour le support de la nouvelle API Oobabooga.

Nous apprécions votre compréhension et votre patience pendant que nous travaillons sur ces changements.

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
var settings = new OobaboogaTextCompletionSettings(endpoint: new Uri("http://localhost/"),  blockingPort: 5000, streamingPort: 5005);
var oobabooga = new OobaboogaTextCompletion(settings);

// Obtenir des complétions de texte
var completions = await oobabooga.GetCompletionsAsync("Hello, world!", new OobaboogaCompletionRequestSettings());
```

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
var settings = new MultiTextCompletionSettings();

// (...) Création d'un openAiNamedCompletion principal et de oobaboogaCompletions secondaires

var builder = Microsoft.SemanticKernel.Kernel.Builder;

builder.WithMultiConnectorCompletionService(
    serviceId: null,
    settings: settings,
    mainTextCompletion: openAiNamedCompletion,
    setAsDefault: true,
    analysisTaskCancellationToken: cleanupToken.Token,
    otherCompletions: oobaboogaCompletions.ToArray());

var kernel = builder.Build();

// Obtenir une complétion de texte du connecteur principal d'abord
var result = await kernel.RunAsync(semanticFunctionOrPlan, contextVariables, cancellationToken: cleanupToken.Token).ConfigureAwait(false);

// (...) Effectuer une analyse manuellement ou automatiquement selon les paramètres

// Obtenir une complétion de texte des connecteurs secondaires
var optimizedResult = await kernel.RunAsync(semanticFunctionOrPlan, contextVariables, cancellationToken: cleanupToken.Token).ConfigureAwait(false);
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

## Orientations futures

- **API Open AI** : Oobabooga offre une extension dédiée imitant l'API Open AI. Elle étend le support aux modèles d'embeddings et de génération d'images. Cela sera disponible en tant que package séparé.
- **MultiConnector probabiliste** : Nous ajouterons de la magie Infer.Net pour rendre MultiConnector encore plus intelligent. Plus précisément, les exemples suivants seront fusionnés et intégrés dans le processus de validation des modèles.
   - [Student Skills](https://dotnet.github.io/infer/userguide/Student%20skills.html)
   - [Assessing People's Skills](https://mbmlbook.com/LearningSkills.html)
   - [Difficulty vs Ability](https://dotnet.github.io/infer/userguide/Difficulty%20versus%20ability.html)
   - [Calibrating reviews](https://dotnet.github.io/infer/userguide/Calibrating%20reviews%20of%20conference%20submissions.html)  
- **Intégration Spark.Net** : Préparez-vous à héberger un cluster de mini LLMs locaux.

## Packages NuGet 

Nous fournissons des packages NuGet pour le connecteur Oobabooga et le MultiConnector pour une intégration plus facile dans vos projets.

Voici le [package Nuget pour le connecteur Oobabooga](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)

Et voici le [package Nuget pour le Multiconnector](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Multiconnector/)

## 🤝 Contribuer

Vous avez quelque chose à ajouter ? Nous serions ravis de le voir. Consultez nos [directives de contribution](./CONTRIBUTING.md).

Vous avez quelque chose que vous aimeriez voir ajouté ? Vous voulez déjà ces fonctionnalités futures ? Nous serions ravis que vous [nous contactiez](https://github.com/MyIntelligenceAgency) !
