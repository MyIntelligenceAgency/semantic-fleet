# Système de Détection de Signatures des Prompts

> **Documentation complète** : Pour une documentation détaillée sur l'ensemble du système de détection de signatures des prompts, y compris les structures de données fondamentales, tous les matchers de prompts et le détecteur adaptatif, consultez la [documentation complète](../../../../docs/systeme_detection_signatures_prompts.md).

## Détecteur Adaptatif de Prompts

## Introduction

Le détecteur adaptatif de prompts est une extension du système existant de détection de signatures de prompts qui permet de mieux gérer les prompts qui ne correspondent pas à des patterns connus. Il implémente un mécanisme qui laisse passer les prompts non reconnus jusqu'à ce qu'on en détecte plusieurs du même type, auquel cas on identifie potentiellement un nouveau pattern à analyser.

## Fonctionnalités

- **Cache de prompts non reconnus** : Stocke temporairement les prompts qui ne correspondent à aucun pattern connu
- **Détection de similarité** : Identifie quand plusieurs prompts similaires non reconnus apparaissent
- **Analyse asynchrone** : Traite les nouveaux patterns potentiels en arrière-plan sans bloquer le traitement principal
- **Configuration flexible** : Permet d'ajuster les seuils de similarité, la taille du cache, etc.
- **Activation/désactivation** : Peut être activé ou désactivé via la configuration

## Architecture

Le détecteur adaptatif de prompts est implémenté comme un décorateur du `IPromptMatcher` existant, ce qui permet de l'intégrer facilement dans le système actuel sans modifier le code existant.

```
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│                     │     │                     │     │                     │
│  CompletionJob      │────▶│  AdaptiveDetector   │────▶│  BasePromptMatcher  │
│                     │     │                     │     │                     │
└─────────────────────┘     └─────────────────────┘     └─────────────────────┘
                                     │
                                     ▼
                            ┌─────────────────────┐
                            │                     │
                            │  UnrecognizedCache  │
                            │                     │
                            └─────────────────────┘
```

## Utilisation

### Configuration de base

```csharp
// Créer un détecteur adaptatif avec les paramètres par défaut
var basePromptMatcher = new OptimizedHybridPromptMatcher();
var adaptiveDetector = new AdaptivePromptDetector(basePromptMatcher);

// Utiliser le détecteur comme un IPromptMatcher standard
var job = new CompletionJob("Hello World", new AIRequestSettings());
var settings = adaptiveDetector.MatchPromptSettings(job, promptSettings);
```

### Configuration avancée

```csharp
// Créer un détecteur adaptatif avec des paramètres personnalisés
var adaptiveDetector = new AdaptivePromptDetector(
    basePromptMatcher,
    similarityThreshold: 80,            // Seuil de similarité (0-100)
    minSimilarPromptsToCreatePattern: 5, // Nombre minimum de prompts similaires pour créer un pattern
    cacheEntryExpiration: TimeSpan.FromHours(12), // Durée d'expiration des entrées du cache
    maxCacheSize: 500,                  // Taille maximale du cache
    enabled: true                       // Activer/désactiver le détecteur
);
```

### Intégration avec MultiTextCompletionSettings

```csharp
// Configurer les paramètres de complétion multi-texte pour utiliser le détecteur adaptatif
var settings = new MultiTextCompletionSettings();
settings.UseAdaptivePromptDetector(enabled: true);
```

### Intégration avec l'injection de dépendances

```csharp
// Ajouter le détecteur adaptatif aux services
services.AddAdaptivePromptDetector(options =>
{
    options.SimilarityThreshold = 75;
    options.MinSimilarPromptsToCreatePattern = 4;
    options.MaxCacheSize = 500;
    options.Enabled = true;
});
```

### Extension d'un matcher existant

```csharp
// Étendre un matcher existant avec le détecteur adaptatif
var basePromptMatcher = new OptimizedHybridPromptMatcher();
var adaptiveDetector = basePromptMatcher.WithAdaptiveDetection(options =>
{
    options.SimilarityThreshold = 85;
    options.MinSimilarPromptsToCreatePattern = 4;
    options.MaxCacheSize = 200;
    options.Enabled = true;
});
```

## Paramètres de configuration

| Paramètre | Description | Valeur par défaut |
|-----------|-------------|-------------------|
| `similarityThreshold` | Seuil de similarité pour considérer deux prompts comme similaires (0-100) | 70 |
| `minSimilarPromptsToCreatePattern` | Nombre minimum de prompts similaires pour créer un nouveau pattern | 3 |
| `cacheEntryExpiration` | Durée d'expiration des entrées du cache | 24 heures |
| `maxCacheSize` | Taille maximale du cache | 1000 |
| `enabled` | Indique si le détecteur adaptatif est activé | true |

## Fonctionnement interne

1. Lorsqu'un prompt est reçu, le détecteur tente d'abord de le faire correspondre à un pattern connu en utilisant le matcher de base.
2. Si aucune correspondance n'est trouvée et que le détecteur est activé, le prompt est stocké dans le cache.
3. À chaque nouveau prompt non reconnu, le détecteur vérifie s'il existe des prompts similaires dans le cache.
4. Si suffisamment de prompts similaires sont détectés, un nouveau pattern potentiel est identifié.
5. Ce pattern est analysé de manière asynchrone pour extraire un préfixe commun ou un pattern regex.
6. Si un pattern valide est trouvé, il est ajouté au matcher de base pour les futures correspondances.

## Considérations de performance

- Le cache est thread-safe et a une taille limitée pour éviter les problèmes de mémoire.
- Les entrées du cache expirent après un certain temps pour éviter de conserver des prompts obsolètes.
- L'analyse des nouveaux patterns est effectuée de manière asynchrone pour ne pas bloquer le traitement principal.
- Le détecteur peut être désactivé si nécessaire pour éviter tout impact sur les performances.

## Limitations

- La détection de similarité est basée sur la distance de Levenshtein, qui peut ne pas être adaptée à tous les types de prompts.
- Les patterns générés automatiquement peuvent ne pas être aussi précis que ceux définis manuellement.
- L'analyse asynchrone peut introduire un délai entre la détection d'un nouveau pattern et sa disponibilité pour les correspondances.