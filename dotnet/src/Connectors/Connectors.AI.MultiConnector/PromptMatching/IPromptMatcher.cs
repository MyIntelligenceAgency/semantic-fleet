// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Interface pour les algorithmes de correspondance de prompts.
    /// Permet de trouver les paramètres associés à un prompt donné.
    /// </summary>
    public interface IPromptMatcher
    {
        /// <summary>
        /// Trouve les paramètres de connecteur multi-prompt correspondant à un job de complétion.
        /// </summary>
        /// <param name="completionJob">Le job de complétion à matcher</param>
        /// <param name="promptSettings">La collection de paramètres de prompts disponibles</param>
        /// <returns>Les paramètres correspondants ou null si aucune correspondance n'est trouvée</returns>
        PromptMultiConnectorSettings? MatchPromptSettings(CompletionJob completionJob, IEnumerable<PromptMultiConnectorSettings> promptSettings);

        /// <summary>
        /// Ajoute un nouveau prompt et ses paramètres associés à la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à ajouter</param>
        /// <param name="settings">Les paramètres associés au prompt</param>
        void AddPrompt(PromptSignature promptSignature, PromptMultiConnectorSettings settings);

        /// <summary>
        /// Supprime un prompt et ses paramètres associés de la structure interne.
        /// </summary>
        /// <param name="promptSignature">La signature du prompt à supprimer</param>
        /// <returns>True si le prompt a été supprimé, false sinon</returns>
        bool RemovePrompt(PromptSignature promptSignature);

        /// <summary>
        /// Efface tous les prompts et paramètres associés de la structure interne.
        /// </summary>
        void Clear();

        /// <summary>
        /// Nombre de prompts stockés dans la structure interne.
        /// </summary>
        int Count { get; }
    }
}
