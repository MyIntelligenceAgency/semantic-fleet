#!/usr/bin/env python3
"""
Script pour tester les modèles sur différentes catégories de tâches et niveaux de complexité.
"""

import os
import sys
import json
import time
import requests
from dotenv import load_dotenv
from datetime import datetime
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
MODELS = [
    {"name": "gpt-3.5-turbo", "provider": "openai", "cost_input": 0.0015, "cost_output": 0.002},
    {"name": "gpt-4o", "provider": "openai", "cost_input": 0.01, "cost_output": 0.03},
    {"name": "gpt-4o-mini", "provider": "openai", "cost_input": 0.005, "cost_output": 0.015},
    {"name": "o3", "provider": "openai", "cost_input": 0.015, "cost_output": 0.075, "param_name": "max_completion_tokens", "no_temperature": True},
    {"name": "o4-mini", "provider": "openai", "cost_input": 0.005, "cost_output": 0.015, "param_name": "max_completion_tokens", "no_temperature": True},
    {"name": "anthropic/claude-3.7-sonnet", "provider": "openrouter", "cost_input": 0.008, "cost_output": 0.024},
    {"name": "google/gemini-pro-1.5", "provider": "openrouter", "cost_input": 0.0035, "cost_output": 0.0035},
    {"name": "qwen/qwen3-14b", "provider": "openrouter", "cost_input": 0.002, "cost_output": 0.002},
    {"name": "qwen/qwen3-32b", "provider": "openrouter", "cost_input": 0.004, "cost_output": 0.004}
]

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

def test_model(model, prompt_data):
    """
    Teste un modèle avec un prompt donné.
    
    Args:
        model: Informations sur le modèle
        prompt_data: Données du prompt
        
    Returns:
        Résultat du test
    """
    prompt = prompt_data["prompt"]
    
    provider = model["provider"]
    api_config = API_CONFIGS[provider]
    
    headers = {
        "Authorization": f"Bearer {api_config['api_key']}",
        "Content-Type": "application/json"
    }
    
    # Ajouter les en-têtes spécifiques à OpenRouter
    if provider == "openrouter":
        headers["HTTP-Referer"] = "https://semantic-fleet.myia.io"
        headers["X-Title"] = "Semantic Fleet Model Tester"
    
    # Préparer les données de la requête
    endpoint = f"{api_config['base_url']}/chat/completions"
    
    # Déterminer le nom du paramètre pour la limite de tokens
    max_tokens_param = model.get("param_name", "max_tokens")
    
    data = {
        "model": model["name"],
        "messages": [
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": prompt}
        ]
    }
    
    # Ajouter le paramètre de température sauf pour les modèles qui ne le supportent pas
    if not model.get("no_temperature", False):
        data["temperature"] = 0.7
    
    # Ajouter le paramètre de limite de tokens avec le bon nom
    data[max_tokens_param] = 1000
    
    # Envoyer la requête
    start_time = time.time()
    result = {
        "model": model["name"],
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
            result["completion_text"] = response_json.get("choices", [{}])[0].get("message", {}).get("content", "")
            
            # Extraire les informations sur les tokens
            usage = response_json.get("usage", {})
            result["tokens"]["prompt"] = usage.get("prompt_tokens", 0)
            result["tokens"]["completion"] = usage.get("completion_tokens", 0)
            result["tokens"]["total"] = usage.get("total_tokens", 0)
            
            # Calculer le coût
            result["cost"] = (
                result["tokens"]["prompt"] * model["cost_input"] / 1000 +
                result["tokens"]["completion"] * model["cost_output"] / 1000
            )
            
            # Évaluer la réponse
            result["evaluation"] = evaluate_response(
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

def evaluate_response(response, prompt_data):
    """
    Évalue la réponse d'un modèle.
    
    Args:
        response: Réponse du modèle
        prompt_data: Données du prompt
        
    Returns:
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

def save_raw_response(result, output_dir):
    """
    Sauvegarde la réponse brute d'un modèle.
    
    Args:
        result: Résultat du test
        output_dir: Répertoire de sortie
    """
    model = result["model"]
    category = result["category"]
    complexity = result["complexity"]
    
    # Créer un nom de fichier unique
    filename = f"{model.replace('/', '_')}_{category}_{complexity}.json"
    filepath = os.path.join(output_dir, "raw_responses", filename)
    
    # Sauvegarder la réponse
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2, ensure_ascii=False)

def analyze_results(results):
    """
    Analyse les résultats des tests.
    
    Args:
        results: Liste des résultats
        
    Returns:
        Dictionnaire des analyses
    """
    model_performances = {}
    
    # Analyser les performances par modèle
    for result in results:
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

def generate_report(model_performances, output_file):
    """
    Génère un rapport de synthèse des résultats.
    
    Args:
        model_performances: Dictionnaire des performances par modèle
        output_file: Chemin du fichier de sortie
    """
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("# Rapport de Synthèse des Tests Comparatifs\n\n")
        f.write(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
        
        # Performances par modèle
        f.write("## Performances par Modèle\n\n")
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
        
        categories = set()
        for stats in model_performances.values():
            categories.update(stats["by_category"].keys())
        
        for category in sorted(categories):
            f.write(f"### {category}\n\n")
            f.write("| Modèle | Taux de Réussite | Score Moyen |\n")
            f.write("|--------|-----------------|-------------|\n")
            
            # Collecter les performances pour cette catégorie
            category_performances = []
            for model_name, stats in model_performances.items():
                if category in stats["by_category"]:
                    category_stats = stats["by_category"][category]
                    success_rate = category_stats["successful_tests"] / category_stats["total_tests"] if category_stats["total_tests"] > 0 else 0
                    avg_score = category_stats["total_score"] / category_stats["successful_tests"] if category_stats["successful_tests"] > 0 else 0
                    
                    category_performances.append((model_name, success_rate, avg_score))
            
            # Trier par score moyen décroissant
            category_performances.sort(key=lambda x: x[2], reverse=True)
            
            for model_name, success_rate, avg_score in category_performances:
                f.write(f"| {model_name} | {success_rate:.2%} | {avg_score:.2f} |\n")
            
            f.write("\n")
        
        # Performances par complexité
        f.write("\n## Performances par Niveau de Complexité\n\n")
        
        complexities = set()
        for stats in model_performances.values():
            complexities.update(stats["by_complexity"].keys())
        
        for complexity in ["trivial", "simple", "medium", "hard"]:
            if complexity in complexities:
                f.write(f"### {complexity}\n\n")
                f.write("| Modèle | Taux de Réussite | Score Moyen |\n")
                f.write("|--------|-----------------|-------------|\n")
                
                # Collecter les performances pour cette complexité
                complexity_performances = []
                for model_name, stats in model_performances.items():
                    if complexity in stats["by_complexity"]:
                        complexity_stats = stats["by_complexity"][complexity]
                        success_rate = complexity_stats["successful_tests"] / complexity_stats["total_tests"] if complexity_stats["total_tests"] > 0 else 0
                        avg_score = complexity_stats["total_score"] / complexity_stats["successful_tests"] if complexity_stats["successful_tests"] > 0 else 0
                        
                        complexity_performances.append((model_name, success_rate, avg_score))
                
                # Trier par score moyen décroissant
                complexity_performances.sort(key=lambda x: x[2], reverse=True)
                
                for model_name, success_rate, avg_score in complexity_performances:
                    f.write(f"| {model_name} | {success_rate:.2%} | {avg_score:.2f} |\n")
                
                f.write("\n")
        
        # Recommandations
        f.write("\n## Recommandations\n\n")
        
        # Trouver le meilleur modèle global
        best_model = max(
            [(name, stats["total_score"] / stats["successful_tests"] if stats["successful_tests"] > 0 else 0) for name, stats in model_performances.items()],
            key=lambda x: x[1]
        )[0]
        
        f.write(f"### Meilleur Modèle Global\n\n")
        f.write(f"Le meilleur modèle global est **{best_model}**.\n\n")
        
        # Trouver le meilleur modèle par catégorie
        f.write(f"### Meilleurs Modèles par Catégorie\n\n")
        
        for category in sorted(categories):
            # Collecter les performances pour cette catégorie
            category_performances = []
            for model_name, stats in model_performances.items():
                if category in stats["by_category"]:
                    category_stats = stats["by_category"][category]
                    avg_score = category_stats["total_score"] / category_stats["successful_tests"] if category_stats["successful_tests"] > 0 else 0
                    
                    category_performances.append((model_name, avg_score))
            
            # Trouver le meilleur modèle
            if category_performances:
                best_model_category = max(category_performances, key=lambda x: x[1])[0]
                f.write(f"- **{category}**: {best_model_category}\n")
        
        f.write("\n")
        
        # Trouver le meilleur modèle par complexité
        f.write(f"### Meilleurs Modèles par Niveau de Complexité\n\n")
        
        for complexity in ["trivial", "simple", "medium", "hard"]:
            if complexity in complexities:
                # Collecter les performances pour cette complexité
                complexity_performances = []
                for model_name, stats in model_performances.items():
                    if complexity in stats["by_complexity"]:
                        complexity_stats = stats["by_complexity"][complexity]
                        avg_score = complexity_stats["total_score"] / complexity_stats["successful_tests"] if complexity_stats["successful_tests"] > 0 else 0
                        
                        complexity_performances.append((model_name, avg_score))
                
                # Trouver le meilleur modèle
                if complexity_performances:
                    best_model_complexity = max(complexity_performances, key=lambda x: x[1])[0]
                    f.write(f"- **{complexity}**: {best_model_complexity}\n")
        
        f.write("\n")
        
        # Trouver les modèles les plus efficaces en termes de coût
        f.write(f"### Modèles les Plus Efficaces en Termes de Coût\n\n")
        
        # Calculer l'efficacité coût/performance pour chaque modèle
        cost_efficiencies = []
        for model_name, stats in model_performances.items():
            if stats["successful_tests"] > 0 and stats["total_cost"] > 0:
                avg_score = stats["total_score"] / stats["successful_tests"]
                avg_cost = stats["total_cost"] / stats["successful_tests"]
                cost_efficiency = avg_score / avg_cost
                
                cost_efficiencies.append((model_name, cost_efficiency))
        
        # Trier par efficacité décroissante
        cost_efficiencies.sort(key=lambda x: x[1], reverse=True)
        
        # Afficher les 3 modèles les plus efficaces
        for i, (model_name, efficiency) in enumerate(cost_efficiencies[:3]):
            f.write(f"{i+1}. **{model_name}** (Efficacité: {efficiency:.2f})\n")
        
        f.write("\n")
        
        # Conclusion
        f.write("\n## Conclusion\n\n")
        f.write("Cette analyse comparative des modèles de langage a permis d'identifier les forces et faiblesses de chaque modèle ")
        f.write("en fonction des catégories de tâches et des niveaux de complexité. Les recommandations formulées permettront ")
        f.write("d'optimiser le MultiConnector en utilisant le modèle le plus approprié pour chaque type de requête, ")
        f.write("tout en tenant compte des contraintes de coût et de performance.\n\n")
        
        f.write("Les modèles les plus performants sont généralement les plus coûteux, mais certains modèles offrent un excellent ")
        f.write("rapport qualité/prix pour des tâches spécifiques. Une stratégie de routage intelligente permettra de maximiser ")
        f.write("les performances tout en optimisant les coûts.\n")

def main():
    """Fonction principale."""
    # Créer les répertoires pour les résultats
    output_dir = "../results/comprehensive_tests"
    os.makedirs(output_dir, exist_ok=True)
    os.makedirs(os.path.join(output_dir, "raw_responses"), exist_ok=True)
    
    # Vérifier les clés API
    if not API_CONFIGS["openai"]["api_key"]:
        print("❌ Erreur: Clé API OpenAI non configurée")
        return 1
    
    if not API_CONFIGS["openrouter"]["api_key"]:
        print("❌ Erreur: Clé API OpenRouter non configurée")
        return 1
    
    # Filtrer les modèles à tester
    models_to_test = MODELS
    
    # Filtrer les prompts à tester
    prompts_to_test = TEST_PROMPTS
    
    # Exécuter les tests
    results = []
    
    print(f"Exécution des tests sur {len(models_to_test)} modèles et {len(prompts_to_test)} prompts...")
    
    # Créer une barre de progression
    total_tests = len(models_to_test) * len(prompts_to_test)
    progress_bar = tqdm(total=total_tests, desc="Tests en cours")
    
    for prompt_data in prompts_to_test:
        for model in models_to_test:
            try:
                result = test_model(model, prompt_data)
                results.append(result)
                
                # Sauvegarder la réponse brute
                if result["success"]:
                    save_raw_response(result, output_dir)
                
                # Afficher un résumé du résultat
                if result["success"]:
                    status = "✅" if result.get("evaluation", {}).get("score", 0) >= 0.5 else "⚠️"
                    score = result.get("evaluation", {}).get("score", 0)
                    progress_bar.write(f"{status} {model['name']} - {prompt_data['category']} ({prompt_data['complexity']}) - Score: {score:.2f} - Temps: {result['response_time']:.2f}s - Coût: ${result['cost']:.6f}")
                else:
                    progress_bar.write(f"❌ {model['name']} - {prompt_data['category']} ({prompt_data['complexity']}) - Erreur: {result.get('error', {}).get('message', 'Inconnue')}")
            
            except Exception as e:
                progress_bar.write(f"❌ {model['name']} - {prompt_data['category']} ({prompt_data['complexity']}) - Exception: {str(e)}")
            
            # Mettre à jour la barre de progression
            progress_bar.update(1)
    
    progress_bar.close()
    
    # Analyser les résultats
    model_performances = analyze_results(results)
    
    # Générer le rapport
    report_file = os.path.join(output_dir, "rapport_synthese.md")
    generate_report(model_performances, report_file)
    
    print(f"Tests terminés. Résultats sauvegardés dans {output_dir}")
    print(f"Rapport de synthèse généré: {report_file}")
    
    return 0

if __name__ == "__main__":
    sys.exit(main())