// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Implémentation adaptative du matcher de prompts qui étend le système existant pour mieux gérer
    /// les prompts qui ne correspondent pas à des patterns connus.
    ///
    /// Cette classe permet de laisser passer les prompts non reconnus jusqu'à ce qu'on en détecte
    /// plusieurs du même type, auquel cas on identifie potentiellement un nouveau pattern à analyser.
    /// </summary>
    public class AdaptivePromptDetector : IPromptMatcher
    {
        private readonly IPromptMatcher _basePromptMatcher;
        private readonly ConcurrentDictionary<string, UnrecognizedPromptInfo> _unrecognizedPromptsCache;
        private readonly ReaderWriterLockSlim _cacheLock = new();
        private readonly Timer _cacheCleanupTimer;

        // Configuration
        private readonly int _similarityThreshold;
        private readonly int _minSimilarPromptsToCreatePattern;
        private readonly TimeSpan _cacheEntryExpiration;
        private readonly int _maxCacheSize;
        private readonly bool _enabled;

        /// <summary>
        /// Nombre de prompts stockés dans le matcher de base
        /// </summary>
        public int Count => _basePromptMatcher.Count;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="AdaptivePromptDetector"/>.
        /// </summary>
        /// <param name="basePromptMatcher">Le matcher de prompts de base à étendre</param>
        /// <param name="similarityThreshold">Seuil de similarité pour considérer deux prompts comme similaires (0-100)</param>
        /// <param name="minSimilarPromptsToCreatePattern">Nombre minimum de prompts similaires pour créer un nouveau pattern</param>
        /// <param name="cacheEntryExpiration">Durée d'expiration des entrées du cache</param>
        /// <param name="maxCacheSize">Taille maximale du cache</param>
        /// <param name="enabled">Indique si le détecteur adaptatif est activé</param>
        public AdaptivePromptDetector(
            IPromptMatcher basePromptMatcher,
            int similarityThreshold = 70,
            int minSimilarPromptsToCreatePattern = 3,
            TimeSpan? cacheEntryExpiration = null,
            int maxCacheSize = 1000,
            bool enabled = true)
        {
            _basePromptMatcher = basePromptMatcher ?? throw new ArgumentNullException(nameof(basePromptMatcher));
            _similarityThreshold = similarityThreshold;
            _minSimilarPromptsToCreatePattern = minSimilarPromptsToCreatePattern;
            _cacheEntryExpiration = cacheEntryExpiration ?? TimeSpan.FromHours(24);
            _maxCacheSize = maxCacheSize;
            _enabled = enabled;

            _unrecognizedPromptsCache = new ConcurrentDictionary<string, UnrecognizedPromptInfo>();

            // Démarrer le timer de nettoyage du cache
            _cacheCleanupTimer = new Timer(CleanupCache, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        /// Si aucune correspondance n'est trouvée et que le détecteur adaptatif est activé,
        /// le prompt est stocké dans le cache pour analyse ultérieure.
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

            // Essayer d'abord avec le matcher de base
            var matchedSettings = _basePromptMatcher.MatchPromptSettings(completionJob, promptSettings);

            // Si une correspondance est trouvée ou si le détecteur adaptatif est désactivé, retourner le résultat
            if (matchedSettings != null || !_enabled)
            {
                return matchedSettings;
            }

            // Aucune correspondance trouvée, stocker le prompt dans le cache pour analyse ultérieure
            StoreUnrecognizedPrompt(completionJob);

            // Vérifier si nous avons suffisamment de prompts similaires pour créer un nouveau pattern
            var potentialPattern = IdentifyPotentialPattern(completionJob.Prompt);
            if (potentialPattern != null)
            {
                // Analyser de manière asynchrone ce nouveau pattern potentiel
                ThreadPool.QueueUserWorkItem(AnalyzeNewPattern, potentialPattern);
            }

            // Retourner null car aucune correspondance n'a été trouvée
            return null;
        }

        /// <summary>
        /// Ajoute un nouveau prompt et ses paramètres associés à la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à ajouter</param>
        /// <param name="settings">Les paramètres associés au prompt</param>
        public void AddPrompt(PromptSignature promptSignature, PromptMultiConnectorSettings settings)
        {
            _basePromptMatcher.AddPrompt(promptSignature, settings);
        }

        /// <summary>
        /// Supprime un prompt et ses paramètres associés de la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à supprimer</param>
        /// <returns>True si le prompt a été supprimé, false sinon</returns>
        public bool RemovePrompt(PromptSignature promptSignature)
        {
            return _basePromptMatcher.RemovePrompt(promptSignature);
        }

        /// <summary>
        /// Efface tous les prompts et paramètres associés de la structure interne.
        /// </summary>
        public void Clear()
        {
            _basePromptMatcher.Clear();

            _cacheLock.EnterWriteLock();
            try
            {
                _unrecognizedPromptsCache.Clear();
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Stocke un prompt non reconnu dans le cache pour analyse ultérieure.
        /// </summary>
        /// <param name="completionJob">Le job de complétion non reconnu</param>
        private void StoreUnrecognizedPrompt(CompletionJob completionJob)
        {
            // Limiter la taille du cache si nécessaire
            if (_unrecognizedPromptsCache.Count >= _maxCacheSize)
            {
                // Supprimer les entrées les plus anciennes
                CleanupCache(null);
            }

            // Extraire une signature du prompt (par exemple, les 50 premiers caractères)
            string promptSignatureKey = ExtractSignatureKey(completionJob.Prompt);

            // Mettre à jour ou ajouter l'entrée dans le cache
            _unrecognizedPromptsCache.AddOrUpdate(
                promptSignatureKey,
                // Ajouter une nouvelle entrée
                _ => new UnrecognizedPromptInfo
                {
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    Count = 1,
                    Prompts = new List<string> { completionJob.Prompt },
                    RequestSettings = completionJob.RequestSettings
                },
                // Mettre à jour une entrée existante
                (_, info) =>
                {
                    info.LastSeen = DateTime.UtcNow;
                    info.Count++;

                    // Limiter le nombre de prompts stockés par entrée
                    if (info.Prompts.Count < 10)
                    {
                        info.Prompts.Add(completionJob.Prompt);
                    }

                    return info;
                });
        }

        /// <summary>
        /// Extrait une clé de signature à partir d'un prompt.
        /// </summary>
        /// <param name="prompt">Le prompt à analyser</param>
        /// <returns>Une clé de signature pour le prompt</returns>
        private string ExtractSignatureKey(string prompt)
        {
            // Utiliser les 50 premiers caractères comme clé de signature
            int length = Math.Min(50, prompt.Length);
            return prompt.Substring(0, length);
        }

        /// <summary>
        /// Identifie un pattern potentiel à partir d'un prompt non reconnu.
        /// </summary>
        /// <param name="prompt">Le prompt à analyser</param>
        /// <returns>Un pattern potentiel ou null si aucun pattern n'est identifié</returns>
        private PotentialPattern? IdentifyPotentialPattern(string prompt)
        {
            string promptSignatureKey = ExtractSignatureKey(prompt);

            _cacheLock.EnterReadLock();
            try
            {
                // Vérifier si nous avons suffisamment d'occurrences de ce prompt
                if (_unrecognizedPromptsCache.TryGetValue(promptSignatureKey, out var info) &&
                    info.Count >= _minSimilarPromptsToCreatePattern)
                {
                    // Trouver les prompts similaires dans le cache
                    var similarPrompts = FindSimilarPrompts(prompt);

                    // Si nous avons suffisamment de prompts similaires, créer un pattern potentiel
                    if (similarPrompts.Count >= _minSimilarPromptsToCreatePattern)
                    {
                        return new PotentialPattern
                        {
                            SimilarPrompts = similarPrompts,
                            RequestSettings = info.RequestSettings
                        };
                    }
                }

                return null;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Trouve les prompts similaires à un prompt donné dans le cache.
        /// </summary>
        /// <param name="prompt">Le prompt à comparer</param>
        /// <returns>Une liste de prompts similaires</returns>
        private List<string> FindSimilarPrompts(string prompt)
        {
            var similarPrompts = new List<string>();

            foreach (var entry in _unrecognizedPromptsCache)
            {
                foreach (var cachedPrompt in entry.Value.Prompts)
                {
                    if (CalculateSimilarity(prompt, cachedPrompt) >= _similarityThreshold)
                    {
                        similarPrompts.Add(cachedPrompt);
                    }
                }
            }

            return similarPrompts;
        }

        /// <summary>
        /// Calcule la similarité entre deux chaînes (0-100).
        /// </summary>
        /// <param name="str1">Première chaîne</param>
        /// <param name="str2">Deuxième chaîne</param>
        /// <returns>Score de similarité entre 0 et 100</returns>
        private int CalculateSimilarity(string str1, string str2)
        {
            // Utiliser la distance de Levenshtein pour calculer la similarité
            int levenshteinDistance = ComputeLevenshteinDistance(str1, str2);
            int maxLength = Math.Max(str1.Length, str2.Length);

            // Convertir la distance en score de similarité (0-100)
            return (int)((1.0 - (double)levenshteinDistance / maxLength) * 100);
        }

        /// <summary>
        /// Calcule la distance de Levenshtein entre deux chaînes.
        /// </summary>
        /// <param name="s">Première chaîne</param>
        /// <param name="t">Deuxième chaîne</param>
        /// <returns>Distance de Levenshtein</returns>
        private int ComputeLevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;
            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Analyse un nouveau pattern potentiel de manière asynchrone.
        /// </summary>
        /// <param name="state">Le pattern potentiel à analyser</param>
        private void AnalyzeNewPattern(object? state)
        {
            if (state is not PotentialPattern potentialPattern)
                return;

            try
            {
                // Extraire le préfixe commun des prompts similaires
                string commonPrefix = ExtractCommonPrefix(potentialPattern.SimilarPrompts);

                // Si le préfixe commun est trop court, essayer d'extraire un pattern regex
                string pattern = commonPrefix.Length >= 10 ? commonPrefix : ExtractRegexPattern(potentialPattern.SimilarPrompts);

                if (!string.IsNullOrEmpty(pattern))
                {
                    // Créer une nouvelle signature de prompt
                    var promptSignature = new PromptSignature
                    {
                        PromptStart = pattern,
                        RequestSettings = potentialPattern.RequestSettings,
                        MatchingRegex = ContainsRegexSpecialChars(pattern) ? pattern : null
                    };

                    // Créer les paramètres pour le nouveau type de prompt
                    var settings = new PromptMultiConnectorSettings
                    {
                        PromptType = new PromptType
                        {
                            PromptName = $"adaptive_pattern_{DateTime.UtcNow.Ticks}",
                            Signature = promptSignature,
                            SignatureNeedsAdjusting = true
                        }
                    };

                    // Ajouter les instances au type de prompt
                    foreach (var prompt in potentialPattern.SimilarPrompts)
                    {
                        settings.PromptType.Instances.Add(prompt);
                    }

                    // Ajouter le nouveau prompt au matcher de base
                    _basePromptMatcher.AddPrompt(promptSignature, settings);

                    // Supprimer les prompts correspondants du cache
                    RemoveMatchingPromptsFromCache(pattern);
                }
            }
            catch (Exception)
            {
                // Ignorer les exceptions lors de l'analyse asynchrone
            }
        }

        /// <summary>
        /// Extrait le préfixe commun d'une liste de chaînes.
        /// </summary>
        /// <param name="strings">Liste de chaînes</param>
        /// <returns>Le préfixe commun</returns>
        private string ExtractCommonPrefix(List<string> strings)
        {
            if (strings.Count == 0)
                return string.Empty;

            string firstString = strings[0];
            int prefixLength = firstString.Length;

            for (int i = 1; i < strings.Count; i++)
            {
                prefixLength = Math.Min(prefixLength, strings[i].Length);
                for (int j = 0; j < prefixLength; j++)
                {
                    if (firstString[j] != strings[i][j])
                    {
                        prefixLength = j;
                        break;
                    }
                }
            }

            return firstString.Substring(0, prefixLength);
        }

        /// <summary>
        /// Extrait un pattern regex à partir d'une liste de chaînes.
        /// </summary>
        /// <param name="strings">Liste de chaînes</param>
        /// <returns>Un pattern regex</returns>
        private string ExtractRegexPattern(List<string> strings)
        {
            if (strings.Count < 2)
                return string.Empty;

            // Trouver le préfixe commun
            string prefix = ExtractCommonPrefix(strings);

            // Si le préfixe est trop court, essayer de trouver un pattern plus complexe
            if (prefix.Length < 5)
            {
                // Analyser les chaînes pour trouver des motifs récurrents
                var commonWords = FindCommonWords(strings);
                if (commonWords.Count > 0)
                {
                    // Construire un pattern regex à partir des mots communs
                    return string.Join(".*", commonWords);
                }
            }

            return prefix;
        }

        /// <summary>
        /// Trouve les mots communs dans une liste de chaînes.
        /// </summary>
        /// <param name="strings">Liste de chaînes</param>
        /// <returns>Liste des mots communs</returns>
        private List<string> FindCommonWords(List<string> strings)
        {
            if (strings.Count == 0)
                return new List<string>();

            // Diviser la première chaîne en mots
            var words = strings[0].Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            // Filtrer les mots qui apparaissent dans toutes les chaînes
            var commonWords = new List<string>();
            foreach (var word in words)
            {
                if (word.Length >= 3 && strings.All(s => s.Contains(word)))
                {
                    commonWords.Add(word);
                }
            }

            return commonWords;
        }

        /// <summary>
        /// Supprime les prompts correspondant à un pattern du cache.
        /// </summary>
        /// <param name="pattern">Le pattern à rechercher</param>
        private void RemoveMatchingPromptsFromCache(string pattern)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                // Créer une regex à partir du pattern
                Regex regex = new Regex(pattern, RegexOptions.Compiled);

                // Trouver les clés à supprimer
                var keysToRemove = new List<string>();
                foreach (var entry in _unrecognizedPromptsCache)
                {
                    bool allMatch = true;
                    foreach (var prompt in entry.Value.Prompts)
                    {
                        if (!regex.IsMatch(prompt))
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    if (allMatch)
                    {
                        keysToRemove.Add(entry.Key);
                    }
                }

                // Supprimer les entrées correspondantes
                foreach (var key in keysToRemove)
                {
                    _unrecognizedPromptsCache.TryRemove(key, out _);
                }
            }
            catch (Exception)
            {
                // Ignorer les exceptions lors de la suppression
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Nettoie le cache en supprimant les entrées expirées.
        /// </summary>
        /// <param name="state">État du timer (non utilisé)</param>
        private void CleanupCache(object? state)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                DateTime now = DateTime.UtcNow;

                // Supprimer les entrées expirées
                var keysToRemove = _unrecognizedPromptsCache
                    .Where(entry => now - entry.Value.LastSeen > _cacheEntryExpiration)
                    .Select(entry => entry.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _unrecognizedPromptsCache.TryRemove(key, out _);
                }

                // Si le cache est toujours trop grand, supprimer les entrées les plus anciennes
                if (_unrecognizedPromptsCache.Count > _maxCacheSize)
                {
                    var oldestEntries = _unrecognizedPromptsCache
                        .OrderBy(entry => entry.Value.LastSeen)
                        .Take(_unrecognizedPromptsCache.Count - _maxCacheSize / 2)
                        .Select(entry => entry.Key)
                        .ToList();

                    foreach (var key in oldestEntries)
                    {
                        _unrecognizedPromptsCache.TryRemove(key, out _);
                    }
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
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

        /// <summary>
        /// Classe représentant les informations sur un prompt non reconnu.
        /// </summary>
        private class UnrecognizedPromptInfo
        {
            /// <summary>
            /// Date de première observation du prompt
            /// </summary>
            public DateTime FirstSeen { get; set; }

            /// <summary>
            /// Date de dernière observation du prompt
            /// </summary>
            public DateTime LastSeen { get; set; }

            /// <summary>
            /// Nombre d'occurrences du prompt
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// Liste des prompts observés
            /// </summary>
            public List<string> Prompts { get; set; } = new();

            /// <summary>
            /// Paramètres de requête associés au prompt
            /// </summary>
            public AIRequestSettings RequestSettings { get; set; } = new();
        }

        /// <summary>
        /// Classe représentant un pattern potentiel identifié à partir de prompts similaires.
        /// </summary>
        private class PotentialPattern
        {
            /// <summary>
            /// Liste des prompts similaires
            /// </summary>
            public List<string> SimilarPrompts { get; set; } = new();

            /// <summary>
            /// Paramètres de requête associés au pattern
            /// </summary>
            public AIRequestSettings RequestSettings { get; set; } = new();
        }
    }
}
