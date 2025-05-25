# Script simplifié pour ajouter les fichiers au contrôle de version Git

Write-Host "Ajout des fichiers de base..." -ForegroundColor Green

# Ajouter les fichiers de configuration du projet
git add .gitignore
git add README.md
git add semantic-fleet.sln

# Ajouter les fichiers déjà suivis (qui sont dans l'index)
git add dotnet/src/IntegrationTests/testsettings.json
git add model_tester/api_utils.py
git add model_tester/transparent_model_test.py
git add tools/verification/vetting/multi_connector_vetting_test_fixed.py
git add tools/verification/vetting/run_vetting_tests.py

# Ajouter les scripts de nettoyage Git pour documentation
git add clean_git_history*.ps1
git add IMPORTANT_GIT_CLEANUP_INFO.md
git add expressions.txt

# Vérifier le statut
git status

Write-Host "Fichiers ajoutés avec succès!" -ForegroundColor Green
