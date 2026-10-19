"""
Module contenant l'implémentation du SequentialPromptMatcher.

Le SequentialPromptMatcher est une implémentation simple qui vérifie séquentiellement chaque signature.
"""

from typing import Dict, List, Optional
from ..matchers.base import IPromptMatcher, PromptSignature, PromptMultiConnectorSettings, CompletionJob

class SequentialPromptMatcher(IPromptMatcher):
    """
    Une implémentation simple de matcher de prompts qui vérifie séquentiellement chaque signature.
    
    Cette implémentation est la plus simple mais peut être moins performante pour un grand nombre de prompts.
    """
    
    def __init__(self):
        """Initialise une nouvelle instance de SequentialPromptMatcher."""
        self._prompts: Dict[str, PromptMultiConnectorSettings] = {}
    
    @property
    def count(self) -> int:
        """Nombre de prompts stockés dans le matcher."""
        return len(self._prompts)
    
    def match_prompt_settings(self, completion_job: CompletionJob, 
                             prompt_settings: List[PromptMultiConnectorSettings]) -> Optional[PromptMultiConnectorSettings]:
        """
        Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        
        Cette implémentation vérifie séquentiellement chaque signature stockée dans le matcher.
        Si aucune correspondance n'est trouvée, elle vérifie les paramètres fournis.
        
        Args:
            completion_job: Le job de complétion à matcher
            prompt_settings: La collection de paramètres de prompts disponibles
            
        Returns:
            Les paramètres correspondants ou None si aucune correspondance n'est trouvée
        """
        if completion_job is None:
            raise ValueError("Le job de complétion ne peut pas être None")
        
        # Vérifier les signatures stockées dans le matcher
        for settings in self._prompts.values():
            if settings.prompt_type.signature.matches(completion_job):
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
        
        # Utiliser le prompt_start comme clé
        key = prompt_signature.prompt_start
        self._prompts[key] = settings
    
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
        
        key = prompt_signature.prompt_start
        
        if key in self._prompts:
            del self._prompts[key]
            return True
        
        return False
    
    def clear(self) -> None:
        """Supprime tous les prompts et paramètres associés de la structure interne."""
        self._prompts.clear()