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

# Étape 2: Exécuter git filter-repo
Write-Host "Exécution de git filter-repo pour nettoyer l'historique..." -ForegroundColor Yellow
Write-Host "ATTENTION: Cette opération va modifier l'historique Git!" -ForegroundColor Red

try {
    if ($useModule) {
        # Utiliser python -m git_filter_repo
        python -m git_filter_repo --replace-text expressions.txt --path dotnet/src/IntegrationTests/testsettings.json --path model_tester/api_utils.py --path model_tester/transparent_model_test.py --path tools/verification/vetting/multi_connector_vetting_test_fixed.py --path tools/verification/vetting/run_vetting_tests.py
    } else {
        # Utiliser l'exécutable directement
        & $gitFilterRepoPath --replace-text expressions.txt --path dotnet/src/IntegrationTests/testsettings.json --path model_tester/api_utils.py --path model_tester/transparent_model_test.py --path tools/verification/vetting/multi_connector_vetting_test_fixed.py --path tools/verification/vetting/run_vetting_tests.py
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

# Étape 3: Pousser les modifications avec force
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
    exit 1
}

Write-Host "Opération terminée avec succès!" -ForegroundColor Green
Write-Host "Résumé:" -ForegroundColor Cyan
Write-Host "- Clés API OpenAI supprimées de l'historique Git" -ForegroundColor Green
Write-Host "- Historique nettoyé et poussé vers le dépôt distant" -ForegroundColor Green
Write-Host "- Les collaborateurs doivent re-cloner ou synchroniser leur dépôt local" -ForegroundColor Yellow