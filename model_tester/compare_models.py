#!/usr/bin/env python3
"""
Script pour comparer les performances des différents modèles de langage.
Ce script utilise les API OpenAI et OpenRouter pour tester les modèles sur différentes tâches.
"""

import os
import sys
import json
import time
import argparse
import asyncio
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
from typing import Dict, List, Any, Optional, Tuple
from dotenv import load_dotenv
import requests
from tqdm import tqdm

# Chargement des variables d'environnement
load_dotenv()

# Configuration des APIs
API_CONFIGS = {
    "openai": {
        "api_key": os.environ.get("OPENAI_API_KEY", ""),
        "base_url": os.environ.get("OPENAI_BASE_URL", "https://api.openai.com/v1")
    },
    "openrouter": {
        "api_key": os.environ.get("OPENROUTER_API_KEY", ""),
        "base_url": os.environ.get("OPENROUTER_BASE_URL", "https://openrouter.ai/api/v1")
    }
}

# Modèles à tester
MODELS = {
    # Modèles OpenAI
    "gpt-4o": {
        "provider": "openai",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.01,
        "cost_per_1k_tokens_output": 0.03
    },
    "gpt-4o-mini": {
        "provider": "openai",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.005,
        "cost_per_1k_tokens_output": 0.015
    },
    "gpt-3.5-turbo": {
        "provider": "openai",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.0015,
        "cost_per_1k_tokens_output": 0.002
    },
    "o3": {
        "provider": "openai",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.015,
        "cost_per_1k_tokens_output": 0.075
    },
    "o4-mini": {
        "provider": "openai",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.005,
        "cost_per_1k_tokens_output": 0.015
    },
    
    # Modèles via OpenRouter
    "anthropic/claude-3.7-sonnet": {
        "provider": "openrouter",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.008,
        "cost_per_1k_tokens_output": 0.024
    },
    "google/gemini-pro-1.5": {
        "provider": "openrouter",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.0035,
        "cost_per_1k_tokens_output": 0.0035
    },
    "qwen/qwen3-1.7b": {
        "provider": "openrouter",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.0005,
        "cost_per_1k_tokens_output": 0.0005
    },
    "qwen/qwen3-8b": {
        "provider": "openrouter",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.001,
        "cost_per_1k_tokens_output": 0.001
    },
    "qwen/qwen3-14b": {
        "provider": "openrouter",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.002,
# Jeu de prompts pour les tests
TEST_PROMPTS = [
    # Raisonnement
    {
        "category": "raisonnement",
        "complexity": "trivial",
        "prompt": "Quelle est la capitale de la France?",
        "expected_answer": "Paris"
    },
    {
        "category": "raisonnement",
        "complexity": "simple",
        "prompt": "Explique le paradoxe du bateau de Thésée en termes simples.",
        "expected_keywords": ["identité", "remplacement", "même bateau"]
    },
    {
        "category": "raisonnement",
        "complexity": "medium",
        "prompt": "Compare et contraste les approches déontologiques et conséquentialistes en éthique.",
        "expected_keywords": ["déontologique", "conséquentialiste", "Kant", "utilitarisme"]
    },
    
    # Code
    {
        "category": "code",
        "complexity": "simple",
        "prompt": "Écris une fonction Python qui calcule la suite de Fibonacci jusqu'à n termes.",
        "expected_keywords": ["def", "fibonacci", "return"]
    },
    {
        "category": "code",
        "complexity": "medium",
        "prompt": "Implémente un algorithme de tri fusion (merge sort) en JavaScript.",
        "expected_keywords": ["function", "mergeSort", "O(n log n)"]
    },
    
    # Math
    {
        "category": "math",
        "complexity": "simple",
        "prompt": "Résous l'équation quadratique suivante: 3x² + 5x - 2 = 0",
        "expected_keywords": ["-2", "1/3"]
    },
    
    # Summarization
    {
        "category": "summarization",
        "complexity": "simple",
        "prompt": "Résume le texte suivant en 3 phrases: 'Le réchauffement climatique est l'augmentation à long terme de la température moyenne du système climatique de la Terre. C'est un aspect majeur du changement climatique, démontré par des mesures directes de température et par divers effets du réchauffement. Le terme désigne généralement le réchauffement observé depuis le début du 20e siècle, résultant en grande partie des émissions de gaz à effet de serre dues aux activités humaines.'",
        "expected_keywords": ["réchauffement", "température", "gaz à effet de serre"]
    },
    
    # Classification
    {
        "category": "classification",
        "complexity": "simple",
        "prompt": "Classifie le texte suivant comme positif, négatif ou neutre: 'Le nouveau restaurant du quartier offre une cuisine délicieuse et un service impeccable.'",
        "expected_answer": "positif"
    },
    
    # Writing
    {
        "category": "writing",
        "complexity": "simple",
        "prompt": "Écris un email de remerciement à un collègue qui t'a aidé sur un projet.",
        "expected_keywords": ["merci", "aide", "projet"]
    },
    
    # QA
    {
        "category": "qa",
        "complexity": "simple",
        "prompt": "Quels sont les principaux symptômes du COVID-19?",
        "expected_keywords": ["fièvre", "toux", "fatigue", "perte de goût", "perte d'odorat"]
    },
    
    # Creative
    {
        "category": "creative",
        "complexity": "simple",
        "prompt": "Invente une courte histoire sur un robot qui découvre les émotions.",
        "expected_keywords": ["robot", "émotions"]
    }
]

class ModelComparer:
    """Classe pour comparer les performances des modèles de langage."""
    
    def __init__(self, models_to_test=None, output_dir="../results/model_comparison"):
        """
        Initialise le comparateur de modèles.
        
        Args:
            models_to_test: Liste des modèles à tester (None pour tous)
            output_dir: Répertoire de sortie pour les résultats
        """
        self.models_to_test = models_to_test or list(MODELS.keys())
        self.output_dir = output_dir
        self.results = []
        
        # Créer le répertoire de sortie s'il n'existe pas
        os.makedirs(output_dir, exist_ok=True)
        os.makedirs(os.path.join(output_dir, "visualizations"), exist_ok=True)
        os.makedirs(os.path.join(output_dir, "raw_responses"), exist_ok=True)
        
        # Vérifier les clés API
        self._check_api_keys()
    
    def _check_api_keys(self):
        """Vérifie que les clés API nécessaires sont configurées."""
        missing_keys = []
        
        if not API_CONFIGS["openai"]["api_key"] and any(MODELS[model]["provider"] == "openai" for model in self.models_to_test):
            missing_keys.append("OPENAI_API_KEY")
        
        if not API_CONFIGS["openrouter"]["api_key"] and any(MODELS[model]["provider"] == "openrouter" for model in self.models_to_test):
            missing_keys.append("OPENROUTER_API_KEY")
        
        if missing_keys:
            print(f"⚠️ Attention: Les clés API suivantes ne sont pas configurées: {', '.join(missing_keys)}")
            print("Certains modèles ne pourront pas être testés.")
            
            # Filtrer les modèles qui peuvent être testés
            self.models_to_test = [
                model for model in self.models_to_test 
                if not (MODELS[model]["provider"] == "openai" and not API_CONFIGS["openai"]["api_key"]) and
                not (MODELS[model]["provider"] == "openrouter" and not API_CONFIGS["openrouter"]["api_key"])
            ]
            
            if not self.models_to_test:
async def test_model(self, model: str, prompt_data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Teste un modèle avec un prompt donné.
        
        Args:
            model: Nom du modèle
            prompt_data: Données du prompt
            
        Returns:
            Résultat du test
        """
        prompt = prompt_data["prompt"]
        
        model_config = MODELS[model]
        provider = model_config["provider"]
        api_config = API_CONFIGS[provider]
        
        headers = {
            "Authorization": f"Bearer {api_config['api_key']}",
            "Content-Type": "application/json"
        }
        
        # Ajouter les en-têtes spécifiques à OpenRouter
        if provider == "openrouter":
            headers["HTTP-Referer"] = "https://semantic-fleet.myia.io"
            headers["X-Title"] = "Semantic Fleet Model Comparer"
        
        # Préparer les données de la requête
        if model_config["type"] == "chat":
            endpoint = f"{api_config['base_url']}/chat/completions"
            data = {
                "model": model,
                "messages": [
                    {"role": "system", "content": "You are a helpful assistant."},
                    {"role": "user", "content": prompt}
                ],
                "max_tokens": 1000,
                "temperature": 0.7
            }
        else:
            endpoint = f"{api_config['base_url']}/completions"
            data = {
                "model": model,
                "prompt": prompt,
                "max_tokens": 1000,
                "temperature": 0.7
            }
        
        # Envoyer la requête
        start_time = time.time()
        result = {
            "model": model,
            "prompt": prompt,
            "category": prompt_data["category"],
            "complexity": prompt_data["complexity"],
            "success": False,
            "error": None,
            "response": None,
            "response_time": 0,
            "tokens": {
                "prompt": 0,
                "completion": 0,
                "total": 0
            },
            "cost": 0
        }
        
        try:
            response = requests.post(
                endpoint,
                headers=headers,
                json=data,
                timeout=60  # Timeout de 60 secondes
            )
            
            # Calculer le temps de réponse
            result["response_time"] = time.time() - start_time
            
            # Traiter la réponse
            if response.status_code == 200:
                response_json = response.json()
                result["response"] = response_json
                result["success"] = True
                
                # Extraire le texte de la réponse
                if model_config["type"] == "chat":
                    result["completion_text"] = response_json.get("choices", [{}])[0].get("message", {}).get("content", "")
                else:
                    result["completion_text"] = response_json.get("choices", [{}])[0].get("text", "")
                
                # Extraire les informations sur les tokens
                usage = response_json.get("usage", {})
                result["tokens"]["prompt"] = usage.get("prompt_tokens", 0)
                result["tokens"]["completion"] = usage.get("completion_tokens", 0)
                result["tokens"]["total"] = usage.get("total_tokens", 0)
                
                # Calculer le coût
                result["cost"] = (
                    result["tokens"]["prompt"] * model_config["cost_per_1k_tokens_input"] / 1000 +
                    result["tokens"]["completion"] * model_config["cost_per_1k_tokens_output"] / 1000
                )
                
                # Évaluer la réponse
                result["evaluation"] = self.evaluate_response(
                    result["completion_text"],
                    prompt_data
                )
                
            else:
                result["error"] = {
                    "status_code": response.status_code,
                    "message": response.text
                }
                
                # Essayer de parser l'erreur JSON si possible
                try:
                    error_json = response.json()
                    result["error"]["details"] = error_json
                except:
                    pass
        
        except Exception as e:
            result["error"] = {
                "exception": str(e),
                "type": type(e).__name__
            }
            result["response_time"] = time.time() - start_time
        
        return result
    
    def evaluate_response(self, response: str, prompt_data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Évalue la réponse d'un modèle.
        
        Args:
            response: Réponse du modèle
            prompt_data: Données du prompt
            
        Returns:
async def run_tests(self, prompts_to_test=None):
        """
        Exécute les tests sur tous les modèles.
        
        Args:
            prompts_to_test: Liste des prompts à tester (None pour tous)
        """
        prompts_to_test = prompts_to_test or TEST_PROMPTS
        
        print(f"Exécution des tests sur {len(self.models_to_test)} modèles et {len(prompts_to_test)} prompts...")
        
        # Créer une barre de progression
        total_tests = len(self.models_to_test) * len(prompts_to_test)
        progress_bar = tqdm(total=total_tests, desc="Tests en cours")
        
        for prompt_data in prompts_to_test:
            for model in self.models_to_test:
                try:
                    result = await self.test_model(model, prompt_data)
                    self.results.append(result)
                    
                    # Sauvegarder la réponse brute
                    self._save_raw_response(result)
                    
                    # Afficher un résumé du résultat
                    if result["success"]:
                        status = "✅" if result.get("evaluation", {}).get("score", 0) >= 0.5 else "⚠️"
                        score = result.get("evaluation", {}).get("score", 0)
                        progress_bar.write(f"{status} {model} - {prompt_data['category']} ({prompt_data['complexity']}) - Score: {score:.2f} - Temps: {result['response_time']:.2f}s - Coût: ${result['cost']:.6f}")
                    else:
                        progress_bar.write(f"❌ {model} - {prompt_data['category']} ({prompt_data['complexity']}) - Erreur: {result.get('error', {}).get('message', 'Inconnue')}")
                
                except Exception as e:
                    progress_bar.write(f"❌ {model} - {prompt_data['category']} ({prompt_data['complexity']}) - Exception: {str(e)}")
                
                # Mettre à jour la barre de progression
                progress_bar.update(1)
        
        progress_bar.close()
        
        # Générer les rapports
        self.generate_reports()
        
        print(f"Tests terminés. Résultats sauvegardés dans {self.output_dir}")
    
    def _save_raw_response(self, result):
        """
        Sauvegarde la réponse brute d'un modèle.
        
        Args:
            result: Résultat du test
        """
        """
        model = result["model"]
        category = result["category"]
        complexity = result["complexity"]
        
        # Créer un nom de fichier unique
        filename = f"{model.replace('/', '_')}_{category}_{complexity}.json"
        filepath = os.path.join(self.output_dir, "raw_responses", filename)
        
        # Sauvegarder la réponse
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(result, f, indent=2, ensure_ascii=False)
    
    def generate_reports(self):
        """Génère les rapports d'analyse."""
        print("Génération des rapports...")
        
        # Analyser les résultats
        model_performances = self._analyze_results()
        
        # Générer le rapport principal
        self._generate_main_report(model_performances)
        
        # Générer les visualisations
        self._generate_visualizations(model_performances)
    
    def _analyze_results(self):
        """
        Analyse les résultats des tests.
        
        Returns:
            Dictionnaire des performances par modèle
        """
        print("Analyse des résultats...")
        
        model_performances = {}
        
        # Analyser les performances par modèle
        for result in self.results:
            model = result["model"]
            
            if model not in model_performances:
                model_performances[model] = {
                    "total_tests": 0,
                    "successful_tests": 0,
                    "total_score": 0,
                    "total_time": 0,
                    "total_cost": 0,
                    "total_tokens": 0,
                    "by_category": {},
                    "by_complexity": {}
                }
            
            # Mettre à jour les statistiques globales
            model_performances[model]["total_tests"] += 1
            
            if result["success"]:
                model_performances[model]["successful_tests"] += 1
                model_performances[model]["total_score"] += result.get("evaluation", {}).get("score", 0)
                model_performances[model]["total_time"] += result["response_time"]
                model_performances[model]["total_cost"] += result["cost"]
                model_performances[model]["total_tokens"] += result["tokens"]["total"]
            
            # Mettre à jour les statistiques par catégorie
            category = result["category"]
            if category not in model_performances[model]["by_category"]:
                model_performances[model]["by_category"][category] = {
                    "total_tests": 0,
                    "successful_tests": 0,
                    "total_score": 0
                }
            
            model_performances[model]["by_category"][category]["total_tests"] += 1
            
            if result["success"]:
                model_performances[model]["by_category"][category]["successful_tests"] += 1
                model_performances[model]["by_category"][category]["total_score"] += result.get("evaluation", {}).get("score", 0)
            
def _generate_main_report(self, model_performances):
        """
        Génère le rapport principal.
        
        Args:
            model_performances: Dictionnaire des performances par modèle
        """
        report_path = os.path.join(self.output_dir, "rapport_analyse.md")
        
        with open(report_path, "w", encoding="utf-8") as f:
            f.write("# Rapport d'Analyse des Modèles de Langage\n\n")
            f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            
            # Résultats globaux
            f.write("## Résultats Globaux\n\n")
            f.write("### Performances Globales des Modèles\n\n")
            f.write("| Modèle | Taux de Réussite | Score Moyen | Temps Moyen (s) | Tokens Moyens | Coût Moyen | Efficacité Coût/Performance |\n")
            f.write("|--------|-----------------|-------------|-----------------|---------------|------------|-----------------------------|\n")
            
            # Trier les modèles par score moyen décroissant
            sorted_models = sorted(
                [(name, stats) for name, stats in model_performances.items()],
                key=lambda x: x[1]["total_score"] / x[1]["successful_tests"] if x[1]["successful_tests"] > 0 else 0,
                reverse=True
            )
            
            for model_name, stats in sorted_models:
                success_rate = stats["successful_tests"] / stats["total_tests"] if stats["total_tests"] > 0 else 0
                avg_score = stats["total_score"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
                avg_time = stats["total_time"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
                avg_tokens = stats["total_tokens"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
                avg_cost = stats["total_cost"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
                
                # Calculer l'efficacité coût/performance
                cost_efficiency = avg_score / avg_cost if avg_cost > 0 else float('inf')
                
                f.write(f"| {model_name} | {success_rate:.2%} | {avg_score:.2f} | {avg_time:.2f} | {avg_tokens:.2f} | ${avg_cost:.6f} | {cost_efficiency:.2f} |\n")
            
            # Performances par catégorie
            f.write("\n## Performances par Catégorie\n\n")
            
            for category in TASK_CATEGORIES:
                category_results = {}
                
                for model_name, stats in model_performances.items():
                    if category in stats["by_category"]:
                        category_stats = stats["by_category"][category]
                        success_rate = category_stats["successful_tests"] / category_stats["total_tests"] if category_stats["total_tests"] > 0 else 0
                        avg_score = category_stats["total_score"] / category_stats["successful_tests"] if category_stats["successful_tests"] > 0 else 0
                        
                        category_results[model_name] = {
                            "success_rate": success_rate,
                            "avg_score": avg_score
                        }
                
                if category_results:
                    f.write(f"### {category}\n\n")
                    f.write("| Modèle | Taux de Réussite | Score Moyen |\n")
                    f.write("|--------|-----------------|-------------|\n")
                    
                    # Trier les modèles par score moyen décroissant
                    sorted_models = sorted(
                        [(name, stats) for name, stats in category_results.items()],
                        key=lambda x: x[1]["avg_score"],
                        reverse=True
                    )
                    
                    for model_name, stats in sorted_models:
                        f.write(f"| {model_name} | {stats['success_rate']:.2%} | {stats['avg_score']:.2f} |\n")
                    
                    f.write("\n")
            
            # Performances par niveau de complexité
            f.write("\n## Performances par Niveau de Complexité\n\n")
            
            for complexity in COMPLEXITY_LEVELS:
                complexity_results = {}
                
                for model_name, stats in model_performances.items():
                    if complexity in stats["by_complexity"]:
                        complexity_stats = stats["by_complexity"][complexity]
                        success_rate = complexity_stats["successful_tests"] / complexity_stats["total_tests"] if complexity_stats["total_tests"] > 0 else 0
                        avg_score = complexity_stats["total_score"] / complexity_stats["successful_tests"] if complexity_stats["successful_tests"] > 0 else 0
                        
                        complexity_results[model_name] = {
                            "success_rate": success_rate,
                            "avg_score": avg_score
                        }
                
                if complexity_results:
                    f.write(f"### {complexity}\n\n")
                    f.write("| Modèle | Taux de Réussite | Score Moyen |\n")
                    f.write("|--------|-----------------|-------------|\n")
                    
                    # Trier les modèles par score moyen décroissant
                    sorted_models = sorted(
                        [(name, stats) for name, stats in complexity_results.items()],
                        key=lambda x: x[1]["avg_score"],
                        reverse=True
                    )
                    
                    for model_name, stats in sorted_models:
                        f.write(f"| {model_name} | {stats['success_rate']:.2%} | {stats['avg_score']:.2f} |\n")
                    
                    f.write("\n")
            
            # Recommandations
            f.write("\n## Recommandations\n\n")
            
            # Recommandations par catégorie de tâche
            f.write("### Recommandations par Catégorie de Tâche\n\n")
            f.write("| Catégorie | Modèle Recommandé | Justification |\n")
            f.write("|-----------|-------------------|---------------|\n")
            
            for category in TASK_CATEGORIES:
                best_model = None
                best_score = -1
                
                for model_name, stats in model_performances.items():
                    if category in stats["by_category"]:
                        category_stats = stats["by_category"][category]
                        if category_stats["successful_tests"] > 0:
                            avg_score = category_stats["total_score"] / category_stats["successful_tests"]
                            if avg_score > best_score:
                                best_score = avg_score
                                best_model = model_name
                
                if best_model:
                    f.write(f"| {category} | {best_model} | Score moyen: {best_score:.2f} |\n")
            
            # Recommandations par niveau de complexité
            f.write("\n### Recommandations par Niveau de Complexité\n\n")
            f.write("| Complexité | Modèle Recommandé | Justification |\n")
            f.write("|------------|-------------------|---------------|\n")
            
            for complexity in COMPLEXITY_LEVELS:
                best_model = None
                best_score = -1
                
                for model_name, stats in model_performances.items():
                    if complexity in stats["by_complexity"]:
                        complexity_stats = stats["by_complexity"][complexity]
                        if complexity_stats["successful_tests"] > 0:
                            avg_score = complexity_stats["total_score"] / complexity_stats["successful_tests"]
                            if avg_score > best_score:
                                best_score = avg_score
                                best_model = model_name
                
                if best_model:
                    f.write(f"| {complexity} | {best_model} | Score moyen: {best_score:.2f} |\n")
        
        print(f"Rapport principal généré: {report_path}")
    
    def _generate_visualizations(self, model_performances):
        """
        Génère les visualisations des résultats.
        
        Args:
            model_performances: Dictionnaire des performances par modèle
        """
        print("Génération des visualisations...")
        
        # Créer un répertoire pour les visualisations
        viz_dir = os.path.join(self.output_dir, "visualizations")
        os.makedirs(viz_dir, exist_ok=True)
        
        # Définir un style pour les visualisations
        plt.style.use('seaborn-v0_8-darkgrid')
        
        # 1. Graphique des scores moyens par modèle
        self._generate_avg_score_chart(model_performances, viz_dir)
        
        # 2. Graphique des temps d'exécution par modèle
        self._generate_execution_time_chart(model_performances, viz_dir)
        
        # 3. Graphique de l'efficacité coût/performance
        self._generate_cost_efficiency_chart(model_performances, viz_dir)
    
    def _generate_avg_score_chart(self, model_performances, viz_dir):
        """
        Génère un graphique des scores moyens par modèle.
        
        Args:
            model_performances: Dictionnaire des performances par modèle
            viz_dir: Répertoire de sortie pour les visualisations
        """
        plt.figure(figsize=(12, 6))
        
        models = []
        avg_scores = []
        
        # Trier les modèles par score moyen décroissant
        sorted_models = sorted(
            [(name, stats) for name, stats in model_performances.items()],
            key=lambda x: x[1]["total_score"] / x[1]["successful_tests"] if x[1]["successful_tests"] > 0 else 0,
            reverse=True
        )
        
        for model_name, stats in sorted_models:
            models.append(model_name)
            avg_score = stats["total_score"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
            avg_scores.append(avg_score)
        
        # Définir des couleurs différentes pour OpenAI et OpenRouter
        colors = []
        for model in models:
            if MODELS[model]["provider"] == "openai":
                colors.append('blue')
            else:
                colors.append('orange')
        
        plt.bar(models, avg_scores, color=colors)
        plt.xlabel('Modèle')
        plt.ylabel('Score Moyen')
        plt.title('Score Moyen par Modèle')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        # Ajouter les valeurs sur les barres
        for i, v in enumerate(avg_scores):
            plt.text(i, v + 0.02, f'{v:.2f}', ha='center')
        
        plt.savefig(os.path.join(viz_dir, 'avg_score_by_model.png'))
        plt.close()
    
    def _generate_execution_time_chart(self, model_performances, viz_dir):
        """
        Génère un graphique des temps d'exécution par modèle.
        
        Args:
            model_performances: Dictionnaire des performances par modèle
            viz_dir: Répertoire de sortie pour les visualisations
        """
        plt.figure(figsize=(12, 6))
        
        models = []
        avg_times = []
        
        # Trier les modèles par temps d'exécution croissant
        sorted_models = sorted(
            [(name, stats) for name, stats in model_performances.items()],
            key=lambda x: x[1]["total_time"] / x[1]["successful_tests"] if x[1]["successful_tests"] > 0 else float('inf')
        )
        
        for model_name, stats in sorted_models:
            models.append(model_name)
            avg_time = stats["total_time"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
            avg_times.append(avg_time)
        
        # Définir des couleurs différentes pour OpenAI et OpenRouter
        colors = []
        for model in models:
            if MODELS[model]["provider"] == "openai":
                colors.append('blue')
            else:
                colors.append('orange')
        
        plt.bar(models, avg_times, color=colors)
        plt.xlabel('Modèle')
        plt.ylabel('Temps d\'Exécution Moyen (s)')
        plt.title('Temps d\'Exécution Moyen par Modèle')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        # Ajouter les valeurs sur les barres
        for i, v in enumerate(avg_times):
            plt.text(i, v + 0.1, f'{v:.2f}s', ha='center')
        
        plt.savefig(os.path.join(viz_dir, 'execution_time_by_model.png'))
        plt.close()
    
    def _generate_cost_efficiency_chart(self, model_performances, viz_dir):
        """
        Génère un graphique de l'efficacité coût/performance par modèle.
        
        Args:
            model_performances: Dictionnaire des performances par modèle
            viz_dir: Répertoire de sortie pour les visualisations
        """
        plt.figure(figsize=(12, 6))
        
        models = []
        cost_efficiencies = []
        
        # Trier les modèles par efficacité coût/performance décroissante
        sorted_models = sorted(
            [(name, stats) for name, stats in model_performances.items()],
            key=lambda x: (x[1]["total_score"] / x[1]["successful_tests"]) / (x[1]["total_cost"] / x[1]["successful_tests"]) if x[1]["successful_tests"] > 0 and x[1]["total_cost"] > 0 else 0,
            reverse=True
        )
        
        for model_name, stats in sorted_models:
            models.append(model_name)
            avg_score = stats["total_score"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
            avg_cost = stats["total_cost"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
            
            # Calculer l'efficacité coût/performance
            cost_efficiency = avg_score / avg_cost if avg_cost > 0 else 0
            cost_efficiencies.append(cost_efficiency)
        
        # Définir des couleurs différentes pour OpenAI et OpenRouter
        colors = []
        for model in models:
            if MODELS[model]["provider"] == "openai":
                colors.append('blue')
            else:
                colors.append('orange')
        
        plt.bar(models, cost_efficiencies, color=colors)
        plt.xlabel('Modèle')
        plt.ylabel('Efficacité Coût/Performance')
        plt.title('Efficacité Coût/Performance par Modèle')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        # Ajouter les valeurs sur les barres
        for i, v in enumerate(cost_efficiencies):
            plt.text(i, v + 5, f'{v:.2f}', ha='center')
        
        plt.savefig(os.path.join(viz_dir, 'cost_efficiency_by_model.png'))
        plt.close()


async def main():
    """Fonction principale."""
    parser = argparse.ArgumentParser(description='Test comparatif des modèles de langage')
    parser.add_argument('--models', type=str, nargs='+', help='Liste des modèles à tester')
    parser.add_argument('--output-dir', type=str, default='../results/model_comparison', help='Répertoire de sortie pour les résultats')
    parser.add_argument('--categories', type=str, nargs='+', help='Catégories de tâches à tester')
    parser.add_argument('--complexities', type=str, nargs='+', help='Niveaux de complexité à tester')
    
    args = parser.parse_args()
    
    # Filtrer les modèles à tester
    models_to_test = args.models if args.models else None
    
    # Filtrer les prompts à tester
    prompts_to_test = None
    if args.categories or args.complexities:
        prompts_to_test = []
        for prompt in TEST_PROMPTS:
            if args.categories and prompt["category"] not in args.categories:
                continue
            if args.complexities and prompt["complexity"] not in args.complexities:
                continue
            prompts_to_test.append(prompt)
    
    # Créer et exécuter le comparateur de modèles
    comparer = ModelComparer(models_to_test, args.output_dir)
    await comparer.run_tests(prompts_to_test)


if __name__ == "__main__":
    asyncio.run(main())
            # Mettre à jour les statistiques par complexité
            complexity = result["complexity"]
            if complexity not in model_performances[model]["by_complexity"]:
                model_performances[model]["by_complexity"][complexity] = {
                    "total_tests": 0,
                    "successful_tests": 0,
                    "total_score": 0
                }
            
            model_performances[model]["by_complexity"][complexity]["total_tests"] += 1
            
            if result["success"]:
                model_performances[model]["by_complexity"][complexity]["successful_tests"] += 1
                model_performances[model]["by_complexity"][complexity]["total_score"] += result.get("evaluation", {}).get("score", 0)
        
        return model_performances
            Résultat de l'évaluation
        """
        response_lower = response.lower()
        
        evaluation = {
            "score": 0,
            "max_score": 1,
            "details": {}
        }
        
        # Vérifier si une réponse exacte est attendue
        if "expected_answer" in prompt_data:
            expected_answer = prompt_data["expected_answer"].lower()
            matched = expected_answer in response_lower
            evaluation["score"] = 1 if matched else 0
            evaluation["details"]["matched"] = matched
        
        # Vérifier si des mots-clés sont attendus
        elif "expected_keywords" in prompt_data:
            expected_keywords = prompt_data["expected_keywords"]
            matches = [keyword.lower() in response_lower for keyword in expected_keywords]
            match_ratio = sum(matches) / len(expected_keywords) if expected_keywords else 0
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
            evaluation["details"]["matched_keywords"] = [keyword for keyword, matched in zip(expected_keywords, matches) if matched]
        
        # Pas de critère d'évaluation spécifique
        else:
            evaluation["score"] = 0.5  # Score par défaut
            evaluation["details"]["note"] = "Pas de critère d'évaluation spécifique"
        
        return evaluation
                raise ValueError("Aucun modèle ne peut être testé. Veuillez configurer au moins une clé API.")
        "cost_per_1k_tokens_output": 0.002
    },
    "qwen/qwen3-30b-a3b": {
        "provider": "openrouter",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.003,
        "cost_per_1k_tokens_output": 0.003
    },
    "qwen/qwen3-32b": {
        "provider": "openrouter",
        "type": "chat",
        "max_tokens": 4096,
        "cost_per_1k_tokens_input": 0.004,
        "cost_per_1k_tokens_output": 0.004
    }
}

# Catégories de tâches et niveaux de complexité
TASK_CATEGORIES = ["raisonnement", "code", "math", "summarization", "classification", "writing", "qa", "creative"]
COMPLEXITY_LEVELS = ["trivial", "simple", "medium", "hard"]