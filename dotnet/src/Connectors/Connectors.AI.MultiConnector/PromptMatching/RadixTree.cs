// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Implémentation d'un arbre à préfixe compressé (RadixTree)
    /// </summary>
    /// <typeparam name="K">Type de clé (typiquement string)</typeparam>
    /// <typeparam name="C">Type de caractère (typiquement char)</typeparam>
    /// <typeparam name="V">Type de valeur associée</typeparam>
    public class RadixTree<K, C, V> : ITrie<K, C, V> where K : IEnumerable<C>
    {
        /// <summary>
        /// Nœud interne de l'arbre à préfixe compressé
        /// </summary>
        protected class RadixNode
        {
            /// <summary>
            /// Préfixe stocké dans ce nœud
            /// </summary>
            public List<C> Prefix { get; set; }

            /// <summary>
            /// Enfants du nœud, indexés par le premier caractère de leur préfixe
            /// </summary>
            public HybridDictionary<C, RadixNode> Children { get; }

            /// <summary>
            /// Indique si ce nœud est la fin d'une clé
            /// </summary>
            public bool IsEndOfKey { get; set; }

            /// <summary>
            /// Valeur associée à la clé se terminant à ce nœud
            /// </summary>
            public V? Value { get; set; }

            /// <summary>
            /// Constructeur
            /// </summary>
            /// <param name="prefix">Préfixe du nœud</param>
            public RadixNode(IEnumerable<C> prefix)
            {
                Prefix = new List<C>(prefix);
                Children = new HybridDictionary<C, RadixNode>();
            }
        }

        /// <summary>
        /// Racine de l'arbre
        /// </summary>
        protected readonly RadixNode Root = new RadixNode(Array.Empty<C>());

        /// <summary>
        /// Nombre d'éléments dans l'arbre
        /// </summary>
        public int Count { get; protected set; }

        /// <summary>
        /// Fonction pour convertir une clé en liste de caractères
        /// </summary>
        protected readonly Func<K, List<C>> _keyToList;

        /// <summary>
        /// Fonction pour comparer deux caractères
        /// </summary>
        protected readonly IEqualityComparer<C> _charComparer;

        /// <summary>
        /// Constructeur par défaut pour les chaînes de caractères
        /// </summary>
        public RadixTree() : this(
            key => new List<C>(key),
            EqualityComparer<C>.Default)
        {
        }

        /// <summary>
        /// Constructeur avec fonctions personnalisées
        /// </summary>
        /// <param name="keyToList">Fonction pour convertir une clé en liste de caractères</param>
        /// <param name="charComparer">Comparateur pour les caractères</param>
        public RadixTree(Func<K, List<C>> keyToList, IEqualityComparer<C> charComparer)
        {
            _keyToList = keyToList ?? throw new ArgumentNullException(nameof(keyToList));
            _charComparer = charComparer ?? throw new ArgumentNullException(nameof(charComparer));
        }

        /// <summary>
        /// Ajoute ou met à jour une valeur associée à une clé
        /// </summary>
        /// <param name="key">Clé à ajouter</param>
        /// <param name="value">Valeur à associer à la clé</param>
        public void Add(K key, V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            List<C> remainingChars = _keyToList(key);
            bool isNewKey = Insert(Root, remainingChars, value);

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
        public bool TryGetValue(K key, out V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            List<C> keyChars = _keyToList(key);
            RadixNode? node = FindNode(Root, keyChars, 0);

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
        public bool TryGetValueByPrefix(K prefix, out V value)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            List<C> prefixChars = _keyToList(prefix);
            RadixNode? lastMatchingNode = null;
            int currentIndex = 0;

            RadixNode current = Root;
            while (currentIndex < prefixChars.Count)
            {
                C firstChar = prefixChars[currentIndex];
                if (!current.Children.TryGetValue(firstChar, out RadixNode? child))
                {
                    break;
                }

                int matchLength = GetMatchingPrefixLength(child.Prefix, prefixChars, currentIndex);

                if (matchLength < child.Prefix.Count)
                {
                    // Préfixe partiel, pas de correspondance complète
                    break;
                }

                currentIndex += matchLength;
                current = child;

                if (current.IsEndOfKey)
                {
                    lastMatchingNode = current;
                }

                if (currentIndex == prefixChars.Count)
                {
                    break;
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
        public bool Remove(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            List<C> keyChars = _keyToList(key);
            bool removed = RemoveRecursive(Root, keyChars, 0);

            if (removed)
            {
                Count--;
            }

            return removed;
        }

        /// <summary>
        /// Efface toutes les entrées de l'arbre
        /// </summary>
        public void Clear()
        {
            Root.Children.Clear();
            Count = 0;
        }

        /// <summary>
        /// Insère une clé dans l'arbre
        /// </summary>
        /// <param name="node">Nœud courant</param>
        /// <param name="remainingChars">Caractères restants à insérer</param>
        /// <param name="value">Valeur à associer à la clé</param>
        /// <returns>True si une nouvelle clé a été insérée, false si une clé existante a été mise à jour</returns>
        protected bool Insert(RadixNode node, List<C> remainingChars, V value)
        {
            if (remainingChars.Count == 0)
            {
                bool isNewKey = !node.IsEndOfKey;
                node.IsEndOfKey = true;
                node.Value = value;
                return isNewKey;
            }

            C firstChar = remainingChars[0];

            if (!node.Children.TryGetValue(firstChar, out RadixNode? child))
            {
                // Aucun enfant ne commence par ce caractère, créer un nouveau nœud
                RadixNode newNode = new RadixNode(remainingChars)
                {
                    IsEndOfKey = true,
                    Value = value
                };

                node.Children.Add(firstChar, newNode);
                return true;
            }

            // Trouver la longueur du préfixe commun
            int matchLength = GetMatchingPrefixLength(child.Prefix, remainingChars, 0);

            if (matchLength == child.Prefix.Count)
            {
                // Le préfixe de l'enfant est entièrement inclus dans la clé
                List<C> newRemainingChars = remainingChars.GetRange(matchLength, remainingChars.Count - matchLength);
                return Insert(child, newRemainingChars, value);
            }
            else if (matchLength > 0)
            {
                // Préfixe commun partiel, diviser le nœud
                List<C> commonPrefix = child.Prefix.GetRange(0, matchLength);
                List<C> childSuffix = child.Prefix.GetRange(matchLength, child.Prefix.Count - matchLength);
                List<C> keySuffix = remainingChars.GetRange(matchLength, remainingChars.Count - matchLength);

                // Créer un nouveau nœud intermédiaire avec le préfixe commun
                RadixNode newNode = new RadixNode(commonPrefix);

                // Ajuster le préfixe de l'enfant existant
                child.Prefix = childSuffix;

                // Ajouter l'enfant existant au nouveau nœud
                newNode.Children.Add(childSuffix[0], child);

                // Remplacer l'enfant dans le nœud parent par le nouveau nœud
                node.Children.Remove(firstChar);
                node.Children.Add(firstChar, newNode);

                if (keySuffix.Count == 0)
                {
                    // La clé se termine exactement au nouveau nœud intermédiaire
                    newNode.IsEndOfKey = true;
                    newNode.Value = value;
                    return true;
                }
                else
                {
                    // Créer un nouveau nœud pour le suffixe de la clé
                    RadixNode keySuffixNode = new RadixNode(keySuffix)
                    {
                        IsEndOfKey = true,
                        Value = value
                    };

                    newNode.Children.Add(keySuffix[0], keySuffixNode);
                    return true;
                }
            }
            else
            {
                // Aucun préfixe commun, ce qui ne devrait pas arriver car on a déjà vérifié le premier caractère
                throw new InvalidOperationException("Erreur interne: aucun préfixe commun trouvé alors que le premier caractère correspond.");
            }
        }

        /// <summary>
        /// Recherche un nœud correspondant à une clé
        /// </summary>
        /// <param name="node">Nœud courant</param>
        /// <param name="keyChars">Caractères de la clé</param>
        /// <param name="startIndex">Index de départ dans la clé</param>
        /// <returns>Nœud correspondant à la clé, ou null si non trouvé</returns>
        protected RadixNode? FindNode(RadixNode node, List<C> keyChars, int startIndex)
        {
            if (startIndex == keyChars.Count)
            {
                return node;
            }

            C firstChar = keyChars[startIndex];
            if (!node.Children.TryGetValue(firstChar, out RadixNode? child))
            {
                return null;
            }

            int matchLength = GetMatchingPrefixLength(child.Prefix, keyChars, startIndex);

            if (matchLength < child.Prefix.Count)
            {
                // Préfixe partiel, pas de correspondance complète
                return null;
            }

            int newStartIndex = startIndex + matchLength;

            if (newStartIndex == keyChars.Count)
            {
                return child;
            }

            return FindNode(child, keyChars, newStartIndex);
        }

        /// <summary>
        /// Supprime récursivement une clé de l'arbre
        /// </summary>
        /// <param name="node">Nœud courant</param>
        /// <param name="keyChars">Caractères de la clé</param>
        /// <param name="startIndex">Index de départ dans la clé</param>
        /// <returns>True si le nœud peut être supprimé, false sinon</returns>
        protected bool RemoveRecursive(RadixNode node, List<C> keyChars, int startIndex)
        {
            if (startIndex == keyChars.Count)
            {
                if (!node.IsEndOfKey)
                {
                    return false;
                }

                node.IsEndOfKey = false;
                node.Value = default;

                return node.Children.Count == 0;
            }

            C firstChar = keyChars[startIndex];
            if (!node.Children.TryGetValue(firstChar, out RadixNode? child))
            {
                return false;
            }

            int matchLength = GetMatchingPrefixLength(child.Prefix, keyChars, startIndex);

            if (matchLength < child.Prefix.Count)
            {
                // Préfixe partiel, pas de correspondance complète
                return false;
            }

            int newStartIndex = startIndex + matchLength;

            bool shouldRemoveChild = RemoveRecursive(child, keyChars, newStartIndex);

            if (shouldRemoveChild)
            {
                node.Children.Remove(firstChar);

                // Si ce nœud n'est pas la fin d'une clé et n'a pas d'autres enfants, il peut être supprimé
                return !node.IsEndOfKey && node.Children.Count == 0;
            }

            return false;
        }

        /// <summary>
        /// Calcule la longueur du préfixe commun entre deux séquences de caractères
        /// </summary>
        /// <param name="prefix">Premier préfixe</param>
        /// <param name="chars">Deuxième séquence de caractères</param>
        /// <param name="startIndex">Index de départ dans la deuxième séquence</param>
        /// <returns>Longueur du préfixe commun</returns>
        protected int GetMatchingPrefixLength(List<C> prefix, List<C> chars, int startIndex)
        {
            int i = 0;
            int maxLength = Math.Min(prefix.Count, chars.Count - startIndex);

            while (i < maxLength && _charComparer.Equals(prefix[i], chars[startIndex + i]))
            {
                i++;
            }

            return i;
        }
    }
}
