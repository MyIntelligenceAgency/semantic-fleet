// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Implémentation générique d'un arbre à préfixe (Trie)
    /// </summary>
    /// <typeparam name="K">Type de clé (typiquement string)</typeparam>
    /// <typeparam name="C">Type de caractère (typiquement char)</typeparam>
    /// <typeparam name="V">Type de valeur associée</typeparam>
    public class Trie<K, C, V> : ITrie<K, C, V> where K : IEnumerable<C>
    {
        /// <summary>
        /// Nœud interne de l'arbre à préfixe
        /// </summary>
        protected class TrieNode
        {
            /// <summary>
            /// Enfants du nœud, indexés par caractère
            /// </summary>
            public HybridDictionary<C, TrieNode> Children { get; } = new HybridDictionary<C, TrieNode>();

            /// <summary>
            /// Indique si ce nœud est la fin d'une clé
            /// </summary>
            public bool IsEndOfKey { get; set; }

            /// <summary>
            /// Valeur associée à la clé se terminant à ce nœud
            /// </summary>
            public V? Value { get; set; }
        }

        /// <summary>
        /// Racine de l'arbre
        /// </summary>
        protected readonly TrieNode Root = new TrieNode();

        /// <summary>
        /// Nombre d'éléments dans l'arbre
        /// </summary>
        public int Count { get; protected set; }

        /// <summary>
        /// Ajoute ou met à jour une valeur associée à une clé
        /// </summary>
        /// <param name="key">Clé à ajouter</param>
        /// <param name="value">Valeur à associer à la clé</param>
        public virtual void Add(K key, V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            TrieNode current = Root;
            bool isNewKey = false;

            foreach (C c in key)
            {
                if (!current.Children.TryGetValue(c, out TrieNode? child))
                {
                    child = new TrieNode();
                    current.Children.Add(c, child);
                }

                current = child;
            }

            isNewKey = !current.IsEndOfKey;
            current.IsEndOfKey = true;
            current.Value = value;

            if (isNewKey)
            {
                Count++;
            }
        }

        /// <summary>
        /// Recherche une valeur associée à une clé exacte
        /// </summary>
        /// <param name="key">Clé à rechercher</param>
        /// <param name="value">Valeur associée à la clé si trouvée</param>
        /// <returns>True si la clé existe, false sinon</returns>
        public virtual bool TryGetValue(K key, out V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            TrieNode? node = FindNode(key);

            if (node != null && node.IsEndOfKey)
            {
                value = node.Value!;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Recherche une valeur associée à un préfixe
        /// </summary>
        /// <param name="prefix">Préfixe à rechercher</param>
        /// <param name="value">Valeur associée au préfixe le plus long correspondant</param>
        /// <returns>True si un préfixe correspondant existe, false sinon</returns>
        public virtual bool TryGetValueByPrefix(K prefix, out V value)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            TrieNode current = Root;
            TrieNode? lastMatchingNode = null;

            foreach (C c in prefix)
            {
                if (!current.Children.TryGetValue(c, out TrieNode? child))
                {
                    break;
                }

                current = child;

                if (current.IsEndOfKey)
                {
                    lastMatchingNode = current;
                }
            }

            if (lastMatchingNode != null)
            {
                value = lastMatchingNode.Value!;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Supprime une clé et sa valeur associée
        /// </summary>
        /// <param name="key">Clé à supprimer</param>
        /// <returns>True si la clé a été supprimée, false si elle n'existait pas</returns>
        public virtual bool Remove(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            bool removed = RemoveRecursive(Root, key.GetEnumerator(), 0);

            if (removed)
            {
                Count--;
            }

            return removed;
        }

        /// <summary>
        /// Efface toutes les entrées de l'arbre
        /// </summary>
        public virtual void Clear()
        {
            Root.Children.Clear();
            Count = 0;
        }

        /// <summary>
        /// Recherche un nœud correspondant à une clé
        /// </summary>
        /// <param name="key">Clé à rechercher</param>
        /// <returns>Nœud correspondant à la clé, ou null si non trouvé</returns>
        protected TrieNode? FindNode(K key)
        {
            TrieNode current = Root;

            foreach (C c in key)
            {
                if (!current.Children.TryGetValue(c, out TrieNode? child))
                {
                    return null;
                }

                current = child;
            }

            return current;
        }

        /// <summary>
        /// Supprime récursivement une clé de l'arbre
        /// </summary>
        /// <param name="node">Nœud courant</param>
        /// <param name="keyEnumerator">Énumérateur de la clé</param>
        /// <param name="depth">Profondeur actuelle dans l'arbre</param>
        /// <returns>True si la clé a été supprimée, false sinon</returns>
        protected bool RemoveRecursive(TrieNode node, IEnumerator<C> keyEnumerator, int depth)
        {
            if (!keyEnumerator.MoveNext())
            {
                // Fin de la clé, vérifier si c'est une clé valide
                if (!node.IsEndOfKey)
                {
                    return false;
                }

                node.IsEndOfKey = false;
                node.Value = default;

                // Si le nœud n'a pas d'enfants, il peut être supprimé
                return node.Children.Count == 0;
            }

            C currentChar = keyEnumerator.Current;

            if (!node.Children.TryGetValue(currentChar, out TrieNode? child))
            {
                return false;
            }

            bool shouldRemoveChild = RemoveRecursive(child, keyEnumerator, depth + 1);

            if (shouldRemoveChild)
            {
                node.Children.Remove(currentChar);

                // Si ce nœud n'est pas la fin d'une clé et n'a pas d'autres enfants, il peut être supprimé
                return !node.IsEndOfKey && node.Children.Count == 0;
            }

            return false;
        }
    }
}
