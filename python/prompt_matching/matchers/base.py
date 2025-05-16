"""
Module contenant les classes de base et les interfaces pour les matchers de prompts.

Ce module définit les interfaces et les classes de base pour les différentes implémentations
de matchers de prompts.
"""

from abc import ABC, abstractmethod
from typing import Dict, List, Optional, Any, Pattern
import re
from dataclasses import dataclass, field

@dataclass
class AIRequestSettings:
    """Paramètres de requête pour l'IA."""
    
    model_id: str = ""
    temperature: float = 0.7
    top_p: float = 1.0
    max_tokens: int = 1000
    stop_sequences: List[str] = field(default_factory=list)
    frequency_penalty: float = 0.0
    presence_penalty: float = 0.0
    
    # Dictionnaire pour les paramètres supplémentaires spécifiques au modèle
    additional_parameters: Dict[str, Any] = field(default_factory=dict)

@dataclass
class PromptSignature:
    """Signature d'un prompt."""
    
    prompt_start: str = ""
    request_settings: AIRequestSettings = field(default_factory=AIRequestSettings)
    matching_regex: Optional[str] = None
    
    def matches(self, completion_job: 'CompletionJob') -> bool:
        """
        Vérifie si cette signature correspond à un job de complétion.
        
        Args:
            completion_job: Le job de complétion à vérifier
            
        Returns:
            True si la signature correspond au job, False sinon
        """
        if self.matching_regex:
            return bool(re.match(self.matching_regex, completion_job.prompt))
        
        return completion_job.prompt.startswith(self.prompt_start)

@dataclass
class PromptType:
    """Type de prompt avec sa signature et ses instances."""
    
    prompt_name: str = ""
    signature: PromptSignature = field(default_factory=PromptSignature)
    instances: List[str] = field(default_factory=list)
    signature_needs_adjusting: bool = False

@dataclass
class PromptMultiConnectorSettings:
    """Paramètres pour le connecteur multi-prompt."""
    
    prompt_type: PromptType = field(default_factory=PromptType)
    model_id: str = ""
    temperature: float = 0.7
    top_p: float = 1.0
    max_tokens: int = 1000
    stop_sequences: List[str] = field(default_factory=list)
    frequency_penalty: float = 0.0
    presence_penalty: float = 0.0
    
    # Dictionnaire pour les paramètres supplémentaires spécifiques au modèle
    additional_parameters: Dict[str, Any] = field(default_factory=dict)

@dataclass
class CompletionJob:
    """Job de complétion de texte."""
    
    prompt: str
    request_settings: AIRequestSettings
    
    def __init__(self, prompt: str, request_settings: Optional[AIRequestSettings] = None):
        """
        Initialise un nouveau job de complétion.
        
        Args:
            prompt: Le prompt à compléter
            request_settings: Les paramètres de requête pour l'IA
        """
        self.prompt = prompt
        self.request_settings = request_settings or AIRequestSettings()

class IPromptMatcher(ABC):
    """Interface pour les matchers de prompts."""
    
    @property
    @abstractmethod
    def count(self) -> int:
        """Nombre de prompts stockés dans le matcher."""
        pass
    
    @abstractmethod
    def match_prompt_settings(self, completion_job: CompletionJob, 
                             prompt_settings: List[PromptMultiConnectorSettings]) -> Optional[PromptMultiConnectorSettings]:
        """
        Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        
        Args:
            completion_job: Le job de complétion à matcher
            prompt_settings: La collection de paramètres de prompts disponibles
            
        Returns:
            Les paramètres correspondants ou None si aucune correspondance n'est trouvée
        """
        pass
    
    @abstractmethod
    def add_prompt(self, prompt_signature: PromptSignature, settings: PromptMultiConnectorSettings) -> None:
        """
        Ajoute un nouveau prompt et ses paramètres associés à la structure interne.
        
        Args:
            prompt_signature: La signature du prompt à ajouter
            settings: Les paramètres associés au prompt
        """
        pass
    
    @abstractmethod
    def remove_prompt(self, prompt_signature: PromptSignature) -> bool:
        """
        Supprime un prompt et ses paramètres associés de la structure interne.
        
        Args:
            prompt_signature: La signature du prompt à supprimer
            
        Returns:
            True si le prompt a été supprimé, False sinon
        """
        pass
    
    @abstractmethod
    def clear(self) -> None:
        """Supprime tous les prompts et paramètres associés de la structure interne."""
        pass