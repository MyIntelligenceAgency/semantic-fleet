# Script pour nettoyer l'historique Git et supprimer les clés API OpenAI

# Étape 1: Localiser git-filter-repo
Write-Host "Localisation de git-filter-repo..." -ForegroundColor Yellow

$gitFilterRepoPath = "C:\Users\MYIA\Python\Python312\Scripts\git-filter-repo.exe"

if (-not (Test-Path $gitFilterRepoPath)) {
    Write-Host "Recherche de git-filter-repo dans d'autres emplacements..." -ForegroundColor Yellow
    $possiblePaths = @(
        "C:\Users\MYIA\Python\Python312\Scripts\git-filter-repo",
        "C:\Users\MYIA\AppData\Local\Programs\Python\Python312\Scripts\git-filter-repo.exe",
        "C:\Users\MYIA\AppData\Roaming\Python\Python312\Scripts\git-filter-repo.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $gitFilterRepoPath = $path
            break
        }
    }
    
    if (-not (Test-Path $gitFilterRepoPath)) {
        Write-Host "Erreur: git-filter-repo introuvable" -ForegroundColor Red
        Write-Host "Tentative d'utilisation via python -m..." -ForegroundColor Yellow
        $gitFilterRepoPath = "python"
        $useModule = $true
    }
}

Write-Host "Utilisation de: $gitFilterRepoPath" -ForegroundColor Green

# Étape 2: Exécuter git filter-repo avec --force
Write-Host "Exécution de git filter-repo pour nettoyer l'historique..." -ForegroundColor Yellow
Write-Host "ATTENTION: Cette opération va modifier l'historique Git de façon destructive!" -ForegroundColor Red
Write-Host "Utilisation de --force pour forcer l'opération sur ce dépôt existant." -ForegroundColor Yellow

try {
    if ($useModule) {
        # Utiliser python -m git_filter_repo
        python -m git_filter_repo --force --replace-text expressions.txt --path dotnet/src/IntegrationTests/testsettings.json --path model_tester/api_utils.py --path model_tester/transparent_model_test.py --path tools/verification/vetting/multi_connector_vetting_test_fixed.py --path tools/verification/vetting/run_vetting_tests.py
    } else {
        # Utiliser l'exécutable directement
        & $gitFilterRepoPath --force --replace-text expressions.txt --path dotnet/src/IntegrationTests/testsettings.json --path model_tester/api_utils.py --path model_tester/transparent_model_test.py --path tools/verification/vetting/multi_connector_vetting_test_fixed.py --path tools/verification/vetting/run_vetting_tests.py
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Nettoyage de l'historique terminé avec succès!" -ForegroundColor Green
    } else {
        throw "git filter-repo a échoué avec le code de sortie $LASTEXITCODE"
    }
} catch {
    Write-Host "Erreur lors de l'exécution de git filter-repo: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Étape 3: Vérifier le statut Git après le nettoyage
Write-Host "Vérification du statut Git après nettoyage..." -ForegroundColor Yellow
git status

# Étape 4: Pousser les modifications avec force
Write-Host "Poussée forcée vers le dépôt distant..." -ForegroundColor Yellow
Write-Host "ATTENTION: Cette opération va réécrire l'historique du dépôt distant!" -ForegroundColor Red
Write-Host "Les collaborateurs devront faire un 'git pull --rebase' ou re-cloner le dépôt." -ForegroundColor Red

try {
    git push origin main --force
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Poussée forcée terminée avec succès!" -ForegroundColor Green
        Write-Host "L'historique Git a été nettoyé et poussé vers le dépôt distant." -ForegroundColor Green
    } else {
        throw "La poussée forcée a échoué avec le code de sortie $LASTEXITCODE"
    }
} catch {
    Write-Host "Erreur lors de la poussée forcée: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Vous pouvez essayer manuellement: git push origin main --force" -ForegroundColor Yellow
    exit 1
}

Write-Host "" -ForegroundColor White
Write-Host "=== OPÉRATION TERMINÉE AVEC SUCCÈS ==="  -ForegroundColor Green
Write-Host "" -ForegroundColor White
Write-Host "Résumé des actions effectuées:" -ForegroundColor Cyan
Write-Host "✓ Clés API OpenAI supprimées de l'historique Git" -ForegroundColor Green
Write-Host "✓ Historique nettoyé et poussé vers le dépôt distant" -ForegroundColor Green
Write-Host "⚠ Les collaborateurs doivent re-cloner ou synchroniser leur dépôt local" -ForegroundColor Yellow
Write-Host "" -ForegroundColor White
Write-Host "Fichiers traités:" -ForegroundColor Cyan
Write-Host "- dotnet/src/IntegrationTests/testsettings.json" -ForegroundColor Gray
Write-Host "- model_tester/api_utils.py" -ForegroundColor Gray
Write-Host "- model_tester/transparent_model_test.py" -ForegroundColor Gray
Write-Host "- tools/verification/vetting/multi_connector_vetting_test_fixed.py" -ForegroundColor Gray
Write-Host "- tools/verification/vetting/run_vetting_tests.py" -ForegroundColor Gray