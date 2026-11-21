"""
Module contenant l'implémentation du HybridDictionary.

Le HybridDictionary est une structure de données qui combine les avantages d'une liste et d'un dictionnaire,
avec un seuil à partir duquel elle change de comportement interne pour des raisons de performance.
"""

from typing import TypeVar, Generic, Dict, List, Tuple, Iterator, Optional, Callable, Any

K = TypeVar('K')  # Type de clé
V = TypeVar('V')  # Type de valeur

class HybridDictionary(Generic[K, V]):
    """
    Une structure de données qui combine les avantages d'une liste et d'un dictionnaire.
    
    Pour un petit nombre d'éléments, elle utilise une liste de tuples (clé, valeur) pour économiser
    de la mémoire. Une fois que le nombre d'éléments dépasse un seuil, elle bascule vers un
    dictionnaire standard pour de meilleures performances de recherche.
    """
    
    def __init__(self, threshold: int = 10, key_comparer: Callable[[K, K], bool] = None):
        """
        Initialise une nouvelle instance de HybridDictionary.
        
        Args:
            threshold: Seuil à partir duquel basculer vers un dictionnaire standard
            key_comparer: Fonction de comparaison des clés (si None, utilise l'égalité standard)
        """
        self._threshold = threshold
        self._key_comparer = key_comparer
        self._items_list: List[Tuple[K, V]] = []
        self._items_dict: Dict[K, V] = {}
        self._using_dict = False
    
    @property
    def count(self) -> int:
        """Nombre d'éléments dans le dictionnaire."""
        return len(self._items_dict) if self._using_dict else len(self._items_list)
    
    def add(self, key: K, value: V) -> None:
        """
        Ajoute une paire clé-valeur au dictionnaire.
        
        Args:
            key: La clé à ajouter
            value: La valeur associée à la clé
            
        Raises:
            ValueError: Si la clé existe déjà
        """
        if self.contains_key(key):
            raise ValueError(f"La clé '{key}' existe déjà dans le dictionnaire")
        
        if self._using_dict:
            self._items_dict[key] = value
        else:
            self._items_list.append((key, value))
            
            # Basculer vers un dictionnaire si le seuil est dépassé
            if len(self._items_list) > self._threshold:
                self._convert_to_dict()
    
    def __setitem__(self, key: K, value: V) -> None:
        """
        Définit ou remplace une valeur associée à une clé.
        
        Args:
            key: La clé à définir
            value: La valeur à associer à la clé
        """
        if self._using_dict:
            self._items_dict[key] = value
        else:
            for i, (k, _) in enumerate(self._items_list):
                if self._compare_keys(k, key):
                    self._items_list[i] = (key, value)
                    return
            
            # La clé n'existe pas, l'ajouter
            self._items_list.append((key, value))
            
            # Basculer vers un dictionnaire si le seuil est dépassé
            if len(self._items_list) > self._threshold:
                self._convert_to_dict()
    
    def __getitem__(self, key: K) -> V:
        """
        Récupère la valeur associée à une clé.
        
        Args:
            key: La clé à rechercher
            
        Returns:
            La valeur associée à la clé
            
        Raises:
            KeyError: Si la clé n'existe pas
        """
        if self._using_dict:
            return self._items_dict[key]
        else:
            for k, v in self._items_list:
                if self._compare_keys(k, key):
                    return v
            
            raise KeyError(f"La clé '{key}' n'existe pas dans le dictionnaire")
    
    def contains_key(self, key: K) -> bool:
        """
        Vérifie si une clé existe dans le dictionnaire.
        
        Args:
            key: La clé à rechercher
            
        Returns:
            True si la clé existe, False sinon
        """
        if self._using_dict:
            return key in self._items_dict
        else:
            return any(self._compare_keys(k, key) for k, _ in self._items_list)
    
    def try_get_value(self, key: K) -> Tuple[bool, Optional[V]]:
        """
        Tente de récupérer la valeur associée à une clé.
        
        Args:
            key: La clé à rechercher
            
        Returns:
            Un tuple (succès, valeur) où succès est True si la clé existe,
            et valeur est la valeur associée à la clé ou None si la clé n'existe pas
        """
        if self._using_dict:
            if key in self._items_dict:
                return True, self._items_dict[key]
            return False, None
        else:
            for k, v in self._items_list:
                if self._compare_keys(k, key):
                    return True, v
            return False, None
    
    def remove(self, key: K) -> bool:
        """
        Supprime une paire clé-valeur du dictionnaire.
        
        Args:
            key: La clé à supprimer
            
        Returns:
            True si la clé a été supprimée, False si elle n'existait pas
        """
        if self._using_dict:
            if key in self._items_dict:
                del self._items_dict[key]
                return True
            return False
        else:
            for i, (k, _) in enumerate(self._items_list):
                if self._compare_keys(k, key):
                    self._items_list.pop(i)
                    return True
            return False
    
    def clear(self) -> None:
        """Supprime tous les éléments du dictionnaire."""
        if self._using_dict:
            self._items_dict.clear()
        else:
            self._items_list.clear()
    
    @property
    def keys(self) -> List[K]:
        """Liste des clés du dictionnaire."""
        if self._using_dict:
            return list(self._items_dict.keys())
        else:
            return [k for k, _ in self._items_list]
    
    @property
    def values(self) -> List[V]:
        """Liste des valeurs du dictionnaire."""
        if self._using_dict:
            return list(self._items_dict.values())
        else:
            return [v for _, v in self._items_list]
    
    def _convert_to_dict(self) -> None:
        """Convertit la liste interne en dictionnaire."""
        self._items_dict = {k: v for k, v in self._items_list}
        self._items_list = []
        self._using_dict = True
    
    def _compare_keys(self, key1: K, key2: K) -> bool:
        """
        Compare deux clés en utilisant le comparateur personnalisé s'il existe.
        
        Args:
            key1: Première clé
            key2: Deuxième clé
            
        Returns:
            True si les clés sont égales, False sinon
        """
        if self._key_comparer is not None:
            return self._key_comparer(key1, key2)
        return key1 == key2
    
    def __iter__(self) -> Iterator[Tuple[K, V]]:
        """
        Renvoie un itérateur sur les paires clé-valeur du dictionnaire.
        
        Returns:
            Un itérateur sur les paires clé-valeur
        """
        if self._using_dict:
            return iter(self._items_dict.items())
        else:
            return iter(self._items_list)
    
    def __len__(self) -> int:
        """
        Renvoie le nombre d'éléments dans le dictionnaire.
        
        Returns:
            Le nombre d'éléments
        """
        return self.count