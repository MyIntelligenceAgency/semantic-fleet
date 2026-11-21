#!/usr/bin/env python3
"""
Script pour tester un modèle spécifique avec un prompt simple.
"""

import os
import json
import time
import requests
from dotenv import load_dotenv

# Chargement des variables d'environnement
load_dotenv()

def test_openai_model(model_name, prompt):
    """
    Teste un modèle OpenAI avec un prompt donné.
    
    Args:
        model_name: Nom du modèle
        prompt: Prompt à tester
        
    Returns:
        Résultat du test
    """
    api_key = os.environ.get("OPENAI_API_KEY", "")
    base_url = os.environ.get("OPENAI_BASE_URL", "https://api.openai.com/v1")
    
    if not api_key:
        print("❌ Erreur: Clé API OpenAI non configurée")
        return None
    
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json"
    }
    
    # Préparer les données de la requête
    endpoint = f"{base_url}/chat/completions"
    data = {
        "model": model_name,
        "messages": [
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": prompt}
        ],
        "max_tokens": 1000,
        "temperature": 0.7
    }
    
    # Envoyer la requête
    start_time = time.time()
    result = {
        "model": model_name,
        "prompt": prompt,
        "success": False,
        "error": None,
        "response": None,
        "response_time": 0
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
            result["tokens"] = {
                "prompt": usage.get("prompt_tokens", 0),
                "completion": usage.get("completion_tokens", 0),
                "total": usage.get("total_tokens", 0)
            }
        else:
            result["error"] = {
                "status_code": response.status_code,
                "message": response.text
            }
    
    except Exception as e:
        result["error"] = {
            "exception": str(e),
            "type": type(e).__name__
        }
        result["response_time"] = time.time() - start_time
    
    return result

def test_openrouter_model(model_name, prompt):
    """
    Teste un modèle via OpenRouter avec un prompt donné.
    
    Args:
        model_name: Nom du modèle
        prompt: Prompt à tester
        
    Returns:
        Résultat du test
    """
    api_key = os.environ.get("OPENROUTER_API_KEY", "")
    base_url = os.environ.get("OPENROUTER_BASE_URL", "https://openrouter.ai/api/v1")
    
    if not api_key:
        print("❌ Erreur: Clé API OpenRouter non configurée")
        return None
    
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
        "HTTP-Referer": "https://semantic-fleet.myia.io",
        "X-Title": "Semantic Fleet Model Tester"
    }
    
    # Préparer les données de la requête
    endpoint = f"{base_url}/chat/completions"
    data = {
        "model": model_name,
        "messages": [
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": prompt}
        ],
        "max_tokens": 1000,
        "temperature": 0.7
    }
    
    # Envoyer la requête
    start_time = time.time()
    result = {
        "model": model_name,
        "prompt": prompt,
        "success": False,
        "error": None,
        "response": None,
        "response_time": 0
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
            result["tokens"] = {
                "prompt": usage.get("prompt_tokens", 0),
                "completion": usage.get("completion_tokens", 0),
                "total": usage.get("total_tokens", 0)
            }
        else:
            result["error"] = {
                "status_code": response.status_code,
                "message": response.text
            }
    
    except Exception as e:
        result["error"] = {
            "exception": str(e),
            "type": type(e).__name__
        }
        result["response_time"] = time.time() - start_time
    
    return result

def main():
    """Fonction principale."""
    # Définir les modèles à tester
    models = [
        {"name": "gpt-3.5-turbo", "provider": "openai"},
        {"name": "gpt-4o", "provider": "openai"},
        {"name": "o3", "provider": "openai"},
        {"name": "anthropic/claude-3.7-sonnet", "provider": "openrouter"},
        {"name": "google/gemini-pro-1.5", "provider": "openrouter"},
        {"name": "qwen/qwen3-14b", "provider": "openrouter"}
    ]
    
    # Définir un prompt simple
    prompt = "Quelle est la capitale de la France?"
    
    # Créer un répertoire pour les résultats
    os.makedirs("../results/single_model_tests", exist_ok=True)
    
    # Tester chaque modèle
    for model in models:
        print(f"Test du modèle {model['name']}...")
        
        if model["provider"] == "openai":
            result = test_openai_model(model["name"], prompt)
        else:
            result = test_openrouter_model(model["name"], prompt)
        
        if result:
            # Afficher un résumé du résultat
            if result["success"]:
                print(f"✅ {model['name']} - Temps: {result['response_time']:.2f}s")
                print(f"Réponse: {result['completion_text'][:100]}...")
                if "tokens" in result:
                    print(f"Tokens: {result['tokens']['total']}")
            else:
                print(f"❌ {model['name']} - Erreur: {result.get('error', {}).get('message', 'Inconnue')}")
            
            # Sauvegarder le résultat
            filename = f"{model['name'].replace('/', '_')}_test.json"
            filepath = os.path.join("../results/single_model_tests", filename)
            
            with open(filepath, "w", encoding="utf-8") as f:
                json.dump(result, f, indent=2, ensure_ascii=False)
        
        print()

if __name__ == "__main__":
    main()