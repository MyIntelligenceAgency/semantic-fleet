"""
Module contenant l'implémentation de l'OptimizedHybridPromptMatcher.

L'OptimizedHybridPromptMatcher est une version optimisée du HybridPromptMatcher avec des fonctionnalités
supplémentaires comme le traitement parallèle et la combinaison des regex.
"""

import re
import threading
from concurrent.futures import ThreadPoolExecutor
from typing import Dict, List, Optional, Tuple, Pattern, Set
from ..matchers.base import IPromptMatcher, PromptSignature, PromptMultiConnectorSettings, CompletionJob
from ..core.radix_tree import RadixTree

class OptimizedHybridPromptMatcher(IPromptMatcher):
    """
    Une implémentation optimisée du matcher de prompts hybride avec traitement parallèle et combinaison des regex.
    
    Cette implémentation ajoute plusieurs optimisations au HybridPromptMatcher :
    - Cache des expressions régulières compilées
    - Combinaison de plusieurs expressions régulières en une seule pour réduire le nombre de tests
    - Traitement parallèle des expressions régulières lorsque leur nombre dépasse un certain seuil
    - Utilisation de verrous de lecture/écriture pour assurer la thread-safety
    """
    
    def __init__(self):
        """Initialise une nouvelle instance de OptimizedHybridPromptMatcher."""
        self._radix_tree = RadixTree[str, str, PromptMultiConnectorSettings](
            key_to_elements=lambda s: s
        )
        self._regex_cache: Dict[str, Pattern] = {}
        self._regex_prompts: List[Tuple[Pattern, PromptMultiConnectorSettings]] = []
        self._combined_regex_groups: List[Tuple[Pattern, Dict[str, PromptMultiConnectorSettings]]] = []
        
        # Nombre maximum de regex à combiner dans un seul groupe
        self._max_regex_per_group = 10
        
        # Seuil pour basculer entre traitement séquentiel et parallèle
        self._parallel_threshold = 5
        
        # Verrou pour assurer la thread-safety
        self._lock = threading.RLock()
    
    @property
    def count(self) -> int:
        """Nombre de prompts stockés dans le matcher."""
        with self._lock:
            return self._radix_tree.count + len(self._regex_prompts)
    
    def match_prompt_settings(self, completion_job: CompletionJob, 
                             prompt_settings: List[PromptMultiConnectorSettings]) -> Optional[PromptMultiConnectorSettings]:
        """
        Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        
        Cette implémentation utilise plusieurs optimisations pour améliorer les performances :
        1. Recherche par préfixe dans le RadixTree (plus rapide)
        2. Recherche dans les groupes de regex combinés
        3. Recherche dans les regex individuels (en parallèle si nécessaire)
        4. Recherche dans les paramètres fournis
        
        Args:
            completion_job: Le job de complétion à matcher
            prompt_settings: La collection de paramètres de prompts disponibles
            
        Returns:
            Les paramètres correspondants ou None si aucune correspondance n'est trouvée
        """
        if completion_job is None:
            raise ValueError("Le job de complétion ne peut pas être None")
        
        with self._lock:
            # 1. Recherche par préfixe dans le RadixTree (plus rapide)
            success, settings = self._radix_tree.try_get_value_by_prefix(completion_job.prompt)
            
            if success:
                return settings
            
            # 2. Recherche dans les groupes de regex combinés
            for combined_regex, group_settings in self._combined_regex_groups:
                match = combined_regex.match(completion_job.prompt)
                if match:
                    # Trouver quel pattern spécifique a matché
                    for i, group_name in enumerate(combined_regex.groupindex.keys(), 1):
                        if match.group(i):
                            if group_name in group_settings:
                                return group_settings[group_name]
            
            # 3. Recherche dans les regex individuels (en parallèle si nécessaire)
            if self._regex_prompts:
                if len(self._regex_prompts) >= self._parallel_threshold:
                    # Traitement parallèle pour un grand nombre de regex
                    return self._match_regex_in_parallel(completion_job.prompt)
                else:
                    # Traitement séquentiel pour un petit nombre de regex
                    for regex, regex_settings in self._regex_prompts:
                        if regex.match(completion_job.prompt):
                            return regex_settings
            
            # 4. Si aucune correspondance n'est trouvée, recherche dans la collection fournie
            for settings in prompt_settings:
                if settings.prompt_type.signature.matches(completion_job):
                    return settings
            
            return None
    
    def _match_regex_in_parallel(self, prompt: str) -> Optional[PromptMultiConnectorSettings]:
        """
        Recherche en parallèle dans les expressions régulières individuelles.
        
        Args:
            prompt: Le prompt à tester
            
        Returns:
            Les paramètres correspondants ou None si aucune correspondance n'est trouvée
        """
        # Copier la liste pour éviter les problèmes de concurrence
        regex_prompts = self._regex_prompts.copy()
        
        # Utiliser un ThreadPoolExecutor pour tester les regex en parallèle
        with ThreadPoolExecutor() as executor:
            # Fonction pour tester un regex
            def test_regex(item):
                regex, settings = item
                if regex.match(prompt):
                    return settings
                return None
            
            # Exécuter les tests en parallèle
            results = list(executor.map(test_regex, regex_prompts))
            
            # Retourner le premier résultat non nul
            for result in results:
                if result is not None:
                    return result
        
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
        
        with self._lock:
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
        
        with self._lock:
            pattern = prompt_signature.prompt_start
            
            # Si c'est une regex, la supprimer de la liste des regex
            if self._contains_regex_special_chars(pattern):
                removed = False
                
                # Vérifier dans les regex individuels
                if pattern in self._regex_cache:
                    regex = self._regex_cache[pattern]
                    for i, (r, _) in enumerate(self._regex_prompts):
                        if r == regex:
                            self._regex_prompts.pop(i)
                            self._regex_cache.pop(pattern)
                            removed = True
                            break
                
                # Reconstruire les groupes de regex combinés si nécessaire
                if removed and self._regex_prompts:
                    self._rebuild_combined_regex_groups()
                
                return removed
            else:
                # Sinon, la supprimer du RadixTree
                return self._radix_tree.remove(pattern)
    
    def clear(self) -> None:
        """Supprime tous les prompts et paramètres associés de la structure interne."""
        with self._lock:
            self._radix_tree.clear()
            self._regex_prompts.clear()
            self._regex_cache.clear()
            self._combined_regex_groups.clear()
    
    def _add_regex_prompt(self, pattern: str, settings: PromptMultiConnectorSettings) -> None:
        """
        Ajoute un prompt sous forme de regex.
        
        Args:
            pattern: Motif regex
            settings: Paramètres associés
        """
        # Compiler le regex s'il n'existe pas déjà dans le cache
        if pattern not in self._regex_cache:
            regex = re.compile(pattern)
            self._regex_cache[pattern] = regex
        else:
            regex = self._regex_cache[pattern]
        
        # Vérifier si le regex existe déjà
        existing_index = -1
        for i, (r, _) in enumerate(self._regex_prompts):
            if r == regex:
                existing_index = i
                break
        
        if existing_index >= 0:
            # Mettre à jour les paramètres existants
            self._regex_prompts[existing_index] = (regex, settings)
        else:
            # Ajouter une nouvelle entrée
            self._regex_prompts.append((regex, settings))
            
            # Reconstruire les groupes de regex combinés si nécessaire
            if len(self._regex_prompts) % self._max_regex_per_group == 1:
                self._rebuild_combined_regex_groups()
    
    def _rebuild_combined_regex_groups(self) -> None:
        """Reconstruit les groupes de regex combinés."""
        self._combined_regex_groups.clear()
        
        # Regrouper les regex par lots de max_regex_per_group
        for i in range(0, len(self._regex_prompts), self._max_regex_per_group):
            group = self._regex_prompts[i:i + self._max_regex_per_group]
            if group:
                self._try_combine_regex_group(group)
    
    def _try_combine_regex_group(self, regex_group: List[Tuple[Pattern, PromptMultiConnectorSettings]]) -> None:
        """
        Tente de combiner un groupe de regex en un seul regex.
        
        Args:
            regex_group: Groupe de regex à combiner
        """
        try:
            # Construire un pattern combiné avec des groupes nommés
            pattern_builder = []
            group_settings = {}
            
            for i, (regex, settings) in enumerate(regex_group):
                group_name = f"Group{i}"
                
                # Ajouter un OR si ce n'est pas le premier pattern
                if i > 0:
                    pattern_builder.append('|')
                
                # Ajouter le pattern avec un groupe nommé
                pattern_builder.append(f"(?P<{group_name}>{regex.pattern})")
                
                # Stocker les paramètres associés au groupe
                group_settings[group_name] = settings
            
            # Compiler le regex combiné
            combined_regex = re.compile(''.join(pattern_builder))
            
            # Ajouter le groupe combiné
            self._combined_regex_groups.append((combined_regex, group_settings))
        except re.error:
            # Si la combinaison échoue (par exemple, en raison de regex incompatibles),
            # on laisse les regex individuels tels quels
            pass
    
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