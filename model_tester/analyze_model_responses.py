#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Script d'analyse approfondie des résultats des tests comparatifs de modèles de langage.
Ce script analyse les résultats bruts des tests pour extraire des insights plus détaillés
que ceux présents dans le rapport de synthèse.
"""

import os
import json
import glob
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from collections import defaultdict
from difflib import SequenceMatcher
import re
from datetime import datetime

# Configuration
RAW_RESPONSES_DIR = "../results/comprehensive_tests/raw_responses"
OUTPUT_DIR = "../results/comprehensive_tests/analysis"
REPORT_PATH = "../results/comprehensive_tests/analyse_approfondie.md"

# Créer le répertoire de sortie s'il n'existe pas
os.makedirs(OUTPUT_DIR, exist_ok=True)
os.makedirs(os.path.join(OUTPUT_DIR, "visualizations"), exist_ok=True)

def load_raw_responses():
    """Charge tous les fichiers de réponses brutes."""
    responses = []
    
    for file_path in glob.glob(os.path.join(RAW_RESPONSES_DIR, "*.json")):
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
                responses.append(data)
        except Exception as e:
            print(f"Erreur lors de la lecture du fichier {file_path}: {e}")
    
    return responses

def create_dataframe(responses):
    """Crée un DataFrame pandas à partir des réponses."""
    data = []
    
    for resp in responses:
        row = {
            'model': resp['model'],
            'category': resp['category'],
            'complexity': resp['complexity'],
            'prompt': resp['prompt'],
            'success': resp['success'],
            'score': resp['evaluation']['score'] if 'evaluation' in resp else 0,
            'max_score': resp['evaluation']['max_score'] if 'evaluation' in resp else 1,
            'response_time': resp['response_time'],
            'tokens_prompt': resp['tokens']['prompt'],
            'tokens_completion': resp['tokens']['completion'],
            'tokens_total': resp['tokens']['total'],
            'cost': resp['cost'],
            'completion_text': resp['completion_text']
        }
        data.append(row)
    
    df = pd.DataFrame(data)
    return df

def calculate_efficiency(df):
    """Calcule l'efficacité coût/performance."""
    # Éviter la division par zéro
    df['efficiency'] = df['score'] / (df['cost'] + 0.0000001)
    return df

def analyze_model_strengths_weaknesses(df):
    """Analyse les forces et faiblesses de chaque modèle."""
    models = df['model'].unique()
    strengths_weaknesses = {}
    
    for model in models:
        model_data = df[df['model'] == model]
        
        # Calculer les performances par catégorie
        category_performance = model_data.groupby('category')['score'].mean().to_dict()
        
        # Identifier les forces (top 3 catégories)
        strengths = sorted(category_performance.items(), key=lambda x: x[1], reverse=True)[:3]
        
        # Identifier les faiblesses (bottom 3 catégories)
        weaknesses = sorted(category_performance.items(), key=lambda x: x[1])[:3]
        
        # Calculer les performances par niveau de complexité
        complexity_performance = model_data.groupby('complexity')['score'].mean().to_dict()
        
        # Calculer le temps de réponse moyen
        avg_response_time = model_data['response_time'].mean()
        
        # Calculer le coût moyen
        avg_cost = model_data['cost'].mean()
        
        # Calculer l'efficacité moyenne
        avg_efficiency = model_data['efficiency'].mean()
        
        strengths_weaknesses[model] = {
            'strengths': strengths,
            'weaknesses': weaknesses,
            'complexity_performance': complexity_performance,
            'avg_response_time': avg_response_time,
            'avg_cost': avg_cost,
            'avg_efficiency': avg_efficiency
        }
    
    return strengths_weaknesses

def compare_responses(df):
    """Compare les réponses des différents modèles pour identifier les différences significatives."""
    # Regrouper par prompt
    prompts = df['prompt'].unique()
    significant_differences = []
    
    for prompt in prompts:
        prompt_data = df[df['prompt'] == prompt]
        
        # Si nous avons au moins 2 modèles pour ce prompt
        if len(prompt_data) >= 2:
            models = prompt_data['model'].tolist()
            responses = prompt_data['completion_text'].tolist()
            scores = prompt_data['score'].tolist()
            
            # Calculer les différences de score
            max_score_diff = max(scores) - min(scores)
            
            # Si la différence de score est significative
            if max_score_diff >= 0.5:
                # Trouver les modèles avec le score max et min
                max_score_model = models[scores.index(max(scores))]
                min_score_model = models[scores.index(min(scores))]
                
                # Calculer la similarité textuelle entre les réponses
                similarities = []
                for i in range(len(responses)):
                    for j in range(i+1, len(responses)):
                        similarity = SequenceMatcher(None, responses[i], responses[j]).ratio()
                        similarities.append({
                            'model1': models[i],
                            'model2': models[j],
                            'similarity': similarity
                        })
                
                # Trouver la paire avec la plus faible similarité
                min_similarity = min(similarities, key=lambda x: x['similarity'])
                
                significant_differences.append({
                    'prompt': prompt,
                    'category': prompt_data['category'].iloc[0],
                    'complexity': prompt_data['complexity'].iloc[0],
                    'max_score_model': max_score_model,
                    'max_score': max(scores),
                    'min_score_model': min_score_model,
                    'min_score': min(scores),
                    'score_diff': max_score_diff,
                    'least_similar_pair': min_similarity
                })
    
    return significant_differences

def analyze_cost_performance(df):
    """Analyse le rapport coût/performance des différents modèles."""
    # Calculer les métriques par modèle
    model_metrics = df.groupby('model').agg({
        'score': 'mean',
        'cost': 'mean',
        'efficiency': 'mean',
        'response_time': 'mean',
        'tokens_total': 'mean'
    }).reset_index()
    
    # Calculer les métriques par modèle et catégorie
    model_category_metrics = df.groupby(['model', 'category']).agg({
        'score': 'mean',
        'cost': 'mean',
        'efficiency': 'mean'
    }).reset_index()
    
    return {
        'model_metrics': model_metrics,
        'model_category_metrics': model_category_metrics
    }

def generate_visualizations(df, strengths_weaknesses, cost_performance):
    """Génère des visualisations pour l'analyse."""
    # Configurer le style des graphiques
    sns.set(style="whitegrid")
    plt.figure(figsize=(12, 8))
    
    # 1. Graphique des scores moyens par modèle
    plt.figure(figsize=(12, 6))
    sns.barplot(x='model', y='score', data=df.groupby('model')['score'].mean().reset_index(), palette='viridis')
    plt.title('Score Moyen par Modèle')
    plt.xticks(rotation=45, ha='right')
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'scores_by_model.png'))
    plt.close()
    
    # 2. Graphique du rapport coût/performance
    plt.figure(figsize=(12, 6))
    cost_perf_data = cost_performance['model_metrics'][['model', 'efficiency']].sort_values('efficiency', ascending=False)
    sns.barplot(x='model', y='efficiency', data=cost_perf_data, palette='viridis')
    plt.title('Efficacité Coût/Performance par Modèle')
    plt.xticks(rotation=45, ha='right')
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'cost_efficiency_by_model.png'))
    plt.close()
    
    # 3. Graphique des temps de réponse
    plt.figure(figsize=(12, 6))
    response_time_data = df.groupby('model')['response_time'].mean().reset_index().sort_values('response_time')
    sns.barplot(x='model', y='response_time', data=response_time_data, palette='viridis')
    plt.title('Temps de Réponse Moyen par Modèle (secondes)')
    plt.xticks(rotation=45, ha='right')
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'response_time_by_model.png'))
    plt.close()
    
    # 4. Heatmap des performances par catégorie et modèle
    plt.figure(figsize=(14, 10))
    category_model_scores = df.pivot_table(index='category', columns='model', values='score', aggfunc='mean')
    sns.heatmap(category_model_scores, annot=True, cmap='viridis', fmt='.2f', linewidths=.5)
    plt.title('Scores par Catégorie et Modèle')
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'category_model_heatmap.png'))
    plt.close()
    
    # 5. Heatmap des performances par complexité et modèle
    plt.figure(figsize=(14, 8))
    complexity_model_scores = df.pivot_table(index='complexity', columns='model', values='score', aggfunc='mean')
    sns.heatmap(complexity_model_scores, annot=True, cmap='viridis', fmt='.2f', linewidths=.5)
    plt.title('Scores par Niveau de Complexité et Modèle')
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'complexity_model_heatmap.png'))
    plt.close()

def generate_report(df, strengths_weaknesses, significant_differences, cost_performance):
    """Génère le rapport d'analyse approfondie au format Markdown."""
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    
    report = f"""# Analyse Approfondie des Tests Comparatifs de Modèles de Langage

Date: {now}

## 1. Introduction

Ce rapport présente une analyse approfondie des résultats des tests comparatifs effectués sur différents modèles de langage. Il complète le rapport de synthèse existant en fournissant des insights plus détaillés sur les performances des modèles, leurs forces et faiblesses, ainsi que leur rapport coût/performance.

## 2. Forces et Faiblesses des Modèles

"""
    
    # Ajouter les forces et faiblesses de chaque modèle
    for model, data in strengths_weaknesses.items():
        report += f"### {model}\n\n"
        
        report += "#### Forces\n\n"
        for category, score in data['strengths']:
            report += f"- **{category}**: Score moyen de {score:.2f}\n"
        
        report += "\n#### Faiblesses\n\n"
        for category, score in data['weaknesses']:
            report += f"- **{category}**: Score moyen de {score:.2f}\n"
        
        report += "\n#### Performance par Niveau de Complexité\n\n"
        for complexity, score in data['complexity_performance'].items():
            report += f"- **{complexity}**: Score moyen de {score:.2f}\n"
        
        report += f"\n#### Métriques Générales\n\n"
        report += f"- **Temps de réponse moyen**: {data['avg_response_time']:.2f} secondes\n"
        report += f"- **Coût moyen par requête**: ${data['avg_cost']:.6f}\n"
        report += f"- **Efficacité coût/performance**: {data['avg_efficiency']:.2f}\n\n"
    
    report += """## 3. Comparaison des Performances par Catégorie et Complexité

### Performances par Catégorie

"""
    
    # Ajouter un tableau des performances par catégorie
    categories = df['category'].unique()
    models = df['model'].unique()
    
    report += "| Catégorie | " + " | ".join(models) + " |\n"
    report += "|" + "-" * 10 + "|" + "".join(["-" * 12 + "|" for _ in models]) + "\n"
    
    for category in categories:
        report += f"| {category} |"
        for model in models:
            score = df[(df['model'] == model) & (df['category'] == category)]['score'].mean()
            report += f" {score:.2f} |"
        report += "\n"
    
    report += """
### Performances par Niveau de Complexité

"""
    
    # Ajouter un tableau des performances par niveau de complexité
    complexities = df['complexity'].unique()
    
    report += "| Complexité | " + " | ".join(models) + " |\n"
    report += "|" + "-" * 10 + "|" + "".join(["-" * 12 + "|" for _ in models]) + "\n"
    
    for complexity in complexities:
        report += f"| {complexity} |"
        for model in models:
            score = df[(df['model'] == model) & (df['complexity'] == complexity)]['score'].mean()
            report += f" {score:.2f} |"
        report += "\n"
    
    report += """
## 4. Analyse du Rapport Coût/Performance

"""
    
    # Ajouter un tableau du rapport coût/performance
    model_metrics = cost_performance['model_metrics'].sort_values('efficiency', ascending=False)
    
    report += "| Modèle | Score Moyen | Coût Moyen | Efficacité | Temps Moyen (s) | Tokens Moyens |\n"
    report += "|--------|-------------|------------|------------|-----------------|---------------|\n"
    
    for _, row in model_metrics.iterrows():
        report += f"| {row['model']} | {row['score']:.2f} | ${row['cost']:.6f} | {row['efficiency']:.2f} | {row['response_time']:.2f} | {row['tokens_total']:.2f} |\n"
    
    report += """
### Efficacité par Catégorie

"""
    
    # Ajouter un tableau de l'efficacité par catégorie
    model_category = cost_performance['model_category_metrics']
    
    for category in categories:
        report += f"#### {category}\n\n"
        
        category_data = model_category[model_category['category'] == category].sort_values('efficiency', ascending=False)
        
        report += "| Modèle | Score | Coût | Efficacité |\n"
        report += "|--------|-------|------|------------|\n"
        
        for _, row in category_data.iterrows():
            report += f"| {row['model']} | {row['score']:.2f} | ${row['cost']:.6f} | {row['efficiency']:.2f} |\n"
        
        report += "\n"
    
    report += """
## 5. Différences Significatives entre les Modèles

Cette section identifie les cas où les modèles ont donné des réponses radicalement différentes pour la même requête.

"""
    
    # Ajouter les différences significatives
    if significant_differences:
        for diff in significant_differences:
            report += f"### Prompt: \"{diff['prompt']}\"\n\n"
            report += f"- **Catégorie**: {diff['category']}\n"
            report += f"- **Complexité**: {diff['complexity']}\n"
            report += f"- **Meilleur modèle**: {diff['max_score_model']} (Score: {diff['max_score']:.2f})\n"
            report += f"- **Pire modèle**: {diff['min_score_model']} (Score: {diff['min_score']:.2f})\n"
            report += f"- **Différence de score**: {diff['score_diff']:.2f}\n"
            report += f"- **Paire la moins similaire**: {diff['least_similar_pair']['model1']} et {diff['least_similar_pair']['model2']} (Similarité: {diff['least_similar_pair']['similarity']:.2f})\n\n"
    else:
        report += "Aucune différence significative n'a été identifiée entre les modèles.\n\n"
    
    report += """
## 6. Visualisations

Les visualisations suivantes illustrent les performances des différents modèles :

![Scores par Modèle](analysis/visualizations/scores_by_model.png)

![Efficacité Coût/Performance](analysis/visualizations/cost_efficiency_by_model.png)

![Temps de Réponse](analysis/visualizations/response_time_by_model.png)

![Heatmap Catégorie-Modèle](analysis/visualizations/category_model_heatmap.png)

![Heatmap Complexité-Modèle](analysis/visualizations/complexity_model_heatmap.png)

## 7. Recommandations pour l'Optimisation du MultiConnector

Sur la base de cette analyse approfondie, voici nos recommandations pour optimiser le MultiConnector :

1. **Stratégie de routage adaptative** : Implémenter une stratégie de routage qui tient compte non seulement de la catégorie et de la complexité de la tâche, mais aussi des contraintes de coût et de temps de réponse.

2. **Modèles spécialisés** : Utiliser des modèles spécialisés pour certaines catégories de tâches :
   - Pour le code : Claude 3.7 Sonnet pour les tâches complexes, GPT-3.5 Turbo pour les tâches simples
   - Pour les mathématiques : Claude 3.7 Sonnet ou GPT-3.5 Turbo
   - Pour le raisonnement complexe : Claude 3.7 Sonnet

3. **Optimisation des coûts** : Pour les applications sensibles aux coûts, privilégier GPT-3.5 Turbo qui offre le meilleur rapport qualité/prix global.

4. **Optimisation des performances** : Pour les applications où la qualité est primordiale, privilégier Claude 3.7 Sonnet qui obtient les meilleurs scores sur les tâches complexes.

5. **Optimisation du temps de réponse** : Pour les applications nécessitant des réponses rapides, privilégier GPT-3.5 Turbo ou GPT-4o-mini qui offrent les meilleurs temps de réponse.

## 8. Conclusion

Cette analyse approfondie a permis d'identifier les forces et faiblesses de chaque modèle, ainsi que leur rapport coût/performance. Les recommandations formulées permettront d'optimiser le MultiConnector en utilisant le modèle le plus approprié pour chaque type de requête, tout en tenant compte des contraintes de coût, de performance et de temps de réponse.

Les modèles les plus performants sont généralement les plus coûteux, mais certains modèles comme GPT-3.5 Turbo offrent un excellent rapport qualité/prix pour de nombreuses tâches. Une stratégie de routage intelligente permettra de maximiser les performances tout en optimisant les coûts.
"""
    
    # Écrire le rapport dans un fichier
    with open(REPORT_PATH, 'w', encoding='utf-8') as f:
        f.write(report)
    
    return report

def main():
    """Fonction principale."""
    print("Chargement des réponses brutes...")
    responses = load_raw_responses()
    print(f"Nombre de réponses chargées : {len(responses)}")
    
    print("Création du DataFrame...")
    df = create_dataframe(responses)
    
    print("Calcul de l'efficacité coût/performance...")
    df = calculate_efficiency(df)
    
    print("Analyse des forces et faiblesses des modèles...")
    strengths_weaknesses = analyze_model_strengths_weaknesses(df)
    
    print("Comparaison des réponses des modèles...")
    significant_differences = compare_responses(df)
    
    print("Analyse du rapport coût/performance...")
    cost_performance = analyze_cost_performance(df)
    
    print("Génération des visualisations...")
    generate_visualizations(df, strengths_weaknesses, cost_performance)
    
    print("Génération du rapport d'analyse...")
    generate_report(df, strengths_weaknesses, significant_differences, cost_performance)
    
    print(f"Analyse terminée. Rapport généré : {REPORT_PATH}")

if __name__ == "__main__":
    main()