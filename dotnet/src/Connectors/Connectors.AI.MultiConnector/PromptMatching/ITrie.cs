// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Interface générique pour un arbre à préfixe (Trie)
    /// </summary>
    /// <typeparam name="K">Type de clé (typiquement string)</typeparam>
    /// <typeparam name="C">Type de caractère (typiquement char)</typeparam>
    /// <typeparam name="V">Type de valeur associée</typeparam>
    public interface ITrie<K, C, V> where K : IEnumerable<C>
    {
        /// <summary>
        /// Ajoute ou met à jour une valeur associée à une clé
        /// </summary>
        /// <param name="key">Clé à ajouter</param>
        /// <param name="value">Valeur à associer à la clé</param>
        void Add(K key, V value);

        /// <summary>
        /// Recherche une valeur associée à une clé exacte
        /// </summary>
        /// <param name="key">Clé à rechercher</param>
        /// <param name="value">Valeur associée à la clé si trouvée</param>
        /// <returns>True si la clé existe, false sinon</returns>
        bool TryGetValue(K key, out V value);

        /// <summary>
        /// Recherche une valeur associée à un préfixe
        /// </summary>
        /// <param name="prefix">Préfixe à rechercher</param>
        /// <param name="value">Valeur associée au préfixe le plus long correspondant</param>
        /// <returns>True si un préfixe correspondant existe, false sinon</returns>
        bool TryGetValueByPrefix(K prefix, out V value);

        /// <summary>
        /// Supprime une clé et sa valeur associée
        /// </summary>
        /// <param name="key">Clé à supprimer</param>
        /// <returns>True si la clé a été supprimée, false si elle n'existait pas</returns>
        bool Remove(K key);

        /// <summary>
        /// Nombre d'éléments dans l'arbre
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Efface toutes les entrées de l'arbre
        /// </summary>
        void Clear();
    }
}
