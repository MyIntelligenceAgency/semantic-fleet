"""
Module contenant l'implémentation du HybridPromptMatcher.

Le HybridPromptMatcher combine RadixTree pour les correspondances exactes et expressions régulières
pour les patterns plus complexes.
"""

import re
from typing import Dict, List, Optional, Tuple, Pattern
from ..matchers.base import IPromptMatcher, PromptSignature, PromptMultiConnectorSettings, CompletionJob
from ..core.radix_tree import RadixTree

class HybridPromptMatcher(IPromptMatcher):
    """
    Une implémentation de matcher de prompts qui combine RadixTree et expressions régulières.
    
    Cette implémentation utilise un RadixTree pour les correspondances exactes par préfixe
    et des expressions régulières pour les patterns plus complexes.
    """
    
    def __init__(self):
        """Initialise une nouvelle instance de HybridPromptMatcher."""
        self._radix_tree = RadixTree[str, str, PromptMultiConnectorSettings](
            key_to_elements=lambda s: s
        )
        self._regex_patterns: List[Tuple[Pattern, PromptMultiConnectorSettings]] = []
    
    @property
    def count(self) -> int:
        """Nombre de prompts stockés dans le matcher."""
        return self._radix_tree.count + len(self._regex_patterns)
    
    def match_prompt_settings(self, completion_job: CompletionJob, 
                             prompt_settings: List[PromptMultiConnectorSettings]) -> Optional[PromptMultiConnectorSettings]:
        """
        Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        
        Cette implémentation essaie d'abord le RadixTree pour les correspondances exactes,
        puis les expressions régulières pour les patterns plus complexes.
        Si aucune correspondance n'est trouvée, elle vérifie les paramètres fournis.
        
        Args:
            completion_job: Le job de complétion à matcher
            prompt_settings: La collection de paramètres de prompts disponibles
            
        Returns:
            Les paramètres correspondants ou None si aucune correspondance n'est trouvée
        """
        if completion_job is None:
            raise ValueError("Le job de complétion ne peut pas être None")
        
        # 1. Essayer d'abord le RadixTree (plus rapide)
        success, settings = self._radix_tree.try_get_value_by_prefix(completion_job.prompt)
        
        if success:
            return settings
        
        # 2. Essayer ensuite les expressions régulières
        for regex, regex_settings in self._regex_patterns:
            if regex.match(completion_job.prompt):
                return regex_settings
        
        # 3. Si aucune correspondance n'est trouvée, vérifier les paramètres fournis
        for settings in prompt_settings:
            if settings.prompt_type.signature.matches(completion_job):
                return settings
        
        return None
    
    def add_prompt(self, prompt_signature: PromptSignature, settings: PromptMultiConnectorSettings) -> None:
        """
        Ajoute un nouveau prompt et ses paramètres associés à la structure interne.
        
        Args:
            prompt_signature: La signature du prompt à ajouter
            settings: Les paramètres associés au prompt
            
        Raises:
            ValueError: Si la signature ou les paramètres sont None
        """
        if prompt_signature is None:
            raise ValueError("La signature du prompt ne peut pas être None")
        
        if settings is None:
            raise ValueError("Les paramètres ne peuvent pas être None")
        
        # Si la signature contient des caractères spéciaux de regex, l'ajouter comme regex
        if self._contains_regex_special_chars(prompt_signature.prompt_start):
            self._add_regex_prompt(prompt_signature.prompt_start, settings)
        else:
            # Sinon, l'ajouter au RadixTree pour une recherche plus efficace
            self._radix_tree.add(prompt_signature.prompt_start, settings)
    
    def remove_prompt(self, prompt_signature: PromptSignature) -> bool:
        """
        Supprime un prompt et ses paramètres associés de la structure interne.
        
        Args:
            prompt_signature: La signature du prompt à supprimer
            
        Returns:
            True si le prompt a été supprimé, False sinon
            
        Raises:
            ValueError: Si la signature est None
        """
        if prompt_signature is None:
            raise ValueError("La signature du prompt ne peut pas être None")
        
        pattern = prompt_signature.prompt_start
        
        # Si c'est une regex, la supprimer de la liste des regex
        if self._contains_regex_special_chars(pattern):
            for i, (regex, _) in enumerate(self._regex_patterns):
                if regex.pattern == pattern:
                    self._regex_patterns.pop(i)
                    return True
            return False
        else:
            # Sinon, la supprimer du RadixTree
            return self._radix_tree.remove(pattern)
    
    def clear(self) -> None:
        """Supprime tous les prompts et paramètres associés de la structure interne."""
        self._radix_tree.clear()
        self._regex_patterns.clear()
    
    def _add_regex_prompt(self, pattern: str, settings: PromptMultiConnectorSettings) -> None:
        """
        Ajoute un prompt sous forme de regex.
        
        Args:
            pattern: Motif regex
            settings: Paramètres associés
        """
        # Compiler le regex
        regex = re.compile(pattern)
        
        # Vérifier si le regex existe déjà
        for i, (existing_regex, _) in enumerate(self._regex_patterns):
            if existing_regex.pattern == pattern:
                # Mettre à jour les paramètres existants
                self._regex_patterns[i] = (regex, settings)
                return
        
        # Ajouter une nouvelle entrée
        self._regex_patterns.append((regex, settings))
    
    def _contains_regex_special_chars(self, input_str: str) -> bool:
        """
        Vérifie si une chaîne contient des caractères spéciaux de regex.
        
        Args:
            input_str: Chaîne à vérifier
            
        Returns:
            True si la chaîne contient des caractères spéciaux de regex, False sinon
        """
        special_chars = ['*', '+', '?', '|', '{', '}', '[', ']', '(', ')', '^', '$', '\\', '.']
        return any(char in input_str for char in special_chars)