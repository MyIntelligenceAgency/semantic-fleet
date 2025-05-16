"""
Module contenant l'implémentation de l'AdaptivePromptDetector.

L'AdaptivePromptDetector est une extension du système de détection de signatures de prompts qui permet
de mieux gérer les prompts qui ne correspondent pas à des patterns connus.
"""

import re
import time
import threading
from concurrent.futures import ThreadPoolExecutor
from typing import Dict, List, Optional, Tuple, Set, Any
from collections import defaultdict
from dataclasses import dataclass, field
import datetime
from ..matchers.base import IPromptMatcher, PromptSignature, PromptMultiConnectorSettings, CompletionJob, AIRequestSettings

class AdaptivePromptDetector(IPromptMatcher):
    """
    Implémentation adaptative du matcher de prompts qui étend le système existant pour mieux gérer
    les prompts qui ne correspondent pas à des patterns connus.
    
    Cette classe permet de laisser passer les prompts non reconnus jusqu'à ce qu'on en détecte
    plusieurs du même type, auquel cas on identifie potentiellement un nouveau pattern à analyser.
    """
    
    @dataclass
    class UnrecognizedPromptInfo:
        """Informations sur un prompt non reconnu."""
        
        first_seen: datetime.datetime = field(default_factory=datetime.datetime.now)
        last_seen: datetime.datetime = field(default_factory=datetime.datetime.now)
        count: int = 0
        prompts: List[str] = field(default_factory=list)
        request_settings: AIRequestSettings = field(default_factory=AIRequestSettings)
    
    @dataclass
    class PotentialPattern:
        """Pattern potentiel identifié à partir de prompts similaires."""
        
        similar_prompts: List[str] = field(default_factory=list)
        request_settings: AIRequestSettings = field(default_factory=AIRequestSettings)
    
    def __init__(self, base_prompt_matcher: IPromptMatcher, 
                 similarity_threshold: int = 70,
                 min_similar_prompts_to_create_pattern: int = 3,
                 cache_entry_expiration: Optional[datetime.timedelta] = None,
                 max_cache_size: int = 1000,
                 enabled: bool = True):
        """
        Initialise une nouvelle instance de AdaptivePromptDetector.
        
        Args:
            base_prompt_matcher: Le matcher de prompts de base à étendre
            similarity_threshold: Seuil de similarité pour considérer deux prompts comme similaires (0-100)
            min_similar_prompts_to_create_pattern: Nombre minimum de prompts similaires pour créer un nouveau pattern
            cache_entry_expiration: Durée d'expiration des entrées du cache
            max_cache_size: Taille maximale du cache
            enabled: Indique si le détecteur adaptatif est activé
        """
        self._base_prompt_matcher = base_prompt_matcher
        self._similarity_threshold = similarity_threshold
        self._min_similar_prompts_to_create_pattern = min_similar_prompts_to_create_pattern
        self._cache_entry_expiration = cache_entry_expiration or datetime.timedelta(hours=24)
        self._max_cache_size = max_cache_size
        self._enabled = enabled
        
        self._unrecognized_prompts_cache: Dict[str, AdaptivePromptDetector.UnrecognizedPromptInfo] = {}
        self._cache_lock = threading.RLock()
        
        # Démarrer le timer de nettoyage du cache
        self._cleanup_timer = threading.Timer(600, self._cleanup_cache)  # 10 minutes
        self._cleanup_timer.daemon = True
        self._cleanup_timer.start()
    
    @property
    def count(self) -> int:
        """Nombre de prompts stockés dans le matcher de base."""
        return self._base_prompt_matcher.count
    
    def match_prompt_settings(self, completion_job: CompletionJob, 
                             prompt_settings: List[PromptMultiConnectorSettings]) -> Optional[PromptMultiConnectorSettings]:
        """
        Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        
        Si aucune correspondance n'est trouvée et que le détecteur adaptatif est activé,
        le prompt est stocké dans le cache pour analyse ultérieure.
        
        Args:
            completion_job: Le job de complétion à matcher
            prompt_settings: La collection de paramètres de prompts disponibles
            
        Returns:
            Les paramètres correspondants ou None si aucune correspondance n'est trouvée
        """
        if completion_job is None:
            raise ValueError("Le job de complétion ne peut pas être None")
        
        # Essayer d'abord avec le matcher de base
        matched_settings = self._base_prompt_matcher.match_prompt_settings(completion_job, prompt_settings)
        
        # Si une correspondance est trouvée ou si le détecteur adaptatif est désactivé, retourner le résultat
        if matched_settings is not None or not self._enabled:
            return matched_settings
        
        # Aucune correspondance trouvée, stocker le prompt dans le cache pour analyse ultérieure
        self._store_unrecognized_prompt(completion_job)
        
        # Vérifier si nous avons suffisamment de prompts similaires pour créer un nouveau pattern
        potential_pattern = self._identify_potential_pattern(completion_job.prompt)
        if potential_pattern is not None:
            # Analyser de manière asynchrone ce nouveau pattern potentiel
            threading.Thread(target=self._analyze_new_pattern, args=(potential_pattern,), daemon=True).start()
        
        # Retourner None car aucune correspondance n'a été trouvée
        return None
    
    def add_prompt(self, prompt_signature: PromptSignature, settings: PromptMultiConnectorSettings) -> None:
        """
        Ajoute un nouveau prompt et ses paramètres associés à la structure interne.
        
        Args:
            prompt_signature: La signature du prompt à ajouter
            settings: Les paramètres associés au prompt
        """
        self._base_prompt_matcher.add_prompt(prompt_signature, settings)
    
    def remove_prompt(self, prompt_signature: PromptSignature) -> bool:
        """
        Supprime un prompt et ses paramètres associés de la structure interne.
        
        Args:
            prompt_signature: La signature du prompt à supprimer
            
        Returns:
            True si le prompt a été supprimé, False sinon
        """
        return self._base_prompt_matcher.remove_prompt(prompt_signature)
    
    def clear(self) -> None:
        """Supprime tous les prompts et paramètres associés de la structure interne."""
        self._base_prompt_matcher.clear()
        
        with self._cache_lock:
            self._unrecognized_prompts_cache.clear()
    
    def _store_unrecognized_prompt(self, completion_job: CompletionJob) -> None:
        """
        Stocke un prompt non reconnu dans le cache pour analyse ultérieure.
        
        Args:
            completion_job: Le job de complétion non reconnu
        """
        # Limiter la taille du cache si nécessaire
        with self._cache_lock:
            if len(self._unrecognized_prompts_cache) >= self._max_cache_size:
                # Supprimer les entrées les plus anciennes
                self._cleanup_cache()
            
            # Extraire une signature du prompt (par exemple, les 50 premiers caractères)
            prompt_signature_key = self._extract_signature_key(completion_job.prompt)
            
            # Mettre à jour ou ajouter l'entrée dans le cache
            if prompt_signature_key in self._unrecognized_prompts_cache:
                # Mettre à jour une entrée existante
                info = self._unrecognized_prompts_cache[prompt_signature_key]
                info.last_seen = datetime.datetime.now()
                info.count += 1
                
                # Limiter le nombre de prompts stockés par entrée
                if len(info.prompts) < 10:
                    info.prompts.append(completion_job.prompt)
            else:
                # Ajouter une nouvelle entrée
                self._unrecognized_prompts_cache[prompt_signature_key] = self.UnrecognizedPromptInfo(
                    first_seen=datetime.datetime.now(),
                    last_seen=datetime.datetime.now(),
                    count=1,
                    prompts=[completion_job.prompt],
                    request_settings=completion_job.request_settings
                )
    
    def _extract_signature_key(self, prompt: str) -> str:
        """
        Extrait une clé de signature à partir d'un prompt.
        
        Args:
            prompt: Le prompt à analyser
            
        Returns:
            Une clé de signature pour le prompt
        """
        # Utiliser les 50 premiers caractères comme clé de signature
        length = min(50, len(prompt))
        return prompt[:length]
    
    def _identify_potential_pattern(self, prompt: str) -> Optional[PotentialPattern]:
        """
        Identifie un pattern potentiel à partir d'un prompt non reconnu.
        
        Args:
            prompt: Le prompt à analyser
            
        Returns:
            Un pattern potentiel ou None si aucun pattern n'est identifié
        """
        prompt_signature_key = self._extract_signature_key(prompt)
        
        with self._cache_lock:
            # Vérifier si nous avons suffisamment d'occurrences de ce prompt
            if prompt_signature_key in self._unrecognized_prompts_cache:
                info = self._unrecognized_prompts_cache[prompt_signature_key]
                if info.count >= self._min_similar_prompts_to_create_pattern:
                    # Trouver les prompts similaires dans le cache
                    similar_prompts = self._find_similar_prompts(prompt)
                    
                    # Si nous avons suffisamment de prompts similaires, créer un pattern potentiel
                    if len(similar_prompts) >= self._min_similar_prompts_to_create_pattern:
                        return self.PotentialPattern(
                            similar_prompts=similar_prompts,
                            request_settings=info.request_settings
                        )
        
        return None
    
    def _find_similar_prompts(self, prompt: str) -> List[str]:
        """
        Trouve les prompts similaires à un prompt donné dans le cache.
        
        Args:
            prompt: Le prompt à comparer
            
        Returns:
            Une liste de prompts similaires
        """
        similar_prompts = []
        
        for entry in self._unrecognized_prompts_cache.values():
            for cached_prompt in entry.prompts:
                if self._calculate_similarity(prompt, cached_prompt) >= self._similarity_threshold:
                    similar_prompts.append(cached_prompt)
        
        return similar_prompts
    
    def _calculate_similarity(self, str1: str, str2: str) -> int:
        """
        Calcule la similarité entre deux chaînes (0-100).
        
        Args:
            str1: Première chaîne
            str2: Deuxième chaîne
            
        Returns:
            Score de similarité entre 0 et 100
        """
        # Utiliser la distance de Levenshtein pour calculer la similarité
        levenshtein_distance = self._compute_levenshtein_distance(str1, str2)
        max_length = max(len(str1), len(str2))
        
        # Convertir la distance en score de similarité (0-100)
        return int((1.0 - (levenshtein_distance / max_length)) * 100)
    
    def _compute_levenshtein_distance(self, s: str, t: str) -> int:
        """
        Calcule la distance de Levenshtein entre deux chaînes.
        
        Args:
            s: Première chaîne
            t: Deuxième chaîne
            
        Returns:
            Distance de Levenshtein
        """
        if len(s) == 0:
            return len(t)
        if len(t) == 0:
            return len(s)
        
        # Créer une matrice pour stocker les distances
        d = [[0 for _ in range(len(t) + 1)] for _ in range(len(s) + 1)]
        
        # Initialiser la première ligne et la première colonne
        for i in range(len(s) + 1):
            d[i][0] = i
        for j in range(len(t) + 1):
            d[0][j] = j
        
        # Remplir la matrice
        for i in range(1, len(s) + 1):
            for j in range(1, len(t) + 1):
                cost = 0 if s[i-1] == t[j-1] else 1
                d[i][j] = min(
                    d[i-1][j] + 1,      # Suppression
                    d[i][j-1] + 1,      # Insertion
                    d[i-1][j-1] + cost  # Substitution
                )
        
        return d[len(s)][len(t)]
    
    def _analyze_new_pattern(self, potential_pattern: PotentialPattern) -> None:
        """
        Analyse un nouveau pattern potentiel de manière asynchrone.
        
        Args:
            potential_pattern: Le pattern potentiel à analyser
        """
        try:
            # Extraire le préfixe commun des prompts similaires
            common_prefix = self._extract_common_prefix(potential_pattern.similar_prompts)
            
            # Si le préfixe commun est trop court, essayer d'extraire un pattern regex
            pattern = common_prefix if len(common_prefix) >= 10 else self._extract_regex_pattern(potential_pattern.similar_prompts)
            
            if pattern:
                # Créer une nouvelle signature de prompt
                prompt_signature = PromptSignature(
                    prompt_start=pattern,
                    request_settings=potential_pattern.request_settings,
                    matching_regex=pattern if self._contains_regex_special_chars(pattern) else None
                )
                
                # Créer les paramètres pour le nouveau type de prompt
                settings = PromptMultiConnectorSettings()
                settings.prompt_type.prompt_name = f"adaptive_pattern_{int(time.time())}"
                settings.prompt_type.signature = prompt_signature
                settings.prompt_type.signature_needs_adjusting = True
                
                # Ajouter les instances au type de prompt
                settings.prompt_type.instances = potential_pattern.similar_prompts
                
                # Ajouter le nouveau prompt au matcher de base
                self._base_prompt_matcher.add_prompt(prompt_signature, settings)
                
                # Supprimer les prompts correspondants du cache
                self._remove_matching_prompts_from_cache(pattern)
        except Exception:
            # Ignorer les exceptions lors de l'analyse asynchrone
            pass
    
    def _extract_common_prefix(self, strings: List[str]) -> str:
        """
        Extrait le préfixe commun d'une liste de chaînes.
        
        Args:
            strings: Liste de chaînes
            
        Returns:
            Le préfixe commun
        """
        if not strings:
            return ""
        
        first_string = strings[0]
        prefix_length = len(first_string)
        
        for i in range(1, len(strings)):
            prefix_length = min(prefix_length, len(strings[i]))
            for j in range(prefix_length):
                if first_string[j] != strings[i][j]:
                    prefix_length = j
                    break
        
        return first_string[:prefix_length]
    
    def _extract_regex_pattern(self, strings: List[str]) -> str:
        """
        Extrait un pattern regex à partir d'une liste de chaînes.
        
        Args:
            strings: Liste de chaînes
            
        Returns:
            Un pattern regex
        """
        if len(strings) < 2:
            return ""
        
        # Trouver le préfixe commun
        prefix = self._extract_common_prefix(strings)
        
        # Si le préfixe est trop court, essayer de trouver un pattern plus complexe
        if len(prefix) < 5:
            # Analyser les chaînes pour trouver des motifs récurrents
            common_words = self._find_common_words(strings)
            if common_words:
                # Construire un pattern regex à partir des mots communs
                return ".*".join(common_words)
        
        return prefix
    
    def _find_common_words(self, strings: List[str]) -> List[str]:
        """
        Trouve les mots communs dans une liste de chaînes.
        
        Args:
            strings: Liste de chaînes
            
        Returns:
            Liste des mots communs
        """
        if not strings:
            return []
        
        # Diviser la première chaîne en mots
        separators = [' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?']
        words = []
        current_word = ""
        
        for char in strings[0]:
            if char in separators:
                if current_word:
                    words.append(current_word)
                    current_word = ""
            else:
                current_word += char
        
        if current_word:
            words.append(current_word)
        
        # Filtrer les mots qui apparaissent dans toutes les chaînes
        common_words = []
        for word in words:
            if len(word) >= 3 and all(word in s for s in strings):
                common_words.append(word)
        
        return common_words
    
    def _remove_matching_prompts_from_cache(self, pattern: str) -> None:
        """
        Supprime les prompts correspondant à un pattern du cache.
        
        Args:
            pattern: Le pattern à rechercher
        """
        with self._cache_lock:
            try:
                # Créer une regex à partir du pattern
                regex = re.compile(pattern)
                
                # Trouver les clés à supprimer
                keys_to_remove = []
                for key, entry in self._unrecognized_prompts_cache.items():
                    all_match = True
                    for prompt in entry.prompts:
                        if not regex.match(prompt):
                            all_match = False
                            break
                    
                    if all_match:
                        keys_to_remove.append(key)
                
                # Supprimer les entrées correspondantes
                for key in keys_to_remove:
                    self._unrecognized_prompts_cache.pop(key, None)
            except Exception:
                # Ignorer les exceptions lors de la suppression
                pass
    
    def _cleanup_cache(self) -> None:
        """Nettoie le cache en supprimant les entrées expirées."""
        with self._cache_lock:
            now = datetime.datetime.now()
            
            # Supprimer les entrées expirées
            keys_to_remove = [
                key for key, entry in self._unrecognized_prompts_cache.items()
                if now - entry.last_seen > self._cache_entry_expiration
            ]
            
            for key in keys_to_remove:
                self._unrecognized_prompts_cache.pop(key, None)
            
            # Si le cache est toujours trop grand, supprimer les entrées les plus anciennes
            if len(self._unrecognized_prompts_cache) > self._max_cache_size:
                # Trier les entrées par date de dernière observation
                sorted_entries = sorted(
                    self._unrecognized_prompts_cache.items(),
                    key=lambda x: x[1].last_seen
                )
                
                # Supprimer la moitié des entrées les plus anciennes
                num_to_remove = len(self._unrecognized_prompts_cache) - self._max_cache_size // 2
                keys_to_remove = [key for key, _ in sorted_entries[:num_to_remove]]
                
                for key in keys_to_remove:
                    self._unrecognized_prompts_cache.pop(key, None)
        
        # Redémarrer le timer pour le prochain nettoyage
        self._cleanup_timer = threading.Timer(600, self._cleanup_cache)  # 10 minutes
        self._cleanup_timer.daemon = True
        self._cleanup_timer.start()
    
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