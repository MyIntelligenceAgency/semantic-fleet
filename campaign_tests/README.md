# Campagne de Tests Avancés pour le MultiConnector

Ce répertoire contient les scripts et outils nécessaires pour exécuter une campagne de tests avancés visant à évaluer les capacités des différents modèles avec le MultiConnector, en se concentrant sur les fonctions Semantic Kernel et les prompts réguliers.

## Structure du Répertoire

```
campaign_tests/
├── README.md                    # Ce fichier
├── scripts/                     # Scripts pour exécuter les tests
│   ├── analyze_prefixes.cs      # Analyse les préfixes des fonctions Semantic Kernel
│   ├── campaign_plan.md         # Plan détaillé de la campagne de tests
│   ├── generate_analysis_report.py # Génère un rapport d'analyse des résultats
│   ├── generate_test_data.cs    # Génère des données de test pour différents niveaux de complexité
│   ├── run_campaign.ps1         # Script principal pour exécuter la campagne complète
│   └── test_prefix_detection.cs # Teste la détection des préfixes
├── data/                        # Données de test générées
└── results/                     # Résultats des tests et rapports
    ├── logs/                    # Logs d'instrumentation
    └── analysis/                # Rapports d'analyse
```

## Prérequis

- .NET Core SDK 6.0 ou supérieur
- PowerShell 7.0 ou supérieur
- Python 3.8 ou supérieur avec les packages suivants:
  - matplotlib
  - numpy
  - pandas
  - tabulate

## Installation

1. Clonez ce répertoire dans votre environnement de développement.
2. Installez les dépendances Python:

```bash
pip install matplotlib numpy pandas tabulate
```

## Utilisation

### Exécution de la Campagne Complète

Pour exécuter la campagne de tests complète, utilisez le script PowerShell `run_campaign.ps1`:

```powershell
cd campaign_tests/scripts
./run_campaign.ps1
```

Ce script exécutera toutes les phases de la campagne de tests et générera un rapport final dans le répertoire `results`.

### Exécution des Scripts Individuels

Vous pouvez également exécuter chaque script individuellement pour des tests spécifiques:

#### Analyse des Préfixes

```powershell
dotnet run --project analyze_prefixes.cs -- ../../Samples/skills ../results/prefix_analysis_report.md
```

#### Génération des Données de Test

```powershell
dotnet run --project generate_test_data.cs -- ../data
```

#### Test de Détection des Préfixes

```powershell
dotnet run --project test_prefix_detection.cs -- ../../Samples/skills ../results/prefix_detection_report.md
```

#### Génération du Rapport d'Analyse

```bash
python generate_analysis_report.py --log-dir ../results/logs --output-dir ../results/analysis
```

## Personnalisation

### Modèles à Tester

Pour modifier la liste des modèles à tester, éditez la variable `$modelNames` dans le script `run_campaign.ps1`.

### Skills à Tester

Pour modifier la liste des skills à tester, éditez la variable `$skillsToTest` dans le script `run_campaign.ps1`.

### Niveaux de Complexité

Pour modifier les niveaux de complexité à tester, éditez la variable `$complexityLevels` dans le script `run_campaign.ps1`.

## Interprétation des Résultats

Les résultats de la campagne de tests sont présentés dans plusieurs rapports:

1. **Rapport d'Analyse des Préfixes**: Analyse du système de détection de préfixes et des patterns identifiés.
2. **Rapport de Détection des Préfixes**: Résultats des tests de détection des préfixes.
3. **Rapport d'Analyse des Performances**: Analyse des performances des différents modèles pour chaque fonction et niveau de complexité.
4. **Rapport Final**: Synthèse de tous les résultats et recommandations.

Le rapport final contient également des visualisations pour faciliter l'interprétation des résultats.

## Extension de la Campagne

Pour étendre la campagne de tests à d'autres modèles ou fonctions:

1. Ajoutez les nouveaux modèles à la liste `$modelNames` dans `run_campaign.ps1`.
2. Ajoutez les nouvelles fonctions à la liste `$skillsToTest` dans `run_campaign.ps1`.
3. Modifiez les scripts pour prendre en compte les spécificités des nouveaux modèles ou fonctions.

## Dépannage

### Problèmes Courants

1. **Erreur de compilation des scripts C#**:
   - Vérifiez que vous avez installé .NET Core SDK 6.0 ou supérieur.
   - Vérifiez que les références aux packages sont correctes.

2. **Erreur d'exécution des scripts Python**:
   - Vérifiez que vous avez installé Python 3.8 ou supérieur.
   - Vérifiez que vous avez installé tous les packages requis.

3. **Erreur d'accès aux fichiers**:
   - Vérifiez que vous avez les permissions nécessaires pour accéder aux répertoires.
   - Vérifiez que les chemins relatifs sont corrects.

### Support

Pour toute question ou problème, veuillez créer une issue dans le dépôt GitHub du projet.

## Licence

Ce projet est sous licence MIT. Voir le fichier LICENSE pour plus de détails.