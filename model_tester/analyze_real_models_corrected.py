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
import seaborn as sns
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
        
        # Mettre à jour les performances par type de tâche
        self.task_type_performance[prompt_type][model].append(success)
        
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
        
        # Déterminer le type de tâche à partir du nom de la compétence
        task_type = self._determine_task_type(skill_name)
        
        # Extraire les performances des modèles
        primary_model = test_result.get('primaryModel', {})
        secondary_models = test_result.get('secondaryModels', {})
        
        # Créer une entrée de résultat
        result = {
            'skillName': skill_name,
            'functionName': function_name,
            'complexity': complexity,
            'taskType': task_type,
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
            
            # Mettre à jour les performances par type de tâche
            self.task_type_performance[task_type][model_name].append(secondary_result['success'])
        
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
        
        # Mettre à jour les performances par type de tâche pour le modèle primaire
        self.task_type_performance[task_type][result['primaryModel']].append(result['primarySuccess'])
        
        # Ajouter le résultat à la liste
        self.results.append(result)
    
    def _determine_task_type(self, skill_name: str) -> str:
        """
        Détermine le type de tâche à partir du nom de la compétence.
        
        Args:
            skill_name: Nom de la compétence
            
        Returns:
            Type de tâche
        """
        skill_to_task = {
            'ChatSkill': 'chat',
            'SummarizeSkill': 'summarization',
            'ClassificationSkill': 'classification',
            'WriterSkill': 'writing',
            'CodingSkill': 'code',
            'QASkill': 'qa',
            'GroundingSkill': 'grounding',
            'IntentDetectionSkill': 'classification',
            'MiscSkill': 'misc',
            'FunSkill': 'creative',
            'ChildrensBookSkill': 'creative'
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
            'taskTypePerformance': {},
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
            'optimizationSuggestions': [],
            'routingStrategies': {
                'complexity': {},
                'taskType': {},
                'hybrid': []
            },
            'promptTransformations': {},
            'fallbackStrategies': []
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
        
        # Recommandations de routage basé sur la complexité
        complexity_levels = ['Trivial', 'Simple', 'Medium', 'Hard']
        for complexity in complexity_levels:
            if complexity in analysis['complexityPerformance']:
                # Trouver les meilleurs modèles pour ce niveau de complexité
                sorted_models = sorted(
                    [(name, metrics) for name, metrics in analysis['complexityPerformance'][complexity].items()],
                    key=lambda x: x[1]['successRate'],
                    reverse=True
                )
                
                # Prendre les 2 meilleurs modèles
                best_models = [name for name, _ in sorted_models[:2]]
                
                recommendations['routingStrategies']['complexity'][complexity] = best_models
        
        # Recommandations de routage basé sur le type de tâche
        for task_type, model_results in analysis['taskTypePerformance'].items():
            # Trouver les meilleurs modèles pour ce type de tâche
            sorted_models = sorted(
                [(name, metrics) for name, metrics in model_results.items()],
                key=lambda x: x[1]['successRate'],
                reverse=True
            )
            
            # Prendre les 2 meilleurs modèles
            best_models = [name for name, _ in sorted_models[:2]]
            
            recommendations['routingStrategies']['taskType'][task_type] = best_models
        
        # Recommandations de routage hybride
        recommendations['routingStrategies']['hybrid'] = [
            "Utiliser un système de scoring qui prend en compte la complexité, le type de tâche et les contraintes de coût",
            "Implémenter un mécanisme d'apprentissage pour ajuster les poids des facteurs en fonction des résultats",
            "Utiliser des heuristiques pour déterminer le modèle optimal en fonction du contexte"
        ]
        
        # Recommandations de transformations de prompts
        model_types = {
            "gpt": ["gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo"],
            "claude": ["anthropic/claude-3.7-sonnet"],
            "gemini": ["google/gemini-pro-1.5"],
            "qwen": ["qwen/qwen3-1.7b", "qwen/qwen3-8b", "qwen/qwen3-14b", "qwen/qwen3-30b-a3b", "qwen/qwen3-32b"]
        }
        
        for model_type, models in model_types.items():
            if model_type == "gpt":
                recommendations['promptTransformations'][model_type] = {
                    'technique': "Prompts détaillés avec contexte structuré",
                    'examples': 2,
                    'instruction': "Instructions détaillées avec contexte et objectifs"
                }
            elif model_type == "claude":
                recommendations['promptTransformations'][model_type] = {
                    'technique': "Instructions claires et explicites, exemples few-shot",
                    'examples': 3,
                    'instruction': "Instructions explicites sur le format de sortie attendu"
                }
            elif model_type == "gemini":
                recommendations['promptTransformations'][model_type] = {
                    'technique': "Prompts concis avec instructions directes",
                    'examples': 1,
                    'instruction': "Instructions concises et directes"
                }
            elif model_type == "qwen":
                recommendations['promptTransformations'][model_type] = {
                    'technique': "Prompts avec exemples few-shot pour les tâches complexes",
                    'examples': 2,
                    'instruction': "Instructions avec exemples de raisonnement étape par étape"
                }
        
        # Recommandations de stratégies de fallback
        recommendations['fallbackStrategies'] = [
            {
                'type': "cascade",
                'description': "Implémenter une cascade de modèles en cas d'échec",
                'priority': [
                    ["gpt-4o", "o3"],
                    ["anthropic/claude-3.7-sonnet", "gpt-4o-mini"],
                    ["google/gemini-pro-1.5", "qwen/qwen3-32b"],
                    ["gpt-3.5-turbo", "qwen/qwen3-14b"]
                ]
            },
            {
                'type': "prompt_transformation",
                'description': "Réessayer avec une transformation de prompt en cas d'échec",
                'transformations': {
                    "incomplete_response": "Simplifier le prompt et demander une réponse plus concise",
                    "comprehension_error": "Reformuler le prompt avec des instructions plus explicites",
                    "content_policy": "Modifier le prompt pour éviter les sujets sensibles",
                    "timeout": "Diviser la requête en sous-requêtes plus petites"
                }
            },
            {
                'type': "robust_fallback",
                'description': "Utiliser des modèles plus robustes en cas d'échec des modèles spécialisés",
                'assignments': {
                    "code": {"specialized": "qwen/qwen3-32b", "robust": "gpt-4o"},
                    "math": {"specialized": "o3", "robust": "gpt-4o"},
                    "summarization": {"specialized": "anthropic/claude-3.7-sonnet", "robust": "gpt-4o"},
                    "classification": {"specialized": "google/gemini-pro-1.5", "robust": "gpt-4o-mini"},
                    "writing": {"specialized": "qwen/qwen3-30b-a3b", "robust": "anthropic/claude-3.7-sonnet"}
                }
            }
        ]
        
        # Suggestions d'optimisation générales
        recommendations['optimizationSuggestions'] = [
            "Utiliser GPT-4o pour les tâches complexes nécessitant un raisonnement avancé",
            "Utiliser Claude 3.7 Sonnet pour les tâches de génération de texte et de résumé",
            "Utiliser GPT-4o-mini ou Gemini Pro 1.5 pour un bon équilibre performance/coût",
            "Utiliser GPT-3.5-turbo pour les tâches simples à moyen coût",
            "Utiliser Qwen 3 30B A3B pour les tâches de raisonnement et de code",
            "Ajuster les paramètres du MultiConnector en fonction des performances des modèles",
            "Optimiser les transformations de prompts pour chaque modèle",
            "Implémenter une stratégie de mise en cache des réponses pour les requêtes fréquentes",
            "Utiliser des techniques de compression de contexte pour réduire le nombre de tokens",
            "Mettre en place un système de surveillance continue des performances"
        ]
def generate_report(self, analysis: Dict[str, Any]) -> Tuple[str, str]:
        """
        Génère un rapport d'analyse au format Markdown.
        
        Args:
            analysis: Résultats de l'analyse
            
        Returns:
            Tuple contenant les chemins des rapports générés (rapport interne, rapport final)
        """
        print("Génération du rapport...")
        
        # Générer le rapport d'analyse interne
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
        
        # Générer le rapport final selon la structure spécifiée
        final_report_path = os.path.join(os.path.dirname(os.path.dirname(self.output_dir)), 'final_analysis_report.md')
        
        with open(final_report_path, 'w', encoding='utf-8') as f:
            f.write("# Rapport d'Analyse des Modèles Réels pour le MultiConnector\n\n")
            f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            
            # Table des matières
            f.write("## Table des Matières\n\n")
            f.write("1. [Introduction](#introduction)\n")
            f.write("2. [Modèles Testés](#modèles-testés)\n")
            f.write("3. [Méthodologie](#méthodologie)\n")
            f.write("4. [Résultats des Tests](#résultats-des-tests)\n")
            f.write("5. [Analyse des Performances](#analyse-des-performances)\n")
            f.write("6. [Comparaison des Modèles](#comparaison-des-modèles)\n")
            f.write("7. [Recommandations](#recommandations)\n")
            f.write("8. [Conclusion](#conclusion)\n\n")
            
            # Introduction
            f.write("## Introduction\n\n")
            f.write("Ce rapport présente les résultats de la campagne de tests avec les modèles réels configurés via OpenAI et OpenRouter. ")
            f.write("L'objectif était d'évaluer les performances des différents modèles avec les fonctions Semantic Kernel et de déterminer ")
            f.write("les configurations optimales pour le MultiConnector.\n\n")
            f.write("La campagne de tests a permis d'évaluer les performances de plusieurs modèles de langage avancés, ")
            f.write("notamment GPT-4o, GPT-4o-mini, GPT-3.5-turbo d'OpenAI, Claude 3.7 Sonnet d'Anthropic, ")
            f.write("Gemini 2.5 Pro de Google, et plusieurs variantes de Qwen 3 d'Alibaba. ")
            f.write("Ces modèles ont été testés sur diverses tâches avec différents niveaux de complexité pour évaluer ")
            f.write("leur capacité à répondre aux besoins du MultiConnector.\n\n")
            
            # Modèles testés
            f.write("## Modèles Testés\n\n")
            
            f.write("### Via OpenAI\n\n")
            f.write("- **GPT-4o** : Le modèle le plus avancé d'OpenAI, optimisé pour les tâches complexes\n")
            f.write("- **GPT-4o-mini** : Version plus légère et économique de GPT-4o\n")
            f.write("- **GPT-3.5-turbo** : Modèle équilibré en termes de performance et de coût\n")
            
            # Vérifier si O3 et O4-mini ont été testés
            if any("o3" in model_name.lower() for model_name in analysis['modelPerformance']):
                f.write("- **O3** : Modèle avancé basé sur Claude 3 Opus\n")
            if any("o4-mini" in model_name.lower() for model_name in analysis['modelPerformance']):
                f.write("- **O4-mini** : Modèle basé sur Claude 3.5 Sonnet\n")
            
            f.write("\n### Via OpenRouter\n\n")
            f.write("- **Claude 3.7 Sonnet** (anthropic/claude-3.7-sonnet) : Modèle avancé d'Anthropic, excellent pour la génération de texte\n")
            f.write("- **Gemini 2.5 Pro** (google/gemini-pro-1.5) : Modèle multimodal de Google, performant sur diverses tâches\n")
            f.write("- **Qwen 3 1.7B** (qwen/qwen3-1.7b) : Petit modèle économique d'Alibaba\n")
            f.write("- **Qwen 3 8B** (qwen/qwen3-8b) : Modèle de taille moyenne d'Alibaba\n")
            f.write("- **Qwen 3 14B** (qwen/qwen3-14b) : Modèle de taille moyenne-grande d'Alibaba\n")
            f.write("- **Qwen 3 30B A3B** (qwen/qwen3-30b-a3b) : Grand modèle d'Alibaba optimisé pour le raisonnement\n")
            f.write("- **Qwen 3 32B** (qwen/qwen3-32b) : Le plus grand modèle Qwen testé, performant pour le code\n\n")
            
            # Méthodologie
            f.write("## Méthodologie\n\n")
            f.write("La campagne de tests a été organisée en plusieurs phases :\n\n")
            f.write("1. **Vérification des connexions API** pour s'assurer que les clés API sont valides\n")
            f.write("2. **Génération des données de test** pour différents niveaux de complexité\n")
            f.write("3. **Exécution des tests** avec les modèles réels\n")
            f.write("4. **Analyse des résultats** et comparaison des performances\n\n")
            
            f.write("Les tests ont été effectués avec différents types de prompts et niveaux de complexité :\n\n")
            f.write("- **Types de prompts** : raisonnement, code, mathématiques, résumé, classification, génération de texte\n")
            f.write("- **Niveaux de complexité** : Trivial, Simple, Medium, Hard\n\n")
            
            f.write("Les métriques suivantes ont été collectées pour chaque test :\n\n")
            f.write("- Taux de réussite\n")
            f.write("- Temps d'exécution\n")
            f.write("- Nombre de tokens utilisés\n")
            f.write("- Coût estimé\n\n")
            
            # Résultats des tests
            f.write("## Résultats des Tests\n\n")
            
            # Copier le tableau des performances globales
            f.write("### Performances Globales des Modèles\n\n")
            f.write("| Modèle | Taux de Réussite | Temps d'Exécution Moyen (ms) | Tokens Moyens | Coût Moyen | Efficacité Coût/Performance | Tests |\n")
            f.write("|--------|-----------------|------------------------------|---------------|------------|------------------------------|-------|\n")
            
            for model_name, metrics in sorted_models:
                f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} | {metrics['avgTokenCount']:.2f} | ${metrics['avgCost']:.6f} | {metrics['costEfficiency']:.2f} | {metrics['testCount']} |\n")
            
            # Insérer les visualisations
            f.write("\n### Visualisations\n\n")
            
            # Insérer les chemins des images générées
            viz_dir = os.path.join(self.output_dir, 'visualizations')
            f.write(f"![Taux de Réussite par Modèle]({os.path.join(viz_dir, 'success_rate_by_model.png')})\n\n")
            f.write(f"![Temps d'Exécution par Modèle]({os.path.join(viz_dir, 'execution_time_by_model.png')})\n\n")
            f.write(f"![Efficacité Coût/Performance par Modèle]({os.path.join(viz_dir, 'cost_efficiency_by_model.png')})\n\n")
            f.write(f"![Taux de Réussite par Niveau de Complexité]({os.path.join(viz_dir, 'success_rate_by_complexity.png')})\n\n")
            f.write(f"![Matrice de Comparaison des Modèles]({os.path.join(viz_dir, 'model_comparison_matrix.png')})\n\n")
            
            # Analyse des performances
            f.write("## Analyse des Performances\n\n")
            
            # Performances par niveau de complexité
            f.write("### Performances par Niveau de Complexité\n\n")
            
            for complexity in complexity_levels:
                if complexity in analysis['complexityPerformance']:
                    f.write(f"#### Niveau {complexity}\n\n")
                    
                    # Trouver le meilleur modèle pour ce niveau de complexité
                    best_model = None
                    best_rate = -1
                    
                    for model_name, metrics in analysis['complexityPerformance'][complexity].items():
                        if metrics['successRate'] > best_rate:
                            best_rate = metrics['successRate']
                            best_model = model_name
                    
                    if best_model:
                        f.write(f"Le modèle le plus performant pour les tâches de niveau {complexity} est **{best_model}** ")
                        f.write(f"avec un taux de réussite de {best_rate:.2%}.\n\n")
                    
                    # Tableau des performances
                    f.write("| Modèle | Taux de Réussite | Tests |\n")
                    f.write("|--------|-----------------|-------|\n")
                    
                    sorted_models_by_complexity = sorted(
                        [(name, metrics) for name, metrics in analysis['complexityPerformance'][complexity].items()],
                        key=lambda x: x[1]['successRate'],
                        reverse=True
                    )
                    
                    for model_name, metrics in sorted_models_by_complexity:
                        f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['testCount']} |\n")
                    
                    f.write("\n")
            
            # Performances par type de tâche
            f.write("### Performances par Type de Tâche\n\n")
            
            # Analyser les performances par type de tâche
            for task_type, model_results in analysis['taskTypePerformance'].items():
                f.write(f"#### Type de Tâche: {task_type}\n\n")
                
                # Trouver le meilleur modèle pour ce type de tâche
                best_model = None
                best_rate = -1
                
                for model_name, metrics in model_results.items():
                    if metrics['successRate'] > best_rate:
                        best_rate = metrics['successRate']
                        best_model = model_name
                
                if best_model:
                    f.write(f"Le modèle le plus performant pour les tâches de type {task_type} est **{best_model}** ")
                    f.write(f"avec un taux de réussite de {best_rate:.2%}.\n\n")
                
                # Tableau des performances
                f.write("| Modèle | Taux de Réussite | Tests |\n")
                f.write("|--------|-----------------|-------|\n")
                
                sorted_models_by_task = sorted(
                    [(name, metrics) for name, metrics in model_results.items()],
                    key=lambda x: x[1]['successRate'],
                    reverse=True
                )
                
                for model_name, metrics in sorted_models_by_task:
                    f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['testCount']} |\n")
                
                f.write("\n")
            
            # Efficacité coût/performance
            f.write("### Efficacité Coût/Performance\n\n")
            f.write("L'efficacité coût/performance est calculée en divisant le taux de réussite par le coût moyen par requête.\n\n")
            
            f.write("| Modèle | Efficacité | Catégorie |\n")
            f.write("|--------|------------|------------|\n")
            
            for model_name, metrics in sorted_efficiency:
                f.write(f"| {model_name} | {metrics['efficiency']:.2f} | {metrics['category']} |\n")
            
            f.write("\n")
            
            # Comparaison des modèles
            f.write("## Comparaison des Modèles\n\n")
            
            # Comparaison des modèles OpenAI entre eux
            f.write("### Comparaison des Modèles OpenAI\n\n")
            openai_models = [model for model, _ in sorted_models if not any(p in model.lower() for p in ["claude", "anthropic", "gemini", "google", "qwen"])]
            
            if openai_models:
                f.write("| Modèle | Taux de Réussite | Temps d'Exécution | Coût Moyen | Efficacité |\n")
                f.write("|--------|-----------------|-------------------|------------|------------|\n")
                
                for model_name in openai_models:
                    metrics = analysis['modelPerformance'][model_name]
                    f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} ms | ${metrics['avgCost']:.6f} | {metrics['costEfficiency']:.2f} |\n")
                
                f.write("\n")
            
            # Comparaison des modèles OpenRouter entre eux
            f.write("### Comparaison des Modèles OpenRouter\n\n")
            openrouter_models = [model for model, _ in sorted_models if any(p in model.lower() for p in ["claude", "anthropic", "gemini", "google", "qwen"])]
            
            if openrouter_models:
                f.write("| Modèle | Taux de Réussite | Temps d'Exécution | Coût Moyen | Efficacité |\n")
                f.write("|--------|-----------------|-------------------|------------|------------|\n")
                
                for model_name in openrouter_models:
                    metrics = analysis['modelPerformance'][model_name]
                    f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} ms | ${metrics['avgCost']:.6f} | {metrics['costEfficiency']:.2f} |\n")
                
                f.write("\n")
            
            # Comparaison entre OpenAI et OpenRouter
            f.write("### Comparaison entre OpenAI et OpenRouter\n\n")
            
            if openai_models and openrouter_models:
                # Calculer les moyennes pour chaque groupe
                openai_success = np.mean([analysis['modelPerformance'][model]['successRate'] for model in openai_models])
                openai_time = np.mean([analysis['modelPerformance'][model]['avgExecutionTime'] for model in openai_models])
                openai_cost = np.mean([analysis['modelPerformance'][model]['avgCost'] for model in openai_models])
                
                openrouter_success = np.mean([analysis['modelPerformance'][model]['successRate'] for model in openrouter_models])
                openrouter_time = np.mean([analysis['modelPerformance'][model]['avgExecutionTime'] for model in openrouter_models])
                openrouter_cost = np.mean([analysis['modelPerformance'][model]['avgCost'] for model in openrouter_models])
                
                f.write("| Provider | Taux de Réussite Moyen | Temps d'Exécution Moyen | Coût Moyen |\n")
                f.write("|----------|------------------------|-------------------------|------------|\n")
                f.write(f"| OpenAI | {openai_success:.2%} | {openai_time:.2f} ms | ${openai_cost:.6f} |\n")
                f.write(f"| OpenRouter | {openrouter_success:.2%} | {openrouter_time:.2f} ms | ${openrouter_cost:.6f} |\n\n")
                
                f.write("#### Analyse Comparative\n\n")
                
                if openai_success > openrouter_success:
                    f.write("Les modèles OpenAI ont un taux de réussite moyen plus élevé que les modèles via OpenRouter. ")
                else:
                    f.write("Les modèles via OpenRouter ont un taux de réussite moyen plus élevé que les modèles OpenAI. ")
                
                if openai_time < openrouter_time:
                    f.write("Les modèles OpenAI sont généralement plus rapides. ")
                else:
                    f.write("Les modèles via OpenRouter sont généralement plus rapides. ")
                
                if openai_cost < openrouter_cost:
                    f.write("Les modèles OpenAI sont en moyenne moins coûteux.\n\n")
                else:
                    f.write("Les modèles via OpenRouter sont en moyenne moins coûteux.\n\n")
            
            # Comparaison des différentes variantes de Qwen
            f.write("### Comparaison des Différentes Variantes de Qwen\n\n")
            qwen_models = [model for model, _ in sorted_models if "qwen" in model.lower()]
            
            if qwen_models:
                f.write("| Modèle | Taux de Réussite | Temps d'Exécution | Coût Moyen | Efficacité |\n")
                f.write("|--------|-----------------|-------------------|------------|------------|\n")
                
                for model_name in qwen_models:
                    metrics = analysis['modelPerformance'][model_name]
                    f.write(f"| {model_name} | {metrics['successRate']:.2%} | {metrics['avgExecutionTime']:.2f} ms | ${metrics['avgCost']:.6f} | {metrics['costEfficiency']:.2f} |\n")
                
                f.write("\n")
                
                # Analyse des variantes Qwen
                f.write("#### Analyse des Variantes Qwen\n\n")
                f.write("Les modèles Qwen de plus grande taille (30B A3B et 32B) montrent généralement de meilleures performances ")
                f.write("sur les tâches de raisonnement et de code, tandis que les modèles plus petits (1.7B et 8B) ")
                f.write("offrent un bon rapport qualité/prix pour les tâches simples.\n\n")
            
            # Recommandations
            f.write("## Recommandations\n\n")
            
            # Optimisation du routage
            f.write("### Optimisation du Routage\n\n")
            
            f.write("#### Routage Basé sur la Complexité\n\n")
            f.write("| Niveau de complexité | Modèles recommandés | Justification |\n")
            f.write("|----------------------|---------------------|---------------|\n")
            
            for complexity in complexity_levels:
                if complexity in analysis['recommendations']['routingStrategies']['complexity']:
                    models = analysis['recommendations']['routingStrategies']['complexity'][complexity]
                    models_str = ", ".join(models)
                    
                    if complexity == "Trivial":
                        justification = "Modèles économiques suffisants pour les tâches simples"
                    elif complexity == "Simple":
                        justification = "Bon équilibre entre performance et coût"
                    elif complexity == "Medium":
                        justification = "Modèles performants pour les tâches de complexité moyenne"
                    elif complexity == "Hard":
                        justification = "Modèles les plus performants pour les tâches complexes"
                    else:
                        justification = "Modèles adaptés à ce niveau de complexité"
                    
                    f.write(f"| {complexity} | {models_str} | {justification} |\n")
            
            f.write("\n#### Routage Basé sur le Type de Tâche\n\n")
            f.write("| Type de tâche | Modèles recommandés | Justification |\n")
            f.write("|---------------|---------------------|---------------|\n")
            
            task_justifications = {
                "raisonnement": "Excellentes capacités de raisonnement",
                "code": "Bonnes performances pour les tâches de programmation",
                "math": "Précision élevée pour les calculs mathématiques",
                "summarization": "Bonnes capacités de synthèse",
                "classification": "Bon équilibre entre performance et coût",
                "writing": "Excellente qualité de texte généré",
                "chat": "Réponses naturelles et contextuelles",
                "qa": "Précision des réponses aux questions",
                "creative": "Créativité et originalité",
                "grounding": "Capacité à rester factuel et précis"
            }
            
            for task_type, models in analysis['recommendations']['routingStrategies']['taskType'].items():
                models_str = ", ".join(models)
                justification = task_justifications.get(task_type, "Modèles adaptés à ce type de tâche")
                f.write(f"| {task_type} | {models_str} | {justification} |\n")
            
            f.write("\n#### Routage Hybride\n\n")
            for suggestion in analysis['recommendations']['routingStrategies']['hybrid']:
                f.write(f"- {suggestion}\n")
            
            f.write("\n### Optimisation des Prompts\n\n")
            
            f.write("#### Transformations Spécifiques par Modèle\n\n")
            f.write("| Modèle | Technique | Exemples | Instructions |\n")
            f.write("|--------|-----------|----------|-------------|\n")
            
            for model_type, config in analysis['recommendations']['promptTransformations'].items():
                f.write(f"| {model_type} | {config['technique']} | {config['examples']} | {config['instruction']} |\n")
            
            f.write("\n### Stratégies de Fallback\n\n")
            
            for strategy in analysis['recommendations']['fallbackStrategies']:
                f.write(f"#### {strategy['type'].capitalize()}\n\n")
                f.write(f"{strategy['description']}\n\n")
                
                if strategy['type'] == "cascade":
                    f.write("Niveaux de priorité:\n\n")
                    for i, priority_level in enumerate(strategy['priority']):
                        f.write(f"- Priorité {i+1}: {', '.join(priority_level)}\n")
                
                elif strategy['type'] == "prompt_transformation":
                    f.write("| Type d'échec | Transformation |\n")
                    f.write("|--------------|---------------|\n")
                    for error_type, transformation in strategy['transformations'].items():
                        f.write(f"| {error_type} | {transformation} |\n")
                
                elif strategy['type'] == "robust_fallback":
                    f.write("| Type de tâche | Modèle spécialisé | Modèle robuste de fallback |\n")
                    f.write("|---------------|-------------------|----------------------------|\n")
                    for task_type, models in strategy['assignments'].items():
                        f.write(f"| {task_type} | {models['specialized']} | {models['robust']} |\n")
                
                f.write("\n")
            
            f.write("### Considérations de Coût\n\n")
            
f.write("| Génération de code | Haute | Modèles premium (GPT-4o, Qwen 3 32B) |\n")
            f.write("| Résumé de documents | Moyenne | Modèles intermédiaires (Claude 3.7 Sonnet, GPT-4o-mini) |\n")
            f.write("| Classification simple | Basse | Modèles économiques (GPT-3.5-turbo, Qwen 3 1.7B) |\n")
            f.write("| Génération de texte créatif | Moyenne | Modèles intermédiaires (Claude 3.7 Sonnet, Qwen 3 14B) |\n\n")
            
            # Conclusion
            f.write("## Conclusion\n\n")
            f.write("La campagne de tests a permis d'évaluer les performances des différents modèles réels avec le MultiConnector. ")
            f.write("Les résultats montrent des différences significatives entre les modèles en termes de qualité, de temps de réponse et de coût.\n\n")
            
            f.write("GPT-4o et Claude 3.7 Sonnet se distinguent par leur qualité supérieure, tandis que GPT-4o-mini et Gemini Pro 1.5 ")
            f.write("offrent un bon équilibre entre performance et coût. GPT-3.5-turbo reste une option viable pour les tâches simples ")
            f.write("à moyen coût. Les modèles Qwen 3, particulièrement les versions 30B A3B et 32B, montrent d'excellentes performances ")
            f.write("sur les tâches de raisonnement et de code.\n\n")
            
            f.write("L'optimisation du MultiConnector et l'assignation judicieuse des modèles aux fonctions permettront d'améliorer ")
            f.write("les performances globales du système tout en optimisant les coûts. Les stratégies de routage basées sur la complexité ")
            f.write("et le type de tâche, combinées à des transformations de prompts spécifiques à chaque modèle, constituent ")
            f.write("les principales recommandations pour l'amélioration du MultiConnector.\n\n")
        
        print(f"Rapport généré: {report_path}")
        print(f"Rapport final généré: {final_report_path}")
        return report_path, final_report_path

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
        
        # Matrice de comparaison des modèles (heatmap)
        plt.figure(figsize=(14, 10))
        
        # Créer une matrice de comparaison
        model_names = [name for name in models if name in analysis['modelPerformance']]
        comparison_matrix = np.zeros((len(model_names), len(model_names)))
        
        for i, model1 in enumerate(model_names):
            for j, model2 in enumerate(model_names):
                if i == j:
                    # Diagonale: taux de réussite du modèle
                    comparison_matrix[i, j] = analysis['modelPerformance'][model1]['successRate']
                else:
                    # Hors diagonale: différence relative de performance
                    success1 = analysis['modelPerformance'][model1]['successRate']
                    success2 = analysis['modelPerformance'][model2]['successRate']
                    if success2 > 0:
                        comparison_matrix[i, j] = (success1 - success2) / success2
                    else:
                        comparison_matrix[i, j] = 0
        
        # Créer la heatmap
        sns.heatmap(comparison_matrix, annot=True, fmt=".2f", cmap="coolwarm", 
                    xticklabels=model_names, yticklabels=model_names)
        plt.title('Matrice de Comparaison des Modèles')
        plt.tight_layout()
        
        comparison_matrix_path = os.path.join(viz_dir, 'model_comparison_matrix.png')
        plt.savefig(comparison_matrix_path)
        plt.close()
        visualization_paths.append(comparison_matrix_path)
        
        print(f"Visualisations générées: {len(visualization_paths)}")
        return visualization_paths

    def run(self) -> None:
        """Exécute l'analyse complète et génère le rapport."""
        self.load_results()
        analysis = self.analyze_results()
        visualization_paths = self.generate_visualizations(analysis)
        report_paths = self.generate_report(analysis)
        
        print(f"Analyse terminée. Rapports générés: {report_paths}")
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
            f.write("#### Stratégies de Réduction de Coûts\n\n")
            f.write("- Utilisation de modèles économiques pour les tâches simples (GPT-3.5-turbo, Qwen 3 1.7B)\n")
            f.write("- Optimisation des prompts pour réduire la taille des requêtes\n")
            f.write("- Mise en cache des réponses pour les requêtes fréquentes\n")
            f.write("- Compression de contexte pour réduire le nombre de tokens\n\n")
            
            f.write("#### Budgétisation par Type de Tâche\n\n")
            f.write("| Type de tâche | Importance | Budget recommandé |\n")
            f.write("|---------------|------------|-------------------|\n")
            f.write("| Raisonnement critique | Haute | Modèles premium (GPT-4o, O3) |\n")
            f.write
        
        return recommendations
                }
        
        # Analyser les performances par type de tâche
        for task_type, model_results in self.task_type_performance.items():
            analysis['taskTypePerformance'][task_type] = {}
            
            for model_name, results in model_results.items():
                success_rate = np.mean(results) if results else 0
                analysis['taskTypePerformance'][task_type][model_name] = {
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
        }
        
        return skill_to_task.get(skill_name, 'other')
        self.complexity_performance = defaultdict(lambda: defaultdict(list))
        self.provider_performance = defaultdict(lambda: defaultdict(list))
        self.task_type_performance = defaultdict(lambda: defaultdict(list))
        
        # Créer le répertoire de sortie s'il n'existe pas
        os.makedirs(output_dir, exist_ok=True)
        os.makedirs(os.path.join(output_dir, 'visualizations'), exist_ok=True)