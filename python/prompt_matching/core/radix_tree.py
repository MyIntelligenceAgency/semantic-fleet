"""
Module contenant l'implémentation du RadixTree.

Le RadixTree est une optimisation du Trie qui compresse les chemins pour réduire l'espace mémoire utilisé.
"""

from typing import TypeVar, Generic, Dict, List, Tuple, Optional, Callable, Iterable, Any
from collections import deque

K = TypeVar('K')  # Type de clé (séquence)
E = TypeVar('E')  # Type d'élément de la séquence
V = TypeVar('V')  # Type de valeur

class RadixTree(Generic[K, E, V]):
    """
    Une structure de données arborescente optimisée pour stocker des paires clé-valeur où les clés sont des séquences.
    
    Le RadixTree est une optimisation du Trie qui compresse les chemins en fusionnant les nœuds
    qui n'ont qu'un seul enfant, réduisant ainsi l'espace mémoire utilisé.
    """
    
    class Node:
        """Nœud interne du RadixTree."""
        
        def __init__(self):
            """Initialise un nouveau nœud."""
            self.children: Dict[E, Tuple[List[E], RadixTree.Node]] = {}
            self.value: Optional[V] = None
            self.has_value: bool = False
    
    def __init__(self, key_to_elements: Callable[[K], Iterable[E]] = None, element_comparer = None):
        """
        Initialise une nouvelle instance de RadixTree.
        
        Args:
            key_to_elements: Fonction de conversion d'une clé en séquence d'éléments
                            (si None, la clé doit être itérable)
            element_comparer: Fonction de comparaison des éléments
                            (si None, utilise l'égalité standard)
        """
        self._root = self.Node()
        self._count = 0
        self._key_to_elements = key_to_elements or (lambda k: k)
        self._element_comparer = element_comparer or (lambda x, y: x == y)
    
    @property
    def count(self) -> int:
        """Nombre d'éléments dans le RadixTree."""
        return self._count
    
    def add(self, key: K, value: V) -> None:
        """
        Ajoute ou met à jour une paire clé-valeur dans le RadixTree.
        
        Args:
            key: La clé à ajouter (séquence)
            value: La valeur associée à la clé
            
        Raises:
            ValueError: Si la clé est None
        """
        if key is None:
            raise ValueError("La clé ne peut pas être None")
        
        elements = list(self._key_to_elements(key))
        self._add_internal(self._root, elements, 0, value)
    
    def try_get_value(self, key: K) -> Tuple[bool, Optional[V]]:
        """
        Tente de récupérer la valeur associée à une clé.
        
        Args:
            key: La clé à rechercher
            
        Returns:
            Un tuple (succès, valeur) où succès est True si la clé existe,
            et valeur est la valeur associée à la clé ou None si la clé n'existe pas
            
        Raises:
            ValueError: Si la clé est None
        """
        if key is None:
            raise ValueError("La clé ne peut pas être None")
        
        elements = list(self._key_to_elements(key))
        node, remaining = self._find_node(self._root, elements, 0)
        
        if node is not None and node.has_value and not remaining:
            return True, node.value
        return False, None
    
    def try_get_value_by_prefix(self, prefix: K) -> Tuple[bool, Optional[V]]:
        """
        Tente de récupérer la valeur associée à la plus longue clé qui est un préfixe de la séquence donnée.
        
        Args:
            prefix: La séquence à rechercher
            
        Returns:
            Un tuple (succès, valeur) où succès est True si un préfixe existe,
            et valeur est la valeur associée au préfixe le plus long ou None si aucun préfixe n'existe
            
        Raises:
            ValueError: Si le préfixe est None
        """
        if prefix is None:
            raise ValueError("Le préfixe ne peut pas être None")
        
        elements = list(self._key_to_elements(prefix))
        return self._find_longest_prefix(self._root, elements, 0)
    
    def remove(self, key: K) -> bool:
        """
        Supprime une paire clé-valeur du RadixTree.
        
        Args:
            key: La clé à supprimer
            
        Returns:
            True si la clé a été supprimée, False si elle n'existait pas
            
        Raises:
            ValueError: Si la clé est None
        """
        if key is None:
            raise ValueError("La clé ne peut pas être None")
        
        elements = list(self._key_to_elements(key))
        return self._remove_internal(self._root, elements, 0)
    
    def clear(self) -> None:
        """Supprime tous les éléments du RadixTree."""
        self._root = self.Node()
        self._count = 0
    
    def _add_internal(self, node: Node, elements: List[E], index: int, value: V) -> None:
        """
        Ajoute récursivement une clé au RadixTree.
        
        Args:
            node: Le nœud courant
            elements: La liste des éléments de la clé
            index: L'index courant dans la liste des éléments
            value: La valeur à associer à la clé
        """
        # Si nous avons atteint la fin de la clé
        if index == len(elements):
            if not node.has_value:
                self._count += 1
            node.value = value
            node.has_value = True
            return
        
        current_element = elements[index]
        
        # Vérifier si l'élément existe déjà dans les enfants
        for edge_element, (edge_path, child) in node.children.items():
            if self._element_comparer(current_element, edge_element):
                # Trouver le préfixe commun entre le chemin de l'arête et les éléments restants
                common_prefix_length = 0
                remaining_elements = elements[index:]
                
                for i in range(min(len(edge_path), len(remaining_elements))):
                    if self._element_comparer(edge_path[i], remaining_elements[i]):
                        common_prefix_length += 1
                    else:
                        break
                
                # Si le préfixe commun est plus court que le chemin de l'arête, il faut diviser l'arête
                if common_prefix_length < len(edge_path):
                    # Créer un nouveau nœud pour la division
                    new_node = self.Node()
                    
                    # Mettre à jour les enfants du nœud courant
                    node.children[edge_element] = (edge_path[:common_prefix_length], new_node)
                    
                    # Ajouter l'ancien enfant comme enfant du nouveau nœud
                    new_node.children[edge_path[common_prefix_length]] = (edge_path[common_prefix_length+1:], child)
                    
                    # Si la clé à ajouter est plus longue que le préfixe commun, continuer l'ajout
                    if common_prefix_length < len(remaining_elements):
                        self._add_internal(new_node, elements, index + common_prefix_length, value)
                    else:
                        # Sinon, la clé à ajouter est exactement le préfixe commun
                        if not new_node.has_value:
                            self._count += 1
                        new_node.value = value
                        new_node.has_value = True
                else:
                    # Le préfixe commun est égal au chemin de l'arête, continuer l'ajout dans l'enfant
                    self._add_internal(child, elements, index + common_prefix_length, value)
                
                return
        
        # L'élément n'existe pas dans les enfants, créer une nouvelle arête
        new_node = self.Node()
        node.children[current_element] = (elements[index+1:], new_node)
        
        # La clé à ajouter est complète
        if not new_node.has_value:
            self._count += 1
        new_node.value = value
        new_node.has_value = True
    
    def _find_node(self, node: Node, elements: List[E], index: int) -> Tuple[Optional[Node], bool]:
        """
        Trouve le nœud correspondant à une clé.
        
        Args:
            node: Le nœud courant
            elements: La liste des éléments de la clé
            index: L'index courant dans la liste des éléments
            
        Returns:
            Un tuple (nœud, reste) où nœud est le nœud correspondant à la clé ou None si la clé n'existe pas,
            et reste est True s'il reste des éléments non consommés dans la clé
        """
        # Si nous avons atteint la fin de la clé
        if index == len(elements):
            return node, False
        
        current_element = elements[index]
        
        # Vérifier si l'élément existe dans les enfants
        for edge_element, (edge_path, child) in node.children.items():
            if self._element_comparer(current_element, edge_element):
                # Vérifier si le chemin de l'arête est un préfixe des éléments restants
                remaining_elements = elements[index:]
                
                if len(remaining_elements) < len(edge_path) + 1:
                    # La clé est plus courte que le chemin de l'arête
                    return None, True
                
                # Vérifier si les éléments correspondent
                match = True
                for i in range(len(edge_path)):
                    if not self._element_comparer(edge_path[i], remaining_elements[i+1]):
                        match = False
                        break
                
                if match:
                    # Continuer la recherche dans l'enfant
                    return self._find_node(child, elements, index + len(edge_path) + 1)
                
                return None, True
        
        return None, True
    
    def _find_longest_prefix(self, node: Node, elements: List[E], index: int) -> Tuple[bool, Optional[V]]:
        """
        Trouve la plus longue clé qui est un préfixe de la séquence donnée.
        
        Args:
            node: Le nœud courant
            elements: La liste des éléments de la séquence
            index: L'index courant dans la liste des éléments
            
        Returns:
            Un tuple (succès, valeur) où succès est True si un préfixe existe,
            et valeur est la valeur associée au préfixe le plus long ou None si aucun préfixe n'existe
        """
        # Si le nœud courant a une valeur, c'est un candidat pour le préfixe le plus long
        result = (node.has_value, node.value)
        
        # Si nous avons atteint la fin de la séquence
        if index == len(elements):
            return result
        
        current_element = elements[index]
        
        # Vérifier si l'élément existe dans les enfants
        for edge_element, (edge_path, child) in node.children.items():
            if self._element_comparer(current_element, edge_element):
                # Vérifier jusqu'où le chemin de l'arête correspond aux éléments restants
                remaining_elements = elements[index:]
                common_length = 0
                
                for i in range(min(len(edge_path) + 1, len(remaining_elements))):
                    if i == 0:
                        # Le premier élément est déjà vérifié
                        common_length = 1
                        continue
                    
                    if i - 1 < len(edge_path) and self._element_comparer(edge_path[i-1], remaining_elements[i]):
                        common_length += 1
                    else:
                        break
                
                # Si tout le chemin de l'arête correspond, continuer la recherche dans l'enfant
                if common_length == len(edge_path) + 1:
                    child_result = self._find_longest_prefix(child, elements, index + common_length)
                    if child_result[0]:
                        return child_result
                
                # Si le nœud courant a une valeur, c'est le préfixe le plus long trouvé
                return result
        
        # Aucun enfant ne correspond, retourner le résultat du nœud courant
        return result
    
    def _remove_internal(self, node: Node, elements: List[E], index: int) -> bool:
        """
        Supprime récursivement une clé du RadixTree.
        
        Args:
            node: Le nœud courant
            elements: La liste des éléments de la clé
            index: L'index courant dans la liste des éléments
            
        Returns:
            True si la clé a été supprimée, False sinon
        """
        # Si nous avons atteint la fin de la clé
        if index == len(elements):
            if not node.has_value:
                return False
            
            node.has_value = False
            node.value = None
            self._count -= 1
            return True
        
        current_element = elements[index]
        
        # Vérifier si l'élément existe dans les enfants
        for edge_element, (edge_path, child) in list(node.children.items()):
            if self._element_comparer(current_element, edge_element):
                # Vérifier si le chemin de l'arête est un préfixe des éléments restants
                remaining_elements = elements[index:]
                
                if len(remaining_elements) < len(edge_path) + 1:
                    # La clé est plus courte que le chemin de l'arête
                    return False
                
                # Vérifier si les éléments correspondent
                match = True
                for i in range(len(edge_path)):
                    if not self._element_comparer(edge_path[i], remaining_elements[i+1]):
                        match = False
                        break
                
                if match:
                    # Continuer la suppression dans l'enfant
                    result = self._remove_internal(child, elements, index + len(edge_path) + 1)
                    
                    # Si l'enfant n'a plus de valeur et pas d'enfants, le supprimer
                    if result and not child.has_value and not child.children:
                        del node.children[edge_element]
                    
                    # Si le nœud courant n'a qu'un seul enfant et pas de valeur, fusionner avec l'enfant
                    if len(node.children) == 1 and not node.has_value:
                        only_element, (only_path, only_child) = next(iter(node.children.items()))
                        
                        # Fusionner le nœud courant avec son seul enfant
                        node.children = only_child.children
                        node.has_value = only_child.has_value
                        node.value = only_child.value
                    
                    return result
                
                return False
        
        return False