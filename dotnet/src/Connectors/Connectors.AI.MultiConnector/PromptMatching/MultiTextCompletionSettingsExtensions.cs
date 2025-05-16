// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Extensions pour faciliter l'intégration des matchers de prompts avec MultiTextCompletionSettings
    /// </summary>
    public static class MultiTextCompletionSettingsExtensions
    {
        /// <summary>
        /// Configure le MultiTextCompletionSettings pour utiliser un matcher de prompts séquentiel (comportement par défaut)
        /// </summary>
        /// <param name="settings">Les paramètres à configurer</param>
        /// <returns>Les paramètres configurés</returns>
        public static MultiTextCompletionSettings UseSequentialPromptMatcher(this MultiTextCompletionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var matcher = new SequentialPromptMatcher();

            // Pré-remplir le matcher avec les prompts existants
            foreach (var promptSettings in settings.PromptMultiConnectorSettings)
            {
                matcher.AddPrompt(promptSettings.PromptType.Signature, promptSettings);
            }

            settings.PromptMatcher = matcher.MatchPromptSettings;
            return settings;
        }

        /// <summary>
        /// Configure le MultiTextCompletionSettings pour utiliser un matcher de prompts basé sur RadixTree
        /// </summary>
        /// <param name="settings">Les paramètres à configurer</param>
        /// <returns>Les paramètres configurés</returns>
        public static MultiTextCompletionSettings UseRadixTreePromptMatcher(this MultiTextCompletionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var matcher = new RadixTreePromptMatcher();

            // Pré-remplir le matcher avec les prompts existants
            foreach (var promptSettings in settings.PromptMultiConnectorSettings)
            {
                matcher.AddPrompt(promptSettings.PromptType.Signature, promptSettings);
            }

            settings.PromptMatcher = matcher.MatchPromptSettings;
            return settings;
        }

        /// <summary>
        /// Configure le MultiTextCompletionSettings pour utiliser un matcher de prompts hybride
        /// </summary>
        /// <param name="settings">Les paramètres à configurer</param>
        /// <returns>Les paramètres configurés</returns>
        public static MultiTextCompletionSettings UseHybridPromptMatcher(this MultiTextCompletionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var matcher = new HybridPromptMatcher();

            // Pré-remplir le matcher avec les prompts existants
            foreach (var promptSettings in settings.PromptMultiConnectorSettings)
            {
                matcher.AddPrompt(promptSettings.PromptType.Signature, promptSettings);
            }

            settings.PromptMatcher = matcher.MatchPromptSettings;
            return settings;
        }

        /// <summary>
        /// Configure le MultiTextCompletionSettings pour utiliser un matcher de prompts personnalisé
        /// </summary>
        /// <param name="settings">Les paramètres à configurer</param>
        /// <param name="matcher">Le matcher de prompts à utiliser</param>
        /// <returns>Les paramètres configurés</returns>
        public static MultiTextCompletionSettings UsePromptMatcher(this MultiTextCompletionSettings settings, IPromptMatcher matcher)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (matcher == null)
            {
                throw new ArgumentNullException(nameof(matcher));
            }

            // Pré-remplir le matcher avec les prompts existants
            foreach (var promptSettings in settings.PromptMultiConnectorSettings)
            {
                matcher.AddPrompt(promptSettings.PromptType.Signature, promptSettings);
            }

            settings.PromptMatcher = matcher.MatchPromptSettings;
            return settings;
        }
        /// <summary>
        /// Configure le MultiTextCompletionSettings pour utiliser un matcher de prompts hybride optimisé
        /// avec traitement parallèle et combinaison des regex
        /// </summary>
        /// <param name="settings">Les paramètres à configurer</param>
        /// <returns>Les paramètres configurés</returns>
        public static MultiTextCompletionSettings UseOptimizedHybridPromptMatcher(this MultiTextCompletionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var matcher = new OptimizedHybridPromptMatcher();

            // Pré-remplir le matcher avec les prompts existants
            foreach (var promptSettings in settings.PromptMultiConnectorSettings)
            {
                matcher.AddPrompt(promptSettings.PromptType.Signature, promptSettings);
            }

            settings.PromptMatcher = matcher.MatchPromptSettings;
            return settings;
        }
    }
}
