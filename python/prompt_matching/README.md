# Système de Détection de Signatures de Prompts en Python

> **Documentation complète** : Pour une documentation détaillée sur l'ensemble du système de détection de signatures des prompts, y compris les structures de données fondamentales, tous les matchers de prompts et le détecteur adaptatif, consultez la [documentation complète](../../docs/systeme_detection_signatures_prompts.md).

Ce module fournit une implémentation Python du système de détection de signatures de prompts, initialement développé en C#. Il permet d'identifier efficacement les patterns dans les prompts et de leur associer des paramètres spécifiques.

## Fonctionnalités

- **Structures de données optimisées** : HybridDictionary, Trie et RadixTree pour une recherche efficace
- **Différents matchers de prompts** : Sequential, RadixTree, Hybrid et Optimized
- **Détection adaptative** : Identification automatique de nouveaux patterns de prompts
- **Thread-safe** : Utilisation sécurisée dans un environnement multi-thread
- **Haute performance** : Optimisations pour gérer un grand nombre de prompts

## Installation

```bash
# À partir du répertoire racine du projet
pip install -e python/
```

## Structure du projet

```
python/prompt_matching/
├── core/                  # Structures de données de base
│   ├── hybrid_dictionary.py
│   ├── trie.py
│   └── radix_tree.py
├── matchers/              # Implémentations des matchers
│   ├── base.py
│   ├── sequential_matcher.py
│   ├── radix_tree_matcher.py
│   ├── hybrid_matcher.py
│   ├── optimized_matcher.py
│   └── adaptive_detector.py
└── tests/                 # Tests unitaires
    └── test_hybrid_dictionary.py
```

## Utilisation

### Exemple de base

```python
from prompt_matching.matchers.base import PromptSignature, PromptMultiConnectorSettings, CompletionJob, AIRequestSettings, PromptType
from prompt_matching.matchers.optimized_matcher import OptimizedHybridPromptMatcher

# Créer un matcher de prompts
matcher = OptimizedHybridPromptMatcher()

# Créer une signature de prompt
signature = PromptSignature(
    prompt_start="Bonjour, je m'appelle",
    request_settings=AIRequestSettings(
        temperature=0.7,
        max_tokens=100
    )
)

# Créer des paramètres pour le prompt
settings = PromptMultiConnectorSettings()
settings.prompt_type = PromptType(
    prompt_name="greeting",
    signature=signature,
    instances=["Bonjour, je m'appelle Alice", "Bonjour, je m'appelle Bob"]
)
settings.temperature = 0.5
settings.max_tokens = 200

# Ajouter le prompt au matcher
matcher.add_prompt(signature, settings)

# Utiliser le matcher pour trouver les paramètres correspondant à un prompt
job = CompletionJob("Bonjour, je m'appelle Charlie", AIRequestSettings())
matched_settings = matcher.match_prompt_settings(job, [])

if matched_settings:
    print(f"Prompt reconnu: {matched_settings.prompt_type.prompt_name}")
    print(f"Température: {matched_settings.temperature}")
    print(f"Tokens max: {matched_settings.max_tokens}")
else:
    print("Aucun prompt correspondant trouvé")
```

### Utilisation du détecteur adaptatif

```python
from prompt_matching.matchers.optimized_matcher import OptimizedHybridPromptMatcher
from prompt_matching.matchers.adaptive_detector import AdaptivePromptDetector
from prompt_matching.matchers.base import CompletionJob, AIRequestSettings

# Créer un matcher de base
base_matcher = OptimizedHybridPromptMatcher()

# Créer un détecteur adaptatif
adaptive_detector = AdaptivePromptDetector(
    base_matcher,
    similarity_threshold=70,
    min_similar_prompts_to_create_pattern=3,
    max_cache_size=500,
    enabled=True
)

# Utiliser le détecteur adaptatif
job = CompletionJob("Nouveau type de prompt non reconnu", AIRequestSettings())
settings = adaptive_detector.match_prompt_settings(job, [])

# Le détecteur adaptatif stockera ce prompt non reconnu
# Si plusieurs prompts similaires sont détectés, un nouveau pattern sera créé automatiquement
```

## Choix du matcher approprié

- **SequentialPromptMatcher** : Simple mais moins performant, adapté pour un petit nombre de prompts
- **RadixTreePromptMatcher** : Efficace pour la recherche par préfixe, adapté pour un nombre modéré de prompts
- **HybridPromptMatcher** : Combine RadixTree et expressions régulières, bon équilibre entre flexibilité et performance
- **OptimizedHybridPromptMatcher** : Version optimisée avec traitement parallèle, adapté pour un grand nombre de prompts
- **AdaptivePromptDetector** : Étend n'importe quel matcher avec des capacités d'apprentissage, idéal pour les environnements dynamiques

## Performances

Le choix du matcher approprié dépend du nombre de prompts et du type de correspondance recherché :

| Matcher | Nombre de prompts | Type de correspondance | Performance |
|---------|------------------|------------------------|-------------|
| Sequential | < 10 | Exacte | Bonne |
| RadixTree | 10-100 | Préfixe | Très bonne |
| Hybrid | 10-1000 | Préfixe + Regex | Bonne |
| Optimized | > 1000 | Préfixe + Regex | Excellente |
| Adaptive | Variable | Adaptative | Variable |

## Thread-safety

Tous les matchers sont thread-safe et peuvent être utilisés en toute sécurité dans un environnement multi-thread.

## Tests

Pour exécuter les tests unitaires :

```bash
# À partir du répertoire racine du projet
python -m unittest discover -s python/prompt_matching/tests
```

## Licence

Ce projet est sous licence propriétaire. Tous droits réservés.