#!/usr/bin/env python3
"""
Script pour vérifier les connexions API et lister les modèles disponibles.
"""

import os
import sys
import json
import requests
from dotenv import load_dotenv

# Chargement des variables d'environnement
load_dotenv()

def check_openai_connection():
    """
    Vérifie la connexion à l'API OpenAI et liste les modèles disponibles.
    
    Returns:
        bool: True si la connexion est réussie, False sinon
    """
    api_key = os.environ.get("OPENAI_API_KEY")
    base_url = os.environ.get("OPENAI_BASE_URL", "https://api.openai.com/v1")
    
    if not api_key:
        print("❌ Erreur: Clé API OpenAI non configurée")
        return False
    
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json"
    }
    
    try:
        response = requests.get(
            f"{base_url}/models",
            headers=headers
        )
        
        if response.status_code == 200:
            models = response.json().get("data", [])
            print(f"✅ Connexion à OpenAI réussie. {len(models)} modèles disponibles.")
            
            # Lister les modèles GPT
            print("\nModèles GPT disponibles:")
            gpt_models = [model for model in models if "gpt" in model.get("id", "").lower()]
            if gpt_models:
                for model in gpt_models:
                    print(f"  - {model.get('id')}")
            else:
                print("  Aucun modèle GPT trouvé")
            
            # Lister les modèles Claude (O3, O4)
            print("\nModèles Claude via OpenAI disponibles:")
            claude_models = [model for model in models if "o3" in model.get("id", "").lower() or "o4" in model.get("id", "").lower()]
            if claude_models:
                for model in claude_models:
                    print(f"  - {model.get('id')}")
            else:
                print("  Aucun modèle Claude trouvé")
            
            return True
        else:
            print(f"❌ Erreur lors de la connexion à OpenAI: {response.status_code}")
            print(f"Détails: {response.text}")
            return False
    
    except Exception as e:
        print(f"❌ Exception lors de la connexion à OpenAI: {e}")
        return False

def check_openrouter_connection():
    """
    Vérifie la connexion à l'API OpenRouter et liste les modèles disponibles.
    
    Returns:
        bool: True si la connexion est réussie, False sinon
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

def main():
    """Fonction principale."""
    print("=== Vérification des connexions API ===\n")
    
    openai_success = check_openai_connection()
    print("\n" + "-" * 50 + "\n")
    openrouter_success = check_openrouter_connection()
    
    print("\n=== Résumé ===")
    print(f"OpenAI: {'✅ Connecté' if openai_success else '❌ Non connecté'}")
    print(f"OpenRouter: {'✅ Connecté' if openrouter_success else '❌ Non connecté'}")
    
    if not openai_success and not openrouter_success:
        print("\n⚠️ Aucune connexion API n'est disponible. Veuillez configurer au moins une clé API.")
        return 1
    
    return 0

if __name__ == "__main__":
    sys.exit(main())