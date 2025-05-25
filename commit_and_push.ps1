# Script pour effectuer les commits et la poussée vers GitHub

Write-Host "=== Commits et poussée vers GitHub ===" -ForegroundColor Green

# Commit principal avec tous les fichiers
git commit -m "feat: Réorganisation complète après nettoyage Git

- Ajout du fichier .gitignore complet pour .NET et Python
- Ajout du README.md avec description du projet
- Configuration de la solution Visual Studio
- Code source principal et outils de test validés
- Scripts de nettoyage Git documentés

Après nettoyage de l'historique Git avec git filter-repo
Suppression des clés API OpenAI de l'historique
Réorganisation de 465 fichiers en structure propre"

# Vérifier l'état après commit
Write-Host "\nÉtat après commit:" -ForegroundColor Yellow
git status
git log --oneline -3

# Compter les fichiers sous contrôle de version
$trackedFiles = (git ls-files).Count
Write-Host "\nNombre de fichiers sous contrôle de version: $trackedFiles" -ForegroundColor Cyan

# Pousser vers GitHub
Write-Host "\nPoussée vers GitHub..." -ForegroundColor Yellow

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
        Write-Host "\nCommandes manuelles à exécuter:" -ForegroundColor Yellow
        Write-Host "git push origin main --force" -ForegroundColor White
    }
}

Write-Host "\n=== Processus terminé ===" -ForegroundColor Green
