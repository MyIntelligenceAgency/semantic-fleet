// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;

/// <summary>
/// Implémentation optimisée du matcher de prompts hybride avec traitement parallèle et combinaison des regex.
/// </summary>
public class OptimizedHybridPromptMatcher : IPromptMatcher
{
    private readonly RadixTree<string, char, PromptMultiConnectorSettings> _radixTree = new();
    private readonly Dictionary<string, Regex> _regexCache = new();

    // Stockage des regex individuels
    private readonly List<(Regex Regex, PromptMultiConnectorSettings Settings)> _regexPrompts = new();

    // Regex combinés par groupe de compatibilité
    private readonly List<(Regex CombinedRegex, Dictionary<string, PromptMultiConnectorSettings> GroupSettings)> _combinedRegexGroups = new();

    // Regex dont le groupe n'a pas pu être combiné : ce sont les seuls à tester individuellement
    private readonly List<(Regex Regex, PromptMultiConnectorSettings Settings)> _uncombinedRegexPrompts = new();

    // Nombre maximum de regex à combiner dans un seul groupe
    private const int MaxRegexPerGroup = 10;

    // Seuil pour basculer entre traitement séquentiel et parallèle
    private const int ParallelThreshold = 5;

    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Nombre de prompts stockés
    /// </summary>
    public int Count => this._radixTree.Count + this._regexPrompts.Count;

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

        this._lock.EnterReadLock();
        try
        {
            // 1. Recherche par préfixe dans le RadixTree (plus rapide)
            if (this._radixTree.TryGetValueByPrefix(completionJob.Prompt, out PromptMultiConnectorSettings settings))
            {
                return settings;
            }

            // 2. Recherche dans les groupes de regex combinés
            foreach (var (combinedRegex, groupSettings) in this._combinedRegexGroups)
            {
                var match = combinedRegex.Match(completionJob.Prompt);
                if (match.Success)
                {
                    // Trouver quel pattern spécifique a matché
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (match.Groups[i].Success)
                        {
                            // Le nom du groupe correspond à l'index du pattern original
                            string groupName = combinedRegex.GroupNameFromNumber(i);
                            if (groupSettings.TryGetValue(groupName, out var matchedSettings))
                            {
                                return matchedSettings;
                            }
                        }
                    }
                }
            }

            // 3. Recherche dans les regex qu'aucun groupe combiné ne couvre (en parallèle si nécessaire).
            // Les autres ont déjà été testés à l'étape 2 : les re-tester doublerait le coût d'un échec.
            if (this._uncombinedRegexPrompts.Count > 0)
            {
                if (this._uncombinedRegexPrompts.Count >= ParallelThreshold)
                {
                    // Traitement parallèle pour un grand nombre de regex
                    var match = this.MatchRegexInParallel(completionJob.Prompt);
                    if (match != null)
                    {
                        return match;
                    }
                }
                else
                {
                    // Traitement séquentiel pour un petit nombre de regex
                    foreach (var (regex, regexSettings) in this._uncombinedRegexPrompts)
                    {
                        if (regex.IsMatch(completionJob.Prompt))
                        {
                            return regexSettings;
                        }
                    }
                }
            }

            // 4. Si aucune correspondance n'est trouvée dans notre cache, recherche dans la collection fournie
            return promptSettings.FirstOrDefault(s => s.PromptType.Signature.Matches(completionJob));
        }
        finally
        {
            this._lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Recherche en parallèle dans les expressions régulières individuelles
    /// </summary>
    /// <param name="prompt">Le prompt à tester</param>
    /// <returns>Les paramètres correspondants ou null si aucune correspondance n'est trouvée</returns>
    private PromptMultiConnectorSettings? MatchRegexInParallel(string prompt)
    {
        // Copier la liste pour éviter les problèmes de concurrence
        var regexPrompts = this._uncombinedRegexPrompts.ToArray();

        // Utiliser PLINQ pour tester les regex en parallèle. AsOrdered garantit que le premier
        // pattern enregistré l'emporte, comme en séquentiel : sans lui, FirstOrDefault rend
        // n'importe quel élément qui matche.
        var match = regexPrompts
            .AsParallel()
            .AsOrdered()
            .FirstOrDefault(item => item.Regex.IsMatch(prompt));

        return match.Settings;
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

        this._lock.EnterWriteLock();
        try
        {
            // Si la signature contient des caractères spéciaux de regex, on l'ajoute comme regex
            if (ContainsRegexSpecialChars(promptSignature.PromptStart))
            {
                this.AddRegexPrompt(promptSignature.PromptStart, settings);
            }
            else
            {
                // Sinon, on l'ajoute au RadixTree pour une recherche plus efficace
                this._radixTree.Add(promptSignature.PromptStart, settings);
            }
        }
        finally
        {
            this._lock.ExitWriteLock();
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

        this._lock.EnterWriteLock();
        try
        {
            string pattern = promptSignature.PromptStart;

            // Si c'est une regex, on la supprime de la liste des regex
            if (ContainsRegexSpecialChars(pattern))
            {
                bool removed = false;

                // Vérifier dans les regex individuels
                if (this._regexCache.TryGetValue(pattern, out Regex? regex))
                {
                    int index = this._regexPrompts.FindIndex(p => p.Regex == regex);
                    if (index >= 0)
                    {
                        this._regexPrompts.RemoveAt(index);
                        this._regexCache.Remove(pattern);
                        removed = true;
                    }
                }

                // Reconstruire les groupes combinés, y compris quand la liste devient vide :
                // sinon le groupe périmé continue de matcher le pattern supprimé.
                if (removed)
                {
                    this.RebuildCombinedRegexGroups();
                }

                return removed;
            }
            // Sinon, on la supprime du RadixTree
            return this._radixTree.Remove(pattern);
        }
        finally
        {
            this._lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Efface tous les prompts et paramètres associés de la structure interne.
    /// </summary>
    public void Clear()
    {
        this._lock.EnterWriteLock();
        try
        {
            this._radixTree.Clear();
            this._regexPrompts.Clear();
            this._regexCache.Clear();
            this._combinedRegexGroups.Clear();
        }
        finally
        {
            this._lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Ajoute un prompt sous forme de regex
    /// </summary>
    /// <param name="pattern">Motif regex</param>
    /// <param name="settings">Paramètres associés</param>
    private void AddRegexPrompt(string pattern, PromptMultiConnectorSettings settings)
    {
        if (!this._regexCache.TryGetValue(pattern, out Regex? regex))
        {
            regex = new Regex(pattern, RegexOptions.Compiled);
            this._regexCache.Add(pattern, regex);
        }

        // Vérifier si la regex existe déjà
        int existingIndex = this._regexPrompts.FindIndex(p => p.Regex == regex);

        if (existingIndex >= 0)
        {
            // Mettre à jour les paramètres existants
            this._regexPrompts[existingIndex] = (regex, settings);
        }
        else
        {
            // Ajouter une nouvelle entrée
            this._regexPrompts.Add((regex, settings));
        }

        // Reconstruire à chaque mutation : un groupe combiné qui ne reflète pas la liste
        // rend des paramètres périmés (mise à jour) ou ignore les derniers patterns ajoutés.
        this.RebuildCombinedRegexGroups();
    }

    /// <summary>
    /// Reconstruit les groupes de regex combinés
    /// </summary>
    private void RebuildCombinedRegexGroups()
    {
        this._combinedRegexGroups.Clear();
        this._uncombinedRegexPrompts.Clear();

        // Regrouper les regex par lots de MaxRegexPerGroup
        for (int i = 0; i < this._regexPrompts.Count; i += MaxRegexPerGroup)
        {
            var group = this._regexPrompts.Skip(i).Take(MaxRegexPerGroup).ToList();
            if (group.Count > 0)
            {
                this.TryCombineRegexGroup(group);
            }
        }
    }

    /// <summary>
    /// Tente de combiner un groupe de regex en un seul regex
    /// </summary>
    /// <param name="regexGroup">Groupe de regex à combiner</param>
    private void TryCombineRegexGroup(List<(Regex Regex, PromptMultiConnectorSettings Settings)> regexGroup)
    {
        try
        {
            // Construire un pattern combiné avec des groupes nommés
            var patternBuilder = new System.Text.StringBuilder();
            var groupSettings = new Dictionary<string, PromptMultiConnectorSettings>();

            for (int i = 0; i < regexGroup.Count; i++)
            {
                var (regex, settings) = regexGroup[i];
                string groupName = $"Group{i}";

                // Ajouter un OR si ce n'est pas le premier pattern
                if (i > 0)
                {
                    patternBuilder.Append('|');
                }

                // Ajouter le pattern avec un groupe nommé. Le préfixe paresseux, ancré en tête par le
                // ^ global, fait tester chaque alternative sur toute la chaîne avant de passer à la
                // suivante : le premier pattern enregistré l'emporte, comme en séquentiel. Sans lui,
                // l'alternance rend le pattern qui matche le plus à gauche, pas le premier enregistré.
                patternBuilder.Append($@"(?<{groupName}>[\s\S]*?(?:{regex}))");

                // Stocker les paramètres associés au groupe
                groupSettings.Add(groupName, settings);
            }

            // Compiler le regex combiné
            var combinedRegex = new Regex($"^(?:{patternBuilder})", RegexOptions.Compiled);

            // Ajouter le groupe combiné
            this._combinedRegexGroups.Add((combinedRegex, groupSettings));
        }
        catch (ArgumentException)
        {
            // Si la combinaison échoue (par exemple, en raison de regex incompatibles),
            // les regex du groupe sont testés individuellement à l'étape 3
            this._uncombinedRegexPrompts.AddRange(regexGroup);
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
