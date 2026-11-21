#!/usr/bin/env python3
"""
Script pour générer un rapport d'analyse des résultats des tests du MultiConnector.
Ce script traite les logs d'instrumentation et génère un rapport détaillé sur les performances
des différents modèles avec les fonctions Semantic Kernel.
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


class TestResultAnalyzer:
    """Analyseur de résultats de tests pour le MultiConnector."""

    def __init__(self, log_dir: str, output_dir: str):
        """
        Initialise l'analyseur de résultats.
        
        Args:
            log_dir: Répertoire contenant les logs d'instrumentation
            output_dir: Répertoire de sortie pour les rapports
        """
        self.log_dir = log_dir
        self.output_dir = output_dir
        self.results = []
        self.model_performance = defaultdict(lambda: defaultdict(list))
        self.function_performance = defaultdict(lambda: defaultdict(list))
        self.complexity_performance = defaultdict(lambda: defaultdict(list))
        
        # Créer le répertoire de sortie s'il n'existe pas
        os.makedirs(output_dir, exist_ok=True)

    def load_results(self) -> None:
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

    def _process_test_result(self, test_result: Dict[str, Any]) -> None:
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
            secondary_result = {
                'modelName': model_name,
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

    def analyze_results(self) -> Dict[str, Any]:
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
            'thresholds': {},
            'recommendations': {}
        }
        
        # Analyser les performances des modèles
        for model_name, metrics in self.model_performance.items():
            success_rate = np.mean(metrics['success']) if metrics['success'] else 0
            avg_execution_time = np.mean(metrics['executionTime']) if metrics['executionTime'] else 0
            avg_token_count = np.mean(metrics['tokenCount']) if metrics['tokenCount'] else 0
            avg_cost = np.mean(metrics['cost']) if metrics['cost'] else 0
            
            analysis['modelPerformance'][model_name] = {
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
                analysis['functionPerformance'][function_name][model_name] = {
                    'successRate': success_rate,
                    'testCount': len(results)
                }
        
        # Analyser les performances par niveau de complexité
        for complexity, model_results in self.complexity_performance.items():
            analysis['complexityPerformance'][complexity] = {}
            
            for model_name, results in model_results.items():
                success_rate = np.mean(results) if results else 0
                analysis['complexityPerformance'][complexity][model_name] = {
                    'successRate': success_rate,
                    'testCount': len(results)
                }
        
        # Identifier les seuils de complexité pour chaque modèle
        for model_name in self.model_performance.keys():
            thresholds = self._identify_complexity_thresholds(model_name)
            analysis['thresholds'][model_name] = thresholds
        
        # Générer des recommandations
        analysis['recommendations'] = self._generate_recommendations(analysis)
        
        print("Analyse terminée.")
        return analysis

    def _identify_complexity_thresholds(self, model_name: str) -> Dict[str, float]:
        """
        Identifie les seuils de complexité pour un modèle.
        
        Args:
            model_name: Nom du modèle
            
        Returns:
            Dictionnaire contenant les seuils de complexité
        """
        thresholds = {}
        complexity_levels = ['Trivial', 'Simple', 'Medium', 'Hard']
        
        for complexity in complexity_levels:
            if complexity in self.complexity_performance and model_name in self.complexity_performance[complexity]:
                results = self.complexity_performance[complexity][model_name]
                success_rate = np.mean(results) if results else 0
                thresholds[complexity] = success_rate
        
        return thresholds

    def _generate_recommendations(self, analysis: Dict[str, Any]) -> Dict[str, Any]:
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
            'optimizationSuggestions': []
        }
        
        # Assigner les fonctions aux modèles les plus performants
        for function_name, model_results in analysis['functionPerformance'].items():
            best_model = None
            best_success_rate = -1
            
            for model_name, metrics in model_results.items():
                if metrics['successRate'] > best_success_rate:
                    best_success_rate = metrics['successRate']
                    best_model = model_name
            
            if best_model and best_model != 'Primary' and best_success_rate >= 0.7:
                if best_model not in recommendations['modelAssignments']:
                    recommendations['modelAssignments'][best_model] = []
                
                recommendations['modelAssignments'][best_model].append(function_name)
        
        # Définir des lignes directrices de complexité
        for model_name, thresholds in analysis['thresholds'].items():
            recommendations['complexityGuidelines'][model_name] = {}
            
            for complexity, success_rate in thresholds.items():
                recommendations['complexityGuidelines'][model_name][complexity] = {
                    'successRate': success_rate,
                    'recommended': success_rate >= 0.7
                }
        
        # Suggestions d'optimisation générales
        recommendations['optimizationSuggestions'] = [
            "Augmenter le nombre d'échantillons pour les fonctions avec des résultats incohérents",
            "Ajuster les paramètres de température pour les modèles avec des taux de réussite faibles",
            "Utiliser des expressions régulières pour les préfixes qui se chevauchent",
            "Optimiser les transformations de prompts pour les modèles secondaires"
        ]
        
        return recommendations

    def generate_report(self, analysis: Dict[str, Any]) -> str:
        """
        Génère un rapport d'analyse au format Markdown.
        
        Args:
            analysis: Résultats de l'analyse
            
        Returns:
            Chemin du rapport généré
        """
        print("Génération du rapport...")
        
        report_path = os.path.join(self.output_dir, 'analysis_report.md')
        
        with open(report_path, 'w', encoding='utf-8') as f:
            f.write("# Rapport d'Analyse des Tests du MultiConnector\n\n")
            f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            
            f.write("## Résumé\n\n")
            f.write(f"- Nombre total de tests: {analysis['totalTests']}\n")
            f.write(f"- Nombre de modèles testés: {len(analysis['modelPerformance'])}\n")
            f.write(f"- Nombre de fonctions testées: {len(analysis['functionPerformance'])}\n\n")
            
            f.write("## Performances des Modèles\n\n")
            f.write("| Modèle | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Tests |\n")
            f.write("|--------|-----------------|------------------------------|---------------|------------|-------|\n")
            
            for model_name, metrics in analysis['modelPerformance'].items():
                f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} | {metrics['avgTokenCount']:.2f} | ${metrics['avgCost']:.6f} | {metrics['testCount']} |\n")
            
            f.write("\n## Performances par Niveau de Complexité\n\n")
            
            complexity_levels = ['Trivial', 'Simple', 'Medium', 'Hard']
            for complexity in complexity_levels:
                if complexity in analysis['complexityPerformance']:
                    f.write(f"### Niveau {complexity}\n\n")
                    f.write("| Modèle | Taux de Réussite | Tests |\n")
                    f.write("|--------|-----------------|-------|\n")
                    
                    for model_name, metrics in analysis['complexityPerformance'][complexity].items():
                        f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['testCount']} |\n")
                    
                    f.write("\n")
            
            f.write("## Seuils de Complexité\n\n")
            f.write("| Modèle | Trivial | Simple | Medium | Hard |\n")
            f.write("|--------|---------|--------|--------|------|\n")
            
            for model_name, thresholds in analysis['thresholds'].items():
                trivial = f"{thresholds.get('Trivial', 0):.2%}" if 'Trivial' in thresholds else "N/A"
                simple = f"{thresholds.get('Simple', 0):.2%}" if 'Simple' in thresholds else "N/A"
                medium = f"{thresholds.get('Medium', 0):.2%}" if 'Medium' in thresholds else "N/A"
                hard = f"{thresholds.get('Hard', 0):.2%}" if 'Hard' in thresholds else "N/A"
                
                f.write(f"| {model_name} | {trivial} | {simple} | {medium} | {hard} |\n")
            
            f.write("\n## Recommandations\n\n")
            
            f.write("### Assignations de Modèles\n\n")
            
            for model_name, functions in analysis['recommendations']['modelAssignments'].items():
                f.write(f"#### {model_name}\n\n")
                
                for function_name in functions:
                    f.write(f"- {function_name}\n")
                
                f.write("\n")
            
            f.write("### Lignes Directrices de Complexité\n\n")
            f.write("| Modèle | Niveau | Taux de Réussite | Recommandé |\n")
            f.write("|--------|--------|-----------------|------------|\n")
            
            for model_name, guidelines in analysis['recommendations']['complexityGuidelines'].items():
                for complexity, metrics in guidelines.items():
                    f.write(f"| {model_name} | {complexity} | {metrics['successRate']:.2%} | {'Oui' if metrics['recommended'] else 'Non'} |\n")
            
            f.write("\n### Suggestions d'Optimisation\n\n")
            
            for suggestion in analysis['recommendations']['optimizationSuggestions']:
                f.write(f"- {suggestion}\n")
        
        print(f"Rapport généré: {report_path}")
        return report_path

    def generate_visualizations(self, analysis: Dict[str, Any]) -> List[str]:
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
        plt.figure(figsize=(10, 6))
        models = list(analysis['modelPerformance'].keys())
        success_rates = [analysis['modelPerformance'][model]['successRate'] for model in models]
        
        plt.bar(models, success_rates)
        plt.xlabel('Modèle')
        plt.ylabel('Taux de Réussite')
        plt.title('Taux de Réussite par Modèle')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        success_rate_path = os.path.join(viz_dir, 'success_rate_by_model.png')
        plt.savefig(success_rate_path)
        plt.close()
        visualization_paths.append(success_rate_path)
        
        # Graphique des temps d'exécution par modèle
        plt.figure(figsize=(10, 6))
        execution_times = [analysis['modelPerformance'][model]['avgExecutionTime'] for model in models]
        
        plt.bar(models, execution_times)
        plt.xlabel('Modèle')
        plt.ylabel('Temps d\'Exécution Moyen (ms)')
        plt.title('Temps d\'Exécution Moyen par Modèle')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        execution_time_path = os.path.join(viz_dir, 'execution_time_by_model.png')
        plt.savefig(execution_time_path)
        plt.close()
        visualization_paths.append(execution_time_path)
        
        # Graphique des seuils de complexité
        plt.figure(figsize=(12, 8))
        complexity_levels = ['Trivial', 'Simple', 'Medium', 'Hard']
        
        for model_name, thresholds in analysis['thresholds'].items():
            success_rates = [thresholds.get(level, 0) for level in complexity_levels]
            plt.plot(complexity_levels, success_rates, marker='o', label=model_name)
        
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
        
        print(f"Visualisations générées: {len(visualization_paths)}")
        return visualization_paths

    def run(self) -> None:
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
    parser = argparse.ArgumentParser(description='Analyse des résultats de tests du MultiConnector')
    parser.add_argument('--log-dir', type=str, default='../results/logs',
                        help='Répertoire contenant les logs d\'instrumentation')
    parser.add_argument('--output-dir', type=str, default='../results/analysis',
                        help='Répertoire de sortie pour les rapports')
    
    args = parser.parse_args()
    
    analyzer = TestResultAnalyzer(args.log_dir, args.output_dir)
    analyzer.run()


if __name__ == '__main__':
    main()