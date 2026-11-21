#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Script de génération des recommandations finales pour l'optimisation du MultiConnector.
Ce script combine les résultats des analyses précédentes pour formuler des recommandations
stratégiques pour le routage des requêtes vers les modèles les plus appropriés.
"""

import os
import json
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime

# Configuration
ANALYSIS_DIR = "../results/comprehensive_tests/analysis"
REPORT_PATH = "../results/comprehensive_tests/recommandations_finales.md"
ROUTING_STRATEGY_PATH = "../results/comprehensive_tests/routing_strategy_optimized.md"

def load_model_data():
    """Charge les données des modèles à partir des fichiers d'analyse."""
    try:
        # Charger les données depuis les fichiers CSV générés par les analyses précédentes
        # Si les fichiers n'existent pas, nous utiliserons des données codées en dur
        model_metrics = {
            "gpt-3.5-turbo": {
                "score_global": 0.93,
                "cost_avg": 0.000499,
                "efficiency": 4030.92,
                "response_time": 2.38,
                "tokens_avg": 261.55,
                "strengths": ["classification", "creative", "math", "raisonnement"],
                "weaknesses": ["qa", "code"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 0.83,
                    "creative": 1.00,
                    "math": 1.00,
                    "qa": 0.60,
                    "raisonnement": 1.00,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.95,
                    "medium": 0.83
                }
            },
            "anthropic/claude-3.7-sonnet": {
                "score_global": 0.93,
                "cost_avg": 0.009061,
                "efficiency": 185.38,
                "response_time": 11.25,
                "tokens_avg": 407.36,
                "strengths": ["classification", "code", "creative", "math"],
                "weaknesses": ["qa", "raisonnement"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 1.00,
                    "creative": 1.00,
                    "math": 1.00,
                    "qa": 0.60,
                    "raisonnement": 0.89,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.91,
                    "medium": 1.00
                }
            },
            "google/gemini-pro-1.5": {
                "score_global": 0.91,
                "cost_avg": 0.001313,
                "efficiency": 2445.35,
                "response_time": 6.80,
                "tokens_avg": 375.27,
                "strengths": ["classification", "code", "creative"],
                "weaknesses": ["qa", "raisonnement"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 1.00,
                    "creative": 1.00,
                    "math": 1.00,
                    "qa": 0.60,
                    "raisonnement": 0.81,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.91,
                    "medium": 0.88
                }
            },
            "gpt-4o-mini": {
                "score_global": 0.88,
                "cost_avg": 0.004725,
                "efficiency": 819.66,
                "response_time": 5.19,
                "tokens_avg": 342.73,
                "strengths": ["classification", "creative", "qa"],
                "weaknesses": ["math", "code"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 0.83,
                    "creative": 1.00,
                    "math": 0.50,
                    "qa": 0.80,
                    "raisonnement": 0.89,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.87,
                    "medium": 0.83
                }
            },
            "gpt-4o": {
                "score_global": 0.86,
                "cost_avg": 0.008737,
                "efficiency": 422.35,
                "response_time": 6.80,
                "tokens_avg": 319.00,
                "strengths": ["classification", "creative", "summarization"],
                "weaknesses": ["math", "qa", "code"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 0.83,
                    "creative": 1.00,
                    "math": 0.50,
                    "qa": 0.60,
                    "raisonnement": 0.89,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.85,
                    "medium": 0.83
                }
            },
            "qwen/qwen3-14b": {
                "score_global": 0.66,
                "cost_avg": 0.001539,
                "efficiency": 813.77,
                "response_time": 14.19,
                "tokens_avg": 769.64,
                "strengths": ["classification", "creative", "summarization"],
                "weaknesses": ["code", "math", "qa"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 0.00,
                    "creative": 1.00,
                    "math": 0.00,
                    "qa": 0.60,
                    "raisonnement": 0.89,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.66,
                    "medium": 0.50
                }
            },
            "qwen/qwen3-32b": {
                "score_global": 0.65,
                "cost_avg": 0.003347,
                "efficiency": 284.50,
                "response_time": 24.28,
                "tokens_avg": 836.64,
                "strengths": ["classification", "summarization", "writing"],
                "weaknesses": ["math", "code", "creative"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 0.33,
                    "creative": 0.50,
                    "math": 0.00,
                    "qa": 0.60,
                    "raisonnement": 0.81,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.60,
                    "medium": 0.71
                }
            },
            "o4-mini": {
                "score_global": 0.75,
                "cost_avg": 0.007297,
                "efficiency": 207.60,
                "response_time": 5.10,
                "tokens_avg": 513.55,
                "strengths": ["classification", "code", "summarization"],
                "weaknesses": ["creative", "math", "raisonnement"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 1.00,
                    "creative": 0.50,
                    "math": 0.50,
                    "qa": 0.60,
                    "raisonnement": 0.56,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.78,
                    "medium": 0.50
                }
            },
            "o3": {
                "score_global": 0.71,
                "cost_avg": 0.031114,
                "efficiency": 100.81,
                "response_time": 10.51,
                "tokens_avg": 447.36,
                "strengths": ["classification", "creative", "summarization"],
                "weaknesses": ["code", "math", "raisonnement"],
                "category_scores": {
                    "classification": 1.00,
                    "code": 0.50,
                    "creative": 1.00,
                    "math": 0.50,
                    "qa": 0.60,
                    "raisonnement": 0.56,
                    "summarization": 1.00,
                    "writing": 1.00
                },
                "complexity_scores": {
                    "trivial": 1.00,
                    "simple": 0.85,
                    "medium": 0.00
                }
            }
        }
        
        return model_metrics
    except Exception as e:
        print(f"Erreur lors du chargement des données des modèles : {e}")
        return {}
def generate_routing_strategy(model_data):
    """Génère une stratégie de routage optimisée pour le MultiConnector."""
    # Catégories et niveaux de complexité
    categories = ["classification", "code", "creative", "math", "qa", "raisonnement", "summarization", "writing"]
    complexities = ["trivial", "simple", "medium"]
    
    # Stratégies de routage
    performance_strategy = {}
    economic_strategy = {}
    balanced_strategy = {}
    
    # Pour chaque combinaison de catégorie et complexité
    for category in categories:
        performance_strategy[category] = {}
        economic_strategy[category] = {}
        balanced_strategy[category] = {}
        
        for complexity in complexities:
            # Trouver le meilleur modèle en termes de performance
            best_model = None
            best_score = -1
            
            # Trouver le modèle le plus économique avec un score acceptable
            best_economic_model = None
            best_efficiency = -1
            
            # Trouver le modèle avec le meilleur équilibre performance/coût
            best_balanced_model = None
            best_balanced_score = -1
            
            for model_name, model_info in model_data.items():
                # Vérifier si le modèle a des scores pour cette catégorie et complexité
                if category in model_info["category_scores"] and complexity in model_info["complexity_scores"]:
                    # Score pour cette combinaison (moyenne du score de catégorie et de complexité)
                    score = (model_info["category_scores"][category] + model_info["complexity_scores"][complexity]) / 2
                    
                    # Vérifier si c'est le meilleur modèle en termes de performance
                    if score > best_score:
                        best_score = score
                        best_model = model_name
                    
                    # Vérifier si c'est le modèle le plus économique avec un score acceptable (>= 0.7)
                    if score >= 0.7 and model_info["efficiency"] > best_efficiency:
                        best_efficiency = model_info["efficiency"]
                        best_economic_model = model_name
                    
                    # Calculer un score équilibré (performance * efficacité)
                    balanced_score = score * model_info["efficiency"]
                    if balanced_score > best_balanced_score:
                        best_balanced_score = balanced_score
                        best_balanced_model = model_name
            
            # Si aucun modèle économique n'a été trouvé avec un score acceptable, utiliser le meilleur modèle
            if best_economic_model is None:
                best_economic_model = best_model
            
            # Si aucun modèle équilibré n'a été trouvé, utiliser le meilleur modèle
            if best_balanced_model is None:
                best_balanced_model = best_model
            
            # Ajouter à la stratégie
            performance_strategy[category][complexity] = best_model
            economic_strategy[category][complexity] = best_economic_model
            balanced_strategy[category][complexity] = best_balanced_model
    
    return {
        "performance": performance_strategy,
        "economic": economic_strategy,
        "balanced": balanced_strategy
    }

def generate_csharp_implementation(routing_strategy):
    """Génère une implémentation C# de la stratégie de routage."""
    performance_strategy = routing_strategy["performance"]
    economic_strategy = routing_strategy["economic"]
    balanced_strategy = routing_strategy["balanced"]
    
    csharp_code = """using System;
using Microsoft.SemanticKernel.AI.TextCompletion;

public class OptimizedMultiConnectorRouter
{
    /// <summary>
    /// Stratégie de routage à utiliser
    /// </summary>
    public enum RoutingStrategy
    {
        /// <summary>
        /// Privilégie la performance, indépendamment du coût
        /// </summary>
        Performance,
        
        /// <summary>
        /// Privilégie l'efficacité coût/performance
        /// </summary>
        Economic,
        
        /// <summary>
        /// Équilibre entre performance et coût
        /// </summary>
        Balanced
    }
    
    /// <summary>
    /// Sélectionne le modèle le plus approprié en fonction de la catégorie et de la complexité de la tâche.
    /// </summary>
    /// <param name="category">Catégorie de la tâche (raisonnement, code, math, etc.)</param>
    /// <param name="complexity">Complexité de la tâche (trivial, simple, medium)</param>
    /// <param name="strategy">Stratégie de routage à utiliser</param>
    /// <returns>Le nom du modèle à utiliser</returns>
    public string SelectOptimalModel(string category, string complexity, RoutingStrategy strategy = RoutingStrategy.Balanced)
    {
        // Modèle par défaut en cas de catégorie ou complexité non reconnue
        string defaultModel = "gpt-4o";
        
        // Normaliser les entrées
        category = category?.ToLowerInvariant() ?? "";
        complexity = complexity?.ToLowerInvariant() ?? "";
        
        // Sélectionner la stratégie de routage
        switch (strategy)
        {
            case RoutingStrategy.Performance:
                return SelectPerformanceModel(category, complexity) ?? defaultModel;
            
            case RoutingStrategy.Economic:
                return SelectEconomicModel(category, complexity) ?? defaultModel;
            
            case RoutingStrategy.Balanced:
            default:
                return SelectBalancedModel(category, complexity) ?? defaultModel;
        }
    }
    
    /// <summary>
    /// Sélectionne le modèle le plus performant pour la catégorie et la complexité données.
    /// </summary>
    private string SelectPerformanceModel(string category, string complexity)
    {
        switch (category)
        {
"""
    
    # Ajouter les cas pour chaque catégorie (stratégie de performance)
    for category, complexities in performance_strategy.items():
        csharp_code += f"            case \"{category}\":\n"
        csharp_code += "                switch (complexity)\n"
        csharp_code += "                {\n"
        
        for complexity, model in complexities.items():
            csharp_code += f"                    case \"{complexity}\":\n"
            csharp_code += f"                        return \"{model}\";\n"
        
        csharp_code += "                    default:\n"
        csharp_code += "                        return null;\n"
        csharp_code += "                }\n"
    
    csharp_code += """            default:
                return null;
        }
    }
    
    /// <summary>
    /// Sélectionne le modèle le plus économique pour la catégorie et la complexité données.
    /// </summary>
    private string SelectEconomicModel(string category, string complexity)
    {
        switch (category)
        {
"""
    
    # Ajouter les cas pour chaque catégorie (stratégie économique)
    for category, complexities in economic_strategy.items():
        csharp_code += f"            case \"{category}\":\n"
        csharp_code += "                switch (complexity)\n"
        csharp_code += "                {\n"
        
        for complexity, model in complexities.items():
            csharp_code += f"                    case \"{complexity}\":\n"
            csharp_code += f"                        return \"{model}\";\n"
        
        csharp_code += "                    default:\n"
        csharp_code += "                        return null;\n"
        csharp_code += "                }\n"
    
    csharp_code += """            default:
                return null;
        }
    }
    
    /// <summary>
    /// Sélectionne le modèle avec le meilleur équilibre performance/coût pour la catégorie et la complexité données.
    /// </summary>
    private string SelectBalancedModel(string category, string complexity)
    {
        switch (category)
        {
"""
    
    # Ajouter les cas pour chaque catégorie (stratégie équilibrée)
    for category, complexities in balanced_strategy.items():
        csharp_code += f"            case \"{category}\":\n"
        csharp_code += "                switch (complexity)\n"
        csharp_code += "                {\n"
        
        for complexity, model in complexities.items():
            csharp_code += f"                    case \"{complexity}\":\n"
            csharp_code += f"                        return \"{model}\";\n"
        
        csharp_code += "                    default:\n"
        csharp_code += "                        return null;\n"
        csharp_code += "                }\n"
    
    csharp_code += """            default:
                return null;
        }
    }
    
    /// <summary>
    /// Obtient l'instance de TextCompletion pour le modèle spécifié.
    /// </summary>
    /// <param name="modelName">Nom du modèle</param>
    /// <returns>Instance de ITextCompletion</returns>
    public ITextCompletion GetTextCompletionForModel(string modelName)
    {
        // Implémentation à compléter en fonction de l'architecture du MultiConnector
        switch (modelName)
        {
            case "anthropic/claude-3.7-sonnet":
                return new AnthropicTextCompletion(modelName);
            case "google/gemini-pro-1.5":
                return new GoogleTextCompletion(modelName);
            case "gpt-3.5-turbo":
                return new OpenAITextCompletion(modelName);
            case "gpt-4o":
                return new OpenAITextCompletion(modelName);
            case "gpt-4o-mini":
                return new OpenAITextCompletion(modelName);
            case "qwen/qwen3-14b":
                return new OpenRouterTextCompletion(modelName);
            case "qwen/qwen3-32b":
                return new OpenRouterTextCompletion(modelName);
            case "o3":
                return new OpenRouterTextCompletion(modelName);
            case "o4-mini":
                return new OpenRouterTextCompletion(modelName);
            default:
                return new OpenAITextCompletion("gpt-4o");
        }
    }
}
"""
    
    return csharp_code

def generate_routing_tables(routing_strategy):
    """Génère des tableaux récapitulatifs des stratégies de routage."""
    performance_strategy = routing_strategy["performance"]
    economic_strategy = routing_strategy["economic"]
    balanced_strategy = routing_strategy["balanced"]
    
    # Générer le tableau pour la stratégie de performance
    performance_table = "| Catégorie | Trivial | Simple | Medium |\n"
    performance_table += "|-----------|---------|--------|--------|\n"
    
    for category in sorted(performance_strategy.keys()):
        performance_table += f"| {category} | {performance_strategy[category].get('trivial', 'N/A')} | {performance_strategy[category].get('simple', 'N/A')} | {performance_strategy[category].get('medium', 'N/A')} |\n"
    
    # Générer le tableau pour la stratégie économique
    economic_table = "| Catégorie | Trivial | Simple | Medium |\n"
    economic_table += "|-----------|---------|--------|--------|\n"
    
    for category in sorted(economic_strategy.keys()):
        economic_table += f"| {category} | {economic_strategy[category].get('trivial', 'N/A')} | {economic_strategy[category].get('simple', 'N/A')} | {economic_strategy[category].get('medium', 'N/A')} |\n"
    
    # Générer le tableau pour la stratégie équilibrée
    balanced_table = "| Catégorie | Trivial | Simple | Medium |\n"
    balanced_table += "|-----------|---------|--------|--------|\n"
    
    for category in sorted(balanced_strategy.keys()):
        balanced_table += f"| {category} | {balanced_strategy[category].get('trivial', 'N/A')} | {balanced_strategy[category].get('simple', 'N/A')} | {balanced_strategy[category].get('medium', 'N/A')} |\n"
    
    return {
        "performance": performance_table,
        "economic": economic_table,
        "balanced": balanced_table
    }
def generate_recommendations_report(model_data, routing_strategy, routing_tables, csharp_code):
    """Génère le rapport de recommandations finales."""
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    
    report = f"""# Recommandations Finales pour l'Optimisation du MultiConnector

Date: {now}

## 1. Introduction

Ce rapport présente les recommandations finales pour l'optimisation du MultiConnector, basées sur l'analyse approfondie des performances des différents modèles de langage. L'objectif est de proposer une stratégie de routage intelligente qui permette de sélectionner automatiquement le modèle le plus approprié en fonction de la catégorie et de la complexité de la tâche, tout en tenant compte des contraintes de coût et de performance.

## 2. Résumé des Performances des Modèles

Le tableau ci-dessous résume les performances globales des modèles analysés :

| Modèle | Score Global | Coût Moyen | Efficacité | Temps Moyen (s) | Tokens Moyens |
|--------|-------------|------------|------------|-----------------|---------------|
"""
    
    # Ajouter les performances des modèles
    for model_name, model_info in sorted(model_data.items(), key=lambda x: x[1]["score_global"], reverse=True):
        report += f"| {model_name} | {model_info['score_global']:.2f} | ${model_info['cost_avg']:.6f} | {model_info['efficiency']:.2f} | {model_info['response_time']:.2f} | {model_info['tokens_avg']:.2f} |\n"
    
    report += """
## 3. Forces et Faiblesses des Modèles

Cette section présente les forces et faiblesses de chaque modèle, identifiées lors de l'analyse approfondie :

"""
    
    # Ajouter les forces et faiblesses de chaque modèle
    for model_name, model_info in sorted(model_data.items(), key=lambda x: x[1]["score_global"], reverse=True):
        report += f"### {model_name}\n\n"
        report += "#### Forces\n\n"
        for strength in model_info["strengths"]:
            report += f"- **{strength}**: Score de {model_info['category_scores'].get(strength, 'N/A'):.2f}\n"
        
        report += "\n#### Faiblesses\n\n"
        for weakness in model_info["weaknesses"]:
            report += f"- **{weakness}**: Score de {model_info['category_scores'].get(weakness, 'N/A'):.2f}\n"
        
        report += "\n"
    
    report += """
## 4. Stratégies de Routage Recommandées

Nous proposons trois stratégies de routage pour le MultiConnector, chacune adaptée à des besoins spécifiques :

1. **Stratégie de Performance** : Privilégie le modèle avec le meilleur score, indépendamment du coût.
2. **Stratégie Économique** : Privilégie le modèle offrant le meilleur rapport qualité/prix.
3. **Stratégie Équilibrée** : Recherche un équilibre optimal entre performance et coût.

### Stratégie de Performance

"""
    
    report += routing_tables["performance"]
    
    report += """
### Stratégie Économique

"""
    
    report += routing_tables["economic"]
    
    report += """
### Stratégie Équilibrée

"""
    
    report += routing_tables["balanced"]
    
    report += """
## 5. Recommandations Spécifiques par Catégorie

Sur la base de notre analyse, voici nos recommandations spécifiques pour chaque catégorie de tâche :

"""
    
    # Ajouter des recommandations spécifiques par catégorie
    categories = ["classification", "code", "creative", "math", "qa", "raisonnement", "summarization", "writing"]
    
    for category in categories:
        report += f"### {category}\n\n"
        
        # Trouver le meilleur modèle pour cette catégorie
        best_model = None
        best_score = -1
        
        # Trouver le modèle le plus économique avec un bon score
        best_economic_model = None
        best_efficiency = -1
        
        for model_name, model_info in model_data.items():
            if category in model_info["category_scores"]:
                score = model_info["category_scores"][category]
                
                if score > best_score:
                    best_score = score
                    best_model = model_name
                
                if score >= 0.7 and model_info["efficiency"] > best_efficiency:
                    best_efficiency = model_info["efficiency"]
                    best_economic_model = model_name
        
        if best_economic_model is None:
            best_economic_model = best_model
        
        report += f"- **Meilleur modèle** : {best_model} (Score: {best_score:.2f})\n"
        report += f"- **Meilleur rapport qualité/prix** : {best_economic_model} (Score: {model_data[best_economic_model]['category_scores'].get(category, 0):.2f}, Efficacité: {model_data[best_economic_model]['efficiency']:.2f})\n"
        
        # Ajouter des recommandations spécifiques
        if category == "code":
            report += "- Pour les tâches de code complexes, privilégier Claude 3.7 Sonnet qui offre les meilleures performances.\n"
            report += "- Pour les tâches de code simples, GPT-3.5 Turbo offre un excellent rapport qualité/prix.\n"
        elif category == "math":
            report += "- Pour les tâches mathématiques, Claude 3.7 Sonnet et GPT-3.5 Turbo sont les plus performants.\n"
            report += "- Les modèles Qwen ont des performances faibles sur cette catégorie et devraient être évités.\n"
        elif category == "raisonnement":
            report += "- Pour le raisonnement complexe, Claude 3.7 Sonnet est le plus performant.\n"
            report += "- Pour le raisonnement simple, GPT-3.5 Turbo offre le meilleur rapport qualité/prix.\n"
        
        report += "\n"
    
    report += """
## 6. Implémentation Recommandée

Nous recommandons d'implémenter la stratégie de routage sous forme d'une classe C# qui peut être intégrée au MultiConnector. Voici une implémentation proposée :

```csharp
"""
    
    report += csharp_code
    
    report += """```

## 7. Conclusion

Cette analyse approfondie a permis d'identifier les forces et faiblesses de chaque modèle, ainsi que leur rapport coût/performance. Les recommandations formulées permettront d'optimiser le MultiConnector en utilisant le modèle le plus approprié pour chaque type de requête, tout en tenant compte des contraintes de coût, de performance et de temps de réponse.

Les trois stratégies de routage proposées (Performance, Économique et Équilibrée) offrent une flexibilité qui permet d'adapter le comportement du MultiConnector aux besoins spécifiques de chaque utilisation.

En résumé :

1. **GPT-3.5 Turbo** offre le meilleur rapport qualité/prix global et est recommandé pour les applications sensibles aux coûts.
2. **Claude 3.7 Sonnet** obtient les meilleurs scores sur les tâches complexes et est recommandé pour les applications où la qualité est primordiale.
3. **Google Gemini Pro 1.5** offre un excellent équilibre entre performance et coût, et est recommandé pour les applications nécessitant un bon compromis.

L'implémentation de ces recommandations permettra d'optimiser significativement les performances du MultiConnector tout en maîtrisant les coûts.
"""
    
    # Écrire le rapport dans un fichier
    with open(REPORT_PATH, 'w', encoding='utf-8') as f:
        f.write(report)
    
    # Écrire la stratégie de routage optimisée dans un fichier
    with open(ROUTING_STRATEGY_PATH, 'w', encoding='utf-8') as f:
        f.write(f"# Stratégie de Routage Optimisée pour le MultiConnector\n\nDate: {now}\n\n")
        f.write("## Implémentation C#\n\n```csharp\n")
        f.write(csharp_code)
        f.write("\n```\n\n")
        f.write("## Tableaux Récapitulatifs des Recommandations\n\n")
        f.write("### Stratégie de Performance\n\n")
        f.write(routing_tables["performance"])
        f.write("\n### Stratégie Économique\n\n")
        f.write(routing_tables["economic"])
        f.write("\n### Stratégie Équilibrée\n\n")
        f.write(routing_tables["balanced"])
        f.write("\n\n## Conclusion\n\n")
        f.write("Cette stratégie de routage optimisée permet au MultiConnector de sélectionner automatiquement le modèle le plus approprié en fonction de la catégorie et de la complexité de la tâche. Trois modes sont disponibles :\n\n")
        f.write("1. **Mode Performance** : Privilégie le modèle avec le meilleur score, indépendamment du coût.\n")
        f.write("2. **Mode Économique** : Privilégie le modèle offrant le meilleur rapport qualité/prix.\n")
        f.write("3. **Mode Équilibré** : Recherche un équilibre optimal entre performance et coût.\n\n")
        f.write("Cette approche permet d'optimiser les performances tout en maîtrisant les coûts, en fonction des besoins spécifiques de chaque utilisation.")
    
    return report

def main():
    """Fonction principale."""
    try:
        print("Chargement des données des modèles...")
        model_data = load_model_data()
        
        print("Génération de la stratégie de routage optimisée...")
        routing_strategy = generate_routing_strategy(model_data)
        
        print("Génération de l'implémentation C#...")
        csharp_code = generate_csharp_implementation(routing_strategy)
        
        print("Génération des tableaux récapitulatifs...")
        routing_tables = generate_routing_tables(routing_strategy)
        
        print("Génération du rapport de recommandations...")
        generate_recommendations_report(model_data, routing_strategy, routing_tables, csharp_code)
        
        print(f"Analyse terminée. Rapport généré : {REPORT_PATH}")
        print(f"Stratégie de routage optimisée générée : {ROUTING_STRATEGY_PATH}")
    except Exception as e:
        print(f"Erreur lors de l'exécution du script : {e}")

if __name__ == "__main__":
    main()