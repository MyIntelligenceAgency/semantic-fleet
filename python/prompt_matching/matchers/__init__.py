"""
Module contenant les implémentations des matchers pour la détection de signatures de prompts.

Ce module inclut:
- SequentialPromptMatcher: Une implémentation simple qui vérifie séquentiellement chaque signature
- RadixTreePromptMatcher: Utilise un RadixTree pour une recherche plus efficace par préfixe
- HybridPromptMatcher: Combine RadixTree pour les correspondances exactes et expressions régulières
- OptimizedHybridPromptMatcher: Version optimisée avec traitement parallèle et combinaison des regex
- AdaptivePromptDetector: Extension qui permet de détecter de nouveaux patterns de prompts
"""