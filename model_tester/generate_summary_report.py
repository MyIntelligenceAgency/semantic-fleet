#!/usr/bin/env python3
"""
Script pour générer un rapport de synthèse des résultats des tests.
Ce script analyse les résultats des tests et génère un rapport de synthèse
avec des recommandations pour l'optimisation du MultiConnector.
"""

import os
import sys
import json
import argparse
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
from typing import Dict, List, Any

def load_results(results_dir: str) -> List[Dict[str, Any]]:
    """
    Charge les résultats des tests à partir des fichiers JSON.
    
    Args:
        results_dir: Répertoire contenant les résultats
        
    Returns:
        Liste des résultats
    """
    results = []
    raw_responses_dir = os.path.join(results_dir, "raw_responses")
    
    if not os.path.exists(raw_responses_dir):
        print(f"❌ Erreur: Répertoire {raw_responses_dir} introuvable")
        return []
    
    for filename in os.listdir(raw_responses_dir):
        if filename.endswith(".json"):
            filepath = os.path.join(raw_responses_dir, filename)
            try:
                with open(filepath, "r", encoding="utf-8") as f:
                    result = json.load(f)
                    results.append(result)
            except Exception as e:
                print(f"❌ Erreur lors du chargement de {filepath}: {e}")
    
    return results

def analyze_results(results: List[Dict[str, Any]]) -> Dict[str, Any]:
    """
    Analyse les résultats des tests.
    
    Args:
        results: Liste des résultats
        
    Returns:
        Dictionnaire des analyses
    """
    if not results:
        return {}
    
    # Créer un DataFrame pour l'analyse
    df = pd.DataFrame([
        {
            "model": r["model"],
            "category": r["category"],
            "complexity": r["complexity"],
            "success": r["success"],
            "score": r.get("evaluation", {}).get("score", 0) if r["success"] else 0,
            "response_time": r["response_time"],
            "cost": r["cost"],
            "tokens": r["tokens"]["total"] if r["success"] else 0
        }
        for r in results
    ])
    
    # Analyse globale
    analysis = {
        "total_tests": len(results),
        "successful_tests": df["success"].sum(),
        "success_rate": df["success"].mean(),
        "avg_score": df[df["success"]]["score"].mean(),
        "avg_response_time": df[df["success"]]["response_time"].mean(),
        "avg_cost": df[df["success"]]["cost"].mean(),
        "avg_tokens": df[df["success"]]["tokens"].mean(),
        "total_cost": df["cost"].sum(),
        "models": df["model"].unique().tolist(),
        "categories": df["category"].unique().tolist(),
        "complexities": df["complexity"].unique().tolist(),
        "by_model": {},
        "by_category": {},
        "by_complexity": {},
        "best_model_overall": "",
        "best_model_by_category": {},
        "best_model_by_complexity": {},
        "cost_efficient_models": []
    }
    
    # Analyse par modèle
    for model in analysis["models"]:
        model_df = df[df["model"] == model]
        model_success_df = model_df[model_df["success"]]
        
        if len(model_success_df) > 0:
            analysis["by_model"][model] = {
                "total_tests": len(model_df),
                "successful_tests": len(model_success_df),
                "success_rate": len(model_success_df) / len(model_df),
                "avg_score": model_success_df["score"].mean(),
                "avg_response_time": model_success_df["response_time"].mean(),
                "avg_cost": model_success_df["cost"].mean(),
                "avg_tokens": model_success_df["tokens"].mean(),
                "total_cost": model_df["cost"].sum(),
                "cost_efficiency": model_success_df["score"].mean() / model_success_df["cost"].mean() if model_success_df["cost"].mean() > 0 else float('inf')
            }
    
    # Analyse par catégorie
    for category in analysis["categories"]:
        category_df = df[df["category"] == category]
        category_success_df = category_df[category_df["success"]]
        
        if len(category_success_df) > 0:
            analysis["by_category"][category] = {
                "total_tests": len(category_df),
                "successful_tests": len(category_success_df),
                "success_rate": len(category_success_df) / len(category_df),
                "avg_score": category_success_df["score"].mean(),
                "avg_response_time": category_success_df["response_time"].mean(),
                "avg_cost": category_success_df["cost"].mean(),
                "by_model": {}
            }
            
            # Analyse par modèle pour cette catégorie
            for model in analysis["models"]:
                model_category_df = category_df[category_df["model"] == model]
                model_category_success_df = model_category_df[model_category_df["success"]]
                
                if len(model_category_success_df) > 0:
                    analysis["by_category"][category]["by_model"][model] = {
                        "total_tests": len(model_category_df),
                        "successful_tests": len(model_category_success_df),
                        "success_rate": len(model_category_success_df) / len(model_category_df),
                        "avg_score": model_category_success_df["score"].mean(),
                        "avg_response_time": model_category_success_df["response_time"].mean(),
                        "avg_cost": model_category_success_df["cost"].mean()
                    }
    
    # Analyse par complexité
    for complexity in analysis["complexities"]:
        complexity_df = df[df["complexity"] == complexity]
        complexity_success_df = complexity_df[complexity_df["success"]]
        
        if len(complexity_success_df) > 0:
            analysis["by_complexity"][complexity] = {
                "total_tests": len(complexity_df),
                "successful_tests": len(complexity_success_df),
                "success_rate": len(complexity_success_df) / len(complexity_df),
                "avg_score": complexity_success_df["score"].mean(),
                "avg_response_time": complexity_success_df["response_time"].mean(),
                "avg_cost": complexity_success_df["cost"].mean(),
                "by_model": {}
            }
            
            # Analyse par modèle pour cette complexité
            for model in analysis["models"]:
                model_complexity_df = complexity_df[complexity_df["model"] == model]
                model_complexity_success_df = model_complexity_df[model_complexity_df["success"]]
                
                if len(model_complexity_success_df) > 0:
                    analysis["by_complexity"][complexity]["by_model"][model] = {
                        "total_tests": len(model_complexity_df),
                        "successful_tests": len(model_complexity_success_df),
                        "success_rate": len(model_complexity_success_df) / len(model_complexity_df),
                        "avg_score": model_complexity_success_df["score"].mean(),
                        "avg_response_time": model_complexity_success_df["response_time"].mean(),
                        "avg_cost": model_complexity_success_df["cost"].mean()
                    }
    
    # Trouver le meilleur modèle global
    if analysis["by_model"]:
        analysis["best_model_overall"] = max(
            analysis["by_model"].items(),
            key=lambda x: x[1]["avg_score"]
        )[0]
    
    # Trouver le meilleur modèle par catégorie
    for category, category_data in analysis["by_category"].items():
        if category_data["by_model"]:
            analysis["best_model_by_category"][category] = max(
                category_data["by_model"].items(),
                key=lambda x: x[1]["avg_score"]
            )[0]
    
    # Trouver le meilleur modèle par complexité
    for complexity, complexity_data in analysis["by_complexity"].items():
        if complexity_data["by_model"]:
            analysis["best_model_by_complexity"][complexity] = max(
                complexity_data["by_model"].items(),
                key=lambda x: x[1]["avg_score"]
            )[0]
    
    # Trouver les modèles les plus efficaces en termes de coût
    if analysis["by_model"]:
        # Trier les modèles par efficacité coût/performance décroissante
        sorted_models = sorted(
            [(name, stats) for name, stats in analysis["by_model"].items()],
            key=lambda x: x[1]["cost_efficiency"],
            reverse=True
        )
        
        # Prendre les 3 premiers modèles
        analysis["cost_efficient_models"] = [name for name, _ in sorted_models[:3]]
    
    return analysis

def generate_report(analysis: Dict[str, Any], output_file: str):
    """
    Génère un rapport de synthèse des résultats.
    
    Args:
        analysis: Dictionnaire des analyses
        output_file: Chemin du fichier de sortie
    """
    if not analysis:
        print("❌ Erreur: Aucune analyse à rapporter")
        return
    
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("# Rapport de Synthèse des Tests Comparatifs\n\n")
        f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
        
        # Résumé global
        f.write("## Résumé Global\n\n")
        f.write(f"- **Tests totaux**: {analysis['total_tests']}\n")
        f.write(f"- **Tests réussis**: {analysis['successful_tests']} ({analysis['success_rate']:.2%})\n")
        f.write(f"- **Score moyen**: {analysis['avg_score']:.2f}\n")
        f.write(f"- **Temps de réponse moyen**: {analysis['avg_response_time']:.2f}s\n")
        f.write(f"- **Coût moyen**: ${analysis['avg_cost']:.6f}\n")
        f.write(f"- **Tokens moyens**: {analysis['avg_tokens']:.2f}\n")
        f.write(f"- **Coût total**: ${analysis['total_cost']:.6f}\n")
        f.write(f"- **Meilleur modèle global**: {analysis['best_model_overall']}\n\n")
        
        # Performances par modèle
        f.write("## Performances par Modèle\n\n")
        f.write("| Modèle | Taux de Réussite | Score Moyen | Temps Moyen (s) | Coût Moyen | Efficacité Coût/Performance |\n")
        f.write("|--------|-----------------|-------------|-----------------|------------|-----------------------------|\n")
        
        # Trier les modèles par score moyen décroissant
        sorted_models = sorted(
            [(name, stats) for name, stats in analysis["by_model"].items()],
            key=lambda x: x[1]["avg_score"],
            reverse=True
        )
        
        for model_name, stats in sorted_models:
            f.write(f"| {model_name} | {stats['success_rate']:.2%} | {stats['avg_score']:.2f} | {stats['avg_response_time']:.2f} | ${stats['avg_cost']:.6f} | {stats['cost_efficiency']:.2f} |\n")
        
        # Performances par catégorie
        f.write("\n## Performances par Catégorie\n\n")
        
        for category, category_data in analysis["by_category"].items():
            f.write(f"### {category}\n\n")
            f.write(f"- **Score moyen**: {category_data['avg_score']:.2f}\n")
            f.write(f"- **Taux de réussite**: {category_data['success_rate']:.2%}\n")
            f.write(f"- **Meilleur modèle**: {analysis['best_model_by_category'].get(category, 'N/A')}\n\n")
            
            f.write("| Modèle | Taux de Réussite | Score Moyen | Temps Moyen (s) | Coût Moyen |\n")
            f.write("|--------|-----------------|-------------|-----------------|------------|\n")
            
            # Trier les modèles par score moyen décroissant
            sorted_models = sorted(
                [(name, stats) for name, stats in category_data["by_model"].items()],
                key=lambda x: x[1]["avg_score"],
                reverse=True
            )
            
            for model_name, stats in sorted_models:
                f.write(f"| {model_name} | {stats['success_rate']:.2%} | {stats['avg_score']:.2f} | {stats['avg_response_time']:.2f} | ${stats['avg_cost']:.6f} |\n")
            
            f.write("\n")
        
        # Performances par complexité
        f.write("\n## Performances par Niveau de Complexité\n\n")
        
        for complexity in ["trivial", "simple", "medium", "hard"]:
            if complexity in analysis["by_complexity"]:
                complexity_data = analysis["by_complexity"][complexity]
                
                f.write(f"### {complexity}\n\n")
                f.write(f"- **Score moyen**: {complexity_data['avg_score']:.2f}\n")
                f.write(f"- **Taux de réussite**: {complexity_data['success_rate']:.2%}\n")
                f.write(f"- **Meilleur modèle**: {analysis['best_model_by_complexity'].get(complexity, 'N/A')}\n\n")
                
                f.write("| Modèle | Taux de Réussite | Score Moyen | Temps Moyen (s) | Coût Moyen |\n")
                f.write("|--------|-----------------|-------------|-----------------|------------|\n")
                
                # Trier les modèles par score moyen décroissant
                sorted_models = sorted(
                    [(name, stats) for name, stats in complexity_data["by_model"].items()],
                    key=lambda x: x[1]["avg_score"],
                    reverse=True
                )
                
                for model_name, stats in sorted_models:
                    f.write(f"| {model_name} | {stats['success_rate']:.2%} | {stats['avg_score']:.2f} | {stats['avg_response_time']:.2f} | ${stats['avg_cost']:.6f} |\n")
                
                f.write("\n")
        
        # Recommandations pour le MultiConnector
        f.write("\n## Recommandations pour l'Optimisation du MultiConnector\n\n")
        
        # Stratégie de routage
        f.write("### Stratégie de Routage Recommandée\n\n")
        f.write("Basé sur les résultats des tests, nous recommandons la stratégie de routage suivante pour le MultiConnector:\n\n")
        
        f.write("```csharp\n")
        f.write("// Exemple de stratégie de routage pour le MultiConnector\n")
        f.write("public NamedTextCompletion SelectAppropriateModel(string category, string complexity)\n")
        f.write("{\n")
        f.write("    switch (category)\n")
        f.write("    {\n")
        
        for category in analysis["by_category"]:
            f.write(f"        case \"{category}\":\n")
            f.write("            switch (complexity)\n")
            f.write("            {\n")
            
            for complexity in ["trivial", "simple", "medium", "hard"]:
                if complexity in analysis["by_complexity"]:
                    # Trouver le meilleur modèle pour cette catégorie et ce niveau de complexité
                    best_model = None
                    best_score = -1
                    
                    for model_name in analysis["models"]:
                        if (category in analysis["by_category"] and 
                            model_name in analysis["by_category"][category]["by_model"] and
                            complexity in analysis["by_complexity"] and
                            model_name in analysis["by_complexity"][complexity]["by_model"]):
                            
                            category_score = analysis["by_category"][category]["by_model"][model_name]["avg_score"]
                            complexity_score = analysis["by_complexity"][complexity]["by_model"][model_name]["avg_score"]
                            combined_score = (category_score + complexity_score) / 2
                            
                            if combined_score > best_score:
                                best_score = combined_score
                                best_model = model_name
                    
                    if best_model:
                        f.write(f"                case \"{complexity}\":\n")
                        f.write(f"                    return GetNamedTextCompletion(\"{best_model}\");\n")
            
            f.write("                default:\n")
            f.write(f"                    return GetNamedTextCompletion(\"{analysis['best_model_overall']}\"); // Modèle par défaut\n")
            f.write("            }\n")
        
        f.write("        default:\n")
        f.write(f"            return GetNamedTextCompletion(\"{analysis['best_model_overall']}\"); // Modèle par défaut\n")
        f.write("    }\n")
        f.write("}\n")
        f.write("```\n\n")
        
        # Optimisation coût/performance
        f.write("### Optimisation Coût/Performance\n\n")
        f.write("Pour optimiser le rapport coût/performance, nous recommandons d'utiliser les modèles suivants:\n\n")
        
        for model in analysis["cost_efficient_models"]:
            if model in analysis["by_model"]:
                stats = analysis["by_model"][model]
                f.write(f"- **{model}**: Score moyen {stats['avg_score']:.2f}, Coût moyen ${stats['avg_cost']:.6f}, Efficacité {stats['cost_efficiency']:.2f}\n")
        
        f.write("\n")
        
        # Transformations de prompts
        f.write("### Transformations de Prompts Recommandées\n\n")
        f.write("Pour optimiser les performances des modèles, nous recommandons les transformations de prompts suivantes:\n\n")
        
        f.write("| Modèle | Technique de Transformation | Exemple |\n")
        f.write("|--------|----------------------------|--------|\n")
        
        # Exemples de transformations pour les meilleurs modèles
        if "gpt-4o" in analysis["by_model"]:
            f.write("| gpt-4o | Instructions détaillées avec contexte | ```\nVous êtes un assistant expert en {domaine}. Votre tâche est de {tâche}. Soyez précis et concis.\n```|\n")
        
        if "anthropic/claude-3.7-sonnet" in analysis["by_model"]:
            f.write("| claude-3.7-sonnet | Instructions explicites sur le format de sortie | ```\nRépondez à la question suivante en utilisant le format spécifié: {format}.\n```|\n")
        
        if "google/gemini-pro-1.5" in analysis["by_model"]:
            f.write("| gemini-pro-1.5 | Prompts concis avec instructions directes | ```\n{tâche}. Répondez de manière concise.\n```|\n")
        
        if any(model for model in analysis["by_model"] if "qwen" in model):
            qwen_model = next(model for model in analysis["by_model"] if "qwen" in model)
            f.write(f"| {qwen_model} | Prompts avec exemples few-shot | ```\nVoici un exemple: {{exemple}}. Maintenant, {{tâche}}.\n```|\n")
        
        # Conclusion
        f.write("\n## Conclusion\n\n")
        f.write("Cette analyse comparative des modèles de langage a permis d'identifier les forces et faiblesses de chaque modèle ")
        f.write("en fonction des catégories de tâches et des niveaux de complexité. Les recommandations formulées permettront ")
        f.write("d'optimiser le MultiConnector en utilisant le modèle le plus approprié pour chaque type de requête, ")
        f.write("tout en tenant compte des contraintes de coût et de performance.\n\n")
        
        f.write("Les modèles les plus performants sont généralement les plus coûteux, mais certains modèles offrent un excellent ")
        f.write("rapport qualité/prix pour des tâches spécifiques. Une stratégie de routage intelligente permettra de maximiser ")
        f.write("les performances tout en optimisant les coûts.\n")
    
    print(f"✅ Rapport de synthèse généré: {output_file}")

def main():
    """Fonction principale."""
    parser = argparse.ArgumentParser(description='Générer un rapport de synthèse des résultats des tests')
    parser.add_argument('--results-dir', type=str, default='../results/model_comparison', help='Répertoire contenant les résultats')
    parser.add_argument('--output-file', type=str, default=None, help='Fichier de sortie pour le rapport (par défaut: rapport_synthese.md dans le répertoire des résultats)')
    
    args = parser.parse_args()
    
    # Déterminer le fichier de sortie
    if args.output_file is None:
        args.output_file = os.path.join(args.results_dir, "rapport_synthese.md")
    
    # Charger les résultats
    results = load_results(args.results_dir)
    
    if not results:
        print("❌ Erreur: Aucun résultat trouvé")
        return 1
    
    # Analyser les résultats
    analysis = analyze_results(results)
    
    if not analysis:
        print("❌ Erreur: Analyse des résultats échouée")
        return 1
    
    # Générer le rapport
    generate_report(analysis, args.output_file)
    
    return 0

if __name__ == "__main__":
    sys.exit(main())