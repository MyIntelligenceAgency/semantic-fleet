// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Diagnostics;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector
{
    /// <summary>
    /// Stratégie de cascade de modèles en cas d'échec.
    /// </summary>
    public class ModelCascadeStrategy
    {
        private readonly OptimizedMultiConnectorRouter _router;
        private readonly ILogger? _logger;
        private readonly Dictionary<string, List<string>> _fallbackModels;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="ModelCascadeStrategy"/>.
        /// </summary>
        /// <param name="router">Routeur de modèles</param>
        /// <param name="logger">Logger</param>
        public ModelCascadeStrategy(OptimizedMultiConnectorRouter router, ILogger? logger = null)
        {
            _router = router;
            _logger = logger;
            _fallbackModels = InitializeFallbackModels();
        }

        /// <summary>
        /// Exécute une requête avec fallback en cas d'échec.
        /// </summary>
        /// <param name="prompt">Prompt à envoyer</param>
        /// <param name="category">Catégorie de la tâche</param>
        /// <param name="complexity">Complexité de la tâche</param>
        /// <param name="strategy">Stratégie de routage</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Réponse du modèle</returns>
        public async Task<string> ExecuteWithFallbackAsync(
            string prompt,
            string category,
            string complexity,
            OptimizedMultiConnectorRouter.RoutingStrategy strategy = OptimizedMultiConnectorRouter.RoutingStrategy.Balanced,
            CancellationToken cancellationToken = default)
        {
            // Sélectionner le modèle initial
            string initialModel = _router.SelectOptimalModel(category, complexity, strategy);

            // Obtenir la liste des modèles de fallback pour cette catégorie
            List<string> fallbackModelsForCategory = GetFallbackModelsForCategory(category);

            // Ajouter le modèle initial à la liste des modèles à essayer
            List<string> modelsToTry = new List<string> { initialModel };

            // Ajouter les modèles de fallback qui ne sont pas déjà dans la liste
            foreach (string fallbackModel in fallbackModelsForCategory)
            {
                if (fallbackModel != initialModel && !modelsToTry.Contains(fallbackModel))
                {
                    modelsToTry.Add(fallbackModel);
                }
            }

            // Essayer chaque modèle jusqu'à ce qu'un réussisse
            Exception? lastException = null;

            foreach (string model in modelsToTry)
            {
                try
                {
                    _logger?.LogInformation("Essai du modèle {Model} pour la catégorie {Category} et la complexité {Complexity}", model, category, complexity);

                    // Obtenir l'instance de TextCompletion pour ce modèle
                    ITextCompletion textCompletion = _router.GetTextCompletionForModel(model);

                    // Exécuter la requête
                    string response = await textCompletion.CompleteAsync(prompt, null, cancellationToken).ConfigureAwait(false);

                    _logger?.LogInformation("Modèle {Model} a réussi à traiter la requête", model);

                    return response;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Échec du modèle {Model} : {Message}", model, ex.Message);
                    lastException = ex;
                }
            }

            // Si tous les modèles ont échoué, lancer une exception
            throw new SKException("Tous les modèles ont échoué à traiter la requête", lastException);
        }

        /// <summary>
        /// Obtient la liste des modèles de fallback pour une catégorie donnée.
        /// </summary>
        /// <param name="category">Catégorie de la tâche</param>
        /// <returns>Liste des modèles de fallback</returns>
        private List<string> GetFallbackModelsForCategory(string category)
        {
            if (_fallbackModels.TryGetValue(category.ToLowerInvariant(), out var models))
            {
                return models;
            }

            // Retourner une liste par défaut si la catégorie n'est pas trouvée
            return new List<string>
            {
                "gpt-4o",
                "anthropic/claude-3.7-sonnet",
                "google/gemini-pro-1.5",
                "gpt-3.5-turbo"
            };
        }

        /// <summary>
        /// Initialise les modèles de fallback pour chaque catégorie.
        /// </summary>
        /// <returns>Dictionnaire des modèles de fallback par catégorie</returns>
        private Dictionary<string, List<string>> InitializeFallbackModels()
        {
            return new Dictionary<string, List<string>>
            {
                // Modèles de fallback pour les tâches de code
                {
                    "code", new List<string>
                    {
                        "gpt-4o",
                        "anthropic/claude-3.7-sonnet",
                        "qwen/qwen3-32b",
                        "google/gemini-pro-1.5",
                        "gpt-3.5-turbo"
                    }
                },

                // Modèles de fallback pour les tâches de résumé
                {
                    "summarization", new List<string>
                    {
                        "anthropic/claude-3.7-sonnet",
                        "gpt-4o",
                        "google/gemini-pro-1.5",
                        "gpt-3.5-turbo"
                    }
                },

                // Modèles de fallback pour les tâches de raisonnement
                {
                    "reasoning", new List<string>
                    {
                        "gpt-4o",
                        "anthropic/claude-3.7-sonnet",
                        "qwen/qwen3-32b",
                        "google/gemini-pro-1.5",
                        "gpt-3.5-turbo"
                    }
                },

                // Modèles de fallback pour les tâches d'écriture
                {
                    "writing", new List<string>
                    {
                        "anthropic/claude-3.7-sonnet",
                        "qwen/qwen3-32b",
                        "gpt-4o",
                        "google/gemini-pro-1.5",
                        "gpt-3.5-turbo"
                    }
                },

                // Modèles de fallback pour les tâches de classification
                {
                    "classification", new List<string>
                    {
                        "google/gemini-pro-1.5",
                        "gpt-4o-mini",
                        "gpt-4o",
                        "anthropic/claude-3.7-sonnet",
                        "gpt-3.5-turbo"
                    }
                }
            };
        }
    }
}
