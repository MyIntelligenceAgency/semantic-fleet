# Guide d'Intégration des Modèles Plus Petits dans le MultiConnector

Ce document fournit des instructions détaillées pour l'intégration de modèles plus petits dans la campagne de tests du MultiConnector, en se basant sur les résultats de la campagne initiale.

## Table des Matières

1. [Introduction](#introduction)
2. [Prérequis](#prérequis)
3. [Installation et Configuration](#installation-et-configuration)
4. [Exécution des Tests](#exécution-des-tests)
5. [Analyse des Résultats](#analyse-des-résultats)
6. [Optimisation des Paramètres](#optimisation-des-paramètres)
7. [Résolution des Problèmes](#résolution-des-problèmes)
8. [Ressources Additionnelles](#ressources-additionnelles)

## Introduction

L'intégration de modèles plus petits dans le MultiConnector permet d'optimiser les coûts et les performances pour les tâches de complexité faible à moyenne. Ce guide vous accompagne dans le processus d'intégration, de test et d'optimisation de ces modèles.

La campagne de tests initiale a démontré que :
- Le modèle primaire (OpenAI GPT) maintient des performances élevées (>87%) à tous les niveaux de complexité
- Les modèles secondaires sont efficaces uniquement pour les tâches triviales et simples
- Des seuils clairs ont été identifiés pour chaque modèle en fonction de la complexité des tâches

L'objectif de cette nouvelle campagne est d'évaluer l'efficacité des modèles encore plus petits (1B-3B) pour les tâches triviales et simples, afin d'optimiser davantage le MultiConnector.
## Prérequis

Avant de commencer l'intégration des modèles plus petits, assurez-vous de disposer des éléments suivants :

### Environnement Technique

- **.NET SDK 6.0** ou supérieur
- **Python 3.8** ou supérieur avec les packages suivants :
  - matplotlib
  - numpy
  - pandas
  - tabulate
- **PowerShell 7.0** ou supérieur
- **Oobabooga Text Generation WebUI** configuré avec les modèles plus petits

### Modèles Recommandés

Les modèles suivants sont recommandés pour l'intégration :

| Modèle | Taille | Mémoire Requise | Complexité Recommandée |
|--------|--------|-----------------|------------------------|
| microsoft_phi-2 | 2.7B | 4GB RAM | Trivial, Simple |
| TheBloke_TinyLlama-1.1B-Chat-v1.0-GGUF | 1.1B | 2GB RAM | Trivial |
| TheBloke_Gemma-2B-GGUF | 2B | 4GB RAM | Trivial, Simple |
| TheBloke_StableLM-2-1.6B-GGUF | 1.6B | 3GB RAM | Trivial |
| TheBloke_neural-chat-7B-v3-1-GGUF | 7B | 8GB RAM | Trivial, Simple, Medium |

### Fichiers de Configuration

Assurez-vous d'avoir accès aux fichiers suivants :

- `scripts/identify_small_models.ps1` - Script pour identifier les modèles plus petits
- `campaign_tests/scripts/run_small_models_campaign.ps1` - Script pour exécuter la campagne de tests
- `campaign_tests/scripts/generate_small_model_test_data.cs` - Script pour générer des données de test adaptées
- `campaign_tests/scripts/analyze_small_models.py` - Script pour analyser les résultats des tests

## Installation et Configuration

### 1. Installation des Modèles dans Oobabooga

1. Téléchargez les modèles recommandés depuis Hugging Face :

```powershell
# Exemple pour Phi-2
cd [chemin_vers_oobabooga]/models
python download-model.py microsoft/phi-2

# Exemple pour TinyLlama
python download-model.py TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF
```

2. Vérifiez que les modèles sont correctement installés :

```powershell
ls [chemin_vers_oobabooga]/models
```

### 2. Configuration du MultiConnector

1. Modifiez le fichier de configuration du MultiConnector pour inclure les modèles plus petits :

```csharp
// Exemple de configuration pour les modèles plus petits
var config = new MultiConnectorConfiguration
{
    Connectors = new List<ConnectorConfigurationBase>
    {
        // Configuration existante pour le modèle primaire
        new OpenAIConfiguration
        {
            ModelId = "gpt-3.5-turbo",
            ApiKey = "your-api-key",
            IsDefault = true
        },
        
        // Configurations pour les modèles plus petits
        new OobaboogaConnectorConfiguration
        {
            ModelId = "microsoft_phi-2",
            Endpoint = "http://localhost:5000/v1",
            MaxTokens = 256,  // Valeur réduite pour les petits modèles
            Temperature = 0.7
        },
        new OobaboogaConnectorConfiguration
        {
            ModelId = "TheBloke_TinyLlama-1.1B-Chat-v1.0-GGUF",
            Endpoint = "http://localhost:5000/v1",
            MaxTokens = 128,  // Valeur encore plus réduite pour les très petits modèles
            Temperature = 0.8  // Température légèrement plus élevée
        }
        // Ajoutez d'autres modèles selon vos besoins
    }
};
```

## Exécution des Tests

### 1. Identification des Modèles Plus Petits

Exécutez le script d'identification des modèles plus petits pour générer la liste des modèles à tester :

```powershell
cd scripts
./identify_small_models.ps1
```

Ce script génère un fichier `small_models.json` dans le répertoire `results` qui sera utilisé par les scripts de test.

### 2. Génération des Données de Test Adaptées

Générez des données de test spécifiquement adaptées aux capacités des modèles plus petits :

```powershell
cd campaign_tests/scripts
./run_small_models_campaign.ps1
```

Ce script exécute automatiquement le générateur de données de test (`generate_small_model_test_data.cs`) qui crée des jeux de données adaptés aux modèles plus petits.

### 3. Exécution de la Campagne de Tests

La campagne de tests pour les modèles plus petits se concentre sur les niveaux de complexité Trivial et Simple, qui sont les plus adaptés à ces modèles :

```powershell
cd campaign_tests/scripts
./run_small_models_campaign.ps1
```

Ce script exécute les tests pour chaque modèle et niveau de complexité, et génère des logs d'instrumentation dans le répertoire `results/small_models/logs`.

### 4. Analyse des Résultats

Analysez les résultats des tests pour évaluer les performances des modèles plus petits :

```powershell
cd campaign_tests/scripts
python analyze_small_models.py --log-dir ../results/small_models/logs --output-dir ../results/small_models/analysis --small-models-file ../../results/small_models.json
```

Ce script génère un rapport d'analyse détaillé dans le répertoire `results/small_models/analysis`, ainsi que des visualisations des performances des modèles.

## Analyse des Résultats

### Interprétation du Rapport d'Analyse

Le rapport d'analyse généré par le script `analyze_small_models.py` contient plusieurs sections importantes :

1. **Performances Globales des Modèles** : Taux de réussite, temps d'exécution, utilisation de tokens et coût pour chaque modèle.

2. **Performances par Taille de Modèle** : Analyse des performances regroupées par taille de modèle (1B, 2B, etc.).

3. **Performances par Niveau de Complexité** : Taux de réussite pour chaque modèle et niveau de complexité.

4. **Seuils de Complexité** : Identification des seuils de complexité pour chaque modèle, avec des recommandations sur les niveaux de complexité appropriés.

5. **Recommandations** : Suggestions pour l'assignation des modèles aux fonctions, l'optimisation des paramètres, etc.

### Visualisations

Le script génère également plusieurs visualisations pour faciliter l'interprétation des résultats :

- **Taux de Réussite par Modèle** : Graphique à barres montrant le taux de réussite pour chaque modèle.
- **Taux de Réussite par Taille de Modèle** : Graphique à barres montrant le taux de réussite regroupé par taille de modèle.
- **Taux de Réussite par Niveau de Complexité** : Graphique linéaire montrant l'évolution du taux de réussite en fonction du niveau de complexité.
- **Temps d'Exécution et Coût** : Graphiques comparatifs des temps d'exécution et des coûts pour chaque modèle.

### Métriques Clés à Surveiller

Lors de l'analyse des résultats, portez une attention particulière aux métriques suivantes :

- **Taux de Réussite** : Un taux de réussite de 60% ou plus est considéré comme acceptable pour les modèles plus petits (contre 70% pour les modèles plus grands).
- **Temps d'Exécution** : Les modèles plus petits devraient être significativement plus rapides que les modèles plus grands.
- **Utilisation de Tokens** : Les modèles plus petits devraient utiliser moins de tokens, ce qui se traduit par des coûts réduits.
- **Rapport Performance/Coût** : Évaluez le compromis entre performance et coût pour chaque modèle.
2. Ajustez les paramètres spécifiques pour les modèles plus petits :

```csharp
## Optimisation des Paramètres

### Ajustement des Paramètres du MultiConnector

En fonction des résultats de la campagne de tests, vous pouvez optimiser les paramètres du MultiConnector pour améliorer les performances des modèles plus petits :

#### 1. Optimisation des Prompts

Les modèles plus petits bénéficient de prompts plus courts et plus directs :

```csharp
// Exemple de transformation de prompt pour les petits modèles
public static class PromptTransformations
{
    public static string OptimizeForSmallModels(string originalPrompt, string modelId)
    {
        // Vérifier si c'est un petit modèle
        if (IsSmallModel(modelId))
        {
            // Simplifier le prompt
            return SimplifyPrompt(originalPrompt);
        }
        
        return originalPrompt;
    }
    
    private static bool IsSmallModel(string modelId)
    {
        return modelId.Contains("TinyLlama") || 
               modelId.Contains("phi-2") || 
               modelId.Contains("Gemma-2B") || 
               modelId.Contains("StableLM-2-1.6B");
    }
    
    private static string SimplifyPrompt(string prompt)
    {
        // Réduire la longueur du prompt
        if (prompt.Length > 500)
        {
            prompt = prompt.Substring(0, 500);
        }
        
        // Simplifier les instructions
        prompt = prompt.Replace("Veuillez fournir une analyse détaillée", "Analysez")
                       .Replace("Pourriez-vous s'il vous plaît", "Veuillez")
                       .Replace("Je souhaiterais que vous", "Veuillez");
        
        return prompt;
    }
}
```

#### 2. Ajustement des Paramètres de Génération

Modifiez les paramètres de génération en fonction de la taille du modèle :

```csharp
// Exemple d'ajustement des paramètres en fonction de la taille du modèle
public static class ParameterAdjustments
{
    public static MultiCompletionRequestSettings AdjustForModelSize(MultiCompletionRequestSettings settings, string modelId)
    {
        if (modelId.Contains("1.1B") || modelId.Contains("1.6B"))
        {
            // Très petits modèles (1-2B)
            settings.MaxTokens = Math.Min(settings.MaxTokens ?? 256, 128);
            settings.Temperature = Math.Max(settings.Temperature ?? 0.7, 0.8);
        }
        else if (modelId.Contains("2B") || modelId.Contains("phi-2"))
        {
            // Petits modèles (2-3B)
            settings.MaxTokens = Math.Min(settings.MaxTokens ?? 256, 192);
            settings.Temperature = Math.Max(settings.Temperature ?? 0.7, 0.75);
        }
        
        return settings;
    }
}
```

#### 3. Stratégie de Fallback

Implémentez une stratégie de fallback pour rediriger vers le modèle primaire en cas d'échec :

```csharp
// Exemple de stratégie de fallback
public static class FallbackStrategy
{
    public static async Task<string> ExecuteWithFallback(
        MultiTextCompletion multiTextCompletion,
        string prompt,
        MultiCompletionRequestSettings settings)
    {
        try
        {
            // Essayer d'abord avec un petit modèle
            var result = await multiTextCompletion.CompleteAsync(prompt, settings);
            
            // Vérifier si le résultat est acceptable
            if (IsAcceptableResult(result.Text))
            {
                return result.Text;
            }
            
            // Si le résultat n'est pas acceptable, utiliser le modèle primaire
            settings.PreferredModelIds = new[] { "Primary" };
            return (await multiTextCompletion.CompleteAsync(prompt, settings)).Text;
        }
        catch (Exception)
        {
            // En cas d'erreur, utiliser le modèle primaire
            settings.PreferredModelIds = new[] { "Primary" };
            return (await multiTextCompletion.CompleteAsync(prompt, settings)).Text;
        }
    }
    
    private static bool IsAcceptableResult(string result)
    {
        // Implémenter une logique pour vérifier si le résultat est acceptable
        return !string.IsNullOrWhiteSpace(result) && result.Length > 10;
    }
}
```

## Résolution des Problèmes

### Problèmes Courants et Solutions

| Problème | Cause Possible | Solution |
|----------|----------------|----------|
| Taux d'échec élevé pour les modèles plus petits | Prompts trop complexes | Simplifier les prompts, réduire leur longueur |
| Temps d'exécution anormalement long | Contexte trop grand | Réduire la taille du contexte, limiter MaxTokens |
| Erreurs de mémoire insuffisante | Modèle trop grand pour la RAM disponible | Utiliser une quantification plus agressive (4-bit) |
| Résultats incohérents | Température trop élevée | Réduire la température pour les tâches nécessitant de la précision |
| Erreurs de connexion à Oobabooga | API mal configurée | Vérifier les paramètres de l'API et les ports |

### Logs et Débogage

Pour faciliter le débogage, activez les logs détaillés dans le MultiConnector :

```csharp
// Exemple de configuration des logs
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<MultiTextCompletion>();
var multiTextCompletion = new MultiTextCompletion(config, logger);
```

## Ressources Additionnelles

### Documentation

- [Documentation du MultiConnector](../dotnet/src/Connectors/Connectors.AI.MultiConnector/README.md)
- [Documentation d'Oobabooga](../docs/OOBABOOGA.md)
- [Guide d'Optimisation des Prompts pour Petits Modèles](https://huggingface.co/blog/optimizing-prompts-for-small-llms)

### Modèles Recommandés

- [Microsoft Phi-2](https://huggingface.co/microsoft/phi-2)
- [TinyLlama](https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF)
- [Gemma 2B](https://huggingface.co/TheBloke/Gemma-2B-GGUF)
- [StableLM 2 1.6B](https://huggingface.co/TheBloke/StableLM-2-1.6B-GGUF)

### Outils

- [Oobabooga Text Generation WebUI](https://github.com/oobabooga/text-generation-webui)
- [GGUF Quantization Tools](https://github.com/ggerganov/ggml)

---

Ce guide est destiné à évoluer en fonction des retours d'expérience et des nouvelles versions des modèles. N'hésitez pas à contribuer en partageant vos résultats et optimisations.
// Paramètres optimisés pour les petits modèles
var settings = new MultiTextCompletionSettings
{
    PromptTruncationLength = 10,  // Valeur réduite pour les petits modèles
    TestsTemperatureTransform = d => Math.Max(d ?? 0, 0.6),  // Température minimale plus élevée
    NbPromptTests = 2,  // Nombre réduit de tests
    MaxDegreeOfParallelismConnectorsByTest = 2  // Parallélisme réduit
};
```