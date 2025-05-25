# Semantic Fleet

Un projet de connecteurs Semantic Kernel pour l'intégration multi-modèles d'IA.

## Structure du projet

- **dotnet/** - Code source .NET avec connecteurs Semantic Kernel
  - **src/** - Code source principal
    - **Connectors/** - Connecteurs pour différents services d'IA
  - **samples/** - Exemples d'utilisation
  - **tests/** - Tests unitaires et d'intégration
- **python/** - Code Python pour les tests et utilitaires
- **model_tester/** - Outils de test des modèles d'IA
- **tools/** - Outils de vérification et de validation
- **campaign_tests/** - Tests de campagne et résultats
- **results/** - Résultats des tests et analyses

## Fonctionnalités principales

- Connecteurs multi-modèles pour Semantic Kernel
- Support pour OpenAI, Azure OpenAI, Oobabooga et autres
- Tests d'intégration complets
- Outils de validation et de vérification
- Analyses de performance des modèles

## Installation et utilisation

### Prérequis

- .NET 6.0 ou supérieur
- Python 3.8+ (pour les outils de test)
- Visual Studio ou VS Code

### Configuration

1. Cloner le dépôt
2. Configurer les clés API dans les fichiers `.env`
3. Compiler la solution .NET
4. Exécuter les tests

## Contribution

Les contributions sont les bienvenues ! Veuillez suivre les conventions de code existantes et ajouter des tests pour toute nouvelle fonctionnalité.

## Licence

Ce projet est sous licence MIT.
