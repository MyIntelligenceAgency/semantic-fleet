import os
import time
import json
import requests
from typing import Dict, List, Any, Optional, Tuple

# Configuration des APIs
API_CONFIGS = {
    "openai": {
        "api_key": "***REMOVED***",
        "base_url": "https://api.openai.com/v1"
    },
    "micro": {
        "api_key": "32885271D7845A3839F1AE0274676D87",
        "base_url": "https://api.micro.text-generation-webui.myia.io/v1"
    },
    "mini": {
        "api_key": "0EO6JAQITAL2Q0LW0ZUVA55W3YNCX4W9",
        "base_url": "https://api.mini.text-generation-webui.myia.io/v1"
    },
    "medium": {
        "api_key": "X0EC4YYP068CPD5TGARP9VQB5U4MAGHY",
        "base_url": "https://api.medium.text-generation-webui.myia.io/v1"
    }
}

def get_models(provider: str) -> List[Dict[str, Any]]:
    """
    Récupère la liste des modèles disponibles pour un fournisseur donné
    
    Args:
        provider: Nom du fournisseur (openai, micro, mini, medium)
        
    Returns:
        Liste des modèles disponibles
    """
    config = API_CONFIGS.get(provider)
    if not config:
        raise ValueError(f"Fournisseur inconnu: {provider}")
    
    headers = {
        "Authorization": f"Bearer {config['api_key']}",
        "Content-Type": "application/json"
    }
    
    try:
        response = requests.get(
            f"{config['base_url']}/models",
            headers=headers
        )
        response.raise_for_status()
        return response.json().get("data", [])
    except requests.exceptions.RequestException as e:
        print(f"Erreur lors de la récupération des modèles pour {provider}: {e}")
        return []

def get_all_models() -> Dict[str, List[Dict[str, Any]]]:
    """
    Récupère la liste des modèles disponibles pour tous les fournisseurs
    
    Returns:
        Dictionnaire avec les modèles par fournisseur
    """
    all_models = {}
    for provider in API_CONFIGS.keys():
        try:
            models = get_models(provider)
            all_models[provider] = models
            print(f"Modèles disponibles pour {provider}: {len(models)}")
        except Exception as e:
            print(f"Erreur pour {provider}: {e}")
    
    return all_models

def complete_prompt(
    provider: str,
    model: str,
    prompt: str,
    max_tokens: int = 2000,
    temperature: float = 0.7
) -> Tuple[str, float, int]:
    """
    Envoie une requête de complétion à l'API
    
    Args:
        provider: Nom du fournisseur (openai, micro, mini, medium)
        model: Nom du modèle à utiliser
        prompt: Texte de la requête
        max_tokens: Nombre maximum de tokens à générer
        temperature: Température pour la génération (0.0 - 1.0)
        
    Returns:
        Tuple contenant (réponse, temps_d'exécution, nombre_de_tokens)
    """
    # Vérifier si le modèle est un modèle de chat (GPT-4o, GPT-4o-mini, O3, O4-mini)
    chat_models = ["gpt-4o", "gpt-4o-mini", "o3", "o4-mini"]
    is_chat_model = any(model.lower().startswith(cm.lower()) for cm in chat_models)
    
    if is_chat_model:
        return complete_chat(provider, model, prompt, max_tokens, temperature)
    
    config = API_CONFIGS.get(provider)
    if not config:
        raise ValueError(f"Fournisseur inconnu: {provider}")
    
    headers = {
        "Authorization": f"Bearer {config['api_key']}",
        "Content-Type": "application/json"
    }
    
    data = {
        "model": model,
        "prompt": prompt,
        "max_tokens": max_tokens,
        "temperature": temperature
    }
    
    start_time = time.time()
    try:
        response = requests.post(
            f"{config['base_url']}/completions",
            headers=headers,
            json=data
        )
        response.raise_for_status()
        result = response.json()
        
        # Calculer le temps d'exécution
        execution_time = time.time() - start_time
        
        # Extraire la réponse et le nombre de tokens
        response_text = result.get("choices", [{}])[0].get("text", "")
        tokens_used = result.get("usage", {}).get("total_tokens", 0)
        
        return response_text, execution_time, tokens_used
    
    except requests.exceptions.RequestException as e:
        print(f"Erreur lors de la complétion pour {provider}/{model}: {e}")
        return f"ERREUR: {str(e)}", time.time() - start_time, 0

def complete_chat(
    provider: str,
    model: str,
    prompt: str,
    max_tokens: int = 2000,
    temperature: float = 0.7
) -> Tuple[str, float, int]:
    """
    Envoie une requête de chat completion à l'API
    
    Args:
        provider: Nom du fournisseur (openai, micro, mini, medium)
        model: Nom du modèle à utiliser
        prompt: Texte de la requête
        max_tokens: Nombre maximum de tokens à générer
        temperature: Température pour la génération (0.0 - 1.0)
        
    Returns:
        Tuple contenant (réponse, temps_d'exécution, nombre_de_tokens)
    """
    config = API_CONFIGS.get(provider)
    if not config:
        raise ValueError(f"Fournisseur inconnu: {provider}")
    
    headers = {
        "Authorization": f"Bearer {config['api_key']}",
        "Content-Type": "application/json"
    }
    
    data = {
        "model": model,
        "messages": [
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": prompt}
        ],
        "max_tokens": max_tokens,
        "temperature": temperature
    }
    
    start_time = time.time()
    try:
        response = requests.post(
            f"{config['base_url']}/chat/completions",
            headers=headers,
            json=data
        )
        response.raise_for_status()
        result = response.json()
        
        # Calculer le temps d'exécution
        execution_time = time.time() - start_time
        
        # Extraire la réponse et le nombre de tokens
        response_text = result.get("choices", [{}])[0].get("message", {}).get("content", "")
        tokens_used = result.get("usage", {}).get("total_tokens", 0)
        
        return response_text, execution_time, tokens_used
    
    except requests.exceptions.RequestException as e:
        print(f"Erreur lors de la chat completion pour {provider}/{model}: {e}")
        return f"ERREUR: {str(e)}", time.time() - start_time, 0

if __name__ == "__main__":
    # Test de récupération des modèles
    all_models = get_all_models()
    
    # Afficher les résultats
    for provider, models in all_models.items():
        print(f"\nFournisseur: {provider}")
        for model in models:
            print(f"  - {model.get('id', 'ID inconnu')}")
    
    # Sauvegarder les résultats dans un fichier JSON
    with open("available_models.json", "w", encoding="utf-8") as f:
        json.dump(all_models, f, indent=2, ensure_ascii=False)
    
    print("\nLes modèles disponibles ont été sauvegardés dans 'available_models.json'")