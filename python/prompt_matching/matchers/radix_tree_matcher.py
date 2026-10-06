"""
Module contenant l'implémentation du RadixTreePromptMatcher.

Le RadixTreePromptMatcher utilise un RadixTree pour une recherche plus efficace par préfixe.
"""

from typing import Dict, List, Optional
from ..matchers.base import IPromptMatcher, PromptSignature, PromptMultiConnectorSettings, CompletionJob
from ..core.radix_tree import RadixTree

class RadixTreePromptMatcher(IPromptMatcher):
    """
    Une implémentation de matcher de prompts qui utilise un RadixTree pour une recherche efficace par préfixe.
    
    Cette implémentation est plus performante que le SequentialPromptMatcher pour un grand nombre de prompts,
    en particulier pour les recherches par préfixe.
    """
    
    def __init__(self):
        """Initialise une nouvelle instance de RadixTreePromptMatcher."""
        self._radix_tree = RadixTree[str, str, PromptMultiConnectorSettings](
            key_to_elements=lambda s: s
        )
    
    @property
    def count(self) -> int:
        """Nombre de prompts stockés dans le matcher."""
        return self._radix_tree.count
    
    def match_prompt_settings(self, completion_job: CompletionJob, 
                             prompt_settings: List[PromptMultiConnectorSettings]) -> Optional[PromptMultiConnectorSettings]:
        """
        Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        
        Cette implémentation utilise un RadixTree pour rechercher efficacement les préfixes correspondants.
        Si aucune correspondance n'est trouvée, elle vérifie les paramètres fournis.
        
        Args:
            completion_job: Le job de complétion à matcher
            prompt_settings: La collection de paramètres de prompts disponibles
            
        Returns:
            Les paramètres correspondants ou None si aucune correspondance n'est trouvée
        """
        if completion_job is None:
            raise ValueError("Le job de complétion ne peut pas être None")
        
        # Rechercher dans le RadixTree
        success, settings = self._radix_tree.try_get_value_by_prefix(completion_job.prompt)
        
        if success:
            return settings
        
        # Si aucune correspondance n'est trouvée, vérifier les paramètres fournis
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
        
        # Ajouter au RadixTree
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
        
        # Supprimer du RadixTree
        return self._radix_tree.remove(prompt_signature.prompt_start)
    
    def clear(self) -> None:
        """Supprime tous les prompts et paramètres associés de la structure interne."""
        self._radix_tree.clear()