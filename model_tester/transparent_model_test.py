#!/usr/bin/env python3
"""
Script de test transparent pour les modèles OpenAI et OpenRouter
Ce script permet d'observer directement les prompts et les réponses des différents modèles,
et d'exécuter des tests avec les modèles réels configurés via OpenAI et OpenRouter.
"""

import os
import sys
import json
import time
import requests
import asyncio
import argparse
from datetime import datetime
from typing import Dict, List, Any, Optional, Tuple
from dotenv import load_dotenv

# Chargement des variables d'environnement
load_dotenv()

# Configuration des APIs
API_CONFIGS = {
    "openai": {
        "api_key": os.environ.get("OPENAI_API_KEY", "YOUR_OPENAI_API_KEY"),
        "base_url": os.environ.get("OPENAI_BASE_URL", "https://api.openai.com/v1")
    },
    "openrouter": {
        "api_key": os.environ.get("OPENROUTER_API_KEY", "YOUR_OPENROUTER_API_KEY"),
        "base_url": os.environ.get("OPENROUTER_BASE_URL", "https://openrouter.ai/api/v1")
    }
}

# Configuration par défaut (OpenAI)
API_KEY = API_CONFIGS["openai"]["api_key"]
BASE_URL = API_CONFIGS["openai"]["base_url"]

# Modèles à tester
MODELS_TO_TEST = [
    # Modèles OpenAI
    "gpt-4o",           # modèle principal
    "gpt-4o-mini",
    "gpt-3.5-turbo",
    
    # Modèles O3 et O4-mini (à tester)
    "o3",
    "o4-mini",
    
    # Modèles via OpenRouter
    "anthropic/claude-3.7-sonnet",         # Claude 3.7 Sonnet
    "google/gemini-pro-1.5",               # Gemini 2.5 Pro
    
    # Modèles Qwen via OpenRouter
    "qwen/qwen3-1.7b",                     # Qwen 3 1.7B
    "qwen/qwen3-8b",                       # Qwen 3 8B
    "qwen/qwen3-14b",                      # Qwen 3 14B
    "qwen/qwen3-30b-a3b",                  # Qwen 3 30B A3B
    "qwen/qwen3-32b"                       # Qwen 3 32B
]

# Variantes de noms à tester pour O3 et O4-mini
O3_VARIANTS = [
    "o3",
    "o3-preview",
    "o3-2025-04-16",
    "claude-3-opus-20240229",
    "claude-3-opus"
]

O4_MINI_VARIANTS = [
    "o4-mini",
    "o4-mini-preview",
    "o4-mini-2025-04-16",
    "claude-3-5-sonnet-20240620",
    "claude-3-5-sonnet"
]

# Dataset représentatif de prompts
TEST_PROMPTS = [
    {
        "category": "raisonnement",
        "difficulty": "simple",
        "prompt": "Explique-moi le paradoxe du bateau de Thésée en termes simples."
    },
    {
        "category": "raisonnement",
        "difficulty": "complexe",
        "prompt": "Analyse les implications philosophiques du paradoxe de Newcomb et comment il remet en question notre compréhension de la causalité et du libre arbitre."
    },
    {
        "category": "code",
        "difficulty": "simple",
        "prompt": "Écris une fonction Python qui calcule la suite de Fibonacci jusqu'à n termes."
    },
    {
        "category": "code",
        "difficulty": "complexe",
        "prompt": "Implémente un algorithme de tri fusion (merge sort) en JavaScript et explique sa complexité temporelle et spatiale."
    },
    {
        "category": "mathématiques",
        "difficulty": "simple",
        "prompt": "Résous l'équation quadratique suivante: 3x² + 5x - 2 = 0"
    }
]

# Prompts supplémentaires pour les tests avec les modèles réels
REAL_MODEL_PROMPTS = [
    {
        "category": "summarization",
        "difficulty": "simple",
        "prompt": "Résume le texte suivant en 3 phrases: 'Le réchauffement climatique est l'augmentation à long terme de la température moyenne du système climatique de la Terre. C'est un aspect majeur du changement climatique, démontré par des mesures directes de température et par divers effets du réchauffement. Le terme désigne généralement le réchauffement observé depuis le début du 20e siècle, résultant en grande partie des émissions de gaz à effet de serre dues aux activités humaines.'"
    },
    {
        "category": "summarization",
        "difficulty": "complexe",
        "prompt": "Résume cet article scientifique en 5 points clés: 'L'intelligence artificielle (IA) a connu des avancées significatives ces dernières années, notamment grâce aux progrès dans l'apprentissage profond. Les modèles de langage de grande taille (LLM) comme GPT-4 et Claude ont démontré des capacités impressionnantes dans la compréhension et la génération de texte. Cependant, ces systèmes présentent également des défis importants en termes d'explicabilité, de biais et d'alignement avec les valeurs humaines. Les chercheurs travaillent activement sur ces problèmes pour développer des systèmes d'IA plus sûrs et plus fiables. L'avenir de l'IA dépendra de notre capacité à résoudre ces défis tout en exploitant le potentiel de cette technologie pour améliorer la vie humaine.'"
    },
    {
        "category": "classification",
        "difficulty": "simple",
        "prompt": "Classifie le texte suivant comme positif, négatif ou neutre: 'Le nouveau restaurant du quartier offre une cuisine délicieuse et un service impeccable.'"
    },
    {
        "category": "classification",
        "difficulty": "complexe",
        "prompt": "Classifie le texte suivant selon les catégories suivantes: politique, économie, science, technologie, culture ou sport: 'Les récentes avancées en intelligence artificielle soulèvent des questions éthiques importantes concernant la vie privée et l'emploi, alors que les entreprises technologiques continuent d'investir massivement dans ce domaine en pleine expansion.'"
    },
    {
        "category": "writing",
        "difficulty": "simple",
        "prompt": "Écris un email de remerciement à un collègue qui t'a aidé sur un projet."
    },
    {
        "category": "writing",
        "difficulty": "complexe",
        "prompt": "Rédige une lettre de motivation pour un poste d'ingénieur logiciel dans une entreprise spécialisée en intelligence artificielle, en mettant en avant tes compétences en apprentissage automatique et en développement de systèmes distribués."
    },
    {
        "category": "chat",
        "difficulty": "simple",
        "prompt": "Quelle est la capitale de la France?"
    },
    {
        "category": "chat",
        "difficulty": "complexe",
        "prompt": "Explique-moi les différences entre l'apprentissage supervisé, non supervisé et par renforcement en intelligence artificielle."
    }
]

# Combiner les prompts pour les tests
ALL_TEST_PROMPTS = TEST_PROMPTS + REAL_MODEL_PROMPTS

def get_available_models() -> List[Dict[str, Any]]:
    """
    Récupère la liste des modèles disponibles via l'API OpenAI
    
    Returns:
        Liste des modèles disponibles
    """
    headers = {
        "Authorization": f"Bearer {API_KEY}",
        "Content-Type": "application/json"
    }
    
    try:
        response = requests.get(
            f"{BASE_URL}/models",
            headers=headers
        )
        response.raise_for_status()
        return response.json().get("data", [])
    except requests.exceptions.RequestException as e:
        print(f"Erreur lors de la récupération des modèles: {e}")
        if hasattr(e, 'response') and e.response:
            print(f"Détails de l'erreur: {e.response.text}")
        return []

def test_model_completion(
    model: str,
    prompt: str,
    max_tokens: int = 2000,
    temperature: float = 0.7,
    is_chat_model: bool = True,
    verbose: bool = True,
    provider: str = "openai"  # Par défaut, utilise OpenAI
) -> Dict[str, Any]:
    """
    Teste un modèle avec un prompt donné et affiche les détails de la requête et de la réponse
    
    Args:
        model: Nom du modèle à tester
        prompt: Prompt à envoyer
        max_tokens: Nombre maximum de tokens à générer
        temperature: Température pour la génération
        is_chat_model: Si True, utilise l'API chat/completions, sinon utilise completions
        verbose: Si True, affiche les détails de la requête et de la réponse
        
    Returns:
        Dictionnaire contenant les résultats du test
    """
    # Déterminer le provider en fonction du modèle
    if any(provider in model.lower() for provider in ["anthropic", "claude", "google", "gemini", "qwen"]):
        provider = "openrouter"
    
    # Configurer l'API en fonction du provider
    api_key = API_CONFIGS[provider]["api_key"]
    base_url = API_CONFIGS[provider]["base_url"]
    
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json"
    }
    
    # Ajouter les en-têtes spécifiques à OpenRouter si nécessaire
    if provider == "openrouter":
        headers["HTTP-Referer"] = "https://semantic-fleet.myia.io"
        headers["X-Title"] = "Semantic Fleet Model Tester"
    
    # Préparer les données de la requête
    if is_chat_model:
        endpoint = f"{base_url}/chat/completions"
        
        # Tester différentes configurations pour O3 et O4-mini
        if "o3" in model.lower() or "o4" in model.lower():
            # Pour O3 et O4-mini, ne pas utiliser le paramètre temperature
            data = {
                "model": model,
                "messages": [
                    {"role": "system", "content": "You are a helpful assistant."},
                    {"role": "user", "content": prompt}
                ]
            }
        else:
            data = {
                "model": model,
                "messages": [
                    {"role": "system", "content": "You are a helpful assistant."},
                    {"role": "user", "content": prompt}
                ],
                "max_tokens": max_tokens,
                "temperature": temperature
            }
    else:
        endpoint = f"{base_url}/completions"
        data = {
            "model": model,
            "prompt": prompt,
            "max_tokens": max_tokens,
            "temperature": temperature
        }
    
    # Afficher les détails de la requête
    if verbose:
        print("\n" + "="*80)
        print(f"TEST DU MODÈLE: {model}")
        print(f"ENDPOINT: {endpoint}")
        print(f"REQUÊTE:")
        print(json.dumps(data, indent=2, ensure_ascii=False))
        print("="*80)
    
    # Envoyer la requête
    start_time = time.time()
    result = {
        "model": model,
        "prompt": prompt,
        "request_data": data,
        "success": False,
        "error": None,
        "response": None,
        "response_time": 0,
        "tokens": {
            "prompt": 0,
            "completion": 0,
            "total": 0
        }
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
            if is_chat_model:
                result["completion_text"] = response_json.get("choices", [{}])[0].get("message", {}).get("content", "")
            else:
                result["completion_text"] = response_json.get("choices", [{}])[0].get("text", "")
            
            # Extraire les informations sur les tokens
            usage = response_json.get("usage", {})
            result["tokens"]["prompt"] = usage.get("prompt_tokens", 0)
            result["tokens"]["completion"] = usage.get("completion_tokens", 0)
            result["tokens"]["total"] = usage.get("total_tokens", 0)
            
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
    
    # Afficher les résultats
    if verbose:
        if result["success"]:
            print(f"RÉPONSE (Temps: {result['response_time']:.2f}s, Tokens: {result['tokens']['total']}):")
            print("-"*80)
            print(result["completion_text"])
            print("-"*80)
            print(f"Tokens prompt: {result['tokens']['prompt']}")
            print(f"Tokens completion: {result['tokens']['completion']}")
            print(f"Tokens total: {result['tokens']['total']}")
        else:
            print(f"ERREUR (Temps: {result['response_time']:.2f}s):")
            print("-"*80)
            print(json.dumps(result["error"], indent=2, ensure_ascii=False))
            print("-"*80)
    
    return result

def check_model_availability(models: List[str]) -> Dict[str, bool]:
    """
    Vérifie la disponibilité des modèles spécifiés
    
    Args:
        models: Liste des modèles à vérifier
        
    Returns:
        Dictionnaire indiquant si chaque modèle est disponible
    """
    print("\n" + "="*80)
    print("VÉRIFICATION DE LA DISPONIBILITÉ DES MODÈLES")
    print("="*80)
    
    available_models = get_available_models()
    available_model_ids = [model["id"] for model in available_models]
    
    print("\nModèles disponibles dans l'API OpenAI:")
    for model_id in available_model_ids:
        print(f"  - {model_id}")
    
    # Vérifier chaque modèle
    availability = {}
    for model in models:
        is_available = model in available_model_ids
        availability[model] = is_available
        status = "✅ Disponible" if is_available else "❌ Non disponible"
        print(f"\nModèle {model}: {status}")
        
        # Si non disponible, chercher des alternatives similaires
        if not is_available:
            similar_models = [m for m in available_model_ids if model.lower() in m.lower()]
            if similar_models:
                print(f"  Alternatives possibles:")
                for similar in similar_models:
                    print(f"  - {similar}")
    
    return availability

def test_model_variants(base_model: str, variants: List[str], prompt: str) -> Dict[str, Any]:
    """
    Teste différentes variantes de noms pour un modèle
    
    Args:
        base_model: Nom de base du modèle
        variants: Liste des variantes de noms à tester
        prompt: Prompt à utiliser pour le test
        
    Returns:
        Résultats des tests pour chaque variante
    """
    print("\n" + "="*80)
    print(f"TEST DES VARIANTES DE NOMS POUR {base_model}")
    print("="*80)
    
    results = {}
    for variant in variants:
        print(f"\nTest de la variante: {variant}")
        result = test_model_completion(variant, prompt, verbose=False)
        results[variant] = result
        
        if result["success"]:
            print(f"✅ Succès avec {variant}")
            print(f"  Temps de réponse: {result['response_time']:.2f}s")
            print(f"  Tokens: {result['tokens']['total']}")
            print(f"  Début de la réponse: {result['completion_text'][:100]}...")
        else:
            print(f"❌ Échec avec {variant}")
            error_message = result.get("error", {}).get("message", "Erreur inconnue")
            print(f"  Erreur: {error_message}")
    
    return results

def run_comprehensive_tests(models: List[str], prompts: List[Dict[str, Any]]) -> Dict[str, Any]:
    """
    Exécute des tests complets sur tous les modèles avec tous les prompts
    
    Args:
        models: Liste des modèles à tester
        prompts: Liste des prompts à utiliser
        
    Returns:
        Résultats complets des tests
    """
    print("\n" + "="*80)
    print("TESTS COMPLETS DES MODÈLES")
    print("="*80)
    
    results = {
        "timestamp": datetime.now().isoformat(),
        "models_tested": models,
        "prompts_count": len(prompts),
        "results_by_model": {}
    }
    
    # Tester chaque modèle
    for model in models:
        print(f"\nTest du modèle: {model}")
        model_results = []
        
        # Tester chaque prompt
        for prompt_data in prompts:
            category = prompt_data["category"]
            difficulty = prompt_data["difficulty"]
            prompt = prompt_data["prompt"]
            
            print(f"\n  Prompt: {category} ({difficulty})")
            result = test_model_completion(model, prompt, verbose=False)
            
            # Ajouter les métadonnées du prompt
            result["prompt_metadata"] = {
                "category": category,
                "difficulty": difficulty
            }
            
            model_results.append(result)
            
            # Afficher un résumé du résultat
            if result["success"]:
                print(f"  ✅ Succès - Temps: {result['response_time']:.2f}s, Tokens: {result['tokens']['total']}")
            else:
                error_message = result.get("error", {}).get("message", "Erreur inconnue")
                print(f"  ❌ Échec - Erreur: {error_message}")
        
        # Calculer les statistiques pour ce modèle
        successful_tests = sum(1 for r in model_results if r["success"])
        success_rate = successful_tests / len(prompts) if prompts else 0
        avg_response_time = sum(r["response_time"] for r in model_results) / len(model_results) if model_results else 0
        total_tokens = sum(r["tokens"]["total"] for r in model_results if r["success"])
        
        # Stocker les résultats
        results["results_by_model"][model] = {
            "success_rate": success_rate,
            "successful_tests": successful_tests,
            "total_tests": len(prompts),
            "avg_response_time": avg_response_time,
            "total_tokens": total_tokens,
            "detailed_results": model_results
        }
        
        # Afficher un résumé
        print(f"\n  Résumé pour {model}:")
        print(f"  - Taux de succès: {success_rate*100:.1f}% ({successful_tests}/{len(prompts)})")
        print(f"  - Temps de réponse moyen: {avg_response_time:.2f}s")
        print(f"  - Tokens totaux: {total_tokens}")
    
    return results

def calculate_vetting_scores(results: Dict[str, Any], primary_model: str = "gpt-4o") -> Dict[str, Any]:
    """
    Calcule les scores de vetting (similarité avec le modèle principal)
    
    Args:
        results: Résultats des tests
        primary_model: Modèle principal pour la comparaison
        
    Returns:
        Résultats avec scores de vetting ajoutés
    """
    print("\n" + "="*80)
    print("CALCUL DES SCORES DE VETTING")
def generate_test_data(output_dir: str) -> None:
    """
    Génère des données de test pour les différents niveaux de complexité.
    
    Args:
        output_dir: Répertoire de sortie pour les données de test
    """
    print("\n" + "="*80)
    print("GÉNÉRATION DES DONNÉES DE TEST")
    print("="*80)
    
    # Créer le répertoire de sortie s'il n'existe pas
    os.makedirs(output_dir, exist_ok=True)
    
    # Générer des données pour chaque niveau de complexité
    complexity_levels = ['Trivial', 'Simple', 'Medium', 'Hard']
    
    for complexity in complexity_levels:
        # Filtrer les prompts par niveau de complexité
        prompts = []
        
        for prompt_data in ALL_TEST_PROMPTS:
            if complexity.lower() == 'trivial' and prompt_data['difficulty'] == 'simple':
                prompts.append(prompt_data)
            elif complexity.lower() == prompt_data['difficulty'].lower():
                prompts.append(prompt_data)
        
        # Générer le fichier de données
        data = {
            "complexity": complexity,
            "prompts": prompts
        }
        
        output_path = os.path.join(output_dir, f"test_data_{complexity.lower()}.json")
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        
        print(f"Données de test pour le niveau {complexity} générées: {output_path} ({len(prompts)} prompts)")
    
    print("\nGénération des données de test terminée.")
    print("="*80)
    
    # Fin de la génération des données de test
    print("\nGénération des données de test terminée.")
    print("="*80)
    return
    
    primary_results = results["results_by_model"][primary_model]["detailed_results"]
    
    # Pour chaque modèle secondaire
    for model, model_data in results["results_by_model"].items():
        if model == primary_model:
            continue
        
        print(f"\nCalcul des scores de vetting pour {model}:")
        vetting_scores = []
        
        # Pour chaque prompt
        for i, result in enumerate(model_data["detailed_results"]):
            if not result["success"] or not primary_results[i]["success"]:
                vetting_scores.append(0.0)
                continue
            
            # Calculer un score de similarité simple basé sur la longueur
            primary_text = primary_results[i]["completion_text"]
            secondary_text = result["completion_text"]
            
            # Ratio de longueur (mesure très basique)
            len_ratio = min(len(secondary_text), len(primary_text)) / max(len(secondary_text), len(primary_text))
            
            # Simuler un score de similarité (dans un cas réel, on utiliserait des embeddings)
            similarity_score = len_ratio * 0.8 + 0.2  # Score entre 0.2 et 1.0
            
            vetting_scores.append(similarity_score)
            
            print(f"  Prompt {i+1}: Score de vetting = {similarity_score:.2f}")
        
        # Calculer le score moyen
        avg_vetting_score = sum(vetting_scores) / len(vetting_scores) if vetting_scores else 0
        results["results_by_model"][model]["vetting"] = {
            "scores": vetting_scores,
            "average_score": avg_vetting_score
        }
        
        print(f"  Score moyen de vetting: {avg_vetting_score:.2f}")
    
    return results

def save_results(results: Dict[str, Any], filepath: str = "../results/transparent_tests/transparent_test_results.json") -> None:
    """
    Sauvegarde les résultats dans un fichier JSON
    
    Args:
        results: Résultats à sauvegarder
        filepath: Chemin du fichier
    """
    # Créer le répertoire parent s'il n'existe pas
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(results, f, indent=2, ensure_ascii=False)
    
    print(f"\nRésultats sauvegardés dans {filepath}")

def generate_report(results: Dict[str, Any], report_path: str = "../results/reports/transparent_reports/rapport_test_transparent.md") -> str:
    """
    Génère un rapport détaillé des tests
    
    Args:
        results: Résultats des tests
        report_path: Chemin du rapport à générer
        
    Returns:
        Rapport au format Markdown
    """
    report = "# Rapport de Test Transparent des Modèles OpenAI et OpenRouter\n\n"
    report += f"Date: {datetime.fromisoformat(results['timestamp']).strftime('%d/%m/%Y %H:%M:%S')}\n\n"
    
    # Tableau comparatif des performances
    report += "## Performances Globales\n\n"
    report += "| Modèle | Taux de succès | Temps moyen (s) | Tokens totaux | Score de vetting |\n"
    report += "|--------|---------------|-----------------|---------------|------------------|\n"
    
    for model, model_data in results["results_by_model"].items():
        success_rate = f"{model_data['success_rate']*100:.1f}%"
        avg_time = f"{model_data['avg_response_time']:.2f}"
        tokens = str(model_data['total_tokens'])
        
        # Score de vetting (si disponible)
        vetting_score = "N/A"
        if "vetting" in model_data:
            vetting_score = f"{model_data['vetting']['average_score']:.2f}"
        elif model == "gpt-4o":  # Le modèle principal n'a pas de score de vetting
            vetting_score = "Référence"
        
        report += f"| {model} | {success_rate} | {avg_time} | {tokens} | {vetting_score} |\n"
    
    # Analyse des erreurs pour O3 et O4-mini
    report += "\n## Analyse des Erreurs pour O3 et O4-mini\n\n"
    
    for model in ["o3", "o4-mini"]:
        if model in results["results_by_model"]:
            model_data = results["results_by_model"][model]
            report += f"### Modèle: {model}\n\n"
            
            if model_data["success_rate"] > 0:
                report += f"- **Taux de succès**: {model_data['success_rate']*100:.1f}%\n"
                report += f"- **Tests réussis**: {model_data['successful_tests']}/{model_data['total_tests']}\n\n"
            else:
                report += "- **Aucun test réussi**\n\n"
            
            # Analyser les erreurs
            report += "#### Erreurs rencontrées:\n\n"
            has_errors = False
            
            for i, result in enumerate(model_data["detailed_results"]):
                if not result["success"]:
                    has_errors = True
                    category = result["prompt_metadata"]["category"]
                    difficulty = result["prompt_metadata"]["difficulty"]
                    
                    report += f"**Prompt {i+1}** ({category}, {difficulty}):\n\n"
                    
                    # Extraire les détails de l'erreur
                    error = result.get("error", {})
                    status_code = error.get("status_code", "Inconnu")
                    error_message = error.get("message", "Erreur inconnue")
                    
                    report += f"- Code d'erreur: {status_code}\n"
                    report += f"- Message: ```\n{error_message}\n```\n\n"
            
            if not has_errors:
                report += "Aucune erreur rencontrée.\n\n"
    
    # Recommandations
    report += "\n## Recommandations\n\n"
    
    # Vérifier si O3 et O4-mini ont fonctionné
    o3_worked = "o3" in results["results_by_model"] and results["results_by_model"]["o3"]["success_rate"] > 0
    o4_mini_worked = "o4-mini" in results["results_by_model"] and results["results_by_model"]["o4-mini"]["success_rate"] > 0
    
    if not o3_worked:
        report += "### Pour O3:\n\n"
        report += "- Vérifier si le modèle existe réellement dans l'API OpenAI\n"
        report += "- Essayer des variantes de noms comme 'o3-preview' ou 'claude-3-opus'\n"
        report += "- Vérifier les permissions de la clé API pour ce modèle\n\n"
    
    if not o4_mini_worked:
        report += "### Pour O4-mini:\n\n"
        report += "- Vérifier si le modèle existe réellement dans l'API OpenAI\n"
        report += "- Essayer des variantes de noms comme 'o4-mini-preview' ou 'claude-3-5-sonnet'\n"
        report += "- Vérifier les permissions de la clé API pour ce modèle\n\n"
    
    # Sauvegarder le rapport
    os.makedirs(os.path.dirname(report_path), exist_ok=True)
    with open(report_path, "w", encoding="utf-8") as f:
        f.write(report)
    
    print(f"\nRapport généré et sauvegardé dans {report_path}")
    return report

def main():
    """
    Point d'entrée principal
    """
    parser = argparse.ArgumentParser(description='Test transparent des modèles OpenAI et OpenRouter')
    parser.add_argument('--model', type=str, help='Modèle spécifique à tester')
    parser.add_argument('--provider', type=str, default='openai', choices=['openai', 'openrouter'], help='Provider API à utiliser')
    parser.add_argument('--output-dir', type=str, default='../results/transparent_tests', help='Répertoire de sortie pour les résultats')
    parser.add_argument('--verbose', action='store_true', help='Afficher les détails des requêtes et des réponses')
    parser.add_argument('--generate-data', action='store_true', help='Générer des données de test')
    
    args = parser.parse_args()
    
    print("="*80)
    print("TEST QUALITATIF TRANSPARENT DU MULTICONNECTOR")
    print("="*80)
    
    # Créer le répertoire de sortie s'il n'existe pas
    os.makedirs(args.output_dir, exist_ok=True)
    
    # Générer des données de test si demandé
    if args.generate_data:
        generate_test_data(os.path.join(args.output_dir, 'data'))
        return
    
    # Configurer l'API en fonction du provider
    global API_KEY, BASE_URL
    API_KEY = API_CONFIGS[args.provider]["api_key"]
    BASE_URL = API_CONFIGS[args.provider]["base_url"]
    
    # Vérifier les configurations
    print("\nConfigurations API chargées:")
    for provider, config in API_CONFIGS.items():
        api_key_masked = f"{config['api_key'][:8]}...{config['api_key'][-4:]}" if len(config['api_key']) > 12 else "Non configurée"
        print(f"  - {provider.upper()}: {api_key_masked} ({config['base_url']})")
    
    # Si un modèle spécifique est fourni, tester uniquement ce modèle
    if args.model:
        print(f"\nTest du modèle spécifique: {args.model}")
        
        # Tester le modèle avec quelques prompts
        results = {}
        for i, prompt_data in enumerate(ALL_TEST_PROMPTS[:3]):  # Limiter à 3 prompts pour éviter les coûts excessifs
            print(f"\nTest du prompt {i+1}: {prompt_data['category']} ({prompt_data['difficulty']})")
            result = test_model_completion(args.model, prompt_data['prompt'], verbose=args.verbose, provider=args.provider)
            
            # Sauvegarder le résultat
            # Remplacer les caractères spéciaux dans le nom du fichier
            safe_model_name = args.model.replace('/', '_').replace('\\', '_')
            result_filename = f"{safe_model_name}_{args.provider}_{prompt_data['category']}.json"
            result_path = os.path.join(args.output_dir, result_filename)
            
            # Créer le répertoire de sortie s'il n'existe pas
            os.makedirs(os.path.dirname(result_path), exist_ok=True)
            
            with open(result_path, 'w', encoding='utf-8') as f:
                json.dump(result, f, indent=2, ensure_ascii=False)
            
            print(f"Résultat sauvegardé dans: {result_path}")
            
            # Ajouter au dictionnaire de résultats
            results[prompt_data['category']] = result
        
        print("\nTests terminés.")
        return
    
    # Sinon, exécuter le flux de test standard
    
    # 1. Vérifier la disponibilité des modèles
    availability = check_model_availability(MODELS_TO_TEST)
    
    # 2. Tester les variantes de noms pour O3 et O4-mini
    if not availability.get("o3", False):
        print("\nTest des variantes de noms pour O3...")
        o3_results = test_model_variants("O3", O3_VARIANTS, TEST_PROMPTS[0]["prompt"])
        
        # Trouver une variante qui fonctionne
        working_o3 = next((variant for variant, result in o3_results.items() if result["success"]), None)
        if working_o3:
            print(f"\n✅ Variante fonctionnelle trouvée pour O3: {working_o3}")
            # Remplacer O3 dans la liste des modèles à tester
            if "o3" in MODELS_TO_TEST:
                MODELS_TO_TEST[MODELS_TO_TEST.index("o3")] = working_o3
    
    if not availability.get("o4-mini", False):
        print("\nTest des variantes de noms pour O4-mini...")
        o4_mini_results = test_model_variants("O4-mini", O4_MINI_VARIANTS, TEST_PROMPTS[0]["prompt"])
        
        # Trouver une variante qui fonctionne
        working_o4_mini = next((variant for variant, result in o4_mini_results.items() if result["success"]), None)
        if working_o4_mini:
            print(f"\n✅ Variante fonctionnelle trouvée pour O4-mini: {working_o4_mini}")
            # Remplacer O4-mini dans la liste des modèles à tester
            if "o4-mini" in MODELS_TO_TEST:
                MODELS_TO_TEST[MODELS_TO_TEST.index("o4-mini")] = working_o4_mini
    
    # 3. Exécuter les tests complets
    print("\nExécution des tests complets...")
    
    # Filtrer les modèles en fonction des API keys disponibles
    filtered_models = MODELS_TO_TEST.copy()
    if API_CONFIGS["openai"]["api_key"] == "YOUR_OPENAI_API_KEY":
        filtered_models = [m for m in filtered_models if not any(m.startswith(prefix) for prefix in ["gpt-", "text-davinci"])]
        print("⚠️ Clé API OpenAI non configurée, les modèles OpenAI seront ignorés.")
    
    if API_CONFIGS["openrouter"]["api_key"] == "YOUR_OPENROUTER_API_KEY":
        filtered_models = [m for m in filtered_models if not any(provider in m.lower() for provider in ["anthropic", "claude", "google", "gemini", "qwen"])]
        print("⚠️ Clé API OpenRouter non configurée, les modèles via OpenRouter seront ignorés.")
    
    if not filtered_models:
        print("❌ Aucun modèle disponible pour les tests. Veuillez configurer au moins une clé API.")
        return
    
    print(f"Modèles sélectionnés pour les tests: {filtered_models}")
    results = run_comprehensive_tests(filtered_models, ALL_TEST_PROMPTS)
    
    # 4. Calculer les scores de vetting
    results = calculate_vetting_scores(results)
    
    # 5. Sauvegarder les résultats
    save_results(results, os.path.join(args.output_dir, "transparent_test_results.json"))
    
    # 6. Générer le rapport
    report_path = os.path.join(args.output_dir, "reports/transparent_reports/rapport_test_transparent.md")
    generate_report(results, report_path)
    
    print("\n" + "="*80)
    print("TESTS TERMINÉS")
    print("="*80)

if __name__ == "__main__":
    main()


