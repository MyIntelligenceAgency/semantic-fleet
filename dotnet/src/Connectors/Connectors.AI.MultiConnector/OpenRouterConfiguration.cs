// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector
{
    /// <summary>
    /// Configuration pour l'accès à l'API OpenRouter
    /// </summary>
    public class OpenRouterConfiguration
    {
        /// <summary>
        /// Identifiant du service OpenRouter
        /// </summary>
        public string ServiceId { get; set; } = "openrouter";

        /// <summary>
        /// Clé API pour OpenRouter
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// URL de base pour l'API OpenRouter
        /// </summary>
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

        /// <summary>
        /// Configuration des modèles disponibles via OpenRouter
        /// </summary>
        public OpenRouterModels Models { get; set; } = new OpenRouterModels();

        /// <summary>
        /// Paramètres globaux pour tous les modèles OpenRouter
        /// </summary>
        public Dictionary<string, string> GlobalParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Crée une instance de OpenRouterConfiguration avec les valeurs par défaut
        /// </summary>
        public OpenRouterConfiguration()
        {
            // Valeurs par défaut
        }

        /// <summary>
        /// Crée une instance de OpenRouterConfiguration avec une clé API spécifiée
        /// </summary>
        /// <param name="apiKey">Clé API OpenRouter</param>
        public OpenRouterConfiguration(string apiKey)
        {
            ApiKey = apiKey;
        }

        /// <summary>
        /// Crée une instance de OpenRouterConfiguration avec une clé API et une URL de base spécifiées
        /// </summary>
        /// <param name="apiKey">Clé API OpenRouter</param>
        /// <param name="baseUrl">URL de base de l'API OpenRouter</param>
        public OpenRouterConfiguration(string apiKey, string baseUrl)
        {
            ApiKey = apiKey;
            BaseUrl = baseUrl;
        }
    }

    /// <summary>
    /// Configuration des modèles disponibles via OpenRouter
    /// </summary>
    public class OpenRouterModels
    {
        /// <summary>
        /// Configuration pour Claude 3 Sonnet
        /// </summary>
        public ModelConfig ClaudeSonnet { get; set; } = new ModelConfig
        {
            ModelId = "anthropic/claude-3-sonnet-20240229",
            ChatModelId = "anthropic/claude-3-sonnet-20240229"
        };

        /// <summary>
        /// Configuration pour Gemini Pro 2.5
        /// </summary>
        public ModelConfig GeminiPro { get; set; } = new ModelConfig
        {
            ModelId = "google/gemini-pro-1.5",
            ChatModelId = "google/gemini-pro-1.5"
        };

        /// <summary>
        /// Configuration pour Qwen 72B
        /// </summary>
        public ModelConfig Qwen72B { get; set; } = new ModelConfig
        {
            ModelId = "qwen/qwen-72b",
            ChatModelId = "qwen/qwen-72b"
        };

        /// <summary>
        /// Configuration pour Qwen Chat
        /// </summary>
        public ModelConfig QwenChat { get; set; } = new ModelConfig
        {
            ModelId = "qwen/qwen-chat",
            ChatModelId = "qwen/qwen-chat"
        };
    }

    /// <summary>
    /// Configuration d'un modèle spécifique
    /// </summary>
    public class ModelConfig
    {
        /// <summary>
        /// Identifiant du modèle pour les complétion de texte
        /// </summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant du modèle pour les complétion de chat
        /// </summary>
        public string ChatModelId { get; set; } = string.Empty;

        /// <summary>
        /// Nombre maximum de tokens pour ce modèle
        /// </summary>
        public int MaxTokens { get; set; } = 4096;

        /// <summary>
        /// Coût par 1000 tokens pour ce modèle
        /// </summary>
        public decimal CostPer1000Token { get; set; } = 0.001m;
    }
}
