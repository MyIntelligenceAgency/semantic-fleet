# Script pour nettoyer l'historique Git et supprimer les cles API OpenAI

# Etape 1: Localiser git-filter-repo
Write-Host "Localisation de git-filter-repo..." -ForegroundColor Yellow

$gitFilterRepoPath = "C:\Users\MYIA\Python\Python312\Scripts\git-filter-repo.exe"

if (-not (Test-Path $gitFilterRepoPath)) {
    Write-Host "git-filter-repo introuvable, utilisation via python module" -ForegroundColor Yellow
    $gitFilterRepoPath = "python"
    $useModule = $true
}

Write-Host "Utilisation de: $gitFilterRepoPath" -ForegroundColor Green

# Etape 2: Executer git filter-repo avec --force
Write-Host "Execution de git filter-repo pour nettoyer l'historique..." -ForegroundColor Yellow
Write-Host "ATTENTION: Cette operation va modifier l'historique Git de facon destructive!" -ForegroundColor Red

try {
    if ($useModule) {
        python -m git_filter_repo --force --replace-text expressions.txt --path dotnet/src/IntegrationTests/testsettings.json --path model_tester/api_utils.py --path model_tester/transparent_model_test.py --path tools/verification/vetting/multi_connector_vetting_test_fixed.py --path tools/verification/vetting/run_vetting_tests.py
    } else {
        & $gitFilterRepoPath --force --replace-text expressions.txt --path dotnet/src/IntegrationTests/testsettings.json --path model_tester/api_utils.py --path model_tester/transparent_model_test.py --path tools/verification/vetting/multi_connector_vetting_test_fixed.py --path tools/verification/vetting/run_vetting_tests.py
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Nettoyage de l'historique termine avec succes!" -ForegroundColor Green
    } else {
        throw "git filter-repo a echoue avec le code de sortie $LASTEXITCODE"
    }
} catch {
    Write-Host "Erreur lors de l'execution de git filter-repo: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Etape 3: Verifier le statut Git apres le nettoyage
Write-Host "Verification du statut Git apres nettoyage..." -ForegroundColor Yellow
git status

# Etape 4: Pousser les modifications avec force
Write-Host "Poussee forcee vers le depot distant..." -ForegroundColor Yellow
Write-Host "ATTENTION: Cette operation va reecrire l'historique du depot distant!" -ForegroundColor Red
Write-Host "Les collaborateurs devront faire un 'git pull --rebase' ou re-cloner le depot." -ForegroundColor Red

try {
    git push origin main --force
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Poussee forcee terminee avec succes!" -ForegroundColor Green
        Write-Host "L'historique Git a ete nettoye et pousse vers le depot distant." -ForegroundColor Green
    } else {
        throw "La poussee forcee a echoue avec le code de sortie $LASTEXITCODE"
    }
} catch {
    Write-Host "Erreur lors de la poussee forcee: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Vous pouvez essayer manuellement: git push origin main --force" -ForegroundColor Yellow
    exit 1
}

Write-Host "" -ForegroundColor White
Write-Host "=== OPERATION TERMINEE AVEC SUCCES ===" -ForegroundColor Green
Write-Host "" -ForegroundColor White
Write-Host "Resume des actions effectuees:" -ForegroundColor Cyan
Write-Host "- Cles API OpenAI supprimees de l'historique Git" -ForegroundColor Green
Write-Host "- Historique nettoye et pousse vers le depot distant" -ForegroundColor Green
Write-Host "- Les collaborateurs doivent re-cloner ou synchroniser leur depot local" -ForegroundColor Yellow