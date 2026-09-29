"""
Exemple d'utilisation de base du système de détection de signatures de prompts.
"""

import sys
import os

# Ajouter le répertoire parent au chemin de recherche des modules
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from prompt_matching.matchers.base import (
    PromptSignature, 
    PromptMultiConnectorSettings, 
    CompletionJob, 
    AIRequestSettings, 
    PromptType
)
from prompt_matching.matchers.sequential_matcher import SequentialPromptMatcher
from prompt_matching.matchers.radix_tree_matcher import RadixTreePromptMatcher
from prompt_matching.matchers.hybrid_matcher import HybridPromptMatcher
from prompt_matching.matchers.optimized_matcher import OptimizedHybridPromptMatcher
from prompt_matching.matchers.adaptive_detector import AdaptivePromptDetector

def create_prompt_settings(prompt_start, prompt_name, instances, temperature=0.7, max_tokens=100):
    """
    Crée des paramètres pour un prompt.
    
    Args:
        prompt_start: Le début du prompt
        prompt_name: Le nom du prompt
        instances: Des exemples d'instances du prompt
        temperature: La température pour la génération
        max_tokens: Le nombre maximum de tokens à générer
        
    Returns:
        Un tuple (signature, settings) contenant la signature du prompt et les paramètres associés
    """
    signature = PromptSignature(
        prompt_start=prompt_start,
        request_settings=AIRequestSettings(
            temperature=temperature,
            max_tokens=max_tokens
        )
    )
    
    settings = PromptMultiConnectorSettings()
    settings.prompt_type = PromptType(
        prompt_name=prompt_name,
        signature=signature,
        instances=instances
    )
    settings.temperature = temperature
    settings.max_tokens = max_tokens
    
    return signature, settings

def test_matcher(matcher_name, matcher, prompts_to_test):
    """
    Teste un matcher avec une liste de prompts.
    
    Args:
        matcher_name: Le nom du matcher
        matcher: Le matcher à tester
        prompts_to_test: Liste de prompts à tester
    """
    print(f"\n=== Test du {matcher_name} ===")
    
    for prompt in prompts_to_test:
        job = CompletionJob(prompt, AIRequestSettings())
        settings = matcher.match_prompt_settings(job, [])
        
        if settings:
            print(f"Prompt: '{prompt}'")
            print(f"  → Reconnu comme: {settings.prompt_type.prompt_name}")
            print(f"  → Température: {settings.temperature}")
            print(f"  → Tokens max: {settings.max_tokens}")
        else:
            print(f"Prompt: '{prompt}'")
            print("  → Non reconnu")

def main():
    """Fonction principale."""
    # Créer des signatures et paramètres de prompts
    signatures_settings = [
        create_prompt_settings(
            "Bonjour, je m'appelle", 
            "greeting_name", 
            ["Bonjour, je m'appelle Alice", "Bonjour, je m'appelle Bob"],
            temperature=0.5,
            max_tokens=50
        ),
        create_prompt_settings(
            "Résume le texte suivant :", 
            "summarize", 
            ["Résume le texte suivant : Lorem ipsum dolor sit amet"],
            temperature=0.3,
            max_tokens=200
        ),
        create_prompt_settings(
            "Traduis en français :", 
            "translate_to_french", 
            ["Traduis en français : Hello world"],
            temperature=0.2,
            max_tokens=100
        ),
        create_prompt_settings(
            "Explique comme si j'avais 5 ans :", 
            "explain_simple", 
            ["Explique comme si j'avais 5 ans : La théorie de la relativité"],
            temperature=0.8,
            max_tokens=300
        ),
        create_prompt_settings(
            ".*question.*réponse.*", 
            "qa_pattern", 
            ["Voici une question et sa réponse"],
            temperature=0.4,
            max_tokens=150
        )
    ]
    
    # Créer les différents matchers
    matchers = {
        "SequentialPromptMatcher": SequentialPromptMatcher(),
        "RadixTreePromptMatcher": RadixTreePromptMatcher(),
        "HybridPromptMatcher": HybridPromptMatcher(),
        "OptimizedHybridPromptMatcher": OptimizedHybridPromptMatcher()
    }
    
    # Ajouter les prompts à chaque matcher
    for matcher in matchers.values():
        for signature, settings in signatures_settings:
            matcher.add_prompt(signature, settings)
    
    # Créer un détecteur adaptatif basé sur l'OptimizedHybridPromptMatcher
    adaptive_detector = AdaptivePromptDetector(
        matchers["OptimizedHybridPromptMatcher"],
        similarity_threshold=70,
        min_similar_prompts_to_create_pattern=2,
        max_cache_size=100,
        enabled=True
    )
    
    # Ajouter le détecteur adaptatif aux matchers
    matchers["AdaptivePromptDetector"] = adaptive_detector
    
    # Prompts à tester
    prompts_to_test = [
        "Bonjour, je m'appelle Charlie",
        "Résume le texte suivant : Ceci est un exemple de texte à résumer",
        "Traduis en français : The quick brown fox jumps over the lazy dog",
        "Explique comme si j'avais 5 ans : Comment fonctionne Internet",
        "Voici une question et sa réponse : Quelle est la capitale de la France ?",
        "Prompt non reconnu qui ne correspond à aucun pattern"
    ]
    
    # Tester chaque matcher
    for matcher_name, matcher in matchers.items():
        test_matcher(matcher_name, matcher, prompts_to_test)
    
    # Tester le détecteur adaptatif avec des prompts similaires non reconnus
    print("\n=== Test de l'apprentissage adaptatif ===")
    
    similar_unrecognized_prompts = [
        "Génère une histoire avec : un chevalier",
        "Génère une histoire avec : un dragon",
        "Génère une histoire avec : une princesse"
    ]
    
    print("Soumission de prompts similaires non reconnus au détecteur adaptatif...")
    
    for prompt in similar_unrecognized_prompts:
        job = CompletionJob(prompt, AIRequestSettings())
        adaptive_detector.match_prompt_settings(job, [])
        print(f"  Soumis: '{prompt}'")
    
    print("\nAprès soumission de prompts similaires, le détecteur adaptatif devrait avoir créé un nouveau pattern.")
    print("Dans une application réelle, ce pattern serait disponible pour les futures correspondances.")

if __name__ == "__main__":
    main()