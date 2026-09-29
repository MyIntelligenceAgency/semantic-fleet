# Système d'Analyse du MultiConnector

Ce répertoire contient les composants du système d'analyse et d'évaluation du MultiConnector, qui permettent d'évaluer automatiquement les performances des différents modèles et d'optimiser les paramètres de routage.

## Vue d'ensemble

Le système d'analyse du MultiConnector est conçu pour :

1. Collecter des échantillons de prompts et de réponses pendant l'exécution
2. Tester les différents connecteurs sur ces échantillons
3. Évaluer la qualité des réponses générées par chaque connecteur
4. Mettre à jour les paramètres de routage en fonction des résultats

Ce processus permet d'améliorer continuellement les performances du MultiConnector en identifiant les modèles les plus adaptés à chaque type de prompt.

## Composants principaux

### AnalysisJob

`AnalysisJob` est la classe principale qui orchestre le processus d'analyse. Elle gère :

- La collecte des échantillons
- L'exécution des tests sur les différents connecteurs
- L'évaluation des résultats
- La mise à jour des paramètres

### ConnectorTest

`ConnectorTest` représente un test spécifique pour un connecteur donné sur un prompt particulier. Il contient :

- Le prompt à tester
- Le connecteur à évaluer
- Les résultats du test

### ConnectorPromptEvaluation

`ConnectorPromptEvaluation` contient les résultats de l'évaluation d'un connecteur pour un type de prompt spécifique. Il inclut :

- Le niveau de validation (Vetting Level)
- Le score de performance
- Le temps d'exécution
- Le coût

### MultiCompletionAnalysis

`MultiCompletionAnalysis` gère l'analyse globale des performances des différents connecteurs. Il :

- Agrège les résultats des tests
- Calcule les statistiques de performance
- Génère des recommandations pour l'optimisation des paramètres

### MultiCompletionAnalysisSettings

`MultiCompletionAnalysisSettings` contient les paramètres de configuration du système d'analyse, notamment :

- L'activation/désactivation de l'analyse
- Le nombre de tests à effectuer par type de prompt
- Les périodes d'analyse et d'évaluation
- Les paramètres de parallélisme

## Événements

Le système d'analyse utilise plusieurs événements pour notifier les différentes étapes du processus :

- `SamplesReceivedEventArgs` : Notification de réception de nouveaux échantillons
- `EvaluationCompletedEventArgs` : Notification de fin d'évaluation
- `SuggestionCompletedEventArgs` : Notification de nouvelles suggestions de paramètres
- `AnalysisTaskCrashedEvent` : Notification d'erreur dans le processus d'analyse

## Utilisation

### Configuration de base

```csharp
var analysisSettings = new MultiCompletionAnalysisSettings
{
    EnableAnalysis = true,
    NbPromptTests = 3,
    TestsTemperatureTransform = d => Math.Max(d ?? 0, 0.7),
    UpdateSuggestedSettings = true
};

var settings = new MultiTextCompletionSettings
{
    AnalysisSettings = analysisSettings
};
```

### Déclenchement manuel de l'analyse

```csharp
// Créer une instance du MultiTextCompletion
var multiTextCompletion = new MultiTextCompletion(settings, logger);

// Déclencher manuellement l'analyse
await multiTextCompletion.TriggerAnalysisAsync();
```

### Abonnement aux événements d'analyse

```csharp
// S'abonner à l'événement de fin d'évaluation
multiTextCompletion.EvaluationCompleted += (sender, args) =>
{
    Console.WriteLine($"Évaluation terminée pour le connecteur {args.ConnectorName}");
    Console.WriteLine($"Score : {args.Score}");
    Console.WriteLine($"Niveau de validation : {args.VettingLevel}");
};

// S'abonner à l'événement de nouvelles suggestions
multiTextCompletion.SuggestionCompleted += (sender, args) =>
{
    Console.WriteLine($"Nouvelles suggestions disponibles");
    Console.WriteLine($"Nombre de connecteurs validés : {args.VettedConnectors.Count}");
};
```

## Paramètres avancés

### Ajustement des périodes d'analyse

```csharp
var analysisSettings = new MultiCompletionAnalysisSettings
{
    EnableAnalysis = true,
    AnalysisDelay = TimeSpan.FromSeconds(5),
    TestsPeriod = TimeSpan.FromSeconds(30),
    EvaluationPeriod = TimeSpan.FromSeconds(30),
    SuggestionPeriod = TimeSpan.FromMinutes(2)
};
```

### Configuration du parallélisme

```csharp
var analysisSettings = new MultiCompletionAnalysisSettings
{
    EnableAnalysis = true,
    MaxDegreeOfParallelismTests = 2,
    MaxDegreeOfParallelismEvaluations = 3
};
```

### Sauvegarde des paramètres suggérés

```csharp
var analysisSettings = new MultiCompletionAnalysisSettings
{
    EnableAnalysis = true,
    UpdateSuggestedSettings = true,
    SaveSuggestedSettings = true,
    AnalysisFilePath = "./MultiTextCompletion-analysis.json"
};
```

## Bonnes pratiques

1. **Ajustez le nombre de tests** : Utilisez `NbPromptTests` pour contrôler le nombre d'échantillons à tester par type de prompt. Une valeur plus élevée donne des résultats plus précis mais augmente le temps d'analyse.

2. **Configurez les périodes** : Ajustez les périodes d'analyse, de test et d'évaluation en fonction de votre cas d'utilisation. Des périodes plus courtes permettent une adaptation plus rapide, mais peuvent augmenter la charge sur les API.

3. **Gérez le parallélisme** : Utilisez `MaxDegreeOfParallelismTests` et `MaxDegreeOfParallelismEvaluations` pour contrôler le nombre de tests et d'évaluations exécutés en parallèle. Un parallélisme plus élevé accélère l'analyse mais augmente la consommation de ressources.

4. **Activez la sauvegarde** : Utilisez `SaveSuggestedSettings` pour sauvegarder les paramètres suggérés dans un fichier, ce qui permet de les réutiliser ultérieurement sans avoir à refaire l'analyse.

## Intégration avec le MultiConnector

Le système d'analyse est automatiquement intégré au MultiConnector lorsque vous configurez les paramètres d'analyse. Vous n'avez pas besoin de l'instancier ou de le gérer manuellement, sauf si vous souhaitez un contrôle plus fin sur le processus d'analyse.

Pour plus d'informations sur l'intégration avec le MultiConnector, consultez le [README du MultiConnector](../README.md).