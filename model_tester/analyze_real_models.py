#!/usr/bin/env python3
"""
Script pour analyser les résultats des tests avec les modèles réels (OpenAI et OpenRouter).
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


class RealModelAnalyzer:
    """Analyseur de résultats de tests pour les modèles réels."""

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
        self.provider_performance = defaultdict(lambda: defaultdict(list))
        
        # Créer le répertoire de sortie s'il n'existe pas
        os.makedirs(output_dir, exist_ok=True)
        os.makedirs(os.path.join(output_dir, 'visualizations'), exist_ok=True)

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
                if "choices" in data:
                    # Format de réponse directe de l'API
                    self._process_api_result(data, filename)
                elif "testResults" in data:
                    # Format de résultats de test
                    for test_result in data.get('testResults', []):
                        self._process_test_result(test_result)
            except Exception as e:
                print(f"Erreur lors du traitement du fichier {filepath}: {e}")
        
        print(f"Chargement terminé. {len(self.results)} résultats chargés.")

    def _process_api_result(self, data: Dict[str, Any], filename: str) -> None:
        """
        Traite un résultat d'API direct.
        
        Args:
            data: Données de réponse de l'API
            filename: Nom du fichier de log
        """
        # Extraire les informations du nom de fichier
        match = re.match(r'(.+)_(.+)_(.+)\.json', filename)
        if not match:
            print(f"Format de nom de fichier non reconnu: {filename}")
            return
            
        model = match.group(1)
        provider = match.group(2)
        prompt_type = match.group(3)
        
        # Extraire les métriques de performance
        success = "choices" in data and len(data["choices"]) > 0
        execution_time = data.get("response_time", 0)
        token_count = data.get("usage", {}).get("total_tokens", 0)
        prompt_tokens = data.get("usage", {}).get("prompt_tokens", 0)
        completion_tokens = data.get("usage", {}).get("completion_tokens", 0)
        
        # Estimer le coût (approximation)
        cost = self._estimate_cost(model, prompt_tokens, completion_tokens)
        
        # Créer une entrée de résultat
        result = {
            'model': model,
            'provider': provider,
            'promptType': prompt_type,
            'success': success,
            'executionTime': execution_time,
            'tokenCount': token_count,
            'promptTokens': prompt_tokens,
            'completionTokens': completion_tokens,
            'cost': cost
        }
        
        # Mettre à jour les performances du modèle
        self.model_performance[model]['success'].append(success)
        self.model_performance[model]['executionTime'].append(execution_time)
        self.model_performance[model]['tokenCount'].append(token_count)
        self.model_performance[model]['cost'].append(cost)
        
        # Mettre à jour les performances par provider
        self.provider_performance[provider]['success'].append(success)
        self.provider_performance[provider]['executionTime'].append(execution_time)
        self.provider_performance[provider]['tokenCount'].append(token_count)
        self.provider_performance[provider]['cost'].append(cost)
        
        # Ajouter le résultat à la liste
        self.results.append(result)

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
            
            # Déterminer le provider
            provider = "openai"
            if any(p in model_name.lower() for p in ["claude", "anthropic", "gemini", "google", "qwen"]):
                provider = "openrouter"
            
            # Mettre à jour les performances du modèle
            self.model_performance[model_name]['success'].append(secondary_result['success'])
            self.model_performance[model_name]['executionTime'].append(secondary_result['executionTime'])
            self.model_performance[model_name]['tokenCount'].append(secondary_result['tokenCount'])
            self.model_performance[model_name]['cost'].append(secondary_result['cost'])
            
            # Mettre à jour les performances par provider
            self.provider_performance[provider]['success'].append(secondary_result['success'])
            self.provider_performance[provider]['executionTime'].append(secondary_result['executionTime'])
            self.provider_performance[provider]['tokenCount'].append(secondary_result['tokenCount'])
            self.provider_performance[provider]['cost'].append(secondary_result['cost'])
            
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

    def _estimate_cost(self, model: str, prompt_tokens: int, completion_tokens: int) -> float:
        """
        Estime le coût d'une requête en fonction du modèle et du nombre de tokens.
        
        Args:
            model: Nom du modèle
            prompt_tokens: Nombre de tokens dans le prompt
            completion_tokens: Nombre de tokens dans la complétion
            
        Returns:
            Coût estimé en dollars
        """
        # Prix par 1000 tokens (approximation)
        pricing = {
            # OpenAI
            "gpt-4o": {"prompt": 0.01, "completion": 0.03},
            "gpt-4o-mini": {"prompt": 0.005, "completion": 0.015},
            "gpt-3.5-turbo": {"prompt": 0.0015, "completion": 0.002},
            "o3": {"prompt": 0.015, "completion": 0.075},
            "o4-mini": {"prompt": 0.005, "completion": 0.015},
            
            # OpenRouter (Claude)
            "claude-3-sonnet": {"prompt": 0.008, "completion": 0.024},
            "anthropic/claude-3-sonnet": {"prompt": 0.008, "completion": 0.024},
            "anthropic/claude-3.7-sonnet": {"prompt": 0.008, "completion": 0.024},
            
            # OpenRouter (Gemini)
            "gemini-pro": {"prompt": 0.0035, "completion": 0.0035},
            "google/gemini-pro": {"prompt": 0.0035, "completion": 0.0035},
            "google/gemini-pro-1.5": {"prompt": 0.0035, "completion": 0.0035},
            
            # OpenRouter (Qwen)
            "qwen/qwen3-1.7b": {"prompt": 0.0005, "completion": 0.0005},
            "qwen/qwen3-8b": {"prompt": 0.001, "completion": 0.001},
            "qwen/qwen3-14b": {"prompt": 0.002, "completion": 0.002},
            "qwen/qwen3-30b-a3b": {"prompt": 0.003, "completion": 0.003},
            "qwen/qwen3-32b": {"prompt": 0.004, "completion": 0.004}
        }
        
        # Trouver le modèle correspondant
        model_key = None
        for key in pricing.keys():
            if key in model.lower():
                model_key = key
                break
        
        if not model_key:
            # Modèle inconnu, utiliser un prix par défaut
            return (prompt_tokens + completion_tokens) * 0.01 / 1000
        
        # Calculer le coût
        prompt_cost = prompt_tokens * pricing[model_key]["prompt"] / 1000
        completion_cost = completion_tokens * pricing[model_key]["completion"] / 1000
        
        return prompt_cost + completion_cost

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
            'providerPerformance': {},
            'functionPerformance': {},
            'complexityPerformance': {},
            'costEfficiency': {},
            'recommendations': {}
        }
        
        # Analyser les performances des modèles
        for model_name, metrics in self.model_performance.items():
            success_rate = np.mean(metrics['success']) if metrics['success'] else 0
            avg_execution_time = np.mean(metrics['executionTime']) if metrics['executionTime'] else 0
            avg_token_count = np.mean(metrics['tokenCount']) if metrics['tokenCount'] else 0
            avg_cost = np.mean(metrics['cost']) if metrics['cost'] else 0
            
            # Calculer l'efficacité coût/performance
            cost_efficiency = success_rate / avg_cost if avg_cost > 0 else float('inf')
            
            analysis['modelPerformance'][model_name] = {
                'successRate': success_rate,
                'avgExecutionTime': avg_execution_time,
                'avgTokenCount': avg_token_count,
                'avgCost': avg_cost,
                'costEfficiency': cost_efficiency,
                'testCount': len(metrics['success'])
            }
        
        # Analyser les performances par provider
        for provider, metrics in self.provider_performance.items():
            success_rate = np.mean(metrics['success']) if metrics['success'] else 0
            avg_execution_time = np.mean(metrics['executionTime']) if metrics['executionTime'] else 0
            avg_token_count = np.mean(metrics['tokenCount']) if metrics['tokenCount'] else 0
            avg_cost = np.mean(metrics['cost']) if metrics['cost'] else 0
            
            analysis['providerPerformance'][provider] = {
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
        
        # Analyser l'efficacité coût/performance
        for model_name, metrics in analysis['modelPerformance'].items():
            analysis['costEfficiency'][model_name] = metrics['costEfficiency']
        
        # Générer des recommandations
        analysis['recommendations'] = self._generate_recommendations(analysis)
        
        print("Analyse terminée.")
        return analysis

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
            'costEfficiency': {},
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
            
            if best_model and best_success_rate >= 0.7:
                if best_model not in recommendations['modelAssignments']:
                    recommendations['modelAssignments'][best_model] = []
                
                recommendations['modelAssignments'][best_model].append(function_name)
        
        # Recommandations d'efficacité coût/performance
        sorted_models = sorted(
            analysis['costEfficiency'].items(),
            key=lambda x: x[1],
            reverse=True
        )
        
        for i, (model_name, efficiency) in enumerate(sorted_models):
            if i == 0:
                category = "Excellent rapport qualité/prix"
            elif i <= len(sorted_models) // 3:
                category = "Bon rapport qualité/prix"
            elif i <= 2 * len(sorted_models) // 3:
                category = "Rapport qualité/prix moyen"
            else:
                category = "Rapport qualité/prix faible"
            
            recommendations['costEfficiency'][model_name] = {
                'efficiency': efficiency,
                'category': category
            }
        
        # Suggestions d'optimisation
        recommendations['optimizationSuggestions'] = [
            "Utiliser GPT-4o pour les tâches complexes nécessitant un raisonnement avancé",
            "Utiliser Claude 3 Sonnet pour les tâches de génération de texte et de résumé",
            "Utiliser GPT-4o-mini ou Gemini Pro pour un bon équilibre performance/coût",
            "Utiliser GPT-3.5-turbo pour les tâches simples à moyen coût",
            "Ajuster les paramètres du MultiConnector en fonction des performances des modèles",
            "Optimiser les transformations de prompts pour chaque modèle"
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
        
        report_path = os.path.join(self.output_dir, 'real_models_analysis.md')
        
        with open(report_path, 'w', encoding='utf-8') as f:
            f.write("# Analyse des Performances des Modèles Réels\n\n")
            f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            
            f.write("## Résumé\n\n")
            f.write(f"- Nombre total de tests: {analysis['totalTests']}\n")
            f.write(f"- Nombre de modèles testés: {len(analysis['modelPerformance'])}\n")
            f.write(f"- Nombre de fonctions testées: {len(analysis['functionPerformance'])}\n\n")
            
            f.write("## Performances Globales des Modèles\n\n")
            f.write("| Modèle | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Efficacité Coût/Performance | Tests |\n")
            f.write("|--------|-----------------|------------------------------|---------------|------------|------------------------------|-------|\n")
            
            # Trier les modèles par taux de réussite décroissant
            sorted_models = sorted(
                [(name, metrics) for name, metrics in analysis['modelPerformance'].items()],
                key=lambda x: x[1]['successRate'],
                reverse=True
            )
            
            for model_name, metrics in sorted_models:
                f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} | {metrics['avgTokenCount']:.2f} | ${metrics['avgCost']:.6f} | {metrics['costEfficiency']:.2f} | {metrics['testCount']} |\n")
            
            f.write("\n## Performances par Provider\n\n")
            f.write("| Provider | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Tests |\n")
            f.write("|----------|-----------------|------------------------------|---------------|------------|-------|\n")
            
            for provider, metrics in analysis['providerPerformance'].items():
                f.write(f"| {provider} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} | {metrics['avgTokenCount']:.2f} | ${metrics['avgCost']:.6f} | {metrics['testCount']} |\n")
            
            f.write("\n## Efficacité Coût/Performance\n\n")
            f.write("| Modèle | Efficacité | Catégorie |\n")
            f.write("|--------|------------|------------|\n")
            
            # Trier les modèles par efficacité décroissante
            sorted_efficiency = sorted(
                [(name, metrics) for name, metrics in analysis['recommendations']['costEfficiency'].items()],
                key=lambda x: x[1]['efficiency'],
                reverse=True
            )
            
            for model_name, metrics in sorted_efficiency:
                f.write(f"| {model_name} | {metrics['efficiency']:.2f} | {metrics['category']} |\n")
            
            f.write("\n## Performances par Niveau de Complexité\n\n")
            
            complexity_levels = ['Trivial', 'Simple', 'Medium', 'Hard']
            for complexity in complexity_levels:
                if complexity in analysis['complexityPerformance']:
                    f.write(f"### Niveau {complexity}\n\n")
                    f.write("| Modèle | Taux de Réussite | Tests |\n")
                    f.write("|--------|-----------------|-------|\n")
                    
                    # Trier les modèles par taux de réussite décroissant
                    sorted_models = sorted(
                        [(name, metrics) for name, metrics in analysis['complexityPerformance'][complexity].items()],
                        key=lambda x: x[1]['successRate'],
                        reverse=True
                    )
                    
                    for model_name, metrics in sorted_models:
                        f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['testCount']} |\n")
                    
                    f.write("\n")
            
            f.write("## Recommandations\n\n")
            
            f.write("### Assignations de Modèles Recommandées\n\n")
            
            for model_name, functions in analysis['recommendations']['modelAssignments'].items():
                f.write(f"#### {model_name}\n\n")
                
                for function_name in functions:
                    f.write(f"- {function_name}\n")
                
                f.write("\n")
            
            f.write("### Suggestions d'Optimisation\n\n")
            
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
        plt.figure(figsize=(12, 6))
        models = [name for name, _ in sorted(
            [(name, metrics) for name, metrics in analysis['modelPerformance'].items()],
            key=lambda x: x[1]['successRate'],
            reverse=True
        )]
        success_rates = [analysis['modelPerformance'][model]['successRate'] for model in models]
        
        # Définir des couleurs différentes pour OpenAI et OpenRouter
        colors = []
        for model in models:
            if any(p in model.lower() for p in ["claude", "anthropic", "gemini", "google", "qwen"]):
                colors.append('orange')
            else:
                colors.append('blue')
        
        plt.bar(models, success_rates, color=colors)
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
        plt.figure(figsize=(12, 6))
        execution_times = [analysis['modelPerformance'][model]['avgExecutionTime'] for model in models]
        
        plt.bar(models, execution_times, color=colors)
        plt.xlabel('Modèle')
        plt.ylabel('Temps d\'Exécution Moyen (ms)')
        plt.title('Temps d\'Exécution Moyen par Modèle')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        execution_time_path = os.path.join(viz_dir, 'execution_time_by_model.png')
        plt.savefig(execution_time_path)
        plt.close()
        visualization_paths.append(execution_time_path)
        
        # Graphique de l'efficacité coût/performance
        plt.figure(figsize=(12, 6))
        sorted_efficiency = sorted(
            [(name, metrics) for name, metrics in analysis['recommendations']['costEfficiency'].items()],
            key=lambda x: x[1]['efficiency'],
            reverse=True
        )
        
        models = [name for name, _ in sorted_efficiency]
        efficiencies = [metrics['efficiency'] for _, metrics in sorted_efficiency]
        
        # Définir des couleurs différentes pour OpenAI et OpenRouter
        colors = []
        for model in models:
            if any(p in model.lower() for p in ["claude", "anthropic", "gemini", "google", "qwen"]):
                colors.append('orange')
            else:
                colors.append('blue')
        
        plt.bar(models, efficiencies, color=colors)
        plt.xlabel('Modèle')
        plt.ylabel('Efficacité Coût/Performance')
        plt.title('Efficacité Coût/Performance par Modèle')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        efficiency_path = os.path.join(viz_dir, 'cost_efficiency_by_model.png')
        plt.savefig(efficiency_path)
        plt.close()
        visualization_paths.append(efficiency_path)
        
        # Graphique des performances par niveau de complexité
        plt.figure(figsize=(12, 8))
        complexity_levels = ['Trivial', 'Simple', 'Medium', 'Hard']
        
        # Sélectionner les modèles les plus performants pour la lisibilité
        top_models = models[:5]
        
        for model_name in top_models:
            success_rates = []
            for complexity in complexity_levels:
                if complexity in analysis['complexityPerformance'] and model_name in analysis['complexityPerformance'][complexity]:
                    success_rates.append(analysis['complexityPerformance'][complexity][model_name]['successRate'])
                else:
                    success_rates.append(0)
            
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
    parser = argparse.ArgumentParser(description='Analyse des résultats de tests des modèles réels')
    parser.add_argument('--log-dir', type=str, default='../results/real_models/logs',
                        help='Répertoire contenant les logs d\'instrumentation')
    parser.add_argument('--output-dir', type=str, default='../results/real_models/analysis',
                        help='Répertoire de sortie pour les rapports')
    
    args = parser.parse_args()
    
    analyzer = RealModelAnalyzer(args.log_dir, args.output_dir)
    analyzer.run()


if __name__ == '__main__':
    main()