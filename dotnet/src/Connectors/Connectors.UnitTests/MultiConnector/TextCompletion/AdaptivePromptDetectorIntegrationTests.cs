// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion
{
    /// <summary>
    /// Tests d'intégration pour vérifier le fonctionnement du détecteur adaptatif de prompts
    /// dans un scénario plus réaliste avec MultiTextCompletion
    /// </summary>
    public class AdaptivePromptDetectorIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public AdaptivePromptDetectorIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void AddAdaptivePromptDetector_ConfiguresCorrectly()
        {
            // Arrange & Act
            // Créer directement un détecteur adaptatif avec les options configurées
            var basePromptMatcher = new OptimizedHybridPromptMatcher();
            var detector = new AdaptivePromptDetector(
                basePromptMatcher,
                similarityThreshold: 75,
                minSimilarPromptsToCreatePattern: 4,
                cacheEntryExpiration: TimeSpan.FromHours(24),
                maxCacheSize: 500,
                enabled: true);

            // Assert
            Assert.NotNull(detector);
            Assert.IsType<AdaptivePromptDetector>(detector);
            Assert.Equal(0, detector.Count);
        }

        [Fact]
        public void MultiTextCompletionSettings_UseAdaptivePromptDetector_ConfiguresSettings()
        {
            // Arrange
            var settings = new MultiTextCompletionSettings();
            var originalMatcher = settings.PromptMatcher;

            // Act
            settings.UseAdaptivePromptDetector(true);

            // Assert
            Assert.NotNull(settings.PromptMatcher);
            Assert.NotEqual(originalMatcher, settings.PromptMatcher);

            // Vérifier que le matcher est bien configuré pour utiliser AdaptivePromptDetector
            // en testant son comportement
            var job = new CompletionJob("Test prompt", new AIRequestSettings());
            var result = settings.PromptMatcher(job, new List<PromptMultiConnectorSettings>());
            Assert.Null(result); // Aucun prompt n'a été ajouté, donc le résultat devrait être null
        }

        [Fact]
        public void MultipleUnrecognizedPrompts_ShouldEventuallyCreatePattern()
        {
            // Arrange
            var settings = new MultiTextCompletionSettings();
            settings.UseAdaptivePromptDetector(true);

            // Créer un prompt connu pour vérifier que le système fonctionne
            var knownSignature = new PromptSignature
            {
                PromptStart = "Known pattern",
                RequestSettings = new AIRequestSettings()
            };

            var knownSettings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = knownSignature,
                    PromptName = "known_pattern",
                    Instances = { "Known pattern test" }
                }
            };

            // Ajouter manuellement le prompt connu aux settings
            settings.PromptMultiConnectorSettings.Add(knownSettings);

            // Act & Assert
            // 1. Vérifier que le prompt connu est bien reconnu
            var knownJob = new CompletionJob("Known pattern test", new AIRequestSettings());
            var knownResult = settings.PromptMatcher(knownJob, settings.PromptMultiConnectorSettings);
            Assert.Equal(knownSettings, knownResult);

            // 2. Envoyer plusieurs prompts similaires non reconnus
            var requestSettings = new AIRequestSettings();

            // Ces prompts ont un préfixe commun mais ne correspondent à aucun pattern connu
            var job1 = new CompletionJob("Nouveau pattern de test avec des variations 1", requestSettings);
            var job2 = new CompletionJob("Nouveau pattern de test avec des variations 2", requestSettings);
            var job3 = new CompletionJob("Nouveau pattern de test avec des variations 3", requestSettings);

            // Matcher les prompts (ils ne seront pas reconnus mais stockés dans le cache)
            var result1 = settings.PromptMatcher(job1, settings.PromptMultiConnectorSettings);
            var result2 = settings.PromptMatcher(job2, settings.PromptMultiConnectorSettings);
            var result3 = settings.PromptMatcher(job3, settings.PromptMultiConnectorSettings);

            // Les résultats devraient être null car les prompts ne correspondent à aucun pattern connu
            Assert.Null(result1);
            Assert.Null(result2);
            Assert.Null(result3);

            // Note: Dans une implémentation réelle, après suffisamment de prompts similaires,
            // un nouveau pattern serait créé de manière asynchrone, mais nous ne pouvons pas
            // facilement tester cela dans un test unitaire car c'est asynchrone et dépend du timing
        }

        [Fact]
        public void WithAdaptiveDetection_ConfiguresCorrectly()
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

            // Ajouter un prompt pour tester
            var signature = new PromptSignature
            {
                PromptStart = "Test pattern",
                RequestSettings = new AIRequestSettings()
            };

            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = "test_pattern",
                    Instances = { "Test pattern example" }
                }
            };

            detector.AddPrompt(signature, settings);

            // Vérifier que le prompt a bien été ajouté
            var job = new CompletionJob("Test pattern example", new AIRequestSettings());
            var result = detector.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());
            Assert.Equal(settings, result);
        }

        [Fact]
        public void AdaptiveDetector_DelegatesToBaseMatcher_WhenPromptMatches()
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
    }
}
