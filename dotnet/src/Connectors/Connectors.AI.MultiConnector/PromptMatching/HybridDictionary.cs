// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Dictionnaire hybride qui bascule entre un tableau et un dictionnaire en fonction du nombre d'éléments.
    /// Optimisé pour les petites collections avec un accès rapide.
    /// </summary>
    /// <typeparam name="K">Type de clé</typeparam>
    /// <typeparam name="V">Type de valeur</typeparam>
    public class HybridDictionary<K, V> where K : notnull
    {
        // Seuil à partir duquel on bascule d'un tableau à un dictionnaire
        private const int DefaultThreshold = 7;

        private readonly int _threshold;
        private readonly IEqualityComparer<K> _comparer;

        // Stockage sous forme de liste de paires clé-valeur pour les petites collections
        private List<KeyValuePair<K, V>>? _list;

        // Stockage sous forme de dictionnaire pour les grandes collections
        private Dictionary<K, V>? _dictionary;

        /// <summary>
        /// Nombre d'éléments dans la collection
        /// </summary>
        public int Count => _list != null ? _list.Count : _dictionary?.Count ?? 0;

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public HybridDictionary() : this(DefaultThreshold, EqualityComparer<K>.Default)
        {
        }

        /// <summary>
        /// Constructeur avec seuil personnalisé
        /// </summary>
        /// <param name="threshold">Seuil à partir duquel on bascule d'un tableau à un dictionnaire</param>
        public HybridDictionary(int threshold) : this(threshold, EqualityComparer<K>.Default)
        {
        }

        /// <summary>
        /// Constructeur avec comparateur personnalisé
        /// </summary>
        /// <param name="comparer">Comparateur d'égalité pour les clés</param>
        public HybridDictionary(IEqualityComparer<K> comparer) : this(DefaultThreshold, comparer)
        {
        }

        /// <summary>
        /// Constructeur avec seuil et comparateur personnalisés
        /// </summary>
        /// <param name="threshold">Seuil à partir duquel on bascule d'un tableau à un dictionnaire</param>
        /// <param name="comparer">Comparateur d'égalité pour les clés</param>
        public HybridDictionary(int threshold, IEqualityComparer<K> comparer)
        {
            _threshold = threshold > 0 ? threshold : DefaultThreshold;
            _comparer = comparer ?? EqualityComparer<K>.Default;
            _list = new List<KeyValuePair<K, V>>();
        }

        /// <summary>
        /// Indexeur pour accéder aux valeurs par clé
        /// </summary>
        /// <param name="key">Clé à rechercher</param>
        /// <returns>Valeur associée à la clé</returns>
        public V this[K key]
        {
            get
            {
                if (_dictionary != null)
                {
                    return _dictionary[key];
                }

                if (_list != null)
                {
                    foreach (var pair in _list)
                    {
                        if (_comparer.Equals(pair.Key, key))
                        {
                            return pair.Value;
                        }
                    }
                }

                throw new KeyNotFoundException($"La clé '{key}' n'a pas été trouvée dans le dictionnaire.");
            }
            set
            {
                if (_dictionary != null)
                {
                    _dictionary[key] = value;
                    return;
                }

                if (_list != null)
                {
                    for (int i = 0; i < _list.Count; i++)
                    {
                        if (_comparer.Equals(_list[i].Key, key))
                        {
                            _list[i] = new KeyValuePair<K, V>(key, value);
                            return;
                        }
                    }

                    _list.Add(new KeyValuePair<K, V>(key, value));

                    // Si le nombre d'éléments dépasse le seuil, on bascule vers un dictionnaire
                    if (_list.Count > _threshold)
                    {
                        ConvertToDict();
                    }
                }
            }
        }

        /// <summary>
        /// Vérifie si la clé existe dans le dictionnaire
        /// </summary>
        /// <param name="key">Clé à rechercher</param>
        /// <returns>True si la clé existe, false sinon</returns>
        public bool ContainsKey(K key)
        {
            if (_dictionary != null)
            {
                return _dictionary.ContainsKey(key);
            }

            if (_list != null)
            {
                foreach (var pair in _list)
                {
                    if (_comparer.Equals(pair.Key, key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tente de récupérer la valeur associée à une clé
        /// </summary>
        /// <param name="key">Clé à rechercher</param>
        /// <param name="value">Valeur associée à la clé si trouvée</param>
        /// <returns>True si la clé existe, false sinon</returns>
        public bool TryGetValue(K key, out V value)
        {
            if (_dictionary != null)
            {
                return _dictionary.TryGetValue(key, out value!);
            }

            if (_list != null)
            {
                foreach (var pair in _list)
                {
                    if (_comparer.Equals(pair.Key, key))
                    {
                        value = pair.Value;
                        return true;
                    }
                }
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Ajoute une paire clé-valeur au dictionnaire
        /// </summary>
        /// <param name="key">Clé à ajouter</param>
        /// <param name="value">Valeur à associer à la clé</param>
        public void Add(K key, V value)
        {
            if (_dictionary != null)
            {
                _dictionary.Add(key, value);
                return;
            }

            if (_list != null)
            {
                foreach (var pair in _list)
                {
                    if (_comparer.Equals(pair.Key, key))
                    {
                        throw new ArgumentException($"Une entrée avec la même clé existe déjà: '{key}'");
                    }
                }

                _list.Add(new KeyValuePair<K, V>(key, value));

                // Si le nombre d'éléments dépasse le seuil, on bascule vers un dictionnaire
                if (_list.Count > _threshold)
                {
                    ConvertToDict();
                }
            }
        }

        /// <summary>
        /// Supprime une entrée du dictionnaire
        /// </summary>
        /// <param name="key">Clé à supprimer</param>
        /// <returns>True si la clé a été supprimée, false si elle n'existait pas</returns>
        public bool Remove(K key)
        {
            if (_dictionary != null)
            {
                return _dictionary.Remove(key);
            }

            if (_list != null)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    if (_comparer.Equals(_list[i].Key, key))
                    {
                        _list.RemoveAt(i);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Efface toutes les entrées du dictionnaire
        /// </summary>
        public void Clear()
        {
            if (_dictionary != null)
            {
                _dictionary.Clear();
                _dictionary = null;
                _list = new List<KeyValuePair<K, V>>();
            }
            else if (_list != null)
            {
                _list.Clear();
            }
        }

        /// <summary>
        /// Retourne toutes les clés du dictionnaire
        /// </summary>
        public IEnumerable<K> Keys
        {
            get
            {
                if (_dictionary != null)
                {
                    return _dictionary.Keys;
                }

                if (_list != null)
                {
                    return _list.Select(pair => pair.Key);
                }

                return Enumerable.Empty<K>();
            }
        }

        /// <summary>
        /// Retourne toutes les valeurs du dictionnaire
        /// </summary>
        public IEnumerable<V> Values
        {
            get
            {
                if (_dictionary != null)
                {
                    return _dictionary.Values;
                }

                if (_list != null)
                {
                    return _list.Select(pair => pair.Value);
                }

                return Enumerable.Empty<V>();
            }
        }

        /// <summary>
        /// Convertit la liste en dictionnaire lorsque le seuil est dépassé
        /// </summary>
        private void ConvertToDict()
        {
            if (_list == null || _dictionary != null)
            {
                return;
            }

            _dictionary = new Dictionary<K, V>(_list.Count, _comparer);
            foreach (var pair in _list)
            {
                _dictionary.Add(pair.Key, pair.Value);
            }

            _list = null;
        }
    }
}
