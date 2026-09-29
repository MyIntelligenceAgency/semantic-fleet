// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion
{
    /// <summary>
    /// Tests unitaires pour les optimisations du MultiConnector.
    /// </summary>
    public class OptimizedMultiConnectorTests
    {
        private readonly ITestOutputHelper _output;

        public OptimizedMultiConnectorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void OptimizedMultiConnectorRouter_SelectsCorrectModel_ForPerformanceStrategy()
        {
            // Arrange
            var router = new OptimizedMultiConnectorRouter();

            // Act
            string codeModel = router.SelectOptimalModel("code", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Performance);
            string summaryModel = router.SelectOptimalModel("summarization", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Performance);
            string reasoningModel = router.SelectOptimalModel("reasoning", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Performance);
            string writingModel = router.SelectOptimalModel("writing", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Performance);
            string classificationModel = router.SelectOptimalModel("classification", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Performance);

            // Assert
            Assert.Equal("gpt-4o", codeModel);
            Assert.Equal("anthropic/claude-3.7-sonnet", summaryModel);
            Assert.Equal("gpt-4o", reasoningModel);
            Assert.Equal("anthropic/claude-3.7-sonnet", writingModel);
            Assert.Equal("gpt-4o-mini", classificationModel);
        }

        [Fact]
        public void OptimizedMultiConnectorRouter_SelectsCorrectModel_ForEconomicStrategy()
        {
            // Arrange
            var router = new OptimizedMultiConnectorRouter();

            // Act
            string codeModel = router.SelectOptimalModel("code", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Economic);
            string summaryModel = router.SelectOptimalModel("summarization", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Economic);
            string reasoningModel = router.SelectOptimalModel("reasoning", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Economic);
            string writingModel = router.SelectOptimalModel("writing", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Economic);
            string classificationModel = router.SelectOptimalModel("classification", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Economic);

            // Assert
            Assert.Equal("google/gemini-pro-1.5", codeModel);
            Assert.Equal("google/gemini-pro-1.5", summaryModel);
            Assert.Equal("google/gemini-pro-1.5", reasoningModel);
            Assert.Equal("google/gemini-pro-1.5", writingModel);
            Assert.Equal("google/gemini-pro-1.5", classificationModel);
        }

        [Fact]
        public void OptimizedMultiConnectorRouter_SelectsCorrectModel_ForBalancedStrategy()
        {
            // Arrange
            var router = new OptimizedMultiConnectorRouter();

            // Act
            string codeModel = router.SelectOptimalModel("code", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Balanced);
            string summaryModel = router.SelectOptimalModel("summarization", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Balanced);
            string reasoningModel = router.SelectOptimalModel("reasoning", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Balanced);
            string writingModel = router.SelectOptimalModel("writing", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Balanced);
            string classificationModel = router.SelectOptimalModel("classification", "hard", OptimizedMultiConnectorRouter.RoutingStrategy.Balanced);

            // Assert
            Assert.Equal("anthropic/claude-3.7-sonnet", codeModel);
            Assert.Equal("google/gemini-pro-1.5", summaryModel);
            Assert.Equal("gpt-4o-mini", reasoningModel);
            Assert.Equal("anthropic/claude-3.7-sonnet", writingModel);
            Assert.Equal("google/gemini-pro-1.5", classificationModel);
        }

        [Fact]
        public void ModelSpecificPromptTransformer_TransformsPrompt_ForDifferentModels()
        {
            // Arrange
            var transformer = new ModelSpecificPromptTransformer();
            string originalPrompt = "Écrivez une fonction qui calcule la factorielle d'un nombre";
            var context = new Dictionary<string, object>
            {
                { "context", "Développement d'une bibliothèque mathématique" },
                { "objective", "Implémenter une fonction de calcul de factorielle efficace" },
                { "output_format", "Code Python avec documentation" },
                { "examples", "Exemple de fonction factorielle" }
            };

            // Act
            string gptPrompt = transformer.TransformPrompt(originalPrompt, "gpt-4o", context);
            string claudePrompt = transformer.TransformPrompt(originalPrompt, "anthropic/claude-3.7-sonnet", context);
            string geminiPrompt = transformer.TransformPrompt(originalPrompt, "google/gemini-pro-1.5", context);
            string qwenPrompt = transformer.TransformPrompt(originalPrompt, "qwen/qwen3-32b", context);

            // Assert
            Assert.Contains("Contexte: Développement d'une bibliothèque mathématique", gptPrompt);
            Assert.Contains("Objectif: Implémenter une fonction de calcul de factorielle efficace", gptPrompt);
            Assert.Contains("Format de sortie attendu:", gptPrompt);

            Assert.Contains("<instructions>", claudePrompt);
            Assert.Contains("</instructions>", claudePrompt);
            Assert.Contains("<format>", claudePrompt);
            Assert.Contains("<examples>", claudePrompt);

            Assert.Contains("Assurez-vous de fournir une réponse concise et directe", geminiPrompt);

            Assert.Contains("Voici la tâche à accomplir:", qwenPrompt);
            Assert.Contains("Voici quelques exemples pour vous guider:", qwenPrompt);
            Assert.Contains("Veuillez suivre un raisonnement étape par étape", qwenPrompt);
        }

        [Fact]
        public async Task ModelCascadeStrategy_ExecutesWithFallback_WhenPrimaryModelFails()
        {
            // Arrange
            var router = new OptimizedMultiConnectorRouter();

            // Mock pour le logger
            var loggerMock = new Mock<ILogger>();

            // Mock pour les TextCompletion
            var primaryTextCompletionMock = new Mock<ITextCompletion>();
            primaryTextCompletionMock
                .Setup(m => m.CompleteAsync(It.IsAny<string>(), It.IsAny<AIRequestSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Erreur simulée du modèle primaire"));

            var fallbackTextCompletionMock = new Mock<ITextCompletion>();
            fallbackTextCompletionMock
                .Setup(m => m.CompleteAsync(It.IsAny<string>(), It.IsAny<AIRequestSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Réponse du modèle de fallback");

            // Mock pour le routeur
            var routerMock = new Mock<OptimizedMultiConnectorRouter>();
            routerMock
                .Setup(m => m.SelectOptimalModel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OptimizedMultiConnectorRouter.RoutingStrategy>()))
                .Returns("primary-model");

            routerMock
                .Setup(m => m.GetTextCompletionForModel("primary-model"))
                .Returns(primaryTextCompletionMock.Object);

            routerMock
                .Setup(m => m.GetTextCompletionForModel(It.IsAny<string>()))
                .Returns(fallbackTextCompletionMock.Object);

            var cascadeStrategy = new ModelCascadeStrategy(routerMock.Object, loggerMock.Object);

            // Act
            string result = await cascadeStrategy.ExecuteWithFallbackAsync(
                "Test prompt",
                "code",
                "medium",
                OptimizedMultiConnectorRouter.RoutingStrategy.Performance);

            // Assert
            Assert.Equal("Réponse du modèle de fallback", result);

            // Vérifier que le modèle primaire a été appelé
            primaryTextCompletionMock.Verify(
                m => m.CompleteAsync(It.IsAny<string>(), It.IsAny<AIRequestSettings>(), It.IsAny<CancellationToken>()),
                Times.Once);

            // Vérifier qu'au moins un modèle de fallback a été appelé
            fallbackTextCompletionMock.Verify(
                m => m.CompleteAsync(It.IsAny<string>(), It.IsAny<AIRequestSettings>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ModelCascadeStrategy_ThrowsException_WhenAllModelsFail()
        {
            // Arrange
            var router = new OptimizedMultiConnectorRouter();

            // Mock pour le logger
            var loggerMock = new Mock<ILogger>();

            // Mock pour les TextCompletion qui échouent tous
            var failingTextCompletionMock = new Mock<ITextCompletion>();
            failingTextCompletionMock
                .Setup(m => m.CompleteAsync(It.IsAny<string>(), It.IsAny<AIRequestSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Erreur simulée"));

            // Mock pour le routeur
            var routerMock = new Mock<OptimizedMultiConnectorRouter>();
            routerMock
                .Setup(m => m.SelectOptimalModel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OptimizedMultiConnectorRouter.RoutingStrategy>()))
                .Returns("primary-model");

            routerMock
                .Setup(m => m.GetTextCompletionForModel(It.IsAny<string>()))
                .Returns(failingTextCompletionMock.Object);

            var cascadeStrategy = new ModelCascadeStrategy(routerMock.Object, loggerMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Microsoft.SemanticKernel.Diagnostics.SKException>(
                () => cascadeStrategy.ExecuteWithFallbackAsync(
                    "Test prompt",
                    "code",
                    "medium",
                    OptimizedMultiConnectorRouter.RoutingStrategy.Performance));

            Assert.Contains("Tous les modèles ont échoué", exception.Message);
        }
    }
}
