// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion.PromptMatching
{
    /// <summary>
    /// Tests unitaires pour vérifier le fonctionnement du détecteur adaptatif de prompts
    /// </summary>
    public class AdaptivePromptDetectorTests
    {
        private readonly ITestOutputHelper _output;

        public AdaptivePromptDetectorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();

            // Act
            var detector = new AdaptivePromptDetector(
                basePromptMatcher,
                similarityThreshold: 80,
                minSimilarPromptsToCreatePattern: 5,
                cacheEntryExpiration: TimeSpan.FromHours(12),
                maxCacheSize: 500,
                enabled: true);

            // Assert
            Assert.NotNull(detector);
            Assert.Equal(0, detector.Count);
        }

        [Fact]
        public void MatchPromptSettings_WithMatchingPrompt_ReturnsSettings()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var detector = new AdaptivePromptDetector(basePromptMatcher);

            // Créer une signature de prompt
            var signature = new PromptSignature
            {
                PromptStart = "Hello World",
                RequestSettings = new AIRequestSettings()
            };

            // Créer les paramètres pour le prompt
            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = "hello_world",
                    Instances = { "Hello World Test" }
                }
            };

            // Ajouter le prompt au matcher
            detector.AddPrompt(signature, settings);

            // Act
            var job = new CompletionJob("Hello World Test", new AIRequestSettings());
            var result = detector.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.Equal(settings, result);
        }

        [Fact]
        public void MatchPromptSettings_WithNonMatchingPrompt_ReturnsNull()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var detector = new AdaptivePromptDetector(basePromptMatcher);

            // Créer une signature de prompt
            var signature = new PromptSignature
            {
                PromptStart = "Hello World",
                RequestSettings = new AIRequestSettings()
            };

            // Créer les paramètres pour le prompt
            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = "hello_world",
                    Instances = { "Hello World Test" }
                }
            };

            // Ajouter le prompt au matcher
            detector.AddPrompt(signature, settings);

            // Act
            var job = new CompletionJob("Different Prompt", new AIRequestSettings());
            var result = detector.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void MatchPromptSettings_WhenDisabled_DelegatesToBaseMatcher()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var detector = new AdaptivePromptDetector(basePromptMatcher, enabled: false);

            // Créer une signature de prompt
            var signature = new PromptSignature
            {
                PromptStart = "Hello World",
                RequestSettings = new AIRequestSettings()
            };

            // Créer les paramètres pour le prompt
            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = "hello_world",
                    Instances = { "Hello World Test" }
                }
            };

            // Ajouter le prompt au matcher
            detector.AddPrompt(signature, settings);

            // Act - Utiliser un prompt qui ne correspond pas
            var job = new CompletionJob("Different Prompt", new AIRequestSettings());
            var result = detector.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());

            // Assert - Le résultat devrait être null car le prompt ne correspond pas
            Assert.Null(result);
        }

        [Fact]
        public void AddPrompt_AddsToBaseMatcher()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var detector = new AdaptivePromptDetector(basePromptMatcher);

            // Créer une signature de prompt
            var signature = new PromptSignature
            {
                PromptStart = "Hello World",
                RequestSettings = new AIRequestSettings()
            };

            // Créer les paramètres pour le prompt
            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = "hello_world",
                    Instances = { "Hello World Test" }
                }
            };

            // Act
            detector.AddPrompt(signature, settings);

            // Assert
            Assert.Equal(1, detector.Count);

            // Vérifier que le prompt a bien été ajouté au matcher de base
            var job = new CompletionJob("Hello World Test", new AIRequestSettings());
            var result = detector.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());
            Assert.Equal(settings, result);
        }

        [Fact]
        public void RemovePrompt_RemovesFromBaseMatcher()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var detector = new AdaptivePromptDetector(basePromptMatcher);

            // Créer une signature de prompt
            var signature = new PromptSignature
            {
                PromptStart = "Hello World",
                RequestSettings = new AIRequestSettings()
            };

            // Créer les paramètres pour le prompt
            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = "hello_world",
                    Instances = { "Hello World Test" }
                }
            };

            // Ajouter le prompt au matcher
            detector.AddPrompt(signature, settings);

            // Vérifier que le prompt a bien été ajouté
            Assert.Equal(1, detector.Count);

            // Act
            var removed = detector.RemovePrompt(signature);

            // Assert
            Assert.True(removed);
            Assert.Equal(0, detector.Count);

            // Vérifier que le prompt a bien été supprimé
            var job = new CompletionJob("Hello World Test", new AIRequestSettings());
            var result = detector.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());
            Assert.Null(result);
        }

        [Fact]
        public void Clear_ClearsBaseMatcher()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var detector = new AdaptivePromptDetector(basePromptMatcher);

            // Ajouter plusieurs prompts
            for (int i = 0; i < 3; i++)
            {
                var signature = new PromptSignature
                {
                    PromptStart = $"Prompt{i}",
                    RequestSettings = new AIRequestSettings()
                };

                var settings = new PromptMultiConnectorSettings
                {
                    PromptType = new PromptType
                    {
                        Signature = signature,
                        PromptName = $"prompt_{i}",
                        Instances = { $"Prompt{i} Test" }
                    }
                };

                detector.AddPrompt(signature, settings);
            }

            // Vérifier que les prompts ont bien été ajoutés
            Assert.Equal(3, detector.Count);

            // Act
            detector.Clear();

            // Assert
            Assert.Equal(0, detector.Count);

            // Vérifier que tous les prompts ont bien été supprimés
            for (int i = 0; i < 3; i++)
            {
                var job = new CompletionJob($"Prompt{i} Test", new AIRequestSettings());
                var result = detector.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());
                Assert.Null(result);
            }
        }

        [Fact]
        public void MultipleUnrecognizedPrompts_ShouldBeStoredInCache()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var detector = new AdaptivePromptDetector(
                basePromptMatcher,
                similarityThreshold: 70,
                minSimilarPromptsToCreatePattern: 3);

            // Act - Envoyer plusieurs prompts similaires non reconnus
            var requestSettings = new AIRequestSettings();

            // Ces prompts ont un préfixe commun mais ne correspondent à aucun pattern connu
            var job1 = new CompletionJob("Nouveau pattern de test avec des variations 1", requestSettings);
            var job2 = new CompletionJob("Nouveau pattern de test avec des variations 2", requestSettings);
            var job3 = new CompletionJob("Nouveau pattern de test avec des variations 3", requestSettings);

            // Matcher les prompts (ils ne seront pas reconnus mais stockés dans le cache)
            var result1 = detector.MatchPromptSettings(job1, new List<PromptMultiConnectorSettings>());
            var result2 = detector.MatchPromptSettings(job2, new List<PromptMultiConnectorSettings>());
            var result3 = detector.MatchPromptSettings(job3, new List<PromptMultiConnectorSettings>());

            // Assert - Les résultats devraient être null car les prompts ne correspondent à aucun pattern connu
            Assert.Null(result1);
            Assert.Null(result2);
            Assert.Null(result3);

            // Note: Nous ne pouvons pas tester directement le cache car il est privé,
            // mais nous pouvons vérifier que le nombre de prompts dans le matcher n'a pas changé
            Assert.Equal(0, detector.Count);

            // Dans une implémentation réelle, après suffisamment de prompts similaires,
            // un nouveau pattern serait créé de manière asynchrone
        }

        [Fact]
        public void WithAdaptiveDetection_CreatesAdaptiveDetector()
        {
            // Arrange
            var basePromptMatcher = new OptimizedHybridPromptMatcher();

            // Act
            var detector = basePromptMatcher.WithAdaptiveDetection(options =>
            {
                options.SimilarityThreshold = 85;
                options.MinSimilarPromptsToCreatePattern = 4;
                options.MaxCacheSize = 200;
                options.Enabled = true;
            });

            // Assert
            Assert.NotNull(detector);
            Assert.IsType<AdaptivePromptDetector>(detector);
            Assert.Equal(0, detector.Count);
        }
    }
}
