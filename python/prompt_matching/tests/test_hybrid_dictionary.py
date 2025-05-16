"""
Tests unitaires pour la classe HybridDictionary.
"""

import unittest
from python.prompt_matching.core.hybrid_dictionary import HybridDictionary

class TestHybridDictionary(unittest.TestCase):
    """Tests pour la classe HybridDictionary."""
    
    def test_constructor_creates_empty_dictionary(self):
        """Le constructeur doit créer un dictionnaire vide."""
        dictionary = HybridDictionary()
        self.assertEqual(0, dictionary.count)
    
    def test_constructor_with_custom_threshold(self):
        """Le constructeur avec un seuil personnalisé doit créer un dictionnaire vide."""
        dictionary = HybridDictionary(threshold=5)
        self.assertEqual(0, dictionary.count)
    
    def test_constructor_with_custom_comparer(self):
        """Le constructeur avec un comparateur personnalisé doit créer un dictionnaire vide."""
        dictionary = HybridDictionary(key_comparer=lambda x, y: x.lower() == y.lower())
        self.assertEqual(0, dictionary.count)
    
    def test_add_new_key_increases_count(self):
        """L'ajout d'une nouvelle clé doit augmenter le compteur."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        self.assertEqual(1, dictionary.count)
    
    def test_add_duplicate_key_raises_value_error(self):
        """L'ajout d'une clé en double doit lever une ValueError."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        with self.assertRaises(ValueError):
            dictionary.add("key1", 2)
    
    def test_indexer_get_existing_key_returns_value(self):
        """L'accès à une clé existante doit retourner la valeur associée."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        self.assertEqual(1, dictionary["key1"])
    
    def test_indexer_get_non_existing_key_raises_key_error(self):
        """L'accès à une clé inexistante doit lever une KeyError."""
        dictionary = HybridDictionary()
        with self.assertRaises(KeyError):
            _ = dictionary["key1"]
    
    def test_indexer_set_existing_key_updates_value(self):
        """La mise à jour d'une clé existante doit modifier la valeur associée."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        dictionary["key1"] = 2
        self.assertEqual(2, dictionary["key1"])
    
    def test_indexer_set_new_key_adds_key_value(self):
        """L'ajout d'une nouvelle clé via l'indexeur doit ajouter la paire clé-valeur."""
        dictionary = HybridDictionary()
        dictionary["key1"] = 1
        self.assertEqual(1, dictionary["key1"])
        self.assertEqual(1, dictionary.count)
    
    def test_contains_key_existing_key_returns_true(self):
        """La vérification d'une clé existante doit retourner True."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        self.assertTrue(dictionary.contains_key("key1"))
    
    def test_contains_key_non_existing_key_returns_false(self):
        """La vérification d'une clé inexistante doit retourner False."""
        dictionary = HybridDictionary()
        self.assertFalse(dictionary.contains_key("key1"))
    
    def test_try_get_value_existing_key_returns_true_and_value(self):
        """La tentative de récupération d'une clé existante doit retourner True et la valeur."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        success, value = dictionary.try_get_value("key1")
        self.assertTrue(success)
        self.assertEqual(1, value)
    
    def test_try_get_value_non_existing_key_returns_false_and_default_value(self):
        """La tentative de récupération d'une clé inexistante doit retourner False et None."""
        dictionary = HybridDictionary()
        success, value = dictionary.try_get_value("key1")
        self.assertFalse(success)
        self.assertIsNone(value)
    
    def test_remove_existing_key_removes_key_and_returns_true(self):
        """La suppression d'une clé existante doit la supprimer et retourner True."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        result = dictionary.remove("key1")
        self.assertTrue(result)
        self.assertEqual(0, dictionary.count)
    
    def test_remove_non_existing_key_returns_false(self):
        """La suppression d'une clé inexistante doit retourner False."""
        dictionary = HybridDictionary()
        result = dictionary.remove("key1")
        self.assertFalse(result)
    
    def test_clear_non_empty_dictionary_removes_all_entries(self):
        """La suppression de toutes les entrées d'un dictionnaire non vide doit le vider."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        dictionary.add("key2", 2)
        dictionary.clear()
        self.assertEqual(0, dictionary.count)
    
    def test_keys_non_empty_dictionary_returns_all_keys(self):
        """La récupération des clés d'un dictionnaire non vide doit retourner toutes les clés."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        dictionary.add("key2", 2)
        keys = dictionary.keys
        self.assertEqual(2, len(keys))
        self.assertIn("key1", keys)
        self.assertIn("key2", keys)
    
    def test_values_non_empty_dictionary_returns_all_values(self):
        """La récupération des valeurs d'un dictionnaire non vide doit retourner toutes les valeurs."""
        dictionary = HybridDictionary()
        dictionary.add("key1", 1)
        dictionary.add("key2", 2)
        values = dictionary.values
        self.assertEqual(2, len(values))
        self.assertIn(1, values)
        self.assertIn(2, values)
    
    def test_conversion_to_dictionary_exceeding_threshold_works_correctly(self):
        """La conversion en dictionnaire lorsque le seuil est dépassé doit fonctionner correctement."""
        dictionary = HybridDictionary(threshold=3)
        dictionary.add("key1", 1)
        dictionary.add("key2", 2)
        dictionary.add("key3", 3)
        dictionary.add("key4", 4)  # Ceci devrait déclencher la conversion
        self.assertEqual(4, dictionary.count)
        self.assertEqual(4, dictionary["key4"])
        self.assertTrue(dictionary.contains_key("key1"))
        dictionary.remove("key1")
        self.assertEqual(3, dictionary.count)
        self.assertFalse(dictionary.contains_key("key1"))
    
    def test_custom_comparer_case_insensitive_works_correctly(self):
        """Un comparateur personnalisé insensible à la casse doit fonctionner correctement."""
        dictionary = HybridDictionary(key_comparer=lambda x, y: x.lower() == y.lower())
        dictionary.add("key", 1)
        self.assertTrue(dictionary.contains_key("KEY"))
        self.assertEqual(1, dictionary["Key"])
        dictionary["KEY"] = 2
        self.assertEqual(2, dictionary["key"])
        self.assertTrue(dictionary.remove("Key"))
        self.assertEqual(0, dictionary.count)

if __name__ == "__main__":
    unittest.main()