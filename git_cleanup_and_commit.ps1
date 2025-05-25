#!/usr/bin/env pwsh
# Script de nettoyage et organisation du statut Git après git filter-repo

Write-Host "=== Nettoyage et organisation du statut Git ===" -ForegroundColor Green

# 1. Vérifier le statut Git actuel
Write-Host "\n1. Statut Git actuel:" -ForegroundColor Yellow
git status --porcelain

# 2. Supprimer les fichiers de build et temporaires du suivi Git
Write-Host "\n2. Suppression des fichiers de build du suivi Git..." -ForegroundColor Yellow

# Supprimer tous les fichiers bin/ et obj/ du suivi
git rm -r --cached dotnet/samples/ConsoleSamples/bin/ -f 2>$null
git rm -r --cached dotnet/samples/ConsoleSamples/obj/ -f 2>$null
git rm -r --cached dotnet/src/*/bin/ -f 2>$null
git rm -r --cached dotnet/src/*/obj/ -f 2>$null
git rm -r --cached dotnet/src/*/*/bin/ -f 2>$null
git rm -r --cached dotnet/src/*/*/obj/ -f 2>$null
git rm -r --cached dotnet/tests/*/bin/ -f 2>$null
git rm -r --cached dotnet/tests/*/obj/ -f 2>$null
git rm -r --cached dotnet/notebooks/test-packages/bin/ -f 2>$null
git rm -r --cached dotnet/notebooks/test-packages/obj/ -f 2>$null

# Supprimer les fichiers Python cache
git rm -r --cached python/*/__pycache__/ -f 2>$null
git rm -r --cached python/*/*/__pycache__/ -f 2>$null

# Supprimer les fichiers de couverture et temporaires
git rm --cached .coverage -f 2>$null

# 3. Ajouter les fichiers source appropriés
Write-Host "\n3. Ajout des fichiers source au contrôle de version..." -ForegroundColor Yellow

# Fichiers de configuration du projet
git add .gitignore
git add README.md
git add semantic-fleet.sln

# Fichiers source .NET (déjà suivis)
git add dotnet/src/IntegrationTests/testsettings.json

# Fichiers Python (déjà suivis)
git add model_tester/api_utils.py
git add model_tester/transparent_model_test.py
git add tools/verification/vetting/multi_connector_vetting_test_fixed.py
git add tools/verification/vetting/run_vetting_tests.py

# Ajouter les fichiers de projet .NET s'ils existent
$projectFiles = @(
    "dotnet/samples/ConsoleSamples/*.csproj",
    "dotnet/src/Connectors/*/*.csproj",
    "dotnet/src/IntegrationTests/*.csproj",
    "dotnet/src/VisualizerCSharp/*.csproj",
    "dotnet/tests/*/*.csproj",
    "dotnet/notebooks/test-packages/*.csproj"
)

foreach ($pattern in $projectFiles) {
    $files = Get-ChildItem $pattern -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        git add $file.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
    }
}

# Ajouter les fichiers source C# s'ils existent
$sourceFiles = @(
    "dotnet/samples/ConsoleSamples/*.cs",
    "dotnet/src/Connectors/*/*.cs",
    "dotnet/src/Connectors/*/*/*.cs",
    "dotnet/src/IntegrationTests/*.cs",
    "dotnet/src/VisualizerCSharp/*.cs",
    "dotnet/tests/*/*.cs"
)

foreach ($pattern in $sourceFiles) {
    $files = Get-ChildItem $pattern -Recurse -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        git add $file.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
    }
}

# Ajouter les fichiers Python source s'ils existent
$pythonFiles = @(
    "python/*/*.py",
    "python/*/*/*.py",
    "python/*/*/*/*.py"
)

foreach ($pattern in $pythonFiles) {
    $files = Get-ChildItem $pattern -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        if ($file.FullName -notmatch "__pycache__") {
            git add $file.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
        }
    }
}

# Ajouter les fichiers de configuration et documentation
$configFiles = @(
    "*.md",
    "*.txt",
    "*.yml",
    "*.yaml",
    "*.json",
    "dotnet/notebooks/config/*.json",
    "campaign_tests/data/*",
    "data/interoperability_tests/*"
)

foreach ($pattern in $configFiles) {
    $files = Get-ChildItem $pattern -Recurse -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        if ($file.FullName -notmatch "bin|obj|node_modules|__pycache__|coverage") {
            git add $file.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
        }
    }
}

# 4. Vérifier le statut après nettoyage
Write-Host "\n4. Statut Git après nettoyage:" -ForegroundColor Yellow
git status

# 5. Créer les commits organisés
Write-Host "\n5. Création des commits..." -ForegroundColor Yellow

# Commit 1: Configuration du projet
git add .gitignore README.md semantic-fleet.sln
git commit -m "feat: Configuration initiale du projet

- Ajout du fichier .gitignore complet pour .NET et Python
- Ajout du README.md avec description du projet
- Configuration de la solution Visual Studio

Après nettoyage de l'historique Git avec git filter-repo"

# Commit 2: Code source principal
git add dotnet/src/IntegrationTests/testsettings.json
git add model_tester/api_utils.py
git add model_tester/transparent_model_test.py
git add tools/verification/vetting/multi_connector_vetting_test_fixed.py
git add tools/verification/vetting/run_vetting_tests.py
git commit -m "feat: Code source principal et outils de test

- Ajout des utilitaires API et tests de modèles transparents
- Ajout des outils de vérification et validation
- Configuration des tests d'intégration

Fichiers nettoyés et validés après suppression des clés API"

# Commit 3: Fichiers de projet et configuration supplémentaires (s'il y en a)
$hasAdditionalFiles = (git diff --cached --name-only).Count -gt 0
if ($hasAdditionalFiles) {
    git commit -m "feat: Fichiers de projet et configuration additionnels

- Ajout des fichiers de projet .NET
- Ajout des fichiers source C# et Python
- Configuration et documentation supplémentaires"
}

# 6. Vérifier l'état final
Write-Host "\n6. État final du dépôt:" -ForegroundColor Yellow
git status
git log --oneline -5

# 7. Pousser vers GitHub
Write-Host "\n7. Poussée vers GitHub..." -ForegroundColor Yellow

try {
    # Essayer une poussée normale d'abord
    git push origin main
    Write-Host "✅ Poussée réussie vers origin/main" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Poussée normale échouée, tentative avec --force..." -ForegroundColor Yellow
    try {
        git push origin main --force
        Write-Host "✅ Poussée forcée réussie vers origin/main" -ForegroundColor Green
    } catch {
        Write-Host "❌ Erreur lors de la poussée: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Vérifiez la configuration du dépôt distant et les permissions." -ForegroundColor Yellow
    }
}

# 8. Vérification finale
Write-Host "\n8. Vérification finale:" -ForegroundColor Yellow
$trackedFiles = (git ls-files).Count
Write-Host "Nombre de fichiers sous contrôle de version: $trackedFiles" -ForegroundColor Cyan

Write-Host "\n=== Nettoyage Git terminé ===" -ForegroundColor Green
Write-Host "Le dépôt est maintenant propre et organisé." -ForegroundColor Cyan
