#!/usr/bin/env python3
"""
Script pour générer une stratégie de routage optimisée pour le MultiConnector
basée sur les résultats des tests comparatifs.
"""

import os
import json
import pandas as pd
import matplotlib.pyplot as plt
from datetime import datetime

def load_test_results(results_dir="../results/comprehensive_tests"):
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

def analyze_results(results):
    """
    Analyse les résultats des tests pour générer une stratégie de routage.
    
    Args:
        results: Liste des résultats
        
    Returns:
        Dictionnaire des analyses
    """
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
        for r in results if r["success"]
    ])
    
    # Analyser les performances par catégorie et complexité
    performance_by_category_complexity = {}
    
    for category in df["category"].unique():
        performance_by_category_complexity[category] = {}
        
        for complexity in df["complexity"].unique():
            category_complexity_df = df[(df["category"] == category) & (df["complexity"] == complexity)]
            
            if not category_complexity_df.empty:
                # Calculer le score moyen et le coût moyen pour chaque modèle
                model_performances = {}
                
                for model in category_complexity_df["model"].unique():
                    model_df = category_complexity_df[category_complexity_df["model"] == model]
                    
                    avg_score = model_df["score"].mean()
                    avg_cost = model_df["cost"].mean()
                    avg_time = model_df["response_time"].mean()
                    
                    # Calculer l'efficacité coût/performance
                    cost_efficiency = avg_score / avg_cost if avg_cost > 0 else float('inf')
                    
                    model_performances[model] = {
                        "avg_score": avg_score,
                        "avg_cost": avg_cost,
                        "avg_time": avg_time,
                        "cost_efficiency": cost_efficiency
                    }
                
                performance_by_category_complexity[category][complexity] = model_performances
    
    return performance_by_category_complexity

def generate_routing_strategy(performance_data, output_file, cost_sensitive=False):
    """
    Génère une stratégie de routage optimisée pour le MultiConnector.
    
    Args:
        performance_data: Données de performance par catégorie et complexité
        output_file: Chemin du fichier de sortie
        cost_sensitive: Si True, privilégie l'efficacité coût/performance
    """
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("# Stratégie de Routage Optimisée pour le MultiConnector\n\n")
        f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
        
        # Générer le code C# pour la stratégie de routage
        f.write("## Implémentation C#\n\n")
        f.write("```csharp\n")
        f.write("using System;\n")
        f.write("using Microsoft.SemanticKernel.AI.TextCompletion;\n\n")
        
        f.write("public class MultiConnectorRouter\n")
        f.write("{\n")
        f.write("    /// <summary>\n")
        f.write("    /// Sélectionne le modèle le plus approprié en fonction de la catégorie et de la complexité de la tâche.\n")
        f.write("    /// </summary>\n")
        f.write("    /// <param name=\"category\">Catégorie de la tâche (raisonnement, code, math, etc.)</param>\n")
        f.write("    /// <param name=\"complexity\">Complexité de la tâche (trivial, simple, medium, hard)</param>\n")
        f.write("    /// <param name=\"costSensitive\">Si true, privilégie l'efficacité coût/performance</param>\n")
        f.write("    /// <returns>Le nom du modèle à utiliser</returns>\n")
        f.write("    public string SelectOptimalModel(string category, string complexity, bool costSensitive = false)\n")
        f.write("    {\n")
        f.write("        // Modèle par défaut en cas de catégorie ou complexité non reconnue\n")
        f.write("        string defaultModel = \"gpt-4o\";\n\n")
        
        f.write("        // Stratégie de routage basée sur les résultats des tests\n")
        f.write("        switch (category.ToLowerInvariant())\n")
        f.write("        {\n")
        
        # Générer le code pour chaque catégorie
        for category, complexity_data in performance_data.items():
            f.write(f"            case \"{category}\":\n")
            f.write("                switch (complexity.ToLowerInvariant())\n")
            f.write("                {\n")
            
            # Générer le code pour chaque niveau de complexité
            for complexity, model_performances in complexity_data.items():
                f.write(f"                    case \"{complexity}\":\n")
                
                # Sélectionner le meilleur modèle en fonction du critère
                if cost_sensitive:
                    # Trier par efficacité coût/performance
                    sorted_models = sorted(
                        [(name, stats) for name, stats in model_performances.items()],
                        key=lambda x: x[1]["cost_efficiency"],
                        reverse=True
                    )
                else:
                    # Trier par score moyen
                    sorted_models = sorted(
                        [(name, stats) for name, stats in model_performances.items()],
                        key=lambda x: x[1]["avg_score"],
                        reverse=True
                    )
                
                # Sélectionner le meilleur modèle
                if sorted_models:
                    best_model = sorted_models[0][0]
                    f.write(f"                        return \"{best_model}\"; // Score: {sorted_models[0][1]['avg_score']:.2f}, Coût: ${sorted_models[0][1]['avg_cost']:.6f}\n")
                else:
                    f.write(f"                        return \"defaultModel\"; // Aucune donnée disponible\n")
            
            f.write("                    default:\n")
            f.write(f"                        return \"defaultModel\";\n")
            f.write("                }\n")
        
        f.write("            default:\n")
        f.write(f"                return \"defaultModel\";\n")
        f.write("        }\n")
        f.write("    }\n\n")
        
        # Méthode pour obtenir l'instance de TextCompletion
        f.write("    /// <summary>\n")
        f.write("    /// Obtient l'instance de TextCompletion pour le modèle spécifié.\n")
        f.write("    /// </summary>\n")
        f.write("    /// <param name=\"modelName\">Nom du modèle</param>\n")
        f.write("    /// <returns>Instance de ITextCompletion</returns>\n")
        f.write("    public ITextCompletion GetTextCompletionForModel(string modelName)\n")
        f.write("    {\n")
        f.write("        // Implémentation à compléter en fonction de l'architecture du MultiConnector\n")
        f.write("        switch (modelName)\n")
        f.write("        {\n")
        
        # Ajouter les cas pour chaque modèle
        all_models = set()
        for category_data in performance_data.values():
            for complexity_data in category_data.values():
                all_models.update(complexity_data.keys())
        
        for model in sorted(all_models):
            f.write(f"            case \"{model}\":\n")
            
            # Déterminer le fournisseur
            if "anthropic" in model or "claude" in model:
                f.write("                return new AnthropicTextCompletion(modelName);\n")
            elif "google" in model or "gemini" in model:
                f.write("                return new GoogleTextCompletion(modelName);\n")
            elif "qwen" in model:
                f.write("                return new OpenRouterTextCompletion(modelName);\n")
            else:
                f.write("                return new OpenAITextCompletion(modelName);\n")
        
        f.write("            default:\n")
        f.write("                return new OpenAITextCompletion(\"gpt-4o\");\n")
        f.write("        }\n")
        f.write("    }\n")
        f.write("}\n")
        f.write("```\n\n")
        
        # Générer un tableau récapitulatif des recommandations
        f.write("## Tableau Récapitulatif des Recommandations\n\n")
        f.write("| Catégorie | Complexité | Modèle Recommandé | Score | Coût | Modèle Économique | Score | Coût |\n")
        f.write("|-----------|------------|-------------------|-------|------|-------------------|-------|------|\n")
        
        for category, complexity_data in performance_data.items():
            for complexity, model_performances in complexity_data.items():
                # Modèle avec le meilleur score
                best_score_model = max(
                    [(name, stats) for name, stats in model_performances.items()],
                    key=lambda x: x[1]["avg_score"],
                    default=(None, {"avg_score": 0, "avg_cost": 0})
                )
                
                # Modèle avec la meilleure efficacité coût/performance
                best_efficiency_model = max(
                    [(name, stats) for name, stats in model_performances.items()],
                    key=lambda x: x[1]["cost_efficiency"],
                    default=(None, {"avg_score": 0, "avg_cost": 0})
                )
                
                if best_score_model[0] and best_efficiency_model[0]:
                    f.write(f"| {category} | {complexity} | {best_score_model[0]} | {best_score_model[1]['avg_score']:.2f} | ${best_score_model[1]['avg_cost']:.6f} | {best_efficiency_model[0]} | {best_efficiency_model[1]['avg_score']:.2f} | ${best_efficiency_model[1]['avg_cost']:.6f} |\n")
        
        # Conclusion
        f.write("\n## Conclusion\n\n")
        f.write("Cette stratégie de routage optimisée permet au MultiConnector de sélectionner automatiquement le modèle le plus approprié ")
        f.write("en fonction de la catégorie et de la complexité de la tâche. Deux modes sont disponibles :\n\n")
        f.write("1. **Mode Performance** : Privilégie le modèle avec le meilleur score, indépendamment du coût.\n")
        f.write("2. **Mode Économique** : Privilégie le modèle offrant le meilleur rapport qualité/prix.\n\n")
        f.write("Cette approche permet d'optimiser les performances tout en maîtrisant les coûts, en fonction des besoins spécifiques de chaque utilisation.")

def generate_visualizations(performance_data, output_dir):
    """
    Génère des visualisations pour la stratégie de routage.
    
    Args:
        performance_data: Données de performance par catégorie et complexité
        output_dir: Répertoire de sortie pour les visualisations
    """
    # Créer le répertoire de sortie
    os.makedirs(output_dir, exist_ok=True)
    
    # Préparer les données pour les visualisations
    data = []
    
    for category, complexity_data in performance_data.items():
        for complexity, model_performances in complexity_data.items():
            for model, stats in model_performances.items():
                data.append({
                    "category": category,
                    "complexity": complexity,
                    "model": model,
                    "score": stats["avg_score"],
                    "cost": stats["avg_cost"],
                    "time": stats["avg_time"],
                    "efficiency": stats["cost_efficiency"]
                })
    
    df = pd.DataFrame(data)
    
    # Définir un style pour les visualisations
    plt.style.use('seaborn-v0_8-darkgrid')
    
    # 1. Graphique des scores par catégorie et modèle
    plt.figure(figsize=(14, 10))
    pivot_table = df.pivot_table(index="model", columns="category", values="score", aggfunc="mean")
    
    # Créer un graphique à barres groupées
    pivot_table.plot(kind='bar', figsize=(14, 8))
    plt.title('Score Moyen par Catégorie et Modèle')
    plt.xlabel('Modèle')
    plt.ylabel('Score Moyen')
    plt.legend(title='Catégorie')
    plt.tight_layout()
    plt.savefig(os.path.join(output_dir, 'score_by_category_model.png'))
    plt.close()
    
    # 2. Graphique des scores par complexité et modèle
    plt.figure(figsize=(12, 10))
    pivot_table = df.pivot_table(index="model", columns="complexity", values="score", aggfunc="mean")
    
    # Réordonner les colonnes par niveau de complexité
    complexity_order = ["trivial", "simple", "medium", "hard"]
    pivot_table = pivot_table[[col for col in complexity_order if col in pivot_table.columns]]
    
    # Créer un graphique à barres groupées
    pivot_table.plot(kind='bar', figsize=(12, 8))
    plt.title('Score Moyen par Niveau de Complexité et Modèle')
    plt.xlabel('Modèle')
    plt.ylabel('Score Moyen')
    plt.legend(title='Complexité')
    plt.tight_layout()
    plt.savefig(os.path.join(output_dir, 'score_by_complexity_model.png'))
    plt.close()
    
    # 3. Graphique de l'efficacité coût/performance par modèle
    plt.figure(figsize=(12, 6))
    
    # Calculer l'efficacité moyenne par modèle
    efficiency_by_model = df.groupby("model")["efficiency"].mean().reset_index()
    efficiency_by_model = efficiency_by_model.sort_values("efficiency", ascending=False)
    
    plt.bar(efficiency_by_model["model"], efficiency_by_model["efficiency"])
    plt.xlabel('Modèle')
    plt.ylabel('Efficacité Coût/Performance')
    plt.title('Efficacité Coût/Performance par Modèle')
    plt.xticks(rotation=45, ha='right')
    plt.tight_layout()
    
    # Ajouter les valeurs sur les barres
    for i, v in enumerate(efficiency_by_model["efficiency"]):
        plt.text(i, v + 5, f'{v:.2f}', ha='center')
    
    plt.savefig(os.path.join(output_dir, 'cost_efficiency_by_model.png'))
    plt.close()

def main():
    """Fonction principale."""
    # Charger les résultats des tests
    results = load_test_results()
    
    if not results:
        print("❌ Erreur: Aucun résultat trouvé")
        return 1
    
    # Analyser les résultats
    performance_data = analyze_results(results)
    
    # Créer le répertoire de sortie
    output_dir = "../results/routing_strategy"
    os.makedirs(output_dir, exist_ok=True)
    
    # Générer la stratégie de routage optimisée
    output_file = os.path.join(output_dir, "routing_strategy.md")
    generate_routing_strategy(performance_data, output_file, cost_sensitive=False)
    
    # Générer la stratégie de routage économique
    output_file_eco = os.path.join(output_dir, "routing_strategy_economic.md")
    generate_routing_strategy(performance_data, output_file_eco, cost_sensitive=True)
    
    # Générer les visualisations
    generate_visualizations(performance_data, os.path.join(output_dir, "visualizations"))
    
    print(f"✅ Stratégie de routage générée: {output_file}")
    print(f"✅ Stratégie de routage économique générée: {output_file_eco}")
    print(f"✅ Visualisations générées dans: {os.path.join(output_dir, 'visualizations')}")
    
    return 0

if __name__ == "__main__":
    main()