// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextCompletion;
using Microsoft.SemanticKernel.Diagnostics;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector
{
    /// <summary>
    /// Connecteur pour les modèles disponibles via OpenRouter
    /// </summary>
    public class OpenRouterConnector
    {
        private readonly ILogger _logger;
        private readonly OpenRouterConfiguration _configuration;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="OpenRouterConnector"/>
        /// </summary>
        /// <param name="configuration">Configuration pour OpenRouter</param>
        /// <param name="httpClient">Client HTTP à utiliser pour les requêtes</param>
        /// <param name="loggerFactory">Factory pour créer des loggers</param>
        public OpenRouterConnector(
            OpenRouterConfiguration configuration,
            HttpClient? httpClient = null,
            ILoggerFactory? loggerFactory = null)
        {
            Verify.NotNull(configuration, nameof(configuration));

            _configuration = configuration;
            _httpClient = httpClient ?? new HttpClient();
            _logger = loggerFactory?.CreateLogger<OpenRouterConnector>() ?? NullLoggerFactory.Instance.CreateLogger<OpenRouterConnector>();

            // Configurer le client HTTP
            if (string.IsNullOrEmpty(_httpClient.DefaultRequestHeaders.UserAgent.ToString()))
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Semantic-Fleet/1.0");
            }

            // Ajouter les en-têtes spécifiques à OpenRouter
            if (!_httpClient.DefaultRequestHeaders.Contains("HTTP-Referer"))
            {
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://semantic-fleet.myia.io");
            }

            if (!_httpClient.DefaultRequestHeaders.Contains("X-Title"))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Title", "Semantic Fleet");
            }
        }

        /// <summary>
        /// Crée un service de complétion de texte pour un modèle spécifique
        /// </summary>
        /// <param name="modelConfig">Configuration du modèle</param>
        /// <returns>Service de complétion de texte</returns>
        public ITextCompletion GetTextCompletion(ModelConfig modelConfig)
        {
            Verify.NotNull(modelConfig, nameof(modelConfig));

            // Utiliser l'implémentation OpenAI avec l'URL de base d'OpenRouter
            return new OpenAITextCompletion(
                modelConfig.ModelId,
                _configuration.ApiKey,
                _configuration.BaseUrl,
                _httpClient);
        }

        /// <summary>
        /// Crée un service de complétion de chat pour un modèle spécifique
        /// </summary>
        /// <param name="modelConfig">Configuration du modèle</param>
        /// <returns>Service de complétion de chat</returns>
        public OpenAIChatCompletion GetChatCompletion(ModelConfig modelConfig)
        {
            Verify.NotNull(modelConfig, nameof(modelConfig));

            // Utiliser l'implémentation OpenAI avec l'URL de base d'OpenRouter
            return new OpenAIChatCompletion(
                modelConfig.ChatModelId,
                _configuration.ApiKey,
                _configuration.BaseUrl,
                _httpClient);
        }

        /// <summary>
        /// Crée un service de complétion de texte nommé pour un modèle spécifique
        /// </summary>
        /// <param name="modelName">Nom du modèle (ClaudeSonnet, GeminiPro, etc.)</param>
        /// <returns>Service de complétion de texte nommé</returns>
        public NamedTextCompletion GetNamedTextCompletion(string modelName)
        {
            ModelConfig? modelConfig = GetModelConfigByName(modelName);
            Verify.NotNull(modelConfig, nameof(modelConfig));

            var textCompletion = GetTextCompletion(modelConfig);

            return new NamedTextCompletion(modelConfig.ModelId, textCompletion)
            {
                MaxTokens = modelConfig.MaxTokens,
                CostPer1000Token = modelConfig.CostPer1000Token,
                TokenCountFunc = SimpleTokenCountFunction,
                MaxDegreeOfParallelism = 1 // La plupart des modèles via OpenRouter ont des limites de requêtes concurrentes
            };
        }

        /// <summary>
        /// Obtient la configuration d'un modèle par son nom
        /// </summary>
        /// <param name="modelName">Nom du modèle (ClaudeSonnet, GeminiPro, etc.)</param>
        /// <returns>Configuration du modèle</returns>
        private ModelConfig? GetModelConfigByName(string modelName)
        {
            return modelName switch
            {
                "ClaudeSonnet" => _configuration.Models.ClaudeSonnet,
                "GeminiPro" => _configuration.Models.GeminiPro,
                "Qwen72B" => _configuration.Models.Qwen72B,
                "QwenChat" => _configuration.Models.QwenChat,
                _ => null
            };
        }

        /// <summary>
        /// Énumération des fonctions de comptage de tokens disponibles
        /// </summary>
        public enum TokenCountFunction
        {
            /// <summary>
            /// Utilise le tokenizer GPT-3 pour compter les tokens
            /// </summary>
            Gpt3Tokenizer,

            /// <summary>
            /// Approximation simple basée sur la longueur du texte
            /// </summary>
            SimpleApproximation
        }

        /// <summary>
        /// Fonction pour compter les tokens selon la méthode OpenAI (approximation simple)
        /// </summary>
        public static readonly Func<string, int> SimpleTokenCountFunction =
            (text) => text.Length / 4; // Approximation simple, à remplacer par une implémentation plus précise
    }
}
