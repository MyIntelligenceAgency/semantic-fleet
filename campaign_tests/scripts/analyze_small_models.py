#!/usr/bin/env python3
"""
Script pour analyser les résultats des tests des modèles plus petits avec le MultiConnector.
Ce script traite les logs d'instrumentation et génère un rapport détaillé sur les performances
des différents modèles plus petits avec les fonctions Semantic Kernel.
"""

import argparse
import json
import os
import re
import sys
from collections import defaultdict
from datetime import datetime
from typing import Dict, List, Tuple, Any, Optional

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from tabulate import tabulate


class SmallModelAnalyzer:
    """Analyseur de résultats de tests pour les modèles plus petits."""

    def __init__(self, log_dir: str, output_dir: str, small_models_file: str):
        """
        Initialise l'analyseur de résultats.
        
        Args:
            log_dir: Répertoire contenant les logs d'instrumentation
            output_dir: Répertoire de sortie pour les rapports
            small_models_file: Fichier JSON contenant les informations sur les modèles plus petits
        """
        self.log_dir = log_dir
        self.output_dir = output_dir
        self.small_models_file = small_models_file
        self.results = []
        self.model_performance = defaultdict(lambda: defaultdict(list))
        self.function_performance = defaultdict(lambda: defaultdict(list))
        self.complexity_performance = defaultdict(lambda: defaultdict(list))
        self.small_models_info = {}
        
        # Créer le répertoire de sortie s'il n'existe pas
        os.makedirs(output_dir, exist_ok=True)
        
        # Charger les informations sur les modèles plus petits
        self._load_small_models_info()

    def _load_small_models_info(self):
        """Charge les informations sur les modèles plus petits depuis le fichier JSON."""
        try:
            with open(self.small_models_file, 'r', encoding='utf-8-sig') as f:
                models = json.load(f)
                
            for model in models:
                self.small_models_info[model['name']] = model
                
            print(f"Informations sur {len(self.small_models_info)} modèles plus petits chargées.")
        except Exception as e:
            print(f"Erreur lors du chargement des informations sur les modèles plus petits: {e}")
            sys.exit(1)

    def load_results(self):
        """Charge les résultats des logs d'instrumentation."""
        print(f"Chargement des résultats depuis {self.log_dir}...")
        
        # Parcourir tous les fichiers de log
        for filename in os.listdir(self.log_dir):
            if not filename.endswith('.json'):
                continue
                
            filepath = os.path.join(self.log_dir, filename)
            print(f"Traitement du fichier {filepath}")
            
            try:
                with open(filepath, 'r', encoding='utf-8-sig') as f:
                    data = json.load(f)
                    
                # Extraire les informations pertinentes
                for test_result in data.get('testResults', []):
                    self._process_test_result(test_result)
            except Exception as e:
                print(f"Erreur lors du traitement du fichier {filepath}: {e}")
        
        print(f"Chargement terminé. {len(self.results)} résultats de test chargés.")

    def _process_test_result(self, test_result):
        """
        Traite un résultat de test individuel.
        
        Args:
            test_result: Résultat de test à traiter
        """
        # Extraire les informations de base
        skill_name = test_result.get('skillName', 'Unknown')
        function_name = test_result.get('functionName', 'Unknown')
        complexity = test_result.get('complexity', 'Unknown')
        
        # Extraire les performances des modèles
        primary_model = test_result.get('primaryModel', {})
        secondary_models = test_result.get('secondaryModels', {})
        
        # Créer une entrée de résultat
        result = {
            'skillName': skill_name,
            'functionName': function_name,
            'complexity': complexity,
            'primaryModel': primary_model.get('name', 'Unknown'),
            'primarySuccess': primary_model.get('success', False),
            'primaryExecutionTime': primary_model.get('executionTime', 0),
            'primaryTokenCount': primary_model.get('tokenCount', 0),
            'primaryCost': primary_model.get('cost', 0),
            'secondaryResults': []
        }
        
        # Ajouter les résultats des modèles secondaires
        for model_name, model_result in secondary_models.items():
            # Vérifier si le modèle est dans notre liste de modèles plus petits
            if model_name in self.small_models_info:
                secondary_result = {
                    'modelName': model_name,
                    'modelSize': self.small_models_info[model_name]['size'],
                    'success': model_result.get('success', False),
                    'executionTime': model_result.get('executionTime', 0),
                    'tokenCount': model_result.get('tokenCount', 0),
                    'cost': model_result.get('cost', 0)
                }
                result['secondaryResults'].append(secondary_result)
                
                # Mettre à jour les performances du modèle
                self.model_performance[model_name]['success'].append(secondary_result['success'])
                self.model_performance[model_name]['executionTime'].append(secondary_result['executionTime'])
                self.model_performance[model_name]['tokenCount'].append(secondary_result['tokenCount'])
                self.model_performance[model_name]['cost'].append(secondary_result['cost'])
                
                # Mettre à jour les performances par fonction
                function_key = f"{skill_name}.{function_name}"
                self.function_performance[function_key][model_name].append(secondary_result['success'])
                
                # Mettre à jour les performances par niveau de complexité
                self.complexity_performance[complexity][model_name].append(secondary_result['success'])
        
        # Mettre à jour les performances du modèle primaire
        self.model_performance[result['primaryModel']]['success'].append(result['primarySuccess'])
        self.model_performance[result['primaryModel']]['executionTime'].append(result['primaryExecutionTime'])
        self.model_performance[result['primaryModel']]['tokenCount'].append(result['primaryTokenCount'])
        self.model_performance[result['primaryModel']]['cost'].append(result['primaryCost'])
        
        # Mettre à jour les performances par fonction pour le modèle primaire
        function_key = f"{skill_name}.{function_name}"
        self.function_performance[function_key][result['primaryModel']].append(result['primarySuccess'])
        
        # Mettre à jour les performances par niveau de complexité pour le modèle primaire
        self.complexity_performance[complexity][result['primaryModel']].append(result['primarySuccess'])
        
        # Ajouter le résultat à la liste
        self.results.append(result)

    def analyze_results(self):
        """
        Analyse les résultats et génère des statistiques.
        
        Returns:
            Dictionnaire contenant les statistiques d'analyse
        """
        print("Analyse des résultats...")
        
        analysis = {
            'totalTests': len(self.results),
            'modelPerformance': {},
            'functionPerformance': {},
            'complexityPerformance': {},
            'sizePerformance': {},  # Nouvelle section pour analyser les performances par taille de modèle
            'thresholds': {},
            'recommendations': {}
        }
        
        # Analyser les performances des modèles
        for model_name, metrics in self.model_performance.items():
            success_rate = np.mean(metrics['success']) if metrics['success'] else 0
            avg_execution_time = np.mean(metrics['executionTime']) if metrics['executionTime'] else 0
            avg_token_count = np.mean(metrics['tokenCount']) if metrics['tokenCount'] else 0
            avg_cost = np.mean(metrics['cost']) if metrics['cost'] else 0
            
            model_size = self.small_models_info.get(model_name, {}).get('size', 'Unknown') if model_name != 'Primary' else 'N/A'
            
            analysis['modelPerformance'][model_name] = {
                'size': model_size,
                'successRate': success_rate,
                'avgExecutionTime': avg_execution_time,
                'avgTokenCount': avg_token_count,
                'avgCost': avg_cost,
                'testCount': len(metrics['success'])
            }
        
        # Analyser les performances par fonction
        for function_name, model_results in self.function_performance.items():
            analysis['functionPerformance'][function_name] = {}
            
            for model_name, results in model_results.items():
                success_rate = np.mean(results) if results else 0
                model_size = self.small_models_info.get(model_name, {}).get('size', 'Unknown') if model_name != 'Primary' else 'N/A'
                
                analysis['functionPerformance'][function_name][model_name] = {
                    'size': model_size,
                    'successRate': success_rate,
                    'testCount': len(results)
                }
        
        # Analyser les performances par niveau de complexité
        for complexity, model_results in self.complexity_performance.items():
            analysis['complexityPerformance'][complexity] = {}
            
            for model_name, results in model_results.items():
                success_rate = np.mean(results) if results else 0
                model_size = self.small_models_info.get(model_name, {}).get('size', 'Unknown') if model_name != 'Primary' else 'N/A'
                
                analysis['complexityPerformance'][complexity][model_name] = {
                    'size': model_size,
                    'successRate': success_rate,
                    'testCount': len(results)
                }
        
        # Analyser les performances par taille de modèle
        size_performance = defaultdict(lambda: defaultdict(list))
        
        for model_name, metrics in self.model_performance.items():
            if model_name == 'Primary':
                continue
                
            model_size = self.small_models_info.get(model_name, {}).get('size', 'Unknown')
            size_performance[model_size]['success'].extend(metrics['success'])
            size_performance[model_size]['executionTime'].extend(metrics['executionTime'])
            size_performance[model_size]['tokenCount'].extend(metrics['tokenCount'])
            size_performance[model_size]['cost'].extend(metrics['cost'])
        
        for size, metrics in size_performance.items():
            success_rate = np.mean(metrics['success']) if metrics['success'] else 0
            avg_execution_time = np.mean(metrics['executionTime']) if metrics['executionTime'] else 0
            avg_token_count = np.mean(metrics['tokenCount']) if metrics['tokenCount'] else 0
            avg_cost = np.mean(metrics['cost']) if metrics['cost'] else 0
            
            analysis['sizePerformance'][size] = {
                'successRate': success_rate,
                'avgExecutionTime': avg_execution_time,
                'avgTokenCount': avg_token_count,
                'avgCost': avg_cost,
                'testCount': len(metrics['success'])
            }
        
        # Identifier les seuils de complexité pour chaque modèle
        for model_name in self.model_performance.keys():
            if model_name == 'Primary':
                continue
                
            thresholds = self._identify_complexity_thresholds(model_name)
            analysis['thresholds'][model_name] = thresholds
        
        # Générer des recommandations
        analysis['recommendations'] = self._generate_recommendations(analysis)
        
        print("Analyse terminée.")
        return analysis

    def _identify_complexity_thresholds(self, model_name):
        """
        Identifie les seuils de complexité pour un modèle.
        
        Args:
            model_name: Nom du modèle
            
        Returns:
            Dictionnaire contenant les seuils de complexité
        """
        thresholds = {}
        complexity_levels = ['Trivial', 'Simple']
        
        for complexity in complexity_levels:
            if complexity in self.complexity_performance and model_name in self.complexity_performance[complexity]:
                results = self.complexity_performance[complexity][model_name]
                success_rate = np.mean(results) if results else 0
                thresholds[complexity] = success_rate
        
        return thresholds

    def _generate_recommendations(self, analysis):
        """
        Génère des recommandations basées sur l'analyse.
        
        Args:
            analysis: Résultats de l'analyse
            
        Returns:
            Dictionnaire contenant les recommandations
        """
        recommendations = {
            'modelAssignments': {},
            'complexityGuidelines': {},
            'optimizationSuggestions': [],
            'sizeRecommendations': {}
        }
        
        # Assigner les fonctions aux modèles les plus performants
        for function_name, model_results in analysis['functionPerformance'].items():
            best_model = None
            best_success_rate = -1
            
            for model_name, metrics in model_results.items():
                if model_name == 'Primary':
                    continue
                    
                if metrics['successRate'] > best_success_rate:
                    best_success_rate = metrics['successRate']
                    best_model = model_name
            
            if best_model and best_success_rate >= 0.6:  # Seuil plus bas pour les petits modèles
                if best_model not in recommendations['modelAssignments']:
                    recommendations['modelAssignments'][best_model] = []
                
                recommendations['modelAssignments'][best_model].append(function_name)
        
        # Définir des lignes directrices de complexité
        for model_name, thresholds in analysis['thresholds'].items():
            recommendations['complexityGuidelines'][model_name] = {}
            
            for complexity, success_rate in thresholds.items():
                recommendations['complexityGuidelines'][model_name][complexity] = {
                    'successRate': success_rate,
                    'recommended': success_rate >= 0.6  # Seuil plus bas pour les petits modèles
                }
        
        # Recommandations par taille de modèle
        for size, metrics in analysis['sizePerformance'].items():
            success_rate = metrics['successRate']
            
            if success_rate >= 0.7:
                recommendation = "Très efficace pour les tâches triviales et simples"
            elif success_rate >= 0.6:
                recommendation = "Efficace pour les tâches triviales, acceptable pour les tâches simples"
            elif success_rate >= 0.5:
                recommendation = "Acceptable pour les tâches triviales uniquement"
            else:
                recommendation = "Performance insuffisante, non recommandé"
            
            recommendations['sizeRecommendations'][size] = {
                'successRate': success_rate,
                'recommendation': recommendation
            }
        
        # Suggestions d'optimisation spécifiques aux petits modèles
        recommendations['optimizationSuggestions'] = [
            "Simplifier les prompts pour les rendre plus directs et concis",
            "Réduire la longueur des prompts pour éviter de saturer le contexte limité",
            "Ajuster MaxTokens à des valeurs plus basses pour les petits modèles",
            "Augmenter légèrement la température pour compenser la créativité limitée",
            "Implémenter un mécanisme de mise en cache des résultats pour les requêtes fréquentes"
        ]
        
        return recommendations

    def generate_report(self, analysis):
        """
        Génère un rapport d'analyse au format Markdown.
        
        Args:
            analysis: Résultats de l'analyse
            
        Returns:
            Chemin du rapport généré
        """
        print("Génération du rapport...")
        
        report_path = os.path.join(self.output_dir, 'small_models_analysis.md')
        
        with open(report_path, 'w', encoding='utf-8') as f:
            f.write("# Analyse des Performances des Modèles Plus Petits\n\n")
            f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            
            f.write("## Résumé\n\n")
            f.write(f"- Nombre total de tests: {analysis['totalTests']}\n")
            f.write(f"- Nombre de modèles testés: {len(analysis['modelPerformance']) - 1}\n")  # -1 pour exclure le modèle primaire
            f.write(f"- Nombre de fonctions testées: {len(analysis['functionPerformance'])}\n\n")
            
            f.write("## Performances Globales des Modèles\n\n")
            f.write("| Modèle | Taille | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Tests |\n")
            f.write("|--------|--------|-----------------|------------------------------|---------------|------------|-------|\n")
            
            # Trier les modèles par taux de réussite décroissant
            sorted_models = sorted(
                [(name, metrics) for name, metrics in analysis['modelPerformance'].items()],
                key=lambda x: x[1]['successRate'],
                reverse=True
            )
            
            for model_name, metrics in sorted_models:
                size = metrics.get('size', 'N/A')
                f.write(f"| {model_name} | {size} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} | {metrics['avgTokenCount']:.2f} | ${metrics['avgCost']:.6f} | {metrics['testCount']} |\n")
            
            f.write("\n## Performances par Taille de Modèle\n\n")
            f.write("| Taille | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Tests |\n")
            f.write("|--------|-----------------|------------------------------|---------------|------------|-------|\n")
            
            # Trier les tailles par ordre croissant (en convertissant en nombre si possible)
            def size_key(size_str):
                match = re.match(r'(\d+(\.\d+)?)B', size_str)
                return float(match.group(1)) if match else float('inf')
            
            sorted_sizes = sorted(analysis['sizePerformance'].keys(), key=size_key)
            
            for size in sorted_sizes:
                metrics = analysis['sizePerformance'][size]
                f.write(f"| {size} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} | {metrics['avgTokenCount']:.2f} | ${metrics['avgCost']:.6f} | {metrics['testCount']} |\n")
            
            f.write("\n## Performances par Niveau de Complexité\n\n")
            
            complexity_levels = ['Trivial', 'Simple']
            for complexity in complexity_levels:
                if complexity in analysis['complexityPerformance']:
                    f.write(f"### Niveau {complexity}\n\n")
                    f.write("| Modèle | Taille | Taux de Réussite | Tests |\n")
                    f.write("|--------|--------|-----------------|-------|\n")
                    
                    # Trier les modèles par taux de réussite décroissant
                    sorted_models = sorted(
                        [(name, metrics) for name, metrics in analysis['complexityPerformance'][complexity].items()],
                        key=lambda x: x[1]['successRate'],
                        reverse=True
                    )
                    
                    for model_name, metrics in sorted_models:
                        size = metrics.get('size', 'N/A')
                        f.write(f"| {model_name} | {size} | {metrics['successRate']:.2%} | {metrics['testCount']} |\n")
                    
                    f.write("\n")
            
            f.write("## Seuils de Complexité\n\n")
            f.write("| Modèle | Taille | Trivial | Simple | Recommandation |\n")
            f.write("|--------|--------|---------|--------|----------------|\n")
            
            for model_name, thresholds in analysis['thresholds'].items():
                size = self.small_models_info.get(model_name, {}).get('size', 'Unknown')
                trivial = f"{thresholds.get('Trivial', 0):.2%}" if 'Trivial' in thresholds else "N/A"
                simple = f"{thresholds.get('Simple', 0):.2%}" if 'Simple' in thresholds else "N/A"
                
                # Déterminer la recommandation
                trivial_rate = thresholds.get('Trivial', 0)
                simple_rate = thresholds.get('Simple', 0)
                
                if trivial_rate >= 0.7 and simple_rate >= 0.6:
                    recommendation = "Tâches triviales et simples"
                elif trivial_rate >= 0.6:
                    recommendation = "Tâches triviales uniquement"
                else:
                    recommendation = "Non recommandé"
                
                f.write(f"| {model_name} | {size} | {trivial} | {simple} | {recommendation} |\n")
            
            f.write("\n## Recommandations\n\n")
            
            f.write("### Recommandations par Taille de Modèle\n\n")
            f.write("| Taille | Taux de Réussite | Recommandation |\n")
            f.write("|--------|-----------------|----------------|\n")
            
            for size in sorted_sizes:
                metrics = analysis['recommendations']['sizeRecommendations'][size]
                f.write(f"| {size} | {metrics['successRate']:.2%} | {metrics['recommendation']} |\n")
            
            f.write("\n### Assignations de Modèles Recommandées\n\n")
            
            for model_name, functions in analysis['recommendations']['modelAssignments'].items():
                size = self.small_models_info.get(model_name, {}).get('size', 'Unknown')
                f.write(f"#### {model_name} ({size})\n\n")
                
                for function_name in functions:
                    f.write(f"- {function_name}\n")
                
                f.write("\n")
            
            f.write("### Suggestions d'Optimisation\n\n")
            
            for suggestion in analysis['recommendations']['optimizationSuggestions']:
                f.write(f"- {suggestion}\n")
        
        print(f"Rapport généré: {report_path}")
        return report_path
def generate_visualizations(self, analysis):
        """
        Génère des visualisations des résultats.
        
        Args:
            analysis: Résultats de l'analyse
            
        Returns:
            Liste des chemins des visualisations générées
        """
        print("Génération des visualisations...")
        
        visualization_paths = []
        
        # Créer un répertoire pour les visualisations
        viz_dir = os.path.join(self.output_dir, 'visualizations')
        os.makedirs(viz_dir, exist_ok=True)
        
        # Graphique des taux de réussite par modèle
        plt.figure(figsize=(12, 6))
        models = [name for name in analysis['modelPerformance'].keys() if name != 'Primary']
        success_rates = [analysis['modelPerformance'][model]['successRate'] for model in models]
        sizes = [analysis['modelPerformance'][model]['size'] for model in models]
        
        # Créer un colormap basé sur la taille
        size_values = []
        for size in sizes:
            match = re.match(r'(\d+(\.\d+)?)B', size)
            size_values.append(float(match.group(1)) if match else 0)
        
        # Normaliser les valeurs pour le colormap
        norm = plt.Normalize(min(size_values), max(size_values))
        colors = plt.cm.viridis(norm(size_values))
        
        bars = plt.bar(models, success_rates, color=colors)
        plt.xlabel('Modèle')
        plt.ylabel('Taux de Réussite')
        plt.title('Taux de Réussite par Modèle')
        plt.xticks(rotation=45, ha='right')
        
        # Ajouter une barre de couleur pour la taille
        sm = plt.cm.ScalarMappable(cmap=plt.cm.viridis, norm=norm)
        sm.set_array([])
        cbar = plt.colorbar(sm)
        cbar.set_label('Taille du Modèle (B)')
        
        plt.tight_layout()
        
        success_rate_path = os.path.join(viz_dir, 'success_rate_by_model.png')
        plt.savefig(success_rate_path)
        plt.close()
        visualization_paths.append(success_rate_path)
        
        # Graphique des taux de réussite par taille de modèle
        plt.figure(figsize=(10, 6))
        sizes = list(analysis['sizePerformance'].keys())
        success_rates = [analysis['sizePerformance'][size]['successRate'] for size in sizes]
        
        # Trier par taille
        size_values = []
        for size in sizes:
            match = re.match(r'(\d+(\.\d+)?)B', size)
            size_values.append(float(match.group(1)) if match else float('inf'))
        
        sorted_indices = np.argsort(size_values)
        sorted_sizes = [sizes[i] for i in sorted_indices]
        sorted_rates = [success_rates[i] for i in sorted_indices]
        
        plt.bar(sorted_sizes, sorted_rates)
        plt.xlabel('Taille du Modèle')
        plt.ylabel('Taux de Réussite')
        plt.title('Taux de Réussite par Taille de Modèle')
        plt.tight_layout()
        
        size_success_rate_path = os.path.join(viz_dir, 'success_rate_by_size.png')
        plt.savefig(size_success_rate_path)
        plt.close()
        visualization_paths.append(size_success_rate_path)
        
        # Graphique des seuils de complexité
        plt.figure(figsize=(12, 8))
        complexity_levels = ['Trivial', 'Simple']
        
        for model_name, thresholds in analysis['thresholds'].items():
            success_rates = [thresholds.get(level, 0) for level in complexity_levels]
            size = analysis['modelPerformance'][model_name]['size']
            label = f"{model_name} ({size})"
            plt.plot(complexity_levels, success_rates, marker='o', label=label)
        
        plt.xlabel('Niveau de Complexité')
        plt.ylabel('Taux de Réussite')
        plt.title('Taux de Réussite par Niveau de Complexité')
        plt.legend()
        plt.grid(True)
        plt.tight_layout()
        
        complexity_path = os.path.join(viz_dir, 'success_rate_by_complexity.png')
        plt.savefig(complexity_path)
        plt.close()
        visualization_paths.append(complexity_path)
        
        # Graphique comparatif des temps d'exécution et des coûts
        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(15, 6))
        
        # Temps d'exécution
        execution_times = [analysis['modelPerformance'][model]['avgExecutionTime'] for model in models]
        ax1.bar(models, execution_times, color=colors)
        ax1.set_xlabel('Modèle')
        ax1.set_ylabel('Temps d\'Exécution Moyen (ms)')
        ax1.set_title('Temps d\'Exécution Moyen par Modèle')
        ax1.tick_params(axis='x', rotation=45)
        
        # Coûts
        costs = [analysis['modelPerformance'][model]['avgCost'] for model in models]
        ax2.bar(models, costs, color=colors)
        ax2.set_xlabel('Modèle')
        ax2.set_ylabel('Coût Moyen ($)')
        ax2.set_title('Coût Moyen par Modèle')
        ax2.tick_params(axis='x', rotation=45)
        
        plt.tight_layout()
        
        comparison_path = os.path.join(viz_dir, 'execution_time_cost_comparison.png')
        plt.savefig(comparison_path)
        plt.close()
        visualization_paths.append(comparison_path)
        
        print(f"Visualisations générées: {len(visualization_paths)}")
        return visualization_paths

    def run(self):
        """Exécute l'analyse complète et génère le rapport."""
        self.load_results()
        analysis = self.analyze_results()
        report_path = self.generate_report(analysis)
        visualization_paths = self.generate_visualizations(analysis)
        
        print(f"Analyse terminée. Rapport généré: {report_path}")
        print("Visualisations générées:")
        for path in visualization_paths:
            print(f"- {path}")


def main():
    """Fonction principale."""
    parser = argparse.ArgumentParser(description='Analyse des résultats de tests des modèles plus petits')
    parser.add_argument('--log-dir', type=str, default='../results/logs',
                        help='Répertoire contenant les logs d\'instrumentation')
    parser.add_argument('--output-dir', type=str, default='../results/analysis',
                        help='Répertoire de sortie pour les rapports')
    parser.add_argument('--small-models-file', type=str, default='../../results/small_models.json',
                        help='Fichier JSON contenant les informations sur les modèles plus petits')
    
    args = parser.parse_args()
    
    analyzer = SmallModelAnalyzer(args.log_dir, args.output_dir, args.small_models_file)
    analyzer.run()


if __name__ == '__main__':
    main()