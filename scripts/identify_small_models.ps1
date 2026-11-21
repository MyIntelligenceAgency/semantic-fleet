# Script PowerShell pour identifier les modèles plus petits à intégrer dans la campagne de tests

# Configuration
$outputDirectory = "../results"
$smallModelsFile = "$outputDirectory/small_models.json"

# Créer le répertoire de sortie s'il n'existe pas
if (-not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    Write-Host "Répertoire de sortie créé: $outputDirectory"
}

# Liste des modèles plus petits potentiels
# Ces modèles sont sélectionnés en fonction de leur taille et de leur compatibilité avec le MultiConnector
$smallModels = @(
    @{
        "name" = "microsoft_phi-2"
        "size" = "2.7B"
        "type" = "Oobabooga"
        "apiCompatible" = $true
        "memoryRequirements" = "4GB RAM"
        "recommendedComplexity" = "Trivial, Simple"
        "description" = "Version améliorée de Phi-1.5, optimisée pour les tâches de génération de texte simples"
    },
    @{
        "name" = "TheBloke_TinyLlama-1.1B-Chat-v1.0-GGUF"
        "size" = "1.1B"
        "type" = "Oobabooga"
        "apiCompatible" = $true
        "memoryRequirements" = "2GB RAM"
        "recommendedComplexity" = "Trivial"
        "description" = "Version très légère de LLaMA, optimisée pour les appareils à ressources limitées"
    },
    @{
        "name" = "TheBloke_Gemma-2B-GGUF"
        "size" = "2B"
        "type" = "Oobabooga"
        "apiCompatible" = $true
        "memoryRequirements" = "4GB RAM"
        "recommendedComplexity" = "Trivial, Simple"
        "description" = "Modèle léger de Google, bon équilibre entre performance et taille"
    },
    @{
        "name" = "TheBloke_StableLM-2-1.6B-GGUF"
        "size" = "1.6B"
        "type" = "Oobabooga"
        "apiCompatible" = $true
        "memoryRequirements" = "3GB RAM"
        "recommendedComplexity" = "Trivial"
        "description" = "Modèle léger de Stability AI, optimisé pour les tâches de génération de texte basiques"
    },
    @{
        "name" = "TheBloke_neural-chat-7B-v3-1-GGUF"
        "size" = "7B"
        "type" = "Oobabooga"
        "apiCompatible" = $true
        "memoryRequirements" = "8GB RAM"
        "recommendedComplexity" = "Trivial, Simple, Medium"
        "description" = "Modèle de taille moyenne optimisé pour les conversations, bonnes performances sur les tâches de complexité moyenne"
    }
)

# Enregistrer la liste des modèles dans un fichier JSON
$smallModels | ConvertTo-Json -Depth 10 | Out-File -FilePath $smallModelsFile -Encoding utf8

Write-Host "Liste des modèles plus petits enregistrée dans: $smallModelsFile"

# Afficher un résumé des modèles identifiés
Write-Host "`n=== Modèles Plus Petits Identifiés ==="
foreach ($model in $smallModels) {
    Write-Host "- $($model.name) ($($model.size)): $($model.description)"
    Write-Host "  * Mémoire requise: $($model.memoryRequirements)"
    Write-Host "  * Complexité recommandée: $($model.recommendedComplexity)"
}

Write-Host "`n=== Prochaines Étapes ==="
Write-Host "1. Vérifier la disponibilité des modèles dans votre environnement Oobabooga"
Write-Host "2. Adapter les scripts de test pour inclure ces modèles"
Write-Host "3. Exécuter la campagne de tests avec les modèles sélectionnés"