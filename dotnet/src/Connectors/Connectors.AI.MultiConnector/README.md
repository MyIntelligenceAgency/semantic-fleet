# MultiConnector pour Semantic Kernel

## Vue d'ensemble

Le MultiConnector est un composant avancé pour Semantic Kernel qui permet d'utiliser plusieurs modèles de langage (LLMs) de manière harmonisée. Il offre des fonctionnalités sophistiquées pour router intelligemment les requêtes vers les modèles les plus appropriés en fonction du type de prompt, de la complexité de la tâche et d'autres critères.

## Fonctionnalités principales

### 1. Routage intelligent des prompts

Le MultiConnector peut router les prompts vers différents modèles en fonction de plusieurs critères :

- **Signature du prompt** : Identification automatique du type de prompt basée sur son début
- **Niveau de validation (Vetting Level)** : Niveau de validation d'un modèle pour un type de prompt spécifique
- **Comparaison de performances** : Sélection basée sur le coût et la durée d'exécution

### 2. Transformation de prompts

Le système permet de transformer les prompts pour les adapter aux différents modèles :

- **Transformations globales** : Appliquées à tous les prompts
- **Transformations spécifiques au type de prompt** : Adaptées à chaque type de prompt
- **Transformations spécifiques au connecteur** : Adaptées à chaque modèle

### 3. Analyse et évaluation

Le MultiConnector intègre un système sophistiqué d'analyse et d'évaluation :

- **Collecte d'échantillons** : Collecte automatique d'exemples de prompts et de réponses
- **Tests de connecteurs** : Évaluation des performances des différents modèles sur les mêmes prompts
- **Évaluation des résultats** : Validation de la qualité des réponses par un modèle principal
- **Optimisation des paramètres** : Ajustement automatique des paramètres en fonction des résultats

### 4. Gestion des coûts

Le MultiConnector intègre des fonctionnalités pour gérer et optimiser les coûts :

- **Suivi des coûts** : Calcul du coût de chaque requête
- **Optimisation coût/performance** : Sélection des modèles en fonction du rapport coût/performance
- **Crédits** : Système de crédit pour suivre l'utilisation des API

### 5. Stratégies de routage optimisées

Le MultiConnector propose trois stratégies de routage principales :

- **Stratégie Performance** : Privilégie les modèles avec les meilleurs scores de performance
- **Stratégie Économique** : Privilégie les modèles avec le meilleur rapport qualité/prix
- **Stratégie Équilibrée** : Recherche un équilibre optimal entre performance et coût

### 6. Système de fallback robuste

Le MultiConnector intègre un système de fallback qui permet de basculer automatiquement vers des modèles alternatifs en cas d'échec d'un modèle.

## Architecture

Le MultiConnector est structuré autour de plusieurs composants clés :

- **MultiTextCompletion** : Classe principale qui gère le routage des requêtes
- **MultiTextCompletionSettings** : Configuration du processus de complétion multi-modèles
- **PromptSettings** : Gestion des paramètres spécifiques aux prompts et aux connecteurs
- **Analysis** : Système d'analyse et d'évaluation des performances des modèles
- **OptimizedMultiConnectorRouter** : Routeur optimisé pour la sélection des modèles
- **ModelSpecificPromptTransformer** : Transformateur de prompts spécifique à chaque modèle
- **ModelCascadeStrategy** : Stratégie de cascade pour le fallback entre modèles

## Installation

### Via NuGet

```bash
dotnet add package MyIA.SemanticKernel.Connectors.AI.MultiConnector
```

### Dans .NET Interactive

```csharp
#r "nuget: MyIA.SemanticKernel.Connectors.AI.MultiConnector"
```

## Utilisation de base

```csharp
// Créer une instance des paramètres du MultiConnector
var settings = new MultiTextCompletionSettings();

// Créer des instances des connecteurs
var openAiNamedCompletion = new NamedTextCompletion("Primary", openAiCompletion);
var oobaboogaCompletions = new List<NamedTextCompletion>
{
    new NamedTextCompletion("Model1", oobaboogaCompletion1),
    new NamedTextCompletion("Model2", oobaboogaCompletion2)
};

// Configurer le Kernel avec le MultiConnector
var builder = Kernel.Builder;
builder.WithMultiConnectorCompletionService(
    settings: settings,
    mainTextCompletion: openAiNamedCompletion,
    setAsDefault: true,
    otherCompletions: oobaboogaCompletions.ToArray());

var kernel = builder.Build();

// Utiliser le kernel pour exécuter des fonctions sémantiques ou des plans
var result = await kernel.RunAsync(semanticFunction, contextVariables);
```

## Utilisation avancée

### Configuration du routage optimisé

```csharp
// Créer une instance du routeur optimisé
var router = new OptimizedMultiConnectorRouter();

// Sélectionner un modèle avec une stratégie spécifique
string model = router.SelectOptimalModel(
    category: "code", 
    complexity: "medium", 
    strategy: OptimizedMultiConnectorRouter.RoutingStrategy.Balanced);
```

### Transformation de prompts spécifique au modèle

```csharp
// Créer une instance du transformateur de prompts
var transformer = new ModelSpecificPromptTransformer();

// Transformer un prompt pour un modèle spécifique
string originalPrompt = "Écrivez une fonction qui calcule la factorielle d'un nombre";
string transformedPrompt = transformer.TransformPrompt(
    prompt: originalPrompt, 
    modelId: "gpt-4o", 
    parameters: new Dictionary<string, object>
    {
        { "context", "Développement d'une bibliothèque mathématique" },
        { "objective", "Implémenter une fonction de calcul de factorielle efficace" },
        { "output_format", "Code Python avec documentation" }
    });
```

### Utilisation du système de fallback

```csharp
// Créer une instance de la stratégie de cascade
var cascadeStrategy = new ModelCascadeStrategy(router, logger);

// Exécuter une requête avec fallback
try
{
    string response = await cascadeStrategy.ExecuteWithFallbackAsync(
        prompt: "Écrivez une fonction qui calcule la factorielle d'un nombre",
        category: "code",
        complexity: "medium",
        strategy: OptimizedMultiConnectorRouter.RoutingStrategy.Balanced);
    
    Console.WriteLine($"Réponse : {response}");
}
catch (Exception ex)
{
    Console.WriteLine($"Tous les modèles ont échoué : {ex.Message}");
}
```

## Documentation complémentaire

Pour plus d'informations sur le MultiConnector, consultez les documents suivants :

- [Cartographie des fonctionnalités](../../../../docs/MultiConnector_Cartographie.md)
- [Optimisations récentes](../../../../docs/MultiConnector_Optimizations.md)
- [Guide d'intégration des petits modèles](../../../../docs/SMALL_MODELS_INTEGRATION.md)
- [Tests d'intégration](../../../IntegrationTests/Connectors/MultiConnector/README.md)

## Licence

Ce projet est sous licence MIT. Voir le fichier LICENSE pour plus de détails.