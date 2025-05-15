# Tests Comparatifs des Modèles de Langage

Ce répertoire contient des scripts pour tester et comparer les performances de différents modèles de langage (LLMs) sur diverses tâches.

## Fonctionnalités

- Test de plusieurs modèles de langage (OpenAI, Claude, Gemini, Qwen, etc.)
- Évaluation sur différentes catégories de tâches (raisonnement, code, math, etc.)
- Analyse des performances par niveau de complexité
- Génération de rapports détaillés et de visualisations
- Calcul des métriques de performance (score, temps d'exécution, coût)
- Recommandations basées sur les résultats des tests

## Prérequis

- Python 3.8+
- Clés API pour OpenAI et/ou OpenRouter
- Bibliothèques Python requises (voir `requirements.txt`)

## Installation

1. Clonez ce dépôt
2. Installez les dépendances :

```bash
pip install -r requirements.txt
```

3. Créez un fichier `.env` à la racine du projet avec vos clés API :

```
OPENAI_API_KEY=votre_clé_openai
OPENROUTER_API_KEY=votre_clé_openrouter
```

## Utilisation

### Script de comparaison simple

Pour exécuter une comparaison simple entre les modèles :

```bash
python run_model_comparison.py
```

Par défaut, ce script teste un sous-ensemble représentatif de modèles sur toutes les catégories de tâches.

### Options avancées

Vous pouvez personnaliser les tests avec différentes options :

```bash
# Tester tous les modèles disponibles
python run_model_comparison.py --all-models

# Tester uniquement les modèles OpenAI
python run_model_comparison.py --openai-models

# Tester uniquement les modèles via OpenRouter
python run_model_comparison.py --openrouter-models

# Tester des modèles spécifiques
python run_model_comparison.py --models gpt-4o anthropic/claude-3.7-sonnet google/gemini-pro-1.5

# Tester des catégories spécifiques
python run_model_comparison.py --categories raisonnement code math

# Tester des niveaux de complexité spécifiques
python run_model_comparison.py --complexities simple medium

# Spécifier un répertoire de sortie personnalisé
python run_model_comparison.py --output-dir ../results/my_custom_test
```

### Script de comparaison avancé

Pour des tests plus avancés avec toutes les fonctionnalités du MultiConnector :

```bash
python advanced_model_comparison.py
```

Ce script utilise toutes les fonctionnalités du MultiConnector pour des tests plus approfondis.

## Structure des résultats

Les résultats sont sauvegardés dans le répertoire `../results/model_comparison` (par défaut) avec la structure suivante :

```
model_comparison/
├── rapport_analyse.md           # Rapport principal avec les résultats et recommandations
├── raw_responses/               # Réponses brutes de chaque modèle pour chaque prompt
└── visualizations/              # Graphiques et visualisations
    ├── avg_score_by_model.png   # Score moyen par modèle
    ├── execution_time_by_model.png  # Temps d'exécution par modèle
    └── cost_efficiency_by_model.png # Efficacité coût/performance par modèle
```

## Personnalisation des tests

Vous pouvez personnaliser les tests en modifiant les fichiers suivants :

- `compare_models.py` : Définition des modèles, des prompts de test et des méthodes d'évaluation
- `run_model_comparison.py` : Script principal pour exécuter les tests
- `advanced_model_comparison.py` : Version avancée avec toutes les fonctionnalités du MultiConnector

## Modèles supportés

### Via OpenAI
- GPT-4o
- GPT-4o-mini
- GPT-3.5-turbo
- O3
- O4-mini

### Via OpenRouter
- Claude 3.7 Sonnet (anthropic/claude-3.7-sonnet)
- Gemini 2.5 Pro (google/gemini-pro-1.5)
- Qwen 3 1.7B (qwen/qwen3-1.7b)
- Qwen 3 8B (qwen/qwen3-8b)
- Qwen 3 14B (qwen/qwen3-14b)
- Qwen 3 30B A3B (qwen/qwen3-30b-a3b)
- Qwen 3 32B (qwen/qwen3-32b)

## Catégories de tâches

- Raisonnement
- Code
- Math
- Summarization
- Classification
- Writing
- QA
- Creative

## Niveaux de complexité

- Trivial
- Simple
- Medium
- Hard

## Licence

Ce projet est sous licence MIT.