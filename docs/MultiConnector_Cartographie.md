# Cartographie des Fonctionnalités du MultiConnector

## Introduction

Le MultiConnector est un système sophistiqué qui permet d'utiliser différents modèles de langage de manière harmonisée au sein du projet Semantic-Fleet. Il offre des fonctionnalités avancées pour router les requêtes vers les modèles les plus appropriés en fonction du type de prompt, de la complexité de la tâche, et d'autres critères.

Ce document présente une cartographie complète des fonctionnalités du MultiConnector, identifie les paramètres clés et les options de configuration, et met en évidence les fonctionnalités qui pourraient être mieux exploitées dans les tests.

## Architecture Générale

Le MultiConnector est structuré autour de plusieurs composants clés :

1. **MultiTextCompletion** : Classe principale qui gère le routage des requêtes de complétion de texte vers différents modèles.
2. **MultiTextCompletionSettings** : Configuration du processus de complétion multi-modèles.
3. **PromptSettings** : Gestion des paramètres spécifiques aux prompts et aux connecteurs.
4. **Analysis** : Système d'analyse et d'évaluation des performances des modèles.

## Fonctionnalités Clés

### 1. Routage Intelligent des Prompts

Le MultiConnector peut router les prompts vers différents modèles en fonction de plusieurs critères :

- **Signature du Prompt** : Identification automatique du type de prompt basée sur son début.
- **Vetting Level** : Niveau de validation d'un modèle pour un type de prompt spécifique.
- **Comparaison de Performances** : Sélection basée sur le coût et la durée d'exécution.

```csharp
// Exemple de code pour le routage basé sur les performances
ConnectorComparer = MultiTextCompletionSettings.GetWeightedConnectorComparer(durationWeight, costWeight);
```

### 2. Transformation de Prompts

Le système permet de transformer les prompts pour les adapter aux différents modèles :

- **Transformations Globales** : Appliquées à tous les prompts.
- **Transformations Spécifiques au Type de Prompt** : Adaptées à chaque type de prompt.
- **Transformations Spécifiques au Connecteur** : Adaptées à chaque modèle.

Types d'interpolation disponibles :
- **InterpolateKeys** : Remplacement simple des tokens {keyName}.
- **InterpolateFormattable** : Formatage avancé avec {keyName:format}.
- **InterpolateDynamicLinqExpression** : Expressions dynamiques complexes.

```csharp
// Exemple de transformation de prompt
var promptTransform = new PromptTransform
{
    Template = "### System:\n{SystemSupplement}\n\n### User:\n{0}\n\n### Assistant:",
    InterpolationType = PromptInterpolationType.InterpolateKeys
};
```

### 3. Analyse et Évaluation

Le MultiConnector intègre un système sophistiqué d'analyse et d'évaluation :

- **Collecte d'Échantillons** : Collecte automatique d'exemples de prompts et de réponses.
- **Tests de Connecteurs** : Évaluation des performances des différents modèles sur les mêmes prompts.
- **Évaluation des Résultats** : Validation de la qualité des réponses par un modèle principal.
- **Optimisation des Paramètres** : Ajustement automatique des paramètres en fonction des résultats.

```csharp
// Exemple de configuration d'analyse
var analysisSettings = new MultiCompletionAnalysisSettings
{
    EnableAnalysis = true,
    NbPromptTests = 3,
    TestsTemperatureTransform = d => Math.Max(d ?? 0, 0.7),
    UpdateSuggestedSettings = true
};
```

### 4. Gestion des Coûts

Le MultiConnector intègre des fonctionnalités pour gérer et optimiser les coûts :

- **Suivi des Coûts** : Calcul du coût de chaque requête.
- **Optimisation Coût/Performance** : Sélection des modèles en fonction du rapport coût/performance.
- **Crédits** : Système de crédit pour suivre l'utilisation des API.

```csharp
// Exemple de suivi des coûts
var creditor = new CallRequestCostCreditor();
settings.Creditor = creditor;
```

### 5. Intégration avec Semantic Kernel

Le MultiConnector s'intègre parfaitement avec Semantic Kernel :

- **Extension de KernelBuilder** : Ajout facile du MultiConnector à un kernel.
- **Compatibilité ITextCompletion** : Implémentation de l'interface standard.
- **Support des Plans** : Exécution de plans avec le MultiConnector.

```csharp
// Exemple d'intégration avec Semantic Kernel
builder.WithMultiConnectorCompletionService(
    settings: settings,
    mainTextCompletion: openAiNamedCompletion,
    setAsDefault: true,
    otherCompletions: oobaboogaCompletions.ToArray());
```

## Paramètres et Options de Configuration

### MultiTextCompletionSettings

| Paramètre | Description | Valeur par défaut |
|-----------|-------------|------------------|
| FreezePromptTypes | Empêche la découverte automatique de nouveaux types de prompts | false |
| PromptTruncationLength | Longueur de troncature des prompts pour l'extraction de signature | 20 |
| AdjustPromptStarts | Ajuste les débuts de prompts pour une meilleure identification | false |
| EnablePromptSampling | Active la collecte d'échantillons pour les tests | true |
| MaxInstanceNb | Nombre d'échantillons à collecter par type de prompt | 10 |
| LogCallResult | Active la journalisation des résultats des appels | false |
| LogTestCollection | Active la journalisation de la collecte de tests | false |
| PromptLogsJsonEncoded | Encode les logs de prompts en JSON | true |
| ConnectorComparer | Fonction de comparaison des connecteurs | GetWeightedConnectorComparer(1, 1) |
| GlobalParameters | Paramètres globaux pour les templates | SystemSupplement, UserPreamble, SemanticRemarks |
| GlobalPromptTransform | Transformation globale des prompts | null |
| SampleVettedConnectors | Collecte des échantillons pour les connecteurs validés | true |

### MultiCompletionAnalysisSettings

| Paramètre | Description | Valeur par défaut |
|-----------|-------------|------------------|
| EnableAnalysis | Active l'analyse | false |
| AnalysisFilePath | Chemin du fichier d'analyse | ./MultiTextCompletion-analysis.json |
| AnalysisDelay | Délai avant le démarrage de l'analyse | 1 seconde |
| AnalysisAwaitsManualTrigger | Attend un déclenchement manuel | false |
| EnableConnectorTests | Active les tests de connecteurs | true |
| TestPrimaryCompletion | Teste le connecteur principal | true |
| TestsPeriod | Période de tests | 10 secondes |
| MaxDegreeOfParallelismTests | Nombre maximum de tests en parallèle | 1 |
| EnableTestEvaluations | Active l'évaluation des tests | true |
| EvaluationPeriod | Période d'évaluation | 10 secondes |
| MaxDegreeOfParallelismEvaluations | Nombre maximum d'évaluations en parallèle | 5 |
| UseSelfVetting | Utilise l'auto-validation | false |
| EnableSuggestion | Active les suggestions | true |
| SuggestionPeriod | Période de suggestion | 1 minute |
| UpdateSuggestedSettings | Met à jour les paramètres suggérés | true |
| SaveSuggestedSettings | Sauvegarde les paramètres suggérés | false |
| DeleteAnalysisFile | Supprime le fichier d'analyse | true |
| NbPromptTests | Nombre de tests par type de prompt | 3 |

## Fonctionnalités Sous-Utilisées

Après analyse du code et des tests existants, plusieurs fonctionnalités du MultiConnector semblent sous-utilisées :

1. **Transformations de Prompts Avancées** : Les types d'interpolation `InterpolateFormattable` et `InterpolateDynamicLinqExpression` ne sont pas utilisés dans les tests actuels.

2. **Routage Hybride** : Le système permet un routage basé sur plusieurs critères (complexité, type de tâche, coût), mais les tests actuels se concentrent principalement sur un seul critère.

3. **Stratégies de Fallback** : Le MultiConnector peut implémenter des stratégies de repli en cas d'échec d'un modèle, mais cette fonctionnalité n'est pas testée.

4. **Compression de Contexte** : Le système pourrait bénéficier de techniques de compression de contexte pour réduire le nombre de tokens.

5. **Mise en Cache des Réponses** : Une stratégie de mise en cache pourrait être implémentée pour les requêtes fréquentes.

## Modèles Disponibles

Le MultiConnector peut utiliser différents modèles via OpenAI et OpenRouter :

### Via OpenAI
- GPT-4o
- GPT-4o-mini
- GPT-3.5-turbo
- O3
- O4-mini

### Via OpenRouter
- Claude 3.7 Sonnet (anthropic/claude-3.7-sonnet)
- Gemini 2.5 Pro (google/gemini-pro-1.5)
- Qwen 3 1.7B (qwen/qwen3-1.7b)
- Qwen 3 8B (qwen/qwen3-8b)
- Qwen 3 14B (qwen/qwen3-14b)
- Qwen 3 30B A3B (qwen/qwen3-30b-a3b)
- Qwen 3 32B (qwen/qwen3-32b)

## Conclusion

Le MultiConnector est un système puissant et flexible qui permet d'utiliser différents modèles de langage de manière harmonisée. Il offre de nombreuses fonctionnalités avancées pour le routage des prompts, la transformation des prompts, l'analyse et l'évaluation des performances, et la gestion des coûts.

Pour exploiter pleinement le potentiel du MultiConnector, il est recommandé d'utiliser davantage les fonctionnalités avancées de transformation de prompts, de routage hybride, et de stratégies de fallback dans les tests.