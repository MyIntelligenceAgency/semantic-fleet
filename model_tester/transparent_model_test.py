#!/usr/bin/env python3
"""
Script de test transparent pour les modèles OpenAI
Ce script permet d'observer directement les prompts et les réponses des différents modèles,
y compris O3 et O4-mini qui ont généré des erreurs 400 lors des tests précédents.
"""

import os
import sys
import json
import time
import requests
import asyncio
from datetime import datetime
from typing import Dict, List, Any, Optional, Tuple

# Configuration de l'API OpenAI
API_KEY = "YOUR_OPENAI_API_KEY"
BASE_URL = "https://api.openai.com/v1"

# Modèles à tester
MODELS_TO_TEST = [
    "gpt-4o",           # modèle principal
    "gpt-4o-mini",
    "gpt-3.5-turbo",
    "o3",               # vérifier pourquoi ce modèle a généré une erreur 400
    "o4-mini",          # vérifier pourquoi ce modèle a généré une erreur 400
    "gpt-4"
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
    verbose: bool = True
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
    headers = {
        "Authorization": f"Bearer {API_KEY}",
        "Content-Type": "application/json"
    }
    
    # Préparer les données de la requête
    if is_chat_model:
        endpoint = f"{BASE_URL}/chat/completions"
        
        # Tester différentes configurations pour O3 et O4-mini
        if "o3" in model.lower() or "o4" in model.lower():
            # Essayer sans max_tokens pour O3 et O4-mini
            data = {
                "model": model,
                "messages": [
                    {"role": "system", "content": "You are a helpful assistant."},
                    {"role": "user", "content": prompt}
                ],
                "temperature": temperature
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
        endpoint = f"{BASE_URL}/completions"
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
    print("="*80)
    
    # Vérifier que le modèle principal existe dans les résultats
    if primary_model not in results["results_by_model"]:
        print(f"❌ Le modèle principal {primary_model} n'est pas dans les résultats")
        return results
    
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

def save_results(results: Dict[str, Any], filename: str = "transparent_tests/transparent_test_results.json") -> None:
    """
    Sauvegarde les résultats dans un fichier JSON
    
    Args:
        results: Résultats à sauvegarder
        filename: Nom du fichier
    """
    # Créer le répertoire results s'il n'existe pas
    os.makedirs("../results", exist_ok=True)
    
    filepath = os.path.join("../results", filename)
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(results, f, indent=2, ensure_ascii=False)
    
    print(f"\nRésultats sauvegardés dans {filepath}")

def generate_report(results: Dict[str, Any]) -> str:
    """
    Génère un rapport détaillé des tests
    
    Args:
        results: Résultats des tests
        
    Returns:
        Rapport au format Markdown
    """
    report = "# Rapport de Test Transparent des Modèles OpenAI\n\n"
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
    report_filepath = os.path.join("../results", "reports/transparent_reports/rapport_test_transparent.md")
    with open(report_filepath, "w", encoding="utf-8") as f:
        f.write(report)
    
    print(f"\nRapport généré et sauvegardé dans {report_filepath}")
    return report

def main():
    """
    Point d'entrée principal
    """
    print("="*80)
    print("TEST QUALITATIF TRANSPARENT DU MULTICONNECTOR")
    print("="*80)
    
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
            MODELS_TO_TEST[MODELS_TO_TEST.index("o3")] = working_o3
    
    if not availability.get("o4-mini", False):
        print("\nTest des variantes de noms pour O4-mini...")
        o4_mini_results = test_model_variants("O4-mini", O4_MINI_VARIANTS, TEST_PROMPTS[0]["prompt"])
        
        # Trouver une variante qui fonctionne
        working_o4_mini = next((variant for variant, result in o4_mini_results.items() if result["success"]), None)
        if working_o4_mini:
            print(f"\n✅ Variante fonctionnelle trouvée pour O4-mini: {working_o4_mini}")
            # Remplacer O4-mini dans la liste des modèles à tester
            MODELS_TO_TEST[MODELS_TO_TEST.index("o4-mini")] = working_o4_mini
    
    # 3. Exécuter les tests complets
    print("\nExécution des tests complets...")
    results = run_comprehensive_tests(MODELS_TO_TEST, TEST_PROMPTS)
    
    # 4. Calculer les scores de vetting
    results = calculate_vetting_scores(results)
    
    # 5. Sauvegarder les résultats
    save_results(results)
    
    # 6. Générer le rapport
    generate_report(results)
    
    print("\n" + "="*80)
    print("TESTS TERMINÉS")
    print("="*80)

if __name__ == "__main__":
    main()


