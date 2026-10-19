// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion
{
    public class OptimizedPromptMatcherIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public OptimizedPromptMatcherIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task MultiConnector_WithOptimizedMatcher_RoutesCorrectly()
        {
            // Arrange
            // Créer des connecteurs mock pour les tests
            var mockConnector1 = new MockTextCompletion("connector1");
            var mockConnector2 = new MockTextCompletion("connector2");
            var mockConnector3 = new MockTextCompletion("connector3");

            var namedConnector1 = new NamedTextCompletion("connector1", mockConnector1);
            var namedConnector2 = new NamedTextCompletion("connector2", mockConnector2);
            var namedConnector3 = new NamedTextCompletion("connector3", mockConnector3);

            // Créer des signatures de prompts avec des patterns regex
            var regexSignature1 = new PromptSignature
            {
                PromptStart = "Hello.*World",
                RequestSettings = new PromptExecutionSettings()
            };

            var regexSignature2 = new PromptSignature
            {
                PromptStart = "Test.*Pattern",
                RequestSettings = new PromptExecutionSettings()
            };

            var prefixSignature = new PromptSignature
            {
                PromptStart = "Simple prefix",
                RequestSettings = new PromptExecutionSettings()
            };

            // Créer les paramètres pour chaque type de prompt
            var settings1 = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = regexSignature1,
                    PromptName = "hello_world_pattern",
                    Instances = { "Hello beautiful World" }
                }
            };
            // Ajouter les paramètres du connecteur
            var connectorSettings1 = settings1.GetConnectorSettings("connector1");
            connectorSettings1.VettingLevel = VettingLevel.Oracle;

            var settings2 = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = regexSignature2,
                    PromptName = "test_pattern",
                    Instances = { "Test complex Pattern" }
                }
            };
            // Ajouter les paramètres du connecteur
            var connectorSettings2 = settings2.GetConnectorSettings("connector2");
            connectorSettings2.VettingLevel = VettingLevel.Oracle;

            var settings3 = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = prefixSignature,
                    PromptName = "simple_prefix",
                    Instances = { "Simple prefix text" }
                }
            };
            // Ajouter les paramètres du connecteur
            var connectorSettings3 = settings3.GetConnectorSettings("connector3");
            connectorSettings3.VettingLevel = VettingLevel.Oracle;

            // Créer le MultiTextCompletionSettings avec le matcher optimisé
            var multiSettings = new MultiTextCompletionSettings
            {
                PromptMultiConnectorSettings = new List<PromptMultiConnectorSettings>
                {
                    settings1,
                    settings2,
                    settings3
                }
            };

            // Utiliser le matcher optimisé
            multiSettings.UseOptimizedHybridPromptMatcher();

            // Créer le MultiConnector
            var multiConnector = new MultiTextCompletion(
                multiSettings,
                namedConnector1,
                CancellationToken.None,
                null,
                namedConnector2,
                namedConnector3);

            // Act & Assert
            // Tester avec un prompt qui correspond au pattern regex1
            var result1 = await multiConnector.CompleteAsync("Hello beautiful World", new PromptExecutionSettings(), CancellationToken.None);
            Assert.Contains("connector1", result1, StringComparison.Ordinal);

            // Tester avec un prompt qui correspond au pattern regex2
            var result2 = await multiConnector.CompleteAsync("Test complex Pattern with additional text", new PromptExecutionSettings(), CancellationToken.None);
            Assert.Contains("connector2", result2, StringComparison.Ordinal);

            // Tester avec un prompt qui correspond au préfixe simple
            var result3 = await multiConnector.CompleteAsync("Simple prefix with more text", new PromptExecutionSettings(), CancellationToken.None);
            Assert.Contains("connector3", result3, StringComparison.Ordinal);

            // Tester avec un prompt qui ne correspond à aucun pattern (devrait utiliser le connecteur par défaut)
            var result4 = await multiConnector.CompleteAsync("No match for any pattern", new PromptExecutionSettings(), CancellationToken.None);
            Assert.Contains("connector1", result4, StringComparison.Ordinal);
        }

        /// <summary>
        /// Mock simple d'un connecteur de génération de texte pour les tests
        /// </summary>
        private sealed class MockTextCompletion : ITextGenerationService
        {
            private readonly string _name;

            public MockTextCompletion(string name)
            {
                _name = name;
            }

            public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

            public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<TextContent>>(new[] { new TextContent($"Result from {_name}") });
            }

            public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                foreach (var content in await GetTextContentsAsync(prompt, executionSettings, kernel, cancellationToken).ConfigureAwait(false))
                {
                    yield return new StreamingTextContent(content.Text);
                }
            }
        }
    }
}
