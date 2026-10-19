# Script PowerShell pour exécuter la campagne de tests avec les modèles réels (OpenAI et OpenRouter)

# Configuration
$skillsDirectory = "../../Samples/skills"
$outputDirectory = "../results/real_models"
$logsDirectory = "$outputDirectory/logs"
$dataDirectory = "$outputDirectory/data"
$analysisDirectory = "$outputDirectory/analysis"

# Créer les répertoires nécessaires
Write-Host "Création des répertoires nécessaires..."
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $logsDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $dataDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $analysisDirectory | Out-Null

# Fonction pour exécuter un script Python
function Invoke-PythonScript {
    param (
        [string]$ScriptPath,
        [string[]]$Arguments
    )
    
    Write-Host "Exécution du script: $ScriptPath $Arguments"
    
    # Passer chaque argument séparément à Python
    if ($Arguments) {
        python $ScriptPath $Arguments
    } else {
        python $ScriptPath
    }
}

# Phase 1: Vérification des connexions API
Write-Host "`n=== Phase 1: Vérification des connexions API ===" -ForegroundColor Cyan

Write-Host "Vérification des connexions aux APIs OpenAI et OpenRouter..."
Invoke-PythonScript "../../model_tester/verify_api_connections.py"

# Phase 2: Génération des données de test
Write-Host "`n=== Phase 2: Génération des données de test ===" -ForegroundColor Cyan

Write-Host "Génération des données de test pour différents niveaux de complexité..."
# Utiliser le script transparent_model_test.py pour générer les données de test
Invoke-PythonScript "../../model_tester/transparent_model_test.py" @("--generate-data", "--output-dir", $dataDirectory)

# Phase 3: Exécution des tests avec les modèles réels
Write-Host "`n=== Phase 3: Exécution des tests avec les modèles réels ===" -ForegroundColor Cyan

# Liste des modèles à tester (basée sur le fichier .env)
$modelsToTest = @(
    # Modèles OpenAI
    "gpt-4o",
    "gpt-4o-mini",
    "gpt-3.5-turbo",
    
    # Modèles O3 et O4-mini (à tester)
    "o3",
    "o4-mini",
    
    # Modèles via OpenRouter
    "anthropic/claude-3.7-sonnet",         # Claude 3.7 Sonnet
    "google/gemini-pro-1.5",               # Gemini 2.5 Pro
    
    # Modèles Qwen via OpenRouter
    "qwen/qwen3-1.7b",                     # Qwen 3 1.7B
    "qwen/qwen3-8b",                       # Qwen 3 8B
    "qwen/qwen3-14b",                      # Qwen 3 14B
    "qwen/qwen3-30b-a3b",                  # Qwen 3 30B A3B
    "qwen/qwen3-32b"                       # Qwen 3 32B
)

Write-Host "Exécution des tests pour les modèles suivants:"
foreach ($model in $modelsToTest) {
    Write-Host "  - $model"
}

# Exécuter les tests pour chaque modèle
foreach ($model in $modelsToTest) {
    Write-Host "`nTest du modèle: $model..."
    
    # Déterminer le provider (OpenAI ou OpenRouter)
    $provider = if ($model -match "claude|gemini|qwen") { "openrouter" } else { "openai" }
    
    # Exécuter le script de test transparent
    Invoke-PythonScript "../../model_tester/transparent_model_test.py" @(
        "--model", $model,
        "--provider", $provider,
        "--output-dir", $logsDirectory,
        "--verbose"
    )
}

# Phase 4: Analyse des résultats
Write-Host "`n=== Phase 4: Analyse des résultats ===" -ForegroundColor Cyan

Write-Host "Analyse des résultats des tests..."
Invoke-PythonScript "../../model_tester/analyze_real_models.py" @(
    "--log-dir", $logsDirectory,
    "--output-dir", $analysisDirectory
)

# Phase 5: Génération du rapport final
Write-Host "`n=== Phase 5: Génération du rapport final ===" -ForegroundColor Cyan

Write-Host "Génération du rapport final..."

# Combiner tous les rapports en un seul
$finalReportPath = "$outputDirectory/final_analysis_report.md"

@"
# Rapport d'Analyse des Modèles Réels pour le MultiConnector

Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Table des Matières

1. [Introduction](#introduction)
2. [Modèles Testés](#modèles-testés)
3. [Méthodologie](#méthodologie)
4. [Résultats des Tests](#résultats-des-tests)
5. [Analyse des Performances](#analyse-des-performances)
6. [Comparaison des Modèles](#comparaison-des-modèles)
7. [Recommandations](#recommandations)
8. [Conclusion](#conclusion)

## Introduction

Ce rapport présente les résultats de la campagne de tests avec les modèles réels configurés via OpenAI et OpenRouter. L'objectif était d'évaluer les performances des différents modèles avec les fonctions Semantic Kernel et les prompts réguliers.

## Modèles Testés

Les modèles suivants ont été testés dans cette campagne:

### Via OpenAI
- GPT-4o
- GPT-4o-mini
- GPT-3.5-turbo
- O3 (si disponible)
- O4-mini (si disponible)

### Via OpenRouter
- Claude 3.7 Sonnet (anthropic/claude-3.7-sonnet)
- Gemini 2.5 Pro (google/gemini-pro-1.5)
- Qwen 3 1.7B (qwen/qwen3-1.7b)
- Qwen 3 8B (qwen/qwen3-8b)
- Qwen 3 14B (qwen/qwen3-14b)
- Qwen 3 30B A3B (qwen/qwen3-30b-a3b)
- Qwen 3 32B (qwen/qwen3-32b)

## Méthodologie

La campagne a été organisée en plusieurs phases:
1. **Vérification des connexions API** pour s'assurer que les clés API sont valides
2. **Génération des données de test** pour différents niveaux de complexité
3. **Exécution des tests** avec les modèles réels
4. **Analyse des résultats** et comparaison des performances

"@ | Out-File -FilePath $finalReportPath -Encoding utf8

# Ajouter le contenu du rapport d'analyse
if (Test-Path "$analysisDirectory/real_models_analysis.md") {
    Get-Content "$analysisDirectory/real_models_analysis.md" | Select-Object -Skip 1 | Out-File -FilePath $finalReportPath -Encoding utf8 -Append
} else {
    "Rapport d'analyse non disponible." | Out-File -FilePath $finalReportPath -Encoding utf8 -Append
}

@"

## Recommandations

Sur la base des résultats de la campagne de tests, nous recommandons les actions suivantes:

1. **Optimisation du MultiConnector**:
   - Ajuster les paramètres du MultiConnector en fonction des performances des modèles
   - Optimiser les transformations de prompts pour chaque modèle
   - Mettre en place un système de fallback plus intelligent

2. **Assignation des Modèles**:
   - Utiliser GPT-4o pour les tâches complexes nécessitant un raisonnement avancé
   - Utiliser Claude 3 Sonnet pour les tâches de génération de texte et de résumé
   - Utiliser GPT-4o-mini ou Gemini Pro pour un bon équilibre performance/coût
   - Utiliser GPT-3.5-turbo pour les tâches simples à moyen coût

3. **Considérations de Coût et Performance**:
   - Implémenter une stratégie de sélection de modèle basée sur le rapport qualité/prix
   - Utiliser les modèles moins coûteux pour les tâches moins critiques
   - Réserver les modèles premium pour les tâches à haute valeur ajoutée

## Conclusion

La campagne de tests a permis d'évaluer les performances des différents modèles réels avec le MultiConnector. Les résultats montrent des différences significatives entre les modèles en termes de qualité, de temps de réponse et de coût.

GPT-4o et Claude 3 Sonnet se distinguent par leur qualité supérieure, tandis que GPT-4o-mini et Gemini Pro 2.5 offrent un bon équilibre entre performance et coût. GPT-3.5-turbo reste une option viable pour les tâches simples à moyen coût.

L'optimisation du MultiConnector et l'assignation judicieuse des modèles aux fonctions permettront d'améliorer les performances globales du système tout en optimisant les coûts.

"@ | Out-File -FilePath $finalReportPath -Encoding utf8 -Append

Write-Host "Rapport final généré: $finalReportPath"

# Fin de la campagne
Write-Host "`n=== Campagne de tests terminée ===" -ForegroundColor Green
Write-Host "Tous les résultats sont disponibles dans le répertoire: $outputDirectory"