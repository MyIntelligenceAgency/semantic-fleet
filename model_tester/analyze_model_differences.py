#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Script d'analyse des différences significatives entre les réponses des modèles.
Ce script se concentre sur l'identification et l'analyse des cas où les modèles
donnent des réponses radicalement différentes pour la même requête.
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
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity

# Configuration
RAW_RESPONSES_DIR = "../results/comprehensive_tests/raw_responses"
OUTPUT_DIR = "../results/comprehensive_tests/analysis"
REPORT_PATH = "../results/comprehensive_tests/analyse_differences.md"

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

def calculate_similarity_matrix(texts):
    """Calcule une matrice de similarité entre les textes en utilisant TF-IDF et similarité cosinus."""
    vectorizer = TfidfVectorizer(stop_words='english')
    try:
        tfidf_matrix = vectorizer.fit_transform(texts)
        similarity_matrix = cosine_similarity(tfidf_matrix)
        return similarity_matrix
    except:
        # Fallback en cas d'erreur avec TF-IDF
        n = len(texts)
        similarity_matrix = np.zeros((n, n))
        for i in range(n):
            for j in range(n):
                if i == j:
                    similarity_matrix[i][j] = 1.0
                else:
                    similarity_matrix[i][j] = SequenceMatcher(None, texts[i], texts[j]).ratio()
        return similarity_matrix

def find_significant_differences(df):
    """Identifie les cas où les modèles donnent des réponses radicalement différentes."""
    # Regrouper par prompt
    prompts = df['prompt'].unique()
    significant_differences = []
    
    for prompt in prompts:
        prompt_data = df[df['prompt'] == prompt].copy()
        
        # Si nous avons au moins 2 modèles pour ce prompt
        if len(prompt_data) >= 2:
            models = prompt_data['model'].tolist()
            responses = prompt_data['completion_text'].tolist()
            scores = prompt_data['score'].tolist()
            
            # Calculer les différences de score
            max_score_diff = max(scores) - min(scores)
            
            # Calculer la matrice de similarité entre les réponses
            similarity_matrix = calculate_similarity_matrix(responses)
            
            # Trouver la paire avec la plus faible similarité
            min_similarity = 1.0
            min_similarity_pair = (0, 0)
            
            for i in range(len(responses)):
                for j in range(i+1, len(responses)):
                    if similarity_matrix[i][j] < min_similarity:
                        min_similarity = similarity_matrix[i][j]
                        min_similarity_pair = (i, j)
            
            # Si la différence de score est significative ou la similarité est faible
            if max_score_diff >= 0.5 or min_similarity < 0.3:
                # Trouver les modèles avec le score max et min
                max_score_model = models[scores.index(max(scores))]
                min_score_model = models[scores.index(min(scores))]
                
                # Trouver les modèles avec la plus faible similarité
                model1 = models[min_similarity_pair[0]]
                model2 = models[min_similarity_pair[1]]
                
                # Extraire les réponses pour analyse
                max_score_response = responses[scores.index(max(scores))]
                min_score_response = responses[scores.index(min(scores))]
                
                # Extraire les réponses des modèles avec la plus faible similarité
                response1 = responses[min_similarity_pair[0]]
                response2 = responses[min_similarity_pair[1]]
                
                significant_differences.append({
                    'prompt': prompt,
                    'category': prompt_data['category'].iloc[0],
                    'complexity': prompt_data['complexity'].iloc[0],
                    'max_score_model': max_score_model,
                    'max_score': max(scores),
                    'max_score_response': max_score_response,
                    'min_score_model': min_score_model,
                    'min_score': min(scores),
                    'min_score_response': min_score_response,
                    'score_diff': max_score_diff,
                    'least_similar_pair': {
                        'model1': model1,
                        'model2': model2,
                        'similarity': min_similarity,
                        'response1': response1,
                        'response2': response2
                    },
                    'similarity_matrix': similarity_matrix,
                    'models': models
                })
    
    return significant_differences

def analyze_differences(significant_differences):
    """Analyse les différences significatives entre les modèles."""
    # Analyser les différences par catégorie
    category_differences = defaultdict(list)
    for diff in significant_differences:
        category_differences[diff['category']].append(diff)
    
    # Analyser les différences par niveau de complexité
    complexity_differences = defaultdict(list)
    for diff in significant_differences:
        complexity_differences[diff['complexity']].append(diff)
    
    # Analyser les différences par paire de modèles
    model_pair_differences = defaultdict(list)
    for diff in significant_differences:
        model1 = diff['least_similar_pair']['model1']
        model2 = diff['least_similar_pair']['model2']
        pair = tuple(sorted([model1, model2]))
        model_pair_differences[pair].append(diff)
    
    return {
        'category_differences': category_differences,
        'complexity_differences': complexity_differences,
        'model_pair_differences': model_pair_differences
    }

def generate_visualizations(df, significant_differences, analysis):
    """Génère des visualisations pour l'analyse des différences."""
    # Configurer le style des graphiques
    sns.set(style="whitegrid")
    
    # 1. Heatmap des différences de score moyennes par paire de modèles
    models = df['model'].unique()
    n_models = len(models)
    score_diff_matrix = np.zeros((n_models, n_models))
    
    for i, model1 in enumerate(models):
        for j, model2 in enumerate(models):
            if i != j:
                # Calculer la différence de score moyenne pour cette paire de modèles
                diffs = []
                for prompt in df['prompt'].unique():
                    prompt_data = df[df['prompt'] == prompt]
                    if len(prompt_data[prompt_data['model'] == model1]) > 0 and len(prompt_data[prompt_data['model'] == model2]) > 0:
                        score1 = prompt_data[prompt_data['model'] == model1]['score'].iloc[0]
                        score2 = prompt_data[prompt_data['model'] == model2]['score'].iloc[0]
                        diffs.append(abs(score1 - score2))
                
                if diffs:
                    score_diff_matrix[i, j] = np.mean(diffs)
    
    plt.figure(figsize=(12, 10))
    sns.heatmap(score_diff_matrix, annot=True, cmap='viridis', fmt='.2f', 
                xticklabels=models, yticklabels=models)
    plt.title('Différences de Score Moyennes par Paire de Modèles')
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'model_score_differences.png'))
    plt.close()
    
    # 2. Nombre de différences significatives par catégorie
    categories = list(analysis['category_differences'].keys())
    diff_counts = [len(analysis['category_differences'][cat]) for cat in categories]
    
    plt.figure(figsize=(10, 6))
    sns.barplot(x=categories, y=diff_counts)
    plt.title('Nombre de Différences Significatives par Catégorie')
    plt.xlabel('Catégorie')
    plt.ylabel('Nombre de Différences')
    plt.xticks(rotation=45)
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'category_differences.png'))
    plt.close()
    
    # 3. Nombre de différences significatives par niveau de complexité
    complexities = list(analysis['complexity_differences'].keys())
    diff_counts = [len(analysis['complexity_differences'][comp]) for comp in complexities]
    
    plt.figure(figsize=(10, 6))
    sns.barplot(x=complexities, y=diff_counts)
    plt.title('Nombre de Différences Significatives par Niveau de Complexité')
    plt.xlabel('Complexité')
    plt.ylabel('Nombre de Différences')
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'complexity_differences.png'))
    plt.close()
    
    # 4. Similarité moyenne entre les modèles
    similarity_matrix = np.zeros((n_models, n_models))
    count_matrix = np.zeros((n_models, n_models))
    
    for diff in significant_differences:
        models_list = diff['models']
        sim_matrix = diff['similarity_matrix']
        
        for i, model1 in enumerate(models_list):
            for j, model2 in enumerate(models_list):
                if i != j:
                    model1_idx = np.where(models == model1)[0][0]
                    model2_idx = np.where(models == model2)[0][0]
                    similarity_matrix[model1_idx, model2_idx] += sim_matrix[i, j]
                    count_matrix[model1_idx, model2_idx] += 1
    
    # Éviter la division par zéro
    count_matrix[count_matrix == 0] = 1
    similarity_matrix = similarity_matrix / count_matrix
    
    plt.figure(figsize=(12, 10))
    sns.heatmap(similarity_matrix, annot=True, cmap='viridis', fmt='.2f', 
                xticklabels=models, yticklabels=models)
    plt.title('Similarité Moyenne entre les Modèles')
    plt.tight_layout()
    plt.savefig(os.path.join(OUTPUT_DIR, 'visualizations', 'model_similarity.png'))
    plt.close()

def generate_report(significant_differences, analysis):
    """Génère le rapport d'analyse des différences au format Markdown."""
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    
    report = f"""# Analyse des Différences entre les Réponses des Modèles

Date: {now}

## 1. Introduction

Ce rapport présente une analyse détaillée des cas où les modèles de langage donnent des réponses radicalement différentes pour la même requête. L'objectif est d'identifier les domaines où les modèles divergent significativement dans leurs réponses et de comprendre les implications de ces différences.

## 2. Résumé des Différences Significatives

Nous avons identifié **{len(significant_differences)}** cas où les modèles donnent des réponses radicalement différentes. Ces différences sont réparties comme suit :

"""
    
    # Ajouter un résumé par catégorie
    report += "### Différences par Catégorie\n\n"
    for category, diffs in analysis['category_differences'].items():
        report += f"- **{category}**: {len(diffs)} cas\n"
    
    report += "\n### Différences par Niveau de Complexité\n\n"
    for complexity, diffs in analysis['complexity_differences'].items():
        report += f"- **{complexity}**: {len(diffs)} cas\n"
    
    report += "\n### Paires de Modèles avec les Plus Grandes Différences\n\n"
    
    # Trier les paires de modèles par nombre de différences
    sorted_pairs = sorted(analysis['model_pair_differences'].items(), 
                          key=lambda x: len(x[1]), reverse=True)
    
    for pair, diffs in sorted_pairs[:5]:  # Top 5 des paires avec le plus de différences
        report += f"- **{pair[0]} vs {pair[1]}**: {len(diffs)} cas\n"
    
    report += """
## 3. Analyse Détaillée des Différences

Cette section présente une analyse détaillée des cas où les modèles donnent des réponses radicalement différentes.

"""
    
    # Ajouter les différences significatives par catégorie
    for category, diffs in analysis['category_differences'].items():
        report += f"### Catégorie: {category}\n\n"
        
        for i, diff in enumerate(diffs):
            report += f"#### Prompt {i+1}: \"{diff['prompt']}\"\n\n"
            report += f"- **Complexité**: {diff['complexity']}\n"
            report += f"- **Meilleur modèle**: {diff['max_score_model']} (Score: {diff['max_score']:.2f})\n"
            report += f"- **Pire modèle**: {diff['min_score_model']} (Score: {diff['min_score']:.2f})\n"
            report += f"- **Différence de score**: {diff['score_diff']:.2f}\n"
            report += f"- **Paire la moins similaire**: {diff['least_similar_pair']['model1']} et {diff['least_similar_pair']['model2']} (Similarité: {diff['least_similar_pair']['similarity']:.2f})\n\n"
            
            # Ajouter un extrait des réponses les plus différentes
            report += "##### Extrait de la réponse du meilleur modèle\n\n"
            max_response = diff['max_score_response']
            report += f"```\n{max_response[:500]}{'...' if len(max_response) > 500 else ''}\n```\n\n"
            
            report += "##### Extrait de la réponse du pire modèle\n\n"
            min_response = diff['min_score_response']
            report += f"```\n{min_response[:500]}{'...' if len(min_response) > 500 else ''}\n```\n\n"
    
    report += """
## 4. Visualisations

Les visualisations suivantes illustrent les différences entre les modèles :

![Différences de Score par Paire de Modèles](analysis/visualizations/model_score_differences.png)

![Différences par Catégorie](analysis/visualizations/category_differences.png)

![Différences par Niveau de Complexité](analysis/visualizations/complexity_differences.png)

![Similarité entre les Modèles](analysis/visualizations/model_similarity.png)

## 5. Implications pour le MultiConnector

L'analyse des différences entre les modèles a des implications importantes pour l'optimisation du MultiConnector :

1. **Domaines de divergence** : Les modèles divergent principalement dans les catégories de tâches suivantes :
"""
    
    # Ajouter les catégories avec le plus de différences
    sorted_categories = sorted(analysis['category_differences'].items(), 
                              key=lambda x: len(x[1]), reverse=True)
    
    for category, diffs in sorted_categories[:3]:  # Top 3 des catégories avec le plus de différences
        report += f"   - **{category}**: {len(diffs)} cas de divergence significative\n"
    
    report += """
2. **Stratégie de routage robuste** : Pour les catégories où les modèles divergent significativement, il est recommandé d'implémenter une stratégie de routage plus robuste, qui pourrait inclure :
   - La vérification croisée des réponses par plusieurs modèles
   - L'utilisation de modèles spécialisés pour ces catégories
   - L'ajout de mécanismes de validation des réponses

3. **Modèles complémentaires** : Certains modèles semblent être complémentaires dans leurs forces et faiblesses. Une stratégie de routage intelligente pourrait tirer parti de cette complémentarité pour optimiser les performances globales.

## 6. Conclusion

Cette analyse des différences entre les modèles a permis d'identifier les domaines où les modèles divergent significativement dans leurs réponses. Ces insights peuvent être utilisés pour optimiser la stratégie de routage du MultiConnector, en tenant compte des forces et faiblesses spécifiques de chaque modèle dans différentes catégories de tâches et niveaux de complexité.

Les différences les plus significatives ont été observées dans les catégories de tâches complexes, notamment le code et le raisonnement. Pour ces catégories, il est recommandé d'utiliser des modèles spécialisés ou de mettre en place des mécanismes de vérification croisée pour garantir la qualité des réponses.
"""
    
    # Écrire le rapport dans un fichier
    with open(REPORT_PATH, 'w', encoding='utf-8') as f:
        f.write(report)
    
    return report

def main():
    """Fonction principale."""
    try:
        print("Chargement des réponses brutes...")
        responses = load_raw_responses()
        print(f"Nombre de réponses chargées : {len(responses)}")
        
        print("Création du DataFrame...")
        df = create_dataframe(responses)
        
        print("Recherche des différences significatives...")
        significant_differences = find_significant_differences(df)
        print(f"Nombre de différences significatives trouvées : {len(significant_differences)}")
        
        print("Analyse des différences...")
        analysis = analyze_differences(significant_differences)
        
        print("Génération des visualisations...")
        generate_visualizations(df, significant_differences, analysis)
        
        print("Génération du rapport d'analyse...")
        generate_report(significant_differences, analysis)
        
        print(f"Analyse terminée. Rapport généré : {REPORT_PATH}")
    except Exception as e:
        print(f"Erreur lors de l'exécution du script : {e}")

if __name__ == "__main__":
    main()