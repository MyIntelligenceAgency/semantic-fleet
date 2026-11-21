#!/usr/bin/env python3
"""
Script de test comparatif avancé pour les modèles de langage.
Ce script utilise toutes les fonctionnalités du MultiConnector pour comparer les performances
des différents modèles sur diverses tâches avec différents niveaux de complexité.
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
from typing import Dict, List, Any, Optional, Tuple, Set
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

# Transformations de prompts spécifiques à chaque modèle
PROMPT_TRANSFORMS = {
    "gpt-4o": {
        "system_message": "You are a helpful assistant that follows instructions extremely well.",
        "template": "{system_message}\n\n{prompt}"
    },
    "gpt-4o-mini": {
        "system_message": "You are a helpful assistant that follows instructions extremely well.",
        "template": "{system_message}\n\n{prompt}"
    },
    "gpt-3.5-turbo": {
        "system_message": "You are a helpful assistant that follows instructions extremely well.",
        "template": "{system_message}\n\n{prompt}"
    },
    "o3": {
        "system_message": "You are Claude, a helpful AI assistant created by Anthropic.",
        "template": "{system_message}\n\n{prompt}"
    },
    "o4-mini": {
        "system_message": "You are Claude, a helpful AI assistant created by Anthropic.",
        "template": "{system_message}\n\n{prompt}"
    },
    "anthropic/claude-3.7-sonnet": {
        "system_message": "You are Claude, a helpful AI assistant created by Anthropic.",
        "template": "{system_message}\n\n{prompt}"
    },
    "google/gemini-pro-1.5": {
        "system_message": "You are Gemini, a helpful AI assistant created by Google.",
        "template": "{system_message}\n\n{prompt}"
    },
    "qwen/qwen3-1.7b": {
        "system_message": "You are Qwen, a helpful AI assistant created by Alibaba.",
        "template": "{system_message}\n\n{prompt}"
    },
    "qwen/qwen3-8b": {
        "system_message": "You are Qwen, a helpful AI assistant created by Alibaba.",
        "template": "{system_message}\n\n{prompt}"
    },
    "qwen/qwen3-14b": {
        "system_message": "You are Qwen, a helpful AI assistant created by Alibaba.",
        "template": "{system_message}\n\n{prompt}"
    },
# Jeu de prompts pour les tests
TEST_PROMPTS = [
    # Raisonnement
    {
        "category": "raisonnement",
        "complexity": "trivial",
        "prompt": "Quelle est la capitale de la France?",
        "expected_answer_contains": ["Paris"],
        "evaluation_criteria": "exact_match"
    },
    {
        "category": "raisonnement",
        "complexity": "simple",
        "prompt": "Explique le paradoxe du bateau de Thésée en termes simples.",
        "expected_answer_contains": ["identité", "remplacement", "même bateau"],
        "evaluation_criteria": "keyword_match"
    },
    {
        "category": "raisonnement",
        "complexity": "medium",
        "prompt": "Compare et contraste les approches déontologiques et conséquentialistes en éthique.",
        "expected_answer_contains": ["déontologique", "conséquentialiste", "Kant", "utilitarisme"],
        "evaluation_criteria": "keyword_match"
    },
    {
        "category": "raisonnement",
        "complexity": "hard",
        "prompt": "Analyse les implications philosophiques du paradoxe de Newcomb et comment il remet en question notre compréhension de la causalité et du libre arbitre.",
        "expected_answer_contains": ["causalité", "libre arbitre", "prédiction", "choix", "déterminisme"],
        "evaluation_criteria": "keyword_match"
    },
    
    # Code
    {
        "category": "code",
        "complexity": "trivial",
        "prompt": "Écris un programme 'Hello World' en Python.",
        "expected_answer_contains": ["print", "Hello", "World"],
        "evaluation_criteria": "code_execution"
    },
    {
        "category": "code",
        "complexity": "simple",
        "prompt": "Écris une fonction Python qui calcule la suite de Fibonacci jusqu'à n termes.",
        "expected_answer_contains": ["def", "fibonacci", "return"],
        "evaluation_criteria": "code_execution"
    },
    {
        "category": "code",
        "complexity": "medium",
        "prompt": "Implémente un algorithme de tri fusion (merge sort) en JavaScript et explique sa complexité temporelle et spatiale.",
        "expected_answer_contains": ["function", "mergeSort", "O(n log n)"],
        "evaluation_criteria": "code_execution"
    },
    {
        "category": "code",
        "complexity": "hard",
        "prompt": "Implémente un arbre rouge-noir en C++ avec les opérations d'insertion, de suppression et de recherche.",
        "expected_answer_contains": ["class", "RedBlackTree", "insert", "delete", "search", "rotation"],
        "evaluation_criteria": "code_structure"
    },
    
    # Math
    {
        "category": "math",
        "complexity": "trivial",
        "prompt": "Calcule 15 + 27.",
        "expected_answer_contains": ["42"],
        "evaluation_criteria": "exact_match"
    },
    {
        "category": "math",
        "complexity": "simple",
        "prompt": "Résous l'équation quadratique suivante: 3x² + 5x - 2 = 0",
        "expected_answer_contains": ["-2", "1/3"],
        "evaluation_criteria": "math_solution"
    },
    {
        "category": "math",
        "complexity": "medium",
        "prompt": "Calcule l'intégrale de x²sin(x) dx.",
        "expected_answer_contains": ["2xsin(x)", "x²cos(x)", "intégration par parties"],
        "evaluation_criteria": "math_solution"
    },
    {
        "category": "math",
        "complexity": "hard",
        "prompt": "Prouve que la somme des angles intérieurs d'un triangle sphérique est toujours supérieure à 180 degrés.",
        "expected_answer_contains": ["excès sphérique", "courbure", "Gauss-Bonnet"],
        "evaluation_criteria": "math_proof"
    },
    
    # Summarization
    {
        "category": "summarization",
        "complexity": "trivial",
        "prompt": "Résume cette phrase en un mot: 'Le chat noir a sauté par-dessus la clôture blanche.'",
        "expected_answer_contains": ["saut", "chat"],
        "evaluation_criteria": "conciseness"
    },
    {
        "category": "summarization",
        "complexity": "simple",
        "prompt": "Résume le texte suivant en 3 phrases: 'Le réchauffement climatique est l'augmentation à long terme de la température moyenne du système climatique de la Terre. C'est un aspect majeur du changement climatique, démontré par des mesures directes de température et par divers effets du réchauffement. Le terme désigne généralement le réchauffement observé depuis le début du 20e siècle, résultant en grande partie des émissions de gaz à effet de serre dues aux activités humaines.'",
        "expected_answer_contains": ["réchauffement", "température", "gaz à effet de serre"],
        "evaluation_criteria": "conciseness"
    },
    {
        "category": "summarization",
        "complexity": "medium",
        "prompt": "Résume cet article scientifique en 5 points clés: 'L'intelligence artificielle (IA) a connu des avancées significatives ces dernières années, notamment grâce aux progrès dans l'apprentissage profond. Les modèles de langage de grande taille (LLM) comme GPT-4 et Claude ont démontré des capacités impressionnantes dans la compréhension et la génération de texte. Cependant, ces systèmes présentent également des défis importants en termes d'explicabilité, de biais et d'alignement avec les valeurs humaines. Les chercheurs travaillent activement sur ces problèmes pour développer des systèmes d'IA plus sûrs et plus fiables. L'avenir de l'IA dépendra de notre capacité à résoudre ces défis tout en exploitant le potentiel de cette technologie pour améliorer la vie humaine.'",
        "expected_answer_contains": ["IA", "LLM", "défis", "explicabilité", "biais"],
        "evaluation_criteria": "key_points"
    },
    {
        "category": "summarization",
        "complexity": "hard",
        "prompt": "Résume les principales théories de la conscience en neuroscience en un paragraphe concis.",
        "expected_answer_contains": ["information intégrée", "espace de travail global", "ordre supérieur"],
        "evaluation_criteria": "accuracy_and_conciseness"
    },
    
    # Classification
    {
        "category": "classification",
        "complexity": "trivial",
        "prompt": "Classifie ce texte comme positif ou négatif: 'J'adore ce produit!'",
        "expected_answer_contains": ["positif"],
        "evaluation_criteria": "exact_match"
    },
    {
        "category": "classification",
        "complexity": "simple",
        "prompt": "Classifie le texte suivant comme positif, négatif ou neutre: 'Le nouveau restaurant du quartier offre une cuisine délicieuse et un service impeccable.'",
        "expected_answer_contains": ["positif"],
        "evaluation_criteria": "exact_match"
    },
    {
        "category": "classification",
        "complexity": "medium",
        "prompt": "Classifie le texte suivant selon les catégories suivantes: politique, économie, science, technologie, culture ou sport: 'Les récentes avancées en intelligence artificielle soulèvent des questions éthiques importantes concernant la vie privée et l'emploi, alors que les entreprises technologiques continuent d'investir massivement dans ce domaine en pleine expansion.'",
        "expected_answer_contains": ["technologie"],
        "evaluation_criteria": "exact_match"
    },
    {
        "category": "classification",
        "complexity": "hard",
        "prompt": "Classifie ce texte selon la taxonomie de Bloom (connaissance, compréhension, application, analyse, synthèse, évaluation): 'Après avoir examiné les différentes théories économiques, comparez leurs prédictions concernant l'inflation et proposez une politique monétaire optimale pour la situation actuelle.'",
        "expected_answer_contains": ["analyse", "synthèse", "évaluation"],
        "evaluation_criteria": "multi_label"
    },
    
    # Writing
    {
        "category": "writing",
        "complexity": "trivial",
        "prompt": "Écris une phrase sur les chats.",
        "expected_answer_contains": ["chat"],
        "evaluation_criteria": "relevance"
    },
    {
        "category": "writing",
        "complexity": "simple",
        "prompt": "Écris un email de remerciement à un collègue qui t'a aidé sur un projet.",
        "expected_answer_contains": ["merci", "aide", "projet"],
        "evaluation_criteria": "structure"
    },
    {
        "category": "writing",
        "complexity": "medium",
        "prompt": "Rédige un article de blog de 300 mots sur l'importance de la cybersécurité pour les petites entreprises.",
        "expected_answer_contains": ["cybersécurité", "entreprises", "risques", "protection"],
        "evaluation_criteria": "coherence"
    },
    {
        "category": "writing",
        "complexity": "hard",
        "prompt": "Rédige une lettre de motivation pour un poste d'ingénieur logiciel dans une entreprise spécialisée en intelligence artificielle, en mettant en avant tes compétences en apprentissage automatique et en développement de systèmes distribués.",
        "expected_answer_contains": ["ingénieur logiciel", "intelligence artificielle", "apprentissage automatique", "systèmes distribués"],
        "evaluation_criteria": "persuasiveness"
    },
    
    # QA
    {
        "category": "qa",
        "complexity": "trivial",
        "prompt": "Qui a écrit 'Hamlet'?",
        "expected_answer_contains": ["Shakespeare"],
        "evaluation_criteria": "exact_match"
    },
    {
        "category": "qa",
        "complexity": "simple",
        "prompt": "Quels sont les principaux symptômes du COVID-19?",
        "expected_answer_contains": ["fièvre", "toux", "fatigue", "perte de goût", "perte d'odorat"],
        "evaluation_criteria": "keyword_match"
    },
    {
        "category": "qa",
        "complexity": "medium",
        "prompt": "Explique comment fonctionne un réseau neuronal convolutif (CNN) et pourquoi il est efficace pour la reconnaissance d'images.",
        "expected_answer_contains": ["convolution", "pooling", "filtres", "caractéristiques", "hiérarchie"],
        "evaluation_criteria": "explanation_quality"
    },
    {
        "category": "qa",
        "complexity": "hard",
        "prompt": "Explique les différences entre l'apprentissage supervisé, non supervisé et par renforcement en intelligence artificielle, avec des exemples concrets d'applications pour chacun.",
        "expected_answer_contains": ["supervisé", "non supervisé", "renforcement", "étiquettes", "clusters", "récompense"],
        "evaluation_criteria": "comparison_quality"
    },
    
    # Creative
    {
        "category": "creative",
        "complexity": "trivial",
        "prompt": "Écris un haïku sur l'automne.",
        "expected_answer_contains": [],
        "evaluation_criteria": "creativity"
    },
    {
        "category": "creative",
        "complexity": "simple",
        "prompt": "Invente une courte histoire sur un robot qui découvre les émotions.",
        "expected_answer_contains": ["robot", "émotions"],
        "evaluation_criteria": "creativity"
    },
    {
        "category": "creative",
        "complexity": "medium",
        "prompt": "Écris un dialogue entre deux personnages qui se rencontrent pour la première fois après avoir correspondu pendant des années.",
        "expected_answer_contains": ["dialogue", "rencontre"],
        "evaluation_criteria": "character_development"
    },
    {
        "category": "creative",
        "complexity": "hard",
        "prompt": "Écris un poème dans le style de Baudelaire qui explore le thème de la technologie moderne.",
        "expected_answer_contains": ["technologie"],
        "evaluation_criteria": "style_adherence"
    }
]
    },
class AdvancedModelTester:
    """Classe pour tester et comparer les performances des modèles de langage."""
    
    def __init__(self, models_to_test=None, output_dir="../results/advanced_comparison"):
        """
        Initialise le testeur de modèles.
        
        Args:
            models_to_test: Liste des modèles à tester (None pour tous)
            output_dir: Répertoire de sortie pour les résultats
        """
        self.models_to_test = models_to_test or list(MODELS.keys())
        self.output_dir = output_dir
        self.results = []
        self.model_performances = {}
        self.task_performances = {}
        self.complexity_performances = {}
        
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
                raise ValueError("Aucun modèle ne peut être testé. Veuillez configurer au moins une clé API.")
    
    def transform_prompt(self, model: str, prompt: str) -> str:
        """
        Transforme un prompt en fonction du modèle.
        
        Args:
            model: Nom du modèle
            prompt: Prompt original
            
        Returns:
            Prompt transformé
        """
        if model not in PROMPT_TRANSFORMS:
            return prompt
        
        transform = PROMPT_TRANSFORMS[model]
        return transform["template"].format(
            system_message=transform["system_message"],
            prompt=prompt
        )
    
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
        transformed_prompt = self.transform_prompt(model, prompt)
        
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
            headers["X-Title"] = "Semantic Fleet Advanced Model Tester"
        
        # Préparer les données de la requête
        if model_config["type"] == "chat":
            endpoint = f"{api_config['base_url']}/chat/completions"
            data = {
                "model": model,
                "messages": [
                    {"role": "system", "content": PROMPT_TRANSFORMS[model]["system_message"]},
                    {"role": "user", "content": prompt}
                ],
                "max_tokens": 1000,
                "temperature": 0.7
            }
        else:
            endpoint = f"{api_config['base_url']}/completions"
            data = {
                "model": model,
                "prompt": transformed_prompt,
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
            "transformed_prompt": transformed_prompt,
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
                    prompt_data["expected_answer_contains"],
                    prompt_data["evaluation_criteria"]
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
    
    def evaluate_response(self, response: str, expected_answer_contains: List[str], evaluation_criteria: str) -> Dict[str, Any]:
        """
        Évalue la réponse d'un modèle.
        
        Args:
            response: Réponse du modèle
            expected_answer_contains: Liste des éléments attendus dans la réponse
            evaluation_criteria: Critère d'évaluation
            
        Returns:
            Résultat de l'évaluation
        """
        response_lower = response.lower()
        
        evaluation = {
            "criteria": evaluation_criteria,
            "score": 0,
            "max_score": 1,
            "details": {}
        }
        
        if evaluation_criteria == "exact_match":
            # La réponse doit contenir exactement un des éléments attendus
            matched = any(expected.lower() in response_lower for expected in expected_answer_contains)
            evaluation["score"] = 1 if matched else 0
            evaluation["details"]["matched"] = matched
        
        elif evaluation_criteria == "keyword_match":
            # La réponse doit contenir un certain pourcentage des mots-clés attendus
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
            evaluation["details"]["matched_keywords"] = [expected for expected, matched in zip(expected_answer_contains, matches) if matched]
        
        elif evaluation_criteria == "code_execution":
            # Évaluation basique de la structure du code
            # Une évaluation complète nécessiterait d'exécuter le code
            has_code_structure = any(keyword in response_lower for keyword in ["def ", "function ", "class ", "import ", "from "])
            evaluation["score"] = 1 if has_code_structure else 0
            evaluation["details"]["has_code_structure"] = has_code_structure
        
        elif evaluation_criteria == "code_structure":
            # Évaluation plus poussée de la structure du code
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "math_solution":
            # Évaluation basique d'une solution mathématique
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "math_proof":
            # Évaluation basique d'une preuve mathématique
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "conciseness":
            # Évaluation de la concision
            word_count = len(response.split())
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Score basé sur la concision et la présence des mots-clés
            conciseness_score = 1.0 if word_count <= 50 else 0.5 if word_count <= 100 else 0.25
            evaluation["score"] = (match_ratio + conciseness_score) / 2
            evaluation["details"]["match_ratio"] = match_ratio
            evaluation["details"]["word_count"] = word_count
            evaluation["details"]["conciseness_score"] = conciseness_score
        
        elif evaluation_criteria == "key_points":
            # Évaluation des points clés
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "accuracy_and_conciseness":
            # Évaluation de la précision et de la concision
            word_count = len(response.split())
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Score basé sur la concision et la présence des mots-clés
            conciseness_score = 1.0 if word_count <= 100 else 0.5 if word_count <= 200 else 0.25
            evaluation["score"] = (match_ratio + conciseness_score) / 2
            evaluation["details"]["match_ratio"] = match_ratio
            evaluation["details"]["word_count"] = word_count
            evaluation["details"]["conciseness_score"] = conciseness_score
        
        elif evaluation_criteria == "multi_label":
            # Évaluation multi-étiquettes
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "relevance":
            # Évaluation de la pertinence
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            evaluation["score"] = match_ratio if match_ratio > 0 else 0.5  # Score par défaut si pas de mots-clés
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "structure":
            # Évaluation de la structure
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Vérifier la présence d'une structure d'email
            has_greeting = any(greeting in response_lower for greeting in ["bonjour", "cher", "salut", "hello"])
            has_closing = any(closing in response_lower for closing in ["cordialement", "sincèrement", "bien à vous"])
            
            structure_score = (int(has_greeting) + int(has_closing)) / 2
            evaluation["score"] = (match_ratio + structure_score) / 2
            evaluation["details"]["match_ratio"] = match_ratio
            evaluation["details"]["structure_score"] = structure_score
        
        elif evaluation_criteria == "coherence":
            # Évaluation de la cohérence
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Score par défaut basé sur les mots-clés
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "persuasiveness":
            # Évaluation de la persuasion
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Score par défaut basé sur les mots-clés
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "explanation_quality":
            # Évaluation de la qualité de l'explication
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Score par défaut basé sur les mots-clés
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "comparison_quality":
            # Évaluation de la qualité de la comparaison
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Score par défaut basé sur les mots-clés
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "creativity":
            # Évaluation de la créativité
            # Pour la créativité, on ne peut pas vraiment évaluer automatiquement
            # On donne un score par défaut de 0.5
            evaluation["score"] = 0.5
            evaluation["details"]["note"] = "La créativité nécessite une évaluation humaine"
        
        elif evaluation_criteria == "character_development":
            # Évaluation du développement des personnages
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Score par défaut basé sur les mots-clés
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        elif evaluation_criteria == "style_adherence":
            # Évaluation de l'adhérence au style
            matches = [expected.lower() in response_lower for expected in expected_answer_contains]
            match_ratio = sum(matches) / len(expected_answer_contains) if expected_answer_contains else 0
            
            # Score par défaut basé sur les mots-clés
            evaluation["score"] = match_ratio
            evaluation["details"]["match_ratio"] = match_ratio
        
        else:
            # Critère d'évaluation inconnu
            evaluation["score"] = 0
            evaluation["details"]["error"] = f"Critère d'évaluation inconnu: {evaluation_criteria}"
        
        return evaluation
    "qwen/qwen3-30b-a3b": {
        "system_message": "You are Qwen, a helpful AI assistant created by Alibaba.",
        "template": "{system_message}\n\n{prompt}"
    },
    "qwen/qwen3-32b": {
        "system_message": "You are Qwen, a helpful AI assistant created by Alibaba.",
        "template": "{system_message}\n\n{prompt}"
    }
}

# Catégories de tâches et niveaux de complexité
TASK_CATEGORIES = [
    "raisonnement", 
    "code", 
    "math", 
    "summarization", 
    "classification", 
    "writing", 
    "qa", 
    "creative"
]

COMPLEXITY_LEVELS = ["trivial", "simple", "medium", "hard"]
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
        
        # Analyser les résultats
        self._analyze_results()
        
        # Générer les rapports
        self.generate_reports()
        
        print(f"Tests terminés. Résultats sauvegardés dans {self.output_dir}")
    
    def _save_raw_response(self, result):
        """
        Sauvegarde la réponse brute d'un modèle.
        
        Args:
            result: Résultat du test
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
    
    def _analyze_results(self):
        """Analyse les résultats des tests."""
        print("Analyse des résultats...")
        
        # Analyser les performances par modèle
        for result in self.results:
            model = result["model"]
            
            if model not in self.model_performances:
                self.model_performances[model] = {
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
            self.model_performances[model]["total_tests"] += 1
            
            if result["success"]:
                self.model_performances[model]["successful_tests"] += 1
                self.model_performances[model]["total_score"] += result.get("evaluation", {}).get("score", 0)
                self.model_performances[model]["total_time"] += result["response_time"]
                self.model_performances[model]["total_cost"] += result["cost"]
                self.model_performances[model]["total_tokens"] += result["tokens"]["total"]
            
            # Mettre à jour les statistiques par catégorie
            category = result["category"]
            if category not in self.model_performances[model]["by_category"]:
                self.model_performances[model]["by_category"][category] = {
                    "total_tests": 0,
                    "successful_tests": 0,
                    "total_score": 0
                }
            
            self.model_performances[model]["by_category"][category]["total_tests"] += 1
            
            if result["success"]:
                self.model_performances[model]["by_category"][category]["successful_tests"] += 1
                self.model_performances[model]["by_category"][category]["total_score"] += result.get("evaluation", {}).get("score", 0)
            
            # Mettre à jour les statistiques par complexité
            complexity = result["complexity"]
            if complexity not in self.model_performances[model]["by_complexity"]:
                self.model_performances[model]["by_complexity"][complexity] = {
                    "total_tests": 0,
                    "successful_tests": 0,
                    "total_score": 0
                }
            
            self.model_performances[model]["by_complexity"][complexity]["total_tests"] += 1
            
            if result["success"]:
                self.model_performances[model]["by_complexity"][complexity]["successful_tests"] += 1
                self.model_performances[model]["by_complexity"][complexity]["total_score"] += result.get("evaluation", {}).get("score", 0)
        
        # Analyser les performances par catégorie de tâche
        for category in TASK_CATEGORIES:
            self.task_performances[category] = {}
            
            for model in self.models_to_test:
                if model in self.model_performances and category in self.model_performances[model]["by_category"]:
                    category_stats = self.model_performances[model]["by_category"][category]
                    
                    self.task_performances[category][model] = {
                        "success_rate": category_stats["successful_tests"] / category_stats["total_tests"] if category_stats["total_tests"] > 0 else 0,
                        "avg_score": category_stats["total_score"] / category_stats["successful_tests"] if category_stats["successful_tests"] > 0 else 0
                    }
        
        # Analyser les performances par niveau de complexité
        for complexity in COMPLEXITY_LEVELS:
            self.complexity_performances[complexity] = {}
            
            for model in self.models_to_test:
                if model in self.model_performances and complexity in self.model_performances[model]["by_complexity"]:
                    complexity_stats = self.model_performances[model]["by_complexity"][complexity]
                    
                    self.complexity_performances[complexity][model] = {
                        "success_rate": complexity_stats["successful_tests"] / complexity_stats["total_tests"] if complexity_stats["total_tests"] > 0 else 0,
                        "avg_score": complexity_stats["total_score"] / complexity_stats["successful_tests"] if complexity_stats["successful_tests"] > 0 else 0
                    }
    
    def generate_reports(self):
        """Génère les rapports d'analyse."""
        print("Génération des rapports...")
        
        # Générer le rapport principal
        self._generate_main_report()
        
        # Générer les visualisations
        self._generate_visualizations()
    
    def _generate_main_report(self):
        """Génère le rapport principal."""
        report_path = os.path.join(self.output_dir, "rapport_analyse.md")
        
        with open(report_path, "w", encoding="utf-8") as f:
            f.write("# Rapport d'Analyse des Modèles de Langage\n\n")
            f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            
            # Table des matières
            f.write("## Table des Matières\n\n")
            f.write("1. [Introduction](#introduction)\n")
            f.write("2. [Modèles Testés](#modèles-testés)\n")
            f.write("3. [Méthodologie](#méthodologie)\n")
            f.write("4. [Résultats Globaux](#résultats-globaux)\n")
            f.write("5. [Performances par Catégorie](#performances-par-catégorie)\n")
            f.write("6. [Performances par Niveau de Complexité](#performances-par-niveau-de-complexité)\n")
            f.write("7. [Analyse Coût/Performance](#analyse-coûtperformance)\n")
            f.write("8. [Recommandations](#recommandations)\n")
            f.write("9. [Conclusion](#conclusion)\n\n")
            
            # Introduction
            f.write("## Introduction\n\n")
            f.write("Ce rapport présente les résultats des tests comparatifs avancés réalisés sur différents modèles de langage. ")
            f.write("L'objectif est d'évaluer les performances des modèles sur diverses tâches avec différents niveaux de complexité, ")
            f.write("afin d'identifier les forces et faiblesses de chaque modèle et de formuler des recommandations pour l'optimisation du MultiConnector.\n\n")
            
            # Modèles testés
            f.write("## Modèles Testés\n\n")
            
            # Regrouper les modèles par fournisseur
            openai_models = [model for model in self.models_to_test if MODELS[model]["provider"] == "openai"]
            openrouter_models = [model for model in self.models_to_test if MODELS[model]["provider"] == "openrouter"]
            
            if openai_models:
                f.write("### Via OpenAI\n\n")
                for model in openai_models:
                    f.write(f"- **{model}**\n")
                f.write("\n")
            
            if openrouter_models:
                f.write("### Via OpenRouter\n\n")
                for model in openrouter_models:
                    f.write(f"- **{model}**\n")
                f.write("\n")
            
            # Méthodologie
            f.write("## Méthodologie\n\n")
            f.write("Les tests ont été réalisés en utilisant un ensemble de prompts couvrant différentes catégories de tâches ")
            f.write("et niveaux de complexité. Chaque modèle a été évalué sur sa capacité à répondre correctement aux prompts, ")
            f.write("ainsi que sur d'autres métriques telles que le temps de réponse, le nombre de tokens utilisés et le coût.\n\n")
            
            f.write("### Catégories de Tâches\n\n")
            for category in TASK_CATEGORIES:
                f.write(f"- **{category}**\n")
            f.write("\n")
            
            f.write("### Niveaux de Complexité\n\n")
            for complexity in COMPLEXITY_LEVELS:
                f.write(f"- **{complexity}**\n")
            f.write("\n")
            
            f.write("### Critères d'Évaluation\n\n")
            f.write("Les réponses des modèles ont été évaluées selon différents critères en fonction de la tâche, notamment :\n\n")
            f.write("- Exactitude (exact_match)\n")
            f.write("- Présence de mots-clés (keyword_match)\n")
            f.write("- Structure du code (code_structure)\n")
            f.write("- Concision (conciseness)\n")
            f.write("- Qualité de l'explication (explanation_quality)\n")
            f.write("- Créativité (creativity)\n")
            f.write("\n")
            
            # Résultats globaux
            f.write("## Résultats Globaux\n\n")
            f.write("### Performances Globales des Modèles\n\n")
            f.write("| Modèle | Taux de Réussite | Score Moyen | Temps Moyen (s) | Tokens Moyens | Coût Moyen | Efficacité Coût/Performance |\n")
            f.write("|--------|-----------------|-------------|-----------------|---------------|------------|-----------------------------|\n")
            
            # Trier les modèles par score moyen décroissant
            sorted_models = sorted(
                [(name, stats) for name, stats in self.model_performances.items()],
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
                if category in self.task_performances:
                    f.write(f"### {category}\n\n")
                    f.write("| Modèle | Taux de Réussite | Score Moyen |\n")
                    f.write("|--------|-----------------|-------------|\n")
                    
                    # Trier les modèles par score moyen décroissant
                    sorted_models = sorted(
                        [(name, stats) for name, stats in self.task_performances[category].items()],
                        key=lambda x: x[1]["avg_score"],
                        reverse=True
                    )
                    
                    for model_name, stats in sorted_models:
                        f.write(f"| {model_name} | {stats['success_rate']:.2%} | {stats['avg_score']:.2f} |\n")
                    
                    f.write("\n")
            
            # Performances par niveau de complexité
            f.write("\n## Performances par Niveau de Complexité\n\n")
            
            for complexity in COMPLEXITY_LEVELS:
                if complexity in self.complexity_performances:
                    f.write(f"### {complexity}\n\n")
                    f.write("| Modèle | Taux de Réussite | Score Moyen |\n")
                    f.write("|--------|-----------------|-------------|\n")
                    
                    # Trier les modèles par score moyen décroissant
                    sorted_models = sorted(
                        [(name, stats) for name, stats in self.complexity_performances[complexity].items()],
                        key=lambda x: x[1]["avg_score"],
                        reverse=True
                    )
                    
                    for model_name, stats in sorted_models:
                        f.write(f"| {model_name} | {stats['success_rate']:.2%} | {stats['avg_score']:.2f} |\n")
                    
                    f.write("\n")
            
            # Analyse coût/performance
            f.write("\n## Analyse Coût/Performance\n\n")
            f.write("### Efficacité Coût/Performance des Modèles\n\n")
            f.write("| Modèle | Coût Moyen | Score Moyen | Efficacité |\n")
            f.write("|--------|------------|-------------|------------|\n")
            
            # Trier les modèles par efficacité coût/performance décroissante
            sorted_models = sorted(
                [(name, stats) for name, stats in self.model_performances.items()],
                key=lambda x: (x[1]["total_score"] / x[1]["successful_tests"]) / (x[1]["total_cost"] / x[1]["successful_tests"]) if x[1]["successful_tests"] > 0 and x[1]["total_cost"] > 0 else 0,
                reverse=True
            )
            
            for model_name, stats in sorted_models:
                avg_score = stats["total_score"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
                avg_cost = stats["total_cost"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0
                
                # Calculer l'efficacité coût/performance
                cost_efficiency = avg_score / avg_cost if avg_cost > 0 else float('inf')
                
                f.write(f"| {model_name} | ${avg_cost:.6f} | {avg_score:.2f} | {cost_efficiency:.2f} |\n")
            
            # Recommandations
            f.write("\n## Recommandations\n\n")
            
            # Recommandations par catégorie de tâche
            f.write("### Recommandations par Catégorie de Tâche\n\n")
            f.write("| Catégorie | Modèle Recommandé | Justification |\n")
            f.write("|-----------|-------------------|---------------|\n")
            
            for category in TASK_CATEGORIES:
                if category in self.task_performances:
                    # Trouver le meilleur modèle pour cette catégorie
                    best_model = max(
                        self.task_performances[category].items(),
                        key=lambda x: x[1]["avg_score"],
                        default=(None, {"avg_score": 0})
                    )
                    
                    if best_model[0] is not None:
                        f.write(f"| {category} | {best_model[0]} | Score moyen: {best_model[1]['avg_score']:.2f} |\n")
            
            # Recommandations par niveau de complexité
            f.write("\n### Recommandations par Niveau de Complexité\n\n")
            f.write("| Complexité | Modèle Recommandé | Justification |\n")
            f.write("|------------|-------------------|---------------|\n")
            
            for complexity in COMPLEXITY_LEVELS:
                if complexity in self.complexity_performances:
                    # Trouver le meilleur modèle pour ce niveau de complexité
                    best_model = max(
                        self.complexity_performances[complexity].items(),
                        key=lambda x: x[1]["avg_score"],
                        default=(None, {"avg_score": 0})
                    )
                    
                    if best_model[0] is not None:
                        f.write(f"| {complexity} | {best_model[0]} | Score moyen: {best_model[1]['avg_score']:.2f} |\n")
            
            # Recommandations pour l'optimisation du MultiConnector
            f.write("\n### Recommandations pour l'Optimisation du MultiConnector\n\n")
            
            # Stratégie de routage basée sur la catégorie et la complexité
            f.write("#### Stratégie de Routage\n\n")
            f.write("Basé sur les résultats des tests, nous recommandons la stratégie de routage suivante pour le MultiConnector:\n\n")
            
            f.write("```csharp\n")
            f.write("// Exemple de stratégie de routage pour le MultiConnector\n")
            f.write("public NamedTextCompletion SelectAppropriateModel(string category, string complexity)\n")
            f.write("{\n")
            f.write("    switch (category)\n")
            f.write("    {\n")
            
            for category in TASK_CATEGORIES:
                if category in self.task_performances:
                    f.write(f"        case \"{category}\":\n")
                    f.write("            switch (complexity)\n")
                    f.write("            {\n")
                    
                    for complexity in COMPLEXITY_LEVELS:
                        if complexity in self.complexity_performances:
                            # Trouver le meilleur modèle pour cette catégorie et ce niveau de complexité
                            best_model = None
                            best_score = -1
                            
                            for model_name in self.models_to_test:
                                if (model_name in self.task_performances[category] and 
                                    model_name in self.complexity_performances[complexity]):
                                    
                                    category_score = self.task_performances[category][model_name]["avg_score"]
                                    complexity_score = self.complexity_performances[complexity][model_name]["avg_score"]
                                    combined_score = (category_score + complexity_score) / 2
                                    
                                    if combined_score > best_score:
                                        best_score = combined_score
                                        best_model = model_name
                            
                            if best_model:
                                f.write(f"                case \"{complexity}\":\n")
                                f.write(f"                    return GetNamedTextCompletion(\"{best_model}\");\n")
                    
                    f.write("                default:\n")
                    f.write("                    return GetNamedTextCompletion(\"gpt-4o\"); // Modèle par défaut\n")
                    f.write("            }\n")
            
            f.write("        default:\n")
            f.write("            return GetNamedTextCompletion(\"gpt-4o\"); // Modèle par défaut\n")
            f.write("    }\n")
            f.write("}\n")
            f.write("```\n\n")
            
            # Transformations de prompts
            f.write("#### Transformations de Prompts\n\n")
            f.write("Pour optimiser les performances des modèles, nous recommandons les transformations de prompts suivantes:\n\n")
            
            f.write("| Modèle | Technique de Transformation | Exemple |\n")
            f.write("|--------|----------------------------|--------|\n")
            f.write("| gpt-4o | Instructions détaillées avec contexte | ```\nVous êtes un assistant expert en {domaine}. Votre tâche est de {tâche}. Soyez précis et concis.\n```|\n")
            f.write("| claude-3.7-sonnet | Instructions explicites sur le format de sortie | ```\nRépondez à la question suivante en utilisant le format spécifié: {format}.\n```|\n")
            f.write("| gemini-pro-1.5 | Prompts concis avec instructions directes | ```\n{tâche}. Répondez de manière concise.\n```|\n")
            f.write("| qwen3-32b | Prompts avec exemples few-shot | ```\nVoici un exemple: {exemple}. Maintenant, {tâche}.\n```|\n")
            
            # Stratégies de fallback
            f.write("\n#### Stratégies de Fallback\n\n")
            f.write("En cas d'échec d'un modèle, nous recommandons les stratégies de fallback suivantes:\n\n")
            
            f.write("1. **Cascade de modèles**:\n")
            f.write("   - Niveau 1: gpt-4o, claude-3.7-sonnet\n")
            f.write("   - Niveau 2: gpt-4o-mini, gemini-pro-1.5\n")
            f.write("   - Niveau 3: qwen3-32b, qwen3-30b-a3b\n")
            f.write("   - Niveau 4: gpt-3.5-turbo, qwen3-14b\n\n")
            
            f.write("2. **Transformation de prompt en cas d'échec**:\n")
            f.write("   - Réponse incomplète: Simplifier le prompt et demander une réponse plus concise\n")
            f.write("   - Erreur de compréhension: Reformuler le prompt avec des instructions plus explicites\n")
            f.write("   - Politique de contenu: Modifier le prompt pour éviter les sujets sensibles\n")
            f.write("   - Timeout: Diviser la requête en sous-requêtes plus petites\n\n")
            
            # Conclusion
            f.write("\n## Conclusion\n\n")
            f.write("Cette analyse comparative des modèles de langage a permis d'identifier les forces et faiblesses de chaque modèle ")
            f.write("en fonction des catégories de tâches et des niveaux de complexité. Les recommandations formulées permettront ")
            f.write("d'optimiser le MultiConnector en utilisant le modèle le plus approprié pour chaque type de requête, ")
            f.write("tout en tenant compte des contraintes de coût et de performance.\n\n")
            
            f.write("Les modèles les plus performants sont généralement les plus coûteux, mais certains modèles offrent un excellent ")
            f.write("rapport qualité/prix pour des tâches spécifiques. Une stratégie de routage intelligente permettra de maximiser ")
            f.write("les performances tout en optimisant les coûts.\n\n")
            
            f.write("Les transformations de prompts spécifiques à chaque modèle et les stratégies de fallback proposées ")
            f.write("permettront d'améliorer encore davantage les performances du MultiConnector et de garantir une expérience ")
            f.write("utilisateur optimale.\n")
        
        print(f"Rapport principal généré: {report_path}")
    
    def _generate_visualizations(self):
        """Génère les visualisations des résultats."""
        print("Génération des visualisations...")
        
        # Créer un répertoire pour les visualisations
        viz_dir = os.path.join(self.output_dir, "visualizations")
        os.makedirs(viz_dir, exist_ok=True)
        
        # Définir un style pour les visualisations
        plt.style.use('seaborn-v0_8-darkgrid')
        
        # 1. Graphique des taux de réussite par modèle
        self._generate_success_rate_chart(viz_dir)
        
        # 2. Graphique des scores moyens par modèle
        self._generate_avg_score_chart(viz_dir)
        
        # 3. Graphique des temps d'exécution par modèle
        self._generate_execution_time_chart(viz_dir)
        
        # 4. Graphique de l'efficacité coût/performance
        self._generate_cost_efficiency_chart(viz_dir)
        
        # 5. Heatmap des performances par catégorie et modèle
        self._generate_category_heatmap(viz_dir)
        
        # 6. Heatmap des performances par niveau de complexité et modèle
        self._generate_complexity_heatmap(viz_dir)
    
    def _generate_success_rate_chart(self, viz_dir):
        """
        Génère un graphique des taux de réussite par modèle.
        
        Args:
            viz_dir: Répertoire de sortie pour les visualisations
        """
        plt.figure(figsize=(12, 6))
        
        models = []
        success_rates = []
        
        # Trier les modèles par taux de réussite décroissant
        sorted_models = sorted(
            [(name, stats) for name, stats in self.model_performances.items()],
            key=lambda x: x[1]["successful_tests"] / x[1]["total_tests"] if x[1]["total_tests"] > 0 else 0,
            reverse=True
        )
        
        for model_name, stats in sorted_models:
            models.append(model_name)
            success_rate = stats["successful_tests"] / stats["total_tests"] if stats["total_tests"] > 0 else 0
            success_rates.append(success_rate)
        
        # Définir des couleurs différentes pour OpenAI et OpenRouter
        colors = []
        for model in models:
            if MODELS[model]["provider"] == "openai":
                colors.append('blue')
            else:
                colors.append('orange')
        
        plt.bar(models, success_rates, color=colors)
        plt.xlabel('Modèle')
        plt.ylabel('Taux de Réussite')
        plt.title('Taux de Réussite par Modèle')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        
        # Ajouter les valeurs sur les barres
        for i, v in enumerate(success_rates):
            plt.text(i, v + 0.02, f'{v:.2%}', ha='center')
        
        plt.savefig(os.path.join(viz_dir, 'success_rate_by_model.png'))
        plt.close()
    
    def _generate_avg_score_chart(self, viz_dir):
        """
        Génère un graphique des scores moyens par modèle.
        
        Args:
            viz_dir: Répertoire de sortie pour les visualisations
        """
        plt.figure(figsize=(12, 6))
        
        models = []
        avg_scores = []
        
        # Trier les modèles par score moyen décroissant
        sorted_models = sorted(
            [(name, stats) for name, stats in self.model_performances.items()],
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
    
    def _generate_execution_time_chart(self, viz_dir):
        """
        Génère un graphique des temps d'exécution par modèle.
        
        Args:
            viz_dir: Répertoire de sortie pour les visualisations
        """
        plt.figure(figsize=(12, 6))
        
        models = []
        avg_times = []
        
        # Trier les modèles par temps d'exécution croissant
        sorted_models = sorted(
            [(name, stats) for name, stats in self.model_performances.items()],
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
    
    def _generate_cost_efficiency_chart(self, viz_dir):
        """
        Génère un graphique de l'efficacité coût/performance par modèle.
        
        Args:
            viz_dir: Répertoire de sortie pour les visualisations
        """
        plt.figure(figsize=(12, 6))
        
        models = []
        cost_efficiencies = []
        
        # Trier les modèles par efficacité coût/performance décroissante
        sorted_models = sorted(
            [(name, stats) for name, stats in self.model_performances.items()],
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
    
    def _generate_category_heatmap(self, viz_dir):
        """
        Génère une heatmap des performances par catégorie et modèle.
        
        Args:
            viz_dir: Répertoire de sortie pour les visualisations
        """
        # Créer un DataFrame pour la heatmap
        data = []
        
        for category in TASK_CATEGORIES:
            if category in self.task_performances:
                for model in self.models_to_test:
                    if model in self.task_performances[category]:
                        data.append({
                            "category": category,
                            "model": model,
                            "score": self.task_performances[category][model]["avg_score"]
                        })
        
        if not data:
            return
        
        df = pd.DataFrame(data)
        pivot_table = df.pivot(index="model", columns="category", values="score")
        
        plt.figure(figsize=(14, 8))
        sns.heatmap(pivot_table, annot=True, cmap="YlGnBu", fmt=".2f", linewidths=.5)
        plt.title('Performances par Catégorie et Modèle')
        plt.tight_layout()
        
        plt.savefig(os.path.join(viz_dir, 'category_heatmap.png'))
        plt.close()
    
    def _generate_complexity_heatmap(self, viz_dir):
        """
        Génère une heatmap des performances par niveau de complexité et modèle.
        
        Args:
            viz_dir: Répertoire de sortie pour les visualisations
        """
        # Créer un DataFrame pour la heatmap
        data = []
        
        for complexity in COMPLEXITY_LEVELS:
            if complexity in self.complexity_performances:
                for model in self.models_to_test:
                    if model in self.complexity_performances[complexity]:
                        data.append({
                            "complexity": complexity,
                            "model": model,
                            "score": self.complexity_performances[complexity][model]["avg_score"]
                        })
        
        if not data:
            return
        
        df = pd.DataFrame(data)
        pivot_table = df.pivot(index="model", columns="complexity", values="score")
        
        # Réordonner les colonnes par niveau de complexité
        pivot_table = pivot_table[COMPLEXITY_LEVELS]
        
        plt.figure(figsize=(10, 8))
        sns.heatmap(pivot_table, annot=True, cmap="YlGnBu", fmt=".2f", linewidths=.5)
        plt.title('Performances par Niveau de Complexité et Modèle')
        plt.tight_layout()
        
        plt.savefig(os.path.join(viz_dir, 'complexity_heatmap.png'))
        plt.close()


async def main():
    """Fonction principale."""
    parser = argparse.ArgumentParser(description='Test comparatif avancé des modèles de langage')
    parser.add_argument('--models', type=str, nargs='+', help='Liste des modèles à tester')
    parser.add_argument('--output-dir', type=str, default='../results/advanced_comparison', help='Répertoire de sortie pour les résultats')
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
    
    # Créer et exécuter le testeur de modèles
    tester = AdvancedModelTester(models_to_test, args.output_dir)
    await tester.run_tests(prompts_to_test)


if __name__ == "__main__":
    asyncio.run(main())