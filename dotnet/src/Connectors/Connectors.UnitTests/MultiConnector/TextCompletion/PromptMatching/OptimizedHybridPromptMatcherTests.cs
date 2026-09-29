// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion.PromptMatching
{
    /// <summary>
    /// Tests unitaires pour vérifier le fonctionnement du matcher optimisé
    /// </summary>
    public class OptimizedHybridPromptMatcherTests
    {
        private readonly ITestOutputHelper _output;

        public OptimizedHybridPromptMatcherTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void MatchesRegexPatterns_Correctly()
        {
            // Arrange
            var matcher = new OptimizedHybridPromptMatcher();

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

            var settings2 = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = regexSignature2,
                    PromptName = "test_pattern",
                    Instances = { "Test complex Pattern" }
                }
            };

            var settings3 = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = prefixSignature,
                    PromptName = "simple_prefix",
                    Instances = { "Simple prefix text" }
                }
            };

            // Ajouter les paramètres au matcher
            matcher.AddPrompt(regexSignature1, settings1);
            matcher.AddPrompt(regexSignature2, settings2);
            matcher.AddPrompt(prefixSignature, settings3);

            // Act & Assert
            // Tester avec un prompt qui correspond au pattern regex1
            var job1 = new CompletionJob("Hello beautiful World", new PromptExecutionSettings());
            var result1 = matcher.MatchPromptSettings(job1, new List<PromptMultiConnectorSettings>());
            Assert.Equal(settings1, result1);

            // Tester avec un prompt qui correspond au pattern regex2
            var job2 = new CompletionJob("Test complex Pattern with additional text", new PromptExecutionSettings());
            var result2 = matcher.MatchPromptSettings(job2, new List<PromptMultiConnectorSettings>());
            Assert.Equal(settings2, result2);

            // Tester avec un prompt qui correspond au préfixe simple
            var job3 = new CompletionJob("Simple prefix with more text", new PromptExecutionSettings());
            var result3 = matcher.MatchPromptSettings(job3, new List<PromptMultiConnectorSettings>());
            Assert.Equal(settings3, result3);

            // Tester avec un prompt qui ne correspond à aucun pattern
            var job4 = new CompletionJob("No match for any pattern", new PromptExecutionSettings());
            var result4 = matcher.MatchPromptSettings(job4, new List<PromptMultiConnectorSettings>());
            Assert.Null(result4);
        }

        [Fact]
        public void CombinedRegexGroups_MatchCorrectly()
        {
            // Arrange
            var matcher = new OptimizedHybridPromptMatcher();

            // Créer plusieurs signatures de prompts avec des patterns regex similaires
            // pour tester la fonctionnalité de combinaison des regex.
            // Le \b rend chaque pattern non ambigu : sans lui, "Pattern1.*Test" matche aussi
            // "Pattern10 complex Test", et le premier pattern enregistré l'emporte légitimement.
            var patterns = new[]
            {
                @"Pattern1\b.*Test",
                @"Pattern2\b.*Test",
                @"Pattern3\b.*Test",
                @"Pattern4\b.*Test",
                @"Pattern5\b.*Test",
                @"Pattern6\b.*Test",
                @"Pattern7\b.*Test",
                @"Pattern8\b.*Test",
                @"Pattern9\b.*Test",
                @"Pattern10\b.*Test",
                @"Pattern11\b.*Test",
                @"Pattern12\b.*Test"
            };

            var settingsList = new List<PromptMultiConnectorSettings>();

            // Ajouter les patterns au matcher
            for (int i = 0; i < patterns.Length; i++)
            {
                var signature = new PromptSignature
                {
                    PromptStart = patterns[i],
                    RequestSettings = new PromptExecutionSettings()
                };

                var settings = new PromptMultiConnectorSettings
                {
                    PromptType = new PromptType
                    {
                        Signature = signature,
                        PromptName = $"pattern_{i+1}",
                        Instances = { $"Pattern{i+1} complex Test" }
                    }
                };

                matcher.AddPrompt(signature, settings);
                settingsList.Add(settings);
            }

            // Act & Assert
            // Tester chaque pattern individuellement
            for (int i = 0; i < patterns.Length; i++)
            {
                var job = new CompletionJob($"Pattern{i+1} complex Test with additional text", new PromptExecutionSettings());
                var result = matcher.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());
                Assert.Equal(settingsList[i], result);
            }
        }

        [Fact]
        public void ParallelRegexMatching_HandlesMultiplePatterns()
        {
            // Arrange
            var matcher = new OptimizedHybridPromptMatcher();

            // Créer un grand nombre de patterns regex pour tester le traitement parallèle
            var patternCount = 20; // Suffisamment grand pour déclencher le traitement parallèle
            var settingsList = new List<PromptMultiConnectorSettings>();

            for (int i = 0; i < patternCount; i++)
            {
                // \b : sans lui, "Complex.*Pattern1" matche aussi "Complex Test Pattern10".
                var pattern = $@"Complex.*Pattern{i}\b";
                var signature = new PromptSignature
                {
                    PromptStart = pattern,
                    RequestSettings = new PromptExecutionSettings()
                };

                var settings = new PromptMultiConnectorSettings
                {
                    PromptType = new PromptType
                    {
                        Signature = signature,
                        PromptName = $"complex_pattern_{i}",
                        Instances = { $"Complex Test Pattern{i}" }
                    }
                };

                matcher.AddPrompt(signature, settings);
                settingsList.Add(settings);
            }

            // Act & Assert
            // Tester un pattern au milieu de la liste pour s'assurer que le traitement parallèle fonctionne
            var middleIndex = patternCount / 2;
            var job = new CompletionJob($"Complex Test Pattern{middleIndex} with additional text", new PromptExecutionSettings());
            var result = matcher.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());

            Assert.Equal(settingsList[middleIndex], result);
        }

        [Fact]
        public void RemovePrompt_RemovesCorrectly()
        {
            // Arrange
            var matcher = new OptimizedHybridPromptMatcher();

            var regexSignature = new PromptSignature
            {
                PromptStart = "Test.*Pattern",
                RequestSettings = new PromptExecutionSettings()
            };

            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = regexSignature,
                    PromptName = "test_pattern",
                    Instances = { "Test complex Pattern" }
                }
            };

            matcher.AddPrompt(regexSignature, settings);

            // Vérifier que le pattern est bien ajouté
            var job = new CompletionJob("Test complex Pattern", new PromptExecutionSettings());
            var resultBefore = matcher.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());
            Assert.Equal(settings, resultBefore);

            // Act
            var removed = matcher.RemovePrompt(regexSignature);

            // Assert
            Assert.True(removed);

            // Vérifier que le pattern a bien été supprimé
            var resultAfter = matcher.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());
            Assert.Null(resultAfter);
        }

        [Fact]
        public void Clear_RemovesAllPrompts()
        {
            // Arrange
            var matcher = new OptimizedHybridPromptMatcher();

            // Ajouter plusieurs patterns
            for (int i = 0; i < 5; i++)
            {
                var signature = new PromptSignature
                {
                    PromptStart = $"Pattern{i}",
                    RequestSettings = new PromptExecutionSettings()
                };

                var settings = new PromptMultiConnectorSettings
                {
                    PromptType = new PromptType
                    {
                        Signature = signature,
                        PromptName = $"pattern_{i}",
                        Instances = { $"Pattern{i} text" }
                    }
                };

                matcher.AddPrompt(signature, settings);
            }

            // Vérifier que le count est correct
            Assert.Equal(5, matcher.Count);

            // Act
            matcher.Clear();

            // Assert
            Assert.Equal(0, matcher.Count);

            // Vérifier qu'aucun pattern ne matche
            for (int i = 0; i < 5; i++)
            {
                var job = new CompletionJob($"Pattern{i} text", new PromptExecutionSettings());
                var result = matcher.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());
                Assert.Null(result);
            }
        }
    

        [Fact]
        public void OverlappingPatterns_FirstRegisteredWins()
        {
            // Arrange : le premier pattern enregistré matche plus à droite que le second.
            // Une alternance nue rendrait le second (match le plus à gauche) ; le contrat est
            // celui du parcours séquentiel : le premier enregistré qui matche l'emporte.
            var matcher = new OptimizedHybridPromptMatcher();
            var first = CreateRegexSettings("suffix.*end", "first");
            var second = CreateRegexSettings("^start", "second");
            matcher.AddPrompt(first.PromptType.Signature, first);
            matcher.AddPrompt(second.PromptType.Signature, second);

            // Act
            var job = new CompletionJob("start of the prompt, then a suffix and the end", new PromptExecutionSettings());
            var result = matcher.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.Same(first, result);
        }

        [Fact]
        public void OverlappingPatterns_FirstRegisteredWinsAcrossGroups()
        {
            // Arrange : plus de patterns qu'un groupe combiné n'en contient, tous ambigus entre eux.
            var matcher = new OptimizedHybridPromptMatcher();
            var settingsList = new List<PromptMultiConnectorSettings>();
            for (int i = 1; i <= 25; i++)
            {
                var settings = CreateRegexSettings($"Pattern{i}.*Test", $"pattern_{i}");
                matcher.AddPrompt(settings.PromptType.Signature, settings);
                settingsList.Add(settings);
            }

            // Act : "Pattern1.*Test" matche aussi "Pattern10", "Pattern11"... et il est enregistré en premier.
            var job = new CompletionJob("Pattern10 complex Test", new PromptExecutionSettings());
            var result = matcher.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.Same(settingsList[0], result);
        }

        [Fact]
        public void AddPrompt_SamePatternReplacesSettings()
        {
            // Arrange
            var matcher = new OptimizedHybridPromptMatcher();
            var original = CreateRegexSettings("Test.*Pattern", "original");
            var replacement = CreateRegexSettings("Test.*Pattern", "replacement");
            matcher.AddPrompt(original.PromptType.Signature, original);
            matcher.AddPrompt(replacement.PromptType.Signature, replacement);

            // Act
            var job = new CompletionJob("Test complex Pattern", new PromptExecutionSettings());
            var result = matcher.MatchPromptSettings(job, new List<PromptMultiConnectorSettings>());

            // Assert : le groupe combiné ne doit pas garder les anciens paramètres.
            Assert.Same(replacement, result);
            Assert.Equal(1, matcher.Count);
        }

        private static PromptMultiConnectorSettings CreateRegexSettings(string pattern, string name)
        {
            return new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = new PromptSignature
                    {
                        PromptStart = pattern,
                        RequestSettings = new PromptExecutionSettings()
                    },
                    PromptName = name
                }
            };
        }
    }
}
