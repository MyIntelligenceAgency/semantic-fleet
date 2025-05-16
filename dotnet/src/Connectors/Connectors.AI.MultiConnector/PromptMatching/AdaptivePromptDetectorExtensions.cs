// Copyright (c) MyIA. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching
{
    /// <summary>
    /// Extensions pour faciliter l'intégration du détecteur adaptatif de prompts.
    /// </summary>
    public static class AdaptivePromptDetectorExtensions
    {
        /// <summary>
        /// Configure les paramètres du détecteur adaptatif de prompts.
        /// </summary>
        public class AdaptivePromptDetectorOptions
        {
            /// <summary>
            /// Seuil de similarité pour considérer deux prompts comme similaires (0-100).
            /// </summary>
            public int SimilarityThreshold { get; set; } = 70;

            /// <summary>
            /// Nombre minimum de prompts similaires pour créer un nouveau pattern.
            /// </summary>
            public int MinSimilarPromptsToCreatePattern { get; set; } = 3;

            /// <summary>
            /// Durée d'expiration des entrées du cache.
            /// </summary>
            public TimeSpan CacheEntryExpiration { get; set; } = TimeSpan.FromHours(24);

            /// <summary>
            /// Taille maximale du cache.
            /// </summary>
            public int MaxCacheSize { get; set; } = 1000;

            /// <summary>
            /// Indique si le détecteur adaptatif est activé.
            /// </summary>
            public bool Enabled { get; set; } = true;
        }

        /// <summary>
        /// Ajoute le détecteur adaptatif de prompts aux services.
        /// </summary>
        /// <param name="services">Collection de services</param>
        /// <param name="configureOptions">Action de configuration des options</param>
        /// <returns>Collection de services mise à jour</returns>
        public static IServiceCollection AddAdaptivePromptDetector(
            this IServiceCollection services,
            Action<AdaptivePromptDetectorOptions>? configureOptions = null)
        {
            // Configurer les options
            var options = new AdaptivePromptDetectorOptions();
            configureOptions?.Invoke(options);

            // Enregistrer le décorateur pour IPromptMatcher
            services.TryAddSingleton<IPromptMatcher>(serviceProvider =>
            {
                // Récupérer l'implémentation de base (OptimizedHybridPromptMatcher par défaut)
                var basePromptMatcher = serviceProvider.GetService<IPromptMatcher>() ?? new OptimizedHybridPromptMatcher();

                // Créer le détecteur adaptatif
                return new AdaptivePromptDetector(
                    basePromptMatcher,
                    options.SimilarityThreshold,
                    options.MinSimilarPromptsToCreatePattern,
                    options.CacheEntryExpiration,
                    options.MaxCacheSize,
                    options.Enabled);
            });

            return services;
        }

        /// <summary>
        /// Étend un matcher de prompts existant avec le détecteur adaptatif.
        /// </summary>
        /// <param name="basePromptMatcher">Matcher de prompts de base</param>
        /// <param name="configureOptions">Action de configuration des options</param>
        /// <returns>Un détecteur adaptatif de prompts</returns>
        public static AdaptivePromptDetector WithAdaptiveDetection(
            this IPromptMatcher basePromptMatcher,
            Action<AdaptivePromptDetectorOptions>? configureOptions = null)
        {
            // Configurer les options
            var options = new AdaptivePromptDetectorOptions();
            configureOptions?.Invoke(options);

            // Créer le détecteur adaptatif
            return new AdaptivePromptDetector(
                basePromptMatcher,
                options.SimilarityThreshold,
                options.MinSimilarPromptsToCreatePattern,
                options.CacheEntryExpiration,
                options.MaxCacheSize,
                options.Enabled);
        }

        /// <summary>
        /// Étend les paramètres de complétion multi-texte pour activer le détecteur adaptatif de prompts.
        /// </summary>
        /// <param name="settings">Paramètres de complétion multi-texte</param>
        /// <param name="enabled">Indique si le détecteur adaptatif est activé</param>
        /// <returns>Les paramètres mis à jour</returns>
        public static MultiTextCompletionSettings UseAdaptivePromptDetector(
            this MultiTextCompletionSettings settings,
            bool enabled = true)
        {
            // Vérifier si le matcher de prompts actuel est déjà un AdaptivePromptDetector
            // Remplacer la fonction de matching par une fonction qui utilise AdaptivePromptDetector
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var adaptiveDetector = new AdaptivePromptDetector(basePromptMatcher, enabled: enabled);

            // Définir la fonction de matching pour utiliser notre détecteur adaptatif
            settings.PromptMatcher = (job, promptSettings) => adaptiveDetector.MatchPromptSettings(job, promptSettings);

            return settings;
        }
    }
}
