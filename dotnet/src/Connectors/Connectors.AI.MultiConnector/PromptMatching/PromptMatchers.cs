// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Implémentation séquentielle du matcher de prompts.
    /// Reproduit le comportement actuel avec une recherche linéaire.
    /// </summary>
    public class SequentialPromptMatcher : IPromptMatcher
    {
        private readonly List<(PromptSignature Signature, PromptMultiConnectorSettings Settings)> _prompts = new();
        private readonly ReaderWriterLockSlim _lock = new();

        /// <summary>
        /// Nombre de prompts stockés
        /// </summary>
        public int Count => _prompts.Count;

        /// <summary>
        /// Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        /// </summary>
        /// <param name="completionJob">Le job de complétion à matcher</param>
        /// <param name="promptSettings">La collection de paramètres de prompts disponibles</param>
        /// <returns>Les paramètres correspondants ou null si aucune correspondance n'est trouvée</returns>
        public PromptMultiConnectorSettings? MatchPromptSettings(CompletionJob completionJob, IEnumerable<PromptMultiConnectorSettings> promptSettings)
        {
            _lock.EnterReadLock();
            try
            {
                // Recherche séquentielle dans la liste des prompts
                foreach (var (signature, settings) in _prompts)
                {
                    if (signature.Matches(completionJob))
                    {
                        return settings;
                    }
                }

                // Si aucune correspondance n'est trouvée dans notre cache, recherche dans la collection fournie
                return promptSettings.FirstOrDefault(s => s.PromptType.Signature.Matches(completionJob));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Ajoute un nouveau prompt et ses paramètres associés à la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à ajouter</param>
        /// <param name="settings">Les paramètres associés au prompt</param>
        public void AddPrompt(PromptSignature promptSignature, PromptMultiConnectorSettings settings)
        {
            if (promptSignature == null)
            {
                throw new ArgumentNullException(nameof(promptSignature));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _lock.EnterWriteLock();
            try
            {
                // Vérifier si la signature existe déjà
                int existingIndex = _prompts.FindIndex(p => p.Signature.PromptStart == promptSignature.PromptStart);

                if (existingIndex >= 0)
                {
                    // Mettre à jour les paramètres existants
                    _prompts[existingIndex] = (promptSignature, settings);
                }
                else
                {
                    // Ajouter une nouvelle entrée
                    _prompts.Add((promptSignature, settings));
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Supprime un prompt et ses paramètres associés de la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à supprimer</param>
        /// <returns>True si le prompt a été supprimé, false sinon</returns>
        public bool RemovePrompt(PromptSignature promptSignature)
        {
            if (promptSignature == null)
            {
                throw new ArgumentNullException(nameof(promptSignature));
            }

            _lock.EnterWriteLock();
            try
            {
                int index = _prompts.FindIndex(p => p.Signature.PromptStart == promptSignature.PromptStart);

                if (index >= 0)
                {
                    _prompts.RemoveAt(index);
                    return true;
                }

                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Efface tous les prompts et paramètres associés de la structure interne.
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _prompts.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Implémentation du matcher de prompts utilisant un RadixTree pour une recherche efficace.
    /// </summary>
    public class RadixTreePromptMatcher : IPromptMatcher
    {
        private readonly RadixTree<string, char, PromptMultiConnectorSettings> _radixTree = new();
        private readonly ReaderWriterLockSlim _lock = new();

        /// <summary>
        /// Nombre de prompts stockés
        /// </summary>
        public int Count => _radixTree.Count;

        /// <summary>
        /// Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        /// </summary>
        /// <param name="completionJob">Le job de complétion à matcher</param>
        /// <param name="promptSettings">La collection de paramètres de prompts disponibles</param>
        /// <returns>Les paramètres correspondants ou null si aucune correspondance n'est trouvée</returns>
        public PromptMultiConnectorSettings? MatchPromptSettings(CompletionJob completionJob, IEnumerable<PromptMultiConnectorSettings> promptSettings)
        {
            if (completionJob == null)
            {
                throw new ArgumentNullException(nameof(completionJob));
            }

            _lock.EnterReadLock();
            try
            {
                // Recherche par préfixe dans le RadixTree
                if (_radixTree.TryGetValueByPrefix(completionJob.Prompt, out PromptMultiConnectorSettings settings))
                {
                    return settings;
                }

                // Si aucune correspondance n'est trouvée dans notre cache, recherche dans la collection fournie
                return promptSettings.FirstOrDefault(s => s.PromptType.Signature.Matches(completionJob));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Ajoute un nouveau prompt et ses paramètres associés à la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à ajouter</param>
        /// <param name="settings">Les paramètres associés au prompt</param>
        public void AddPrompt(PromptSignature promptSignature, PromptMultiConnectorSettings settings)
        {
            if (promptSignature == null)
            {
                throw new ArgumentNullException(nameof(promptSignature));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _lock.EnterWriteLock();
            try
            {
                _radixTree.Add(promptSignature.PromptStart, settings);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Supprime un prompt et ses paramètres associés de la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à supprimer</param>
        /// <returns>True si le prompt a été supprimé, false sinon</returns>
        public bool RemovePrompt(PromptSignature promptSignature)
        {
            if (promptSignature == null)
            {
                throw new ArgumentNullException(nameof(promptSignature));
            }

            _lock.EnterWriteLock();
            try
            {
                return _radixTree.Remove(promptSignature.PromptStart);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Efface tous les prompts et paramètres associés de la structure interne.
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _radixTree.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Implémentation hybride du matcher de prompts combinant RadixTree et optimisation des regex.
    /// </summary>
    public class HybridPromptMatcher : IPromptMatcher
    {
        private readonly RadixTree<string, char, PromptMultiConnectorSettings> _radixTree = new();
        private readonly Dictionary<string, Regex> _regexCache = new();
        private readonly List<(Regex Regex, PromptMultiConnectorSettings Settings)> _regexPrompts = new();
        private readonly ReaderWriterLockSlim _lock = new();

        /// <summary>
        /// Nombre de prompts stockés
        /// </summary>
        public int Count => _radixTree.Count + _regexPrompts.Count;

        /// <summary>
        /// Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        /// </summary>
        /// <param name="completionJob">Le job de complétion à matcher</param>
        /// <param name="promptSettings">La collection de paramètres de prompts disponibles</param>
        /// <returns>Les paramètres correspondants ou null si aucune correspondance n'est trouvée</returns>
        public PromptMultiConnectorSettings? MatchPromptSettings(CompletionJob completionJob, IEnumerable<PromptMultiConnectorSettings> promptSettings)
        {
            if (completionJob == null)
            {
                throw new ArgumentNullException(nameof(completionJob));
            }

            _lock.EnterReadLock();
            try
            {
                // 1. Recherche par préfixe dans le RadixTree (plus rapide)
                if (_radixTree.TryGetValueByPrefix(completionJob.Prompt, out PromptMultiConnectorSettings settings))
                {
                    return settings;
                }

                // 2. Recherche par regex (plus lent mais plus flexible)
                foreach (var (regex, regexSettings) in _regexPrompts)
                {
                    if (regex.IsMatch(completionJob.Prompt))
                    {
                        return regexSettings;
                    }
                }

                // 3. Si aucune correspondance n'est trouvée dans notre cache, recherche dans la collection fournie
                return promptSettings.FirstOrDefault(s => s.PromptType.Signature.Matches(completionJob));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Ajoute un nouveau prompt et ses paramètres associés à la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à ajouter</param>
        /// <param name="settings">Les paramètres associés au prompt</param>
        public void AddPrompt(PromptSignature promptSignature, PromptMultiConnectorSettings settings)
        {
            if (promptSignature == null)
            {
                throw new ArgumentNullException(nameof(promptSignature));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _lock.EnterWriteLock();
            try
            {
                // Si la signature contient des caractères spéciaux de regex, on l'ajoute comme regex
                if (ContainsRegexSpecialChars(promptSignature.PromptStart))
                {
                    AddRegexPrompt(promptSignature.PromptStart, settings);
                }
                else
                {
                    // Sinon, on l'ajoute au RadixTree pour une recherche plus efficace
                    _radixTree.Add(promptSignature.PromptStart, settings);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Supprime un prompt et ses paramètres associés de la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à supprimer</param>
        /// <returns>True si le prompt a été supprimé, false sinon</returns>
        public bool RemovePrompt(PromptSignature promptSignature)
        {
            if (promptSignature == null)
            {
                throw new ArgumentNullException(nameof(promptSignature));
            }

            _lock.EnterWriteLock();
            try
            {
                string pattern = promptSignature.PromptStart;

                // Si c'est une regex, on la supprime de la liste des regex
                if (ContainsRegexSpecialChars(pattern))
                {
                    if (_regexCache.TryGetValue(pattern, out Regex? regex))
                    {
                        int index = _regexPrompts.FindIndex(p => p.Regex == regex);

                        if (index >= 0)
                        {
                            _regexPrompts.RemoveAt(index);
                            _regexCache.Remove(pattern);
                            return true;
                        }
                    }

                    return false;
                }
                else
                {
                    // Sinon, on la supprime du RadixTree
                    return _radixTree.Remove(pattern);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Efface tous les prompts et paramètres associés de la structure interne.
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _radixTree.Clear();
                _regexPrompts.Clear();
                _regexCache.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Ajoute un prompt sous forme de regex
        /// </summary>
        /// <param name="pattern">Motif regex</param>
        /// <param name="settings">Paramètres associés</param>
        private void AddRegexPrompt(string pattern, PromptMultiConnectorSettings settings)
        {
            if (!_regexCache.TryGetValue(pattern, out Regex? regex))
            {
                regex = new Regex(pattern, RegexOptions.Compiled);
                _regexCache.Add(pattern, regex);
            }

            // Vérifier si la regex existe déjà
            int existingIndex = _regexPrompts.FindIndex(p => p.Regex == regex);

            if (existingIndex >= 0)
            {
                // Mettre à jour les paramètres existants
                _regexPrompts[existingIndex] = (regex, settings);
            }
            else
            {
                // Ajouter une nouvelle entrée
                _regexPrompts.Add((regex, settings));
            }
        }

        /// <summary>
        /// Vérifie si une chaîne contient des caractères spéciaux de regex
        /// </summary>
        /// <param name="input">Chaîne à vérifier</param>
        /// <returns>True si la chaîne contient des caractères spéciaux de regex</returns>
        private static bool ContainsRegexSpecialChars(string input)
        {
            return input.IndexOfAny(new[] { '*', '+', '?', '|', '{', '}', '[', ']', '(', ')', '^', '$', '\\', '.' }) >= 0;
        }
    }
}
