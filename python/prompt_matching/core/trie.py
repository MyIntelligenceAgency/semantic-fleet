"""
Module contenant l'implémentation du Trie.

Le Trie est une structure de données arborescente qui permet de stocker des paires clé-valeur
où les clés sont des séquences (comme des chaînes de caractères), avec des fonctionnalités
de recherche par préfixe.
"""

from typing import TypeVar, Generic, Dict, List, Tuple, Optional, Callable, Iterable, Any

K = TypeVar('K')  # Type de clé (séquence)
E = TypeVar('E')  # Type d'élément de la séquence
V = TypeVar('V')  # Type de valeur

class Trie(Generic[K, E, V]):
    """
    Une structure de données arborescente pour stocker des paires clé-valeur où les clés sont des séquences.
    
    Le Trie permet une recherche efficace par préfixe, ce qui est utile pour la détection
    de signatures de prompts.
    """
    
    class Node:
        """Nœud interne du Trie."""
        
        def __init__(self):
            """Initialise un nouveau nœud."""
            self.children: Dict[E, Trie.Node] = {}
            self.value: Optional[V] = None
            self.has_value: bool = False
    
    def __init__(self, key_to_elements: Callable[[K], Iterable[E]] = None):
        """
        Initialise une nouvelle instance de Trie.
        
        Args:
            key_to_elements: Fonction de conversion d'une clé en séquence d'éléments
                            (si None, la clé doit être itérable)
        """
        self._root = self.Node()
        self._count = 0
        self._key_to_elements = key_to_elements or (lambda k: k)
    
    @property
    def count(self) -> int:
        """Nombre d'éléments dans le Trie."""
        return self._count
    
    def add(self, key: K, value: V) -> None:
        """
        Ajoute ou met à jour une paire clé-valeur dans le Trie.
        
        Args:
            key: La clé à ajouter (séquence)
            value: La valeur associée à la clé
            
        Raises:
            ValueError: Si la clé est None
        """
        if key is None:
            raise ValueError("La clé ne peut pas être None")
        
        elements = self._key_to_elements(key)
        node = self._root
        
        # Parcourir ou créer le chemin pour la clé
        for element in elements:
            if element not in node.children:
                node.children[element] = self.Node()
            node = node.children[element]
        
        # Mettre à jour le compteur si c'est une nouvelle clé
        if not node.has_value:
            self._count += 1
        
        # Stocker la valeur
        node.value = value
        node.has_value = True
    
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
        
        node = self._find_node(key)
        
        if node is not None and node.has_value:
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
        node = self._root
        last_value_node = None if not node.has_value else node
        
        # Parcourir le Trie en suivant le préfixe
        for element in elements:
            if element not in node.children:
                break
            node = node.children[element]
            if node.has_value:
                last_value_node = node
        
        if last_value_node is not None:
            return True, last_value_node.value
        return False, None
    
    def remove(self, key: K) -> bool:
        """
        Supprime une paire clé-valeur du Trie.
        
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
        return self._remove_recursive(self._root, elements, 0)
    
    def clear(self) -> None:
        """Supprime tous les éléments du Trie."""
        self._root = self.Node()
        self._count = 0
    
    def _find_node(self, key: K) -> Optional[Node]:
        """
        Trouve le nœud correspondant à une clé.
        
        Args:
            key: La clé à rechercher
            
        Returns:
            Le nœud correspondant à la clé ou None si la clé n'existe pas
        """
        elements = self._key_to_elements(key)
        node = self._root
        
        for element in elements:
            if element not in node.children:
                return None
            node = node.children[element]
        
        return node
    
    def _remove_recursive(self, node: Node, elements: List[E], depth: int) -> bool:
        """
        Supprime récursivement une clé du Trie.
        
        Args:
            node: Le nœud courant
            elements: La liste des éléments de la clé
            depth: La profondeur actuelle dans l'arbre
            
        Returns:
            True si la clé a été supprimée, False sinon
        """
        # Si nous avons atteint la fin de la clé
        if depth == len(elements):
            if not node.has_value:
                return False
            
            node.has_value = False
            node.value = None
            self._count -= 1
            return True
        
        element = elements[depth]
        
        if element not in node.children:
            return False
        
        result = self._remove_recursive(node.children[element], elements, depth + 1)
        
        # Si le nœud enfant n'a plus d'enfants et pas de valeur, on peut le supprimer
        child = node.children[element]
        if not child.has_value and not child.children:
            del node.children[element]
        
        return result