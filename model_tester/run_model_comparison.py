#!/usr/bin/env python3
"""
Script pour exécuter les tests de comparaison des modèles de langage.
"""

import os
import sys
import asyncio
import argparse
from compare_models import ModelComparer, MODELS, TASK_CATEGORIES, COMPLEXITY_LEVELS

async def main():
    """Fonction principale."""
    parser = argparse.ArgumentParser(description='Exécuter les tests de comparaison des modèles de langage')
    
    # Options pour les modèles
    parser.add_argument('--all-models', action='store_true', help='Tester tous les modèles disponibles')
    parser.add_argument('--openai-models', action='store_true', help='Tester uniquement les modèles OpenAI')
    parser.add_argument('--openrouter-models', action='store_true', help='Tester uniquement les modèles via OpenRouter')
    parser.add_argument('--models', type=str, nargs='+', help='Liste spécifique des modèles à tester')
    
    # Options pour les catégories de tâches
    parser.add_argument('--all-categories', action='store_true', help='Tester toutes les catégories de tâches')
    parser.add_argument('--categories', type=str, nargs='+', choices=TASK_CATEGORIES, help='Catégories de tâches à tester')
    
    # Options pour les niveaux de complexité
    parser.add_argument('--all-complexities', action='store_true', help='Tester tous les niveaux de complexité')
    parser.add_argument('--complexities', type=str, nargs='+', choices=COMPLEXITY_LEVELS, help='Niveaux de complexité à tester')
    
    # Autres options
    parser.add_argument('--output-dir', type=str, default='../results/model_comparison', help='Répertoire de sortie pour les résultats')
    
    args = parser.parse_args()
    
    # Déterminer les modèles à tester
    models_to_test = []
    
    if args.all_models:
        models_to_test = list(MODELS.keys())
    elif args.openai_models:
        models_to_test = [model for model, config in MODELS.items() if config["provider"] == "openai"]
    elif args.openrouter_models:
        models_to_test = [model for model, config in MODELS.items() if config["provider"] == "openrouter"]
    elif args.models:
        models_to_test = args.models
    else:
        # Par défaut, tester un sous-ensemble représentatif de modèles
        models_to_test = ["gpt-4o", "gpt-3.5-turbo", "anthropic/claude-3.7-sonnet", "google/gemini-pro-1.5", "qwen/qwen3-32b"]
    
    # Déterminer les catégories à tester
    categories = None
    if not args.all_categories and args.categories:
        categories = args.categories
    
    # Déterminer les complexités à tester
    complexities = None
    if not args.all_complexities and args.complexities:
        complexities = args.complexities
    
    # Créer et exécuter le comparateur de modèles
    comparer = ModelComparer(models_to_test, args.output_dir)
    
    # Filtrer les prompts à tester
    prompts_to_test = None
    if categories or complexities:
        from compare_models import TEST_PROMPTS
        prompts_to_test = []
        for prompt in TEST_PROMPTS:
            if categories and prompt["category"] not in categories:
                continue
            if complexities and prompt["complexity"] not in complexities:
                continue
            prompts_to_test.append(prompt)
    
    # Exécuter les tests
    await comparer.run_tests(prompts_to_test)
    
    print("Tests terminés. Résultats disponibles dans:", args.output_dir)
    print("Visualisations disponibles dans:", os.path.join(args.output_dir, "visualizations"))
    print("Rapport d'analyse disponible dans:", os.path.join(args.output_dir, "rapport_analyse.md"))

if __name__ == "__main__":
    asyncio.run(main())