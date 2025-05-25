# Script pour nettoyer l'historique Git et supprimer les clés API OpenAI

# Étape 1: Vérifier l'installation de git filter-repo
Write-Host "Vérification de l'installation de git filter-repo..."
try {
    $version = git filter-repo --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "git filter-repo est installé: $version" -ForegroundColor Green
    } else {
        throw "git filter-repo non trouvé"
    }
} catch {
    Write-Host "git filter-repo n'est pas installé. Tentative d'installation via pip..." -ForegroundColor Yellow
    pip install git-filter-repo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Erreur: Impossible d'installer git filter-repo" -ForegroundColor Red
        exit 1
    }
}

# Étape 2: Exécuter git filter-repo
Write-Host "Exécution de git filter-repo pour nettoyer l'historique..." -ForegroundColor Yellow

$filterCommand = @(
    "git", "filter-repo", "--replace-text", "expressions.txt",
    "--path", "dotnet/src/IntegrationTests/testsettings.json",
    "--path", "model_tester/api_utils.py",
    "--path", "model_tester/transparent_model_test.py",
    "--path", "tools/verification/vetting/multi_connector_vetting_test_fixed.py",
    "--path", "tools/verification/vetting/run_vetting_tests.py"
)

& $filterCommand[0] $filterCommand[1..($filterCommand.Length-1)]

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erreur lors de l'exécution de git filter-repo" -ForegroundColor Red
    exit 1
}

Write-Host "Nettoyage de l'historique terminé avec succès!" -ForegroundColor Green

# Étape 3: Pousser les modifications avec force
Write-Host "Poussée forcée vers le dépôt distant..." -ForegroundColor Yellow
Write-Host "ATTENTION: Cette opération va réécrire l'historique du dépôt distant!" -ForegroundColor Red
Write-Host "Les collaborateurs devront faire un 'git pull --rebase' ou re-cloner le dépôt." -ForegroundColor Red

git push origin main --force

if ($LASTEXITCODE -eq 0) {
    Write-Host "Poussée forcée terminée avec succès!" -ForegroundColor Green
    Write-Host "L'historique Git a été nettoyé et poussé vers le dépôt distant." -ForegroundColor Green
} else {
    Write-Host "Erreur lors de la poussée forcée" -ForegroundColor Red
    exit 1
}

Write-Host "Opération terminée avec succès!" -ForegroundColor Green