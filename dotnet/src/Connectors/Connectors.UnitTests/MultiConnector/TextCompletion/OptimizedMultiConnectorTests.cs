// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
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
            Assert.Contains("Contexte: Développement d'une bibliothèque mathématique", gptPrompt, StringComparison.Ordinal);
            Assert.Contains("Objectif: Implémenter une fonction de calcul de factorielle efficace", gptPrompt, StringComparison.Ordinal);
            Assert.Contains("Format de sortie attendu:", gptPrompt, StringComparison.Ordinal);

            Assert.Contains("<instructions>", claudePrompt, StringComparison.Ordinal);
            Assert.Contains("</instructions>", claudePrompt, StringComparison.Ordinal);
            Assert.Contains("<format>", claudePrompt, StringComparison.Ordinal);
            Assert.Contains("<examples>", claudePrompt, StringComparison.Ordinal);

            Assert.Contains("Assurez-vous de fournir une réponse concise et directe", geminiPrompt, StringComparison.Ordinal);

            Assert.Contains("Voici la tâche à accomplir:", qwenPrompt, StringComparison.Ordinal);
            Assert.Contains("Voici quelques exemples pour vous guider:", qwenPrompt, StringComparison.Ordinal);
            Assert.Contains("Veuillez suivre un raisonnement étape par étape", qwenPrompt, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ModelCascadeStrategy_ExecutesWithFallback_WhenPrimaryModelFails()
        {
            // Arrange : le modèle choisi par le routeur échoue, les autres répondent.
            // La version de mai 2025 simulait le routeur avec Moq sur des méthodes non virtuelles,
            // ce que Moq refuse : le routeur réel reçoit désormais une fabrique de services.
            string primaryModel = new OptimizedMultiConnectorRouter().SelectOptimalModel("code", "medium", OptimizedMultiConnectorRouter.RoutingStrategy.Performance);
            var primary = CreateFailingService(new InvalidOperationException("Erreur simulée du modèle primaire"));
            var fallback = CreateAnsweringService("Réponse du modèle de fallback");
            var router = new OptimizedMultiConnectorRouter(model => model == primaryModel ? primary.Object : fallback.Object);
            var cascadeStrategy = new ModelCascadeStrategy(router, new Mock<ILogger>().Object);

            // Act
            string result = await cascadeStrategy.ExecuteWithFallbackAsync(
                "Test prompt",
                "code",
                "medium",
                OptimizedMultiConnectorRouter.RoutingStrategy.Performance);

            // Assert : le modèle primaire est essayé une fois, la cascade s'arrête au premier succès.
            Assert.Equal("Réponse du modèle de fallback", result);
            primary.Verify(TextContentsCall(), Times.Once);
            fallback.Verify(TextContentsCall(), Times.Once);
        }

        [Fact]
        public async Task ModelCascadeStrategy_ThrowsException_WhenAllModelsFail()
        {
            // Arrange
            var failing = CreateFailingService(new InvalidOperationException("Erreur simulée"));
            var router = new OptimizedMultiConnectorRouter(_ => failing.Object);
            var cascadeStrategy = new ModelCascadeStrategy(router, new Mock<ILogger>().Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KernelException>(
                () => cascadeStrategy.ExecuteWithFallbackAsync(
                    "Test prompt",
                    "code",
                    "medium",
                    OptimizedMultiConnectorRouter.RoutingStrategy.Performance));

            Assert.Contains("Tous les modèles ont échoué", exception.Message, StringComparison.Ordinal);
            Assert.Equal("Erreur simulée", exception.InnerException?.Message);
        }

        [Fact]
        public async Task ModelCascadeStrategy_StopsOnCancellation()
        {
            // Arrange : une annulation ne doit pas être traitée comme l'échec d'un modèle.
            var cancelled = CreateFailingService(new OperationCanceledException());
            var fallback = CreateAnsweringService("Ne doit pas être appelé");
            var router = new OptimizedMultiConnectorRouter(model => model == "gpt-4o" ? cancelled.Object : fallback.Object);
            var cascadeStrategy = new ModelCascadeStrategy(router);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => cascadeStrategy.ExecuteWithFallbackAsync(
                    "Test prompt",
                    "code",
                    "hard",
                    OptimizedMultiConnectorRouter.RoutingStrategy.Performance));

            fallback.Verify(TextContentsCall(), Times.Never);
        }

        [Fact]
        public void OptimizedMultiConnectorRouter_WithoutFactory_CannotProvideServices()
        {
            var router = new OptimizedMultiConnectorRouter();

            Assert.Throws<InvalidOperationException>(() => router.GetTextCompletionForModel("gpt-4o"));
        }

        private static System.Linq.Expressions.Expression<Func<ITextGenerationService, Task<IReadOnlyList<TextContent>>>> TextContentsCall()
        {
            return m => m.GetTextContentsAsync(It.IsAny<string>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>());
        }

        private static Mock<ITextGenerationService> CreateAnsweringService(string answer)
        {
            var service = new Mock<ITextGenerationService>();
            service.Setup(TextContentsCall()).ReturnsAsync(new List<TextContent> { new(answer) });
            return service;
        }

        private static Mock<ITextGenerationService> CreateFailingService(Exception exception)
        {
            var service = new Mock<ITextGenerationService>();
            service.Setup(TextContentsCall()).ThrowsAsync(exception);
            return service;
        }
    }
}
