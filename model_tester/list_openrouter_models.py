#!/usr/bin/env python3
"""
Script pour lister les modèles disponibles sur OpenRouter.
"""

import os
import sys
import json
import requests
from dotenv import load_dotenv

# Chargement des variables d'environnement
load_dotenv()

def list_openrouter_models():
    """
    Liste tous les modèles disponibles sur OpenRouter.
    """
    api_key = os.environ.get("OPENROUTER_API_KEY")
    base_url = os.environ.get("OPENROUTER_BASE_URL", "https://openrouter.ai/api/v1")
    
    if not api_key:
        print("❌ Erreur: Clé API OpenRouter non configurée")
        return False
    
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
        "HTTP-Referer": "https://semantic-fleet.myia.io",
        "X-Title": "Semantic Fleet Model Tester"
    }
    
    try:
        response = requests.get(
            f"{base_url}/models",
            headers=headers
        )
        
        if response.status_code == 200:
            models = response.json().get("data", [])
            print(f"✅ Connexion à OpenRouter réussie. {len(models)} modèles disponibles.")
            
            # Lister tous les modèles Qwen disponibles
            print("\nModèles Qwen disponibles sur OpenRouter:")
            qwen_models = [model for model in models if "qwen" in model.get("id", "").lower()]
            if qwen_models:
                for model in qwen_models:
                    print(f"  - {model.get('id')}")
            else:
                print("  Aucun modèle Qwen trouvé")
            
            # Lister tous les modèles Claude disponibles
            print("\nModèles Claude disponibles sur OpenRouter:")
            claude_models = [model for model in models if "claude" in model.get("id", "").lower() or "anthropic" in model.get("id", "").lower()]
            if claude_models:
                for model in claude_models:
                    print(f"  - {model.get('id')}")
            else:
                print("  Aucun modèle Claude trouvé")
            
            # Lister tous les modèles Gemini disponibles
            print("\nModèles Gemini disponibles sur OpenRouter:")
            gemini_models = [model for model in models if "gemini" in model.get("id", "").lower() or "google" in model.get("id", "").lower()]
            if gemini_models:
                for model in gemini_models:
                    print(f"  - {model.get('id')}")
            else:
                print("  Aucun modèle Gemini trouvé")
            
            return True
        else:
            print(f"❌ Erreur lors de la connexion à OpenRouter: {response.status_code}")
            print(f"Détails: {response.text}")
            return False
    
    except Exception as e:
        print(f"❌ Exception lors de la connexion à OpenRouter: {e}")
        return False

if __name__ == "__main__":
    list_openrouter_models()