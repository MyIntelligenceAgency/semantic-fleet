// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel.AI.TextCompletion;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector
{
    /// <summary>
    /// Routeur optimisé pour le MultiConnector qui sélectionne le modèle le plus approprié
    /// en fonction de la catégorie et de la complexité de la tâche.
    /// </summary>
    public class OptimizedMultiConnectorRouter
    {
        private readonly Dictionary<string, ModelPerformanceData> _modelPerformanceData;
        private readonly Dictionary<string, ModelCostData> _modelCostData;
        private readonly Dictionary<string, ModelTimeData> _modelTimeData;
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _routingStrategies;

        /// <summary>
        /// Stratégie de routage à utiliser
        /// </summary>
        public enum RoutingStrategy
        {
            /// <summary>
            /// Privilégie la performance, indépendamment du coût
            /// </summary>
            Performance,

            /// <summary>
            /// Privilégie l'efficacité coût/performance
            /// </summary>
            Economic,

            /// <summary>
            /// Équilibre entre performance et coût
            /// </summary>
            Balanced
        }

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="OptimizedMultiConnectorRouter"/>.
        /// </summary>
        public OptimizedMultiConnectorRouter()
        {
            _modelPerformanceData = InitializeModelPerformanceData();
            _modelCostData = InitializeModelCostData();
            _modelTimeData = InitializeModelTimeData();
            _routingStrategies = InitializeRoutingStrategies();
        }

        /// <summary>
        /// Sélectionne le modèle le plus approprié en fonction de la catégorie et de la complexité de la tâche.
        /// </summary>
        /// <param name="category">Catégorie de la tâche (code, summarization, reasoning, writing, classification)</param>
        /// <param name="complexity">Complexité de la tâche (trivial, simple, medium, hard)</param>
        /// <param name="strategy">Stratégie de routage à utiliser</param>
        /// <returns>Le nom du modèle à utiliser</returns>
        public string SelectOptimalModel(string category, string complexity, RoutingStrategy strategy = RoutingStrategy.Balanced)
        {
            // Modèle par défaut en cas de catégorie ou complexité non reconnue
            string defaultModel = "gpt-4o";

            // Normaliser les entrées
            category = category?.ToLowerInvariant() ?? "";
            complexity = complexity?.ToLowerInvariant() ?? "";

            // Sélectionner la stratégie de routage
            string strategyKey;
            switch (strategy)
            {
                case RoutingStrategy.Performance:
                    strategyKey = "performance";
                    break;
                case RoutingStrategy.Economic:
                    strategyKey = "economic";
                    break;
                case RoutingStrategy.Balanced:
                default:
                    strategyKey = "balanced";
                    break;
            }

            // Vérifier si la catégorie existe dans la stratégie
            if (_routingStrategies.TryGetValue(strategyKey, out var categoryDict))
            {
                // Vérifier si la catégorie existe
                if (categoryDict.TryGetValue(category, out var complexityDict))
                {
                    // Vérifier si la complexité existe
                    if (complexityDict.TryGetValue(complexity, out var model))
                    {
                        return model;
                    }
                }
            }

            return defaultModel;
        }

        /// <summary>
        /// Obtient l'instance de TextCompletion pour le modèle spécifié.
        /// </summary>
        /// <param name="modelName">Nom du modèle</param>
        /// <returns>Instance de ITextCompletion</returns>
        public ITextCompletion GetTextCompletionForModel(string modelName)
        {
            // Implémentation à compléter en fonction de l'architecture du MultiConnector
            switch (modelName)
            {
                case "anthropic/claude-3.7-sonnet":
                    return new AnthropicTextCompletion(modelName);
                case "google/gemini-pro-1.5":
                    return new GoogleTextCompletion(modelName);
                case "gpt-3.5-turbo":
                    return new OpenAITextCompletion(modelName);
                case "gpt-4o":
                    return new OpenAITextCompletion(modelName);
                case "gpt-4o-mini":
                    return new OpenAITextCompletion(modelName);
                case "qwen/qwen3-14b":
                    return new OpenRouterTextCompletion(modelName);
                case "qwen/qwen3-32b":
                    return new OpenRouterTextCompletion(modelName);
                case "o3":
                    return new OpenRouterTextCompletion(modelName);
                case "o4-mini":
                    return new OpenRouterTextCompletion(modelName);
                default:
                    return new OpenAITextCompletion("gpt-4o");
            }
        }

        /// <summary>
        /// Initialise les données de performance des modèles.
        /// </summary>
        private Dictionary<string, ModelPerformanceData> InitializeModelPerformanceData()
        {
            return new Dictionary<string, ModelPerformanceData>
            {
                {
                    "gpt-3.5-turbo", new ModelPerformanceData
                    {
                        GlobalScore = 0.93,
                        CategoryScores = new Dictionary<string, double>
                        {
                            { "classification", 1.00 },
                            { "code", 0.83 },
                            { "creative", 1.00 },
                            { "math", 1.00 },
                            { "qa", 0.60 },
                            { "reasoning", 1.00 },
                            { "summarization", 1.00 },
                            { "writing", 1.00 }
                        },
                        ComplexityScores = new Dictionary<string, double>
                        {
                            { "trivial", 1.00 },
                            { "simple", 0.95 },
                            { "medium", 0.83 }
                        }
                    }
                },
                {
                    "anthropic/claude-3.7-sonnet", new ModelPerformanceData
                    {
                        GlobalScore = 0.93,
                        CategoryScores = new Dictionary<string, double>
                        {
                            { "classification", 1.00 },
                            { "code", 1.00 },
                            { "creative", 1.00 },
                            { "math", 1.00 },
                            { "qa", 0.60 },
                            { "reasoning", 0.89 },
                            { "summarization", 1.00 },
                            { "writing", 1.00 }
                        },
                        ComplexityScores = new Dictionary<string, double>
                        {
                            { "trivial", 1.00 },
                            { "simple", 0.91 },
                            { "medium", 1.00 }
                        }
                    }
                },
                {
                    "google/gemini-pro-1.5", new ModelPerformanceData
                    {
                        GlobalScore = 0.91,
                        CategoryScores = new Dictionary<string, double>
                        {
                            { "classification", 1.00 },
                            { "code", 1.00 },
                            { "creative", 1.00 },
                            { "math", 1.00 },
                            { "qa", 0.60 },
                            { "reasoning", 0.81 },
                            { "summarization", 1.00 },
                            { "writing", 1.00 }
                        },
                        ComplexityScores = new Dictionary<string, double>
                        {
                            { "trivial", 1.00 },
                            { "simple", 0.91 },
                            { "medium", 0.88 }
                        }
                    }
                },
                {
                    "gpt-4o-mini", new ModelPerformanceData
                    {
                        GlobalScore = 0.88,
                        CategoryScores = new Dictionary<string, double>
                        {
                            { "classification", 1.00 },
                            { "code", 0.83 },
                            { "creative", 1.00 },
                            { "math", 0.50 },
                            { "qa", 0.80 },
                            { "reasoning", 0.89 },
                            { "summarization", 1.00 },
                            { "writing", 1.00 }
                        },
                        ComplexityScores = new Dictionary<string, double>
                        {
                            { "trivial", 1.00 },
                            { "simple", 0.87 },
                            { "medium", 0.83 }
                        }
                    }
                },
                {
                    "gpt-4o", new ModelPerformanceData
                    {
                        GlobalScore = 0.86,
                        CategoryScores = new Dictionary<string, double>
                        {
                            { "classification", 1.00 },
                            { "code", 0.83 },
                            { "creative", 1.00 },
                            { "math", 0.50 },
                            { "qa", 0.60 },
                            { "reasoning", 0.89 },
                            { "summarization", 1.00 },
                            { "writing", 1.00 }
                        },
                        ComplexityScores = new Dictionary<string, double>
                        {
                            { "trivial", 1.00 },
                            { "simple", 0.85 },
                            { "medium", 0.83 }
                        }
                    }
                },
                {
                    "qwen/qwen3-14b", new ModelPerformanceData
                    {
                        GlobalScore = 0.66,
                        CategoryScores = new Dictionary<string, double>
                        {
                            { "classification", 1.00 },
                            { "code", 0.00 },
                            { "creative", 1.00 },
                            { "math", 0.00 },
                            { "qa", 0.60 },
                            { "reasoning", 0.89 },
                            { "summarization", 1.00 },
                            { "writing", 1.00 }
                        },
                        ComplexityScores = new Dictionary<string, double>
                        {
                            { "trivial", 1.00 },
                            { "simple", 0.66 },
                            { "medium", 0.50 }
                        }
                    }
                },
                {
                    "qwen/qwen3-32b", new ModelPerformanceData
                    {
                        GlobalScore = 0.65,
                        CategoryScores = new Dictionary<string, double>
                        {
                            { "classification", 1.00 },
                            { "code", 0.33 },
                            { "creative", 0.50 },
                            { "math", 0.00 },
                            { "qa", 0.60 },
                            { "reasoning", 0.81 },
                            { "summarization", 1.00 },
                            { "writing", 1.00 }
                        },
                        ComplexityScores = new Dictionary<string, double>
                        {
                            { "trivial", 1.00 },
                            { "simple", 0.60 },
                            { "medium", 0.71 }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Initialise les données de coût des modèles.
        /// </summary>
        private Dictionary<string, ModelCostData> InitializeModelCostData()
        {
            return new Dictionary<string, ModelCostData>
            {
                { "gpt-3.5-turbo", new ModelCostData { AverageCost = 0.000499m, Efficiency = 4030.92 } },
                { "anthropic/claude-3.7-sonnet", new ModelCostData { AverageCost = 0.009061m, Efficiency = 185.38 } },
                { "google/gemini-pro-1.5", new ModelCostData { AverageCost = 0.001313m, Efficiency = 2445.35 } },
                { "gpt-4o-mini", new ModelCostData { AverageCost = 0.004725m, Efficiency = 819.66 } },
                { "gpt-4o", new ModelCostData { AverageCost = 0.008737m, Efficiency = 422.35 } },
                { "qwen/qwen3-14b", new ModelCostData { AverageCost = 0.001539m, Efficiency = 813.77 } },
                { "qwen/qwen3-32b", new ModelCostData { AverageCost = 0.003347m, Efficiency = 284.50 } }
            };
        }

        /// <summary>
        /// Initialise les données de temps de réponse des modèles.
        /// </summary>
        private Dictionary<string, ModelTimeData> InitializeModelTimeData()
        {
            return new Dictionary<string, ModelTimeData>
            {
                { "gpt-3.5-turbo", new ModelTimeData { AverageResponseTime = 2.38 } },
                { "anthropic/claude-3.7-sonnet", new ModelTimeData { AverageResponseTime = 11.25 } },
                { "google/gemini-pro-1.5", new ModelTimeData { AverageResponseTime = 6.80 } },
                { "gpt-4o-mini", new ModelTimeData { AverageResponseTime = 5.19 } },
                { "gpt-4o", new ModelTimeData { AverageResponseTime = 6.80 } },
                { "qwen/qwen3-14b", new ModelTimeData { AverageResponseTime = 14.19 } },
                { "qwen/qwen3-32b", new ModelTimeData { AverageResponseTime = 24.28 } }
            };
        }

        /// <summary>
        /// Initialise les stratégies de routage.
        /// </summary>
        private Dictionary<string, Dictionary<string, Dictionary<string, string>>> InitializeRoutingStrategies()
        {
            return new Dictionary<string, Dictionary<string, Dictionary<string, string>>>
            {
                // Stratégie de performance
                {
                    "performance", new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "code", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "anthropic/claude-3.7-sonnet" },
                                { "medium", "anthropic/claude-3.7-sonnet" },
                                { "hard", "gpt-4o" }
                            }
                        },
                        {
                            "summarization", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "anthropic/claude-3.7-sonnet" },
                                { "medium", "anthropic/claude-3.7-sonnet" },
                                { "hard", "anthropic/claude-3.7-sonnet" }
                            }
                        },
                        {
                            "reasoning", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "gpt-4o" },
                                { "hard", "gpt-4o" }
                            }
                        },
                        {
                            "writing", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "anthropic/claude-3.7-sonnet" },
                                { "medium", "anthropic/claude-3.7-sonnet" },
                                { "hard", "anthropic/claude-3.7-sonnet" }
                            }
                        },
                        {
                            "classification", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "gpt-4o-mini" }
                            }
                        }
                    }
                },

                // Stratégie économique
                {
                    "economic", new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "code", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "google/gemini-pro-1.5" }
                            }
                        },
                        {
                            "summarization", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "gpt-3.5-turbo" },
                                { "hard", "google/gemini-pro-1.5" }
                            }
                        },
                        {
                            "reasoning", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "google/gemini-pro-1.5" }
                            }
                        },
                        {
                            "writing", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "google/gemini-pro-1.5" }
                            }
                        },
                        {
                            "classification", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "gpt-3.5-turbo" },
                                { "hard", "google/gemini-pro-1.5" }
                            }
                        }
                    }
                },

                // Stratégie équilibrée
                {
                    "balanced", new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "code", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "anthropic/claude-3.7-sonnet" }
                            }
                        },
                        {
                            "summarization", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "google/gemini-pro-1.5" }
                            }
                        },
                        {
                            "reasoning", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "gpt-4o-mini" }
                            }
                        },
                        {
                            "writing", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "anthropic/claude-3.7-sonnet" }
                            }
                        },
                        {
                            "classification", new Dictionary<string, string>
                            {
                                { "trivial", "gpt-3.5-turbo" },
                                { "simple", "gpt-3.5-turbo" },
                                { "medium", "google/gemini-pro-1.5" },
                                { "hard", "google/gemini-pro-1.5" }
                            }
                        }
                    }
                }
            };
        }
    }

    /// <summary>
    /// Données de performance d'un modèle.
    /// </summary>
    public class ModelPerformanceData
    {
        /// <summary>
        /// Score global du modèle.
        /// </summary>
        public double GlobalScore { get; set; }

        /// <summary>
        /// Scores par catégorie.
        /// </summary>
        public Dictionary<string, double> CategoryScores { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Scores par niveau de complexité.
        /// </summary>
        public Dictionary<string, double> ComplexityScores { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Données de coût d'un modèle.
    /// </summary>
    public class ModelCostData
    {
        /// <summary>
        /// Coût moyen par requête.
        /// </summary>
        public decimal AverageCost { get; set; }

        /// <summary>
        /// Efficacité coût/performance.
        /// </summary>
        public double Efficiency { get; set; }
    }

    /// <summary>
    /// Données de temps de réponse d'un modèle.
    /// </summary>
    public class ModelTimeData
    {
        /// <summary>
        /// Temps de réponse moyen en secondes.
        /// </summary>
        public double AverageResponseTime { get; set; }
    }
}
