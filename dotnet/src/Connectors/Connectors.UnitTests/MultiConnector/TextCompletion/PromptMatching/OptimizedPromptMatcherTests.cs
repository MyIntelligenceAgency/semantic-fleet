// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion.PromptMatching
{
    public class OptimizedPromptMatcherTests
    {
        private readonly ITestOutputHelper _output;

        public OptimizedPromptMatcherTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // Méthode utilitaire pour créer des signatures de prompts et des paramètres de test
        private (PromptSignature Signature, PromptMultiConnectorSettings Settings) CreatePromptWithSettings(string promptStart)
        {
            var signature = new PromptSignature
            {
                PromptStart = promptStart,
                RequestSettings = new AIRequestSettings()
            };

            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = promptStart.Replace(" ", "_"),
                    Instances = { promptStart + " additional text" }
                }
            };

            return (signature, settings);
        }

        // Méthode utilitaire pour créer un job de complétion
        private CompletionJob CreateCompletionJob(string prompt)
        {
            return new CompletionJob(prompt, new AIRequestSettings());
        }

        // Méthode utilitaire pour générer des données de test
        private List<(string Prefix, string FullPrompt)> GenerateTestData(int count, int prefixLength = 20, int fullPromptLength = 100)
        {
            var random = new Random(42); // Seed fixe pour la reproductibilité
            var result = new List<(string, string)>();

            // Générer des préfixes uniques
            var prefixes = new HashSet<string>();
            while (prefixes.Count < count)
            {
                var prefix = GenerateRandomString(random, prefixLength);
                prefixes.Add(prefix);
            }

            // Générer les prompts complets
            foreach (var prefix in prefixes)
            {
                var fullPrompt = prefix + GenerateRandomString(random, fullPromptLength - prefixLength);
                result.Add((prefix, fullPrompt));
            }

            return result;
        }

        // Méthode utilitaire pour générer une chaîne aléatoire
        private string GenerateRandomString(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Méthode utilitaire pour mesurer les performances
        private (TimeSpan TotalTime, TimeSpan AverageTime) MeasurePerformance(IPromptMatcher matcher, List<CompletionJob> jobs, List<PromptMultiConnectorSettings> settings)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var job in jobs)
            {
                matcher.MatchPromptSettings(job, settings);
            }

            stopwatch.Stop();
            var totalTime = stopwatch.Elapsed;
            var averageTime = TimeSpan.FromTicks(totalTime.Ticks / jobs.Count);

            return (totalTime, averageTime);
        }

        [Fact]
        public void ComparePerformance_WithRegexPatterns()
        {
            // Arrange
            const int datasetSize = 100;
            const int regexCount = 20; // Augmenter le nombre de regex pour mieux tester l'optimisation
            var testData = GenerateTestData(datasetSize);

            // Créer les matchers
            var hybridMatcher = new HybridPromptMatcher();
            var optimizedMatcher = new OptimizedHybridPromptMatcher();

            // Ajouter les données de test aux matchers
            var settings = new List<PromptMultiConnectorSettings>();

            // Ajouter des patterns normaux
            foreach (var data in testData.Take(datasetSize - regexCount))
            {
                var (signature, promptSettings) = CreatePromptWithSettings(data.Item1);
                hybridMatcher.AddPrompt(signature, promptSettings);
                optimizedMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Ajouter des patterns regex
            for (int i = 0; i < regexCount; i++)
            {
                var regexPattern = $"Test.*Pattern{i}";
                var signature = new PromptSignature
                {
                    PromptStart = regexPattern,
                    RequestSettings = new AIRequestSettings()
                };

                var promptSettings = new PromptMultiConnectorSettings
                {
                    PromptType = new PromptType
                    {
                        Signature = signature,
                        PromptName = $"regex_pattern_{i}",
                        Instances = { $"Test Regex Pattern{i}" }
                    }
                };

                hybridMatcher.AddPrompt(signature, promptSettings);
                optimizedMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Créer les jobs de complétion pour les tests
            var normalJobs = testData.Take(datasetSize - regexCount).Select(data => CreateCompletionJob(data.Item2)).ToList();
            var regexJobs = Enumerable.Range(0, regexCount).Select(i => CreateCompletionJob($"Test Regex Pattern{i} with additional text")).ToList();
            var allJobs = normalJobs.Concat(regexJobs).ToList();

            // Act
            var hybridResult = MeasurePerformance(hybridMatcher, allJobs, settings);
            var optimizedResult = MeasurePerformance(optimizedMatcher, allJobs, settings);

            // Assert - Pas d'assertions strictes, juste des logs
            _output.WriteLine($"Performance comparison with regex patterns:");
            _output.WriteLine($"Hybrid: Total={hybridResult.TotalTime.TotalMilliseconds:F2}ms, Avg={hybridResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"Optimized: Total={optimizedResult.TotalTime.TotalMilliseconds:F2}ms, Avg={optimizedResult.AverageTime.TotalMilliseconds:F4}ms");

            // Calculer le ratio d'amélioration
            var hybridToOptimizedRatio = hybridResult.TotalTime.TotalMilliseconds / optimizedResult.TotalTime.TotalMilliseconds;

            _output.WriteLine($"Optimized is {hybridToOptimizedRatio:F2}x faster than Hybrid with regex patterns");

            // Vérifier que l'implémentation optimisée est plus rapide
            Assert.True(optimizedResult.TotalTime < hybridResult.TotalTime, "Optimized should be faster than Hybrid with regex patterns");
        }

        [Fact]
        public void ComparePerformance_WithManyRegexPatterns()
        {
            // Arrange - Test avec un grand nombre de regex pour mettre en évidence l'optimisation
            const int regexCount = 50;

            // Créer les matchers
            var hybridMatcher = new HybridPromptMatcher();
            var optimizedMatcher = new OptimizedHybridPromptMatcher();

            // Ajouter des patterns regex
            var settings = new List<PromptMultiConnectorSettings>();
            for (int i = 0; i < regexCount; i++)
            {
                var regexPattern = $"Test.*Pattern{i}";
                var signature = new PromptSignature
                {
                    PromptStart = regexPattern,
                    RequestSettings = new AIRequestSettings()
                };

                var promptSettings = new PromptMultiConnectorSettings
                {
                    PromptType = new PromptType
                    {
                        Signature = signature,
                        PromptName = $"regex_pattern_{i}",
                        Instances = { $"Test Regex Pattern{i}" }
                    }
                };

                hybridMatcher.AddPrompt(signature, promptSettings);
                optimizedMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Créer les jobs de complétion pour les tests - tester avec des prompts qui matchent à différentes positions
            var jobs = new List<CompletionJob>();
            for (int i = 0; i < regexCount; i++)
            {
                // Ajouter un job qui matche le pattern i
                jobs.Add(CreateCompletionJob($"Test Regex Pattern{i} with additional text"));

                // Ajouter un job qui ne matche aucun pattern
                jobs.Add(CreateCompletionJob($"No match for Pattern{i}"));
            }

            // Act
            var hybridResult = MeasurePerformance(hybridMatcher, jobs, settings);
            var optimizedResult = MeasurePerformance(optimizedMatcher, jobs, settings);

            // Assert - Pas d'assertions strictes, juste des logs
            _output.WriteLine($"Performance comparison with many regex patterns ({regexCount}):");
            _output.WriteLine($"Hybrid: Total={hybridResult.TotalTime.TotalMilliseconds:F2}ms, Avg={hybridResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"Optimized: Total={optimizedResult.TotalTime.TotalMilliseconds:F2}ms, Avg={optimizedResult.AverageTime.TotalMilliseconds:F4}ms");

            // Calculer le ratio d'amélioration
            var hybridToOptimizedRatio = hybridResult.TotalTime.TotalMilliseconds / optimizedResult.TotalTime.TotalMilliseconds;

            _output.WriteLine($"Optimized is {hybridToOptimizedRatio:F2}x faster than Hybrid with many regex patterns");

            // Avec un grand nombre de regex, l'amélioration devrait être significative
            Assert.True(hybridToOptimizedRatio > 1.5, "Optimized should be at least 1.5x faster than Hybrid with many regex patterns");
        }

        [Fact]
        public void ComparePerformance_ComplexRegexPatterns()
        {
            // Arrange - Test avec des regex plus complexes
            var complexRegexPatterns = new[]
            {
                @"^Test\s+\w+\s+Pattern\d+$",
                @"^Hello\s+\w{3,10}\s+World$",
                @"^\d{2,4}-\d{2}-\d{2,4}$",
                @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
                @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$"
            };

            // Créer les matchers
            var hybridMatcher = new HybridPromptMatcher();
            var optimizedMatcher = new OptimizedHybridPromptMatcher();

            // Ajouter des patterns regex complexes
            var settings = new List<PromptMultiConnectorSettings>();
            for (int i = 0; i < complexRegexPatterns.Length; i++)
            {
                var regexPattern = complexRegexPatterns[i];
                var signature = new PromptSignature
                {
                    PromptStart = regexPattern,
                    RequestSettings = new AIRequestSettings()
                };

                var promptSettings = new PromptMultiConnectorSettings
                {
                    PromptType = new PromptType
                    {
                        Signature = signature,
                        PromptName = $"complex_regex_{i}",
                        Instances = { $"Complex Regex {i}" }
                    }
                };

                hybridMatcher.AddPrompt(signature, promptSettings);
                optimizedMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Créer les jobs de complétion pour les tests
            var matchingPrompts = new[]
            {
                "Test Simple Pattern123",
                "Hello World World",
                "2023-05-16",
                "user@example.com",
                "https://www.example.com/path/to/resource?query=value"
            };

            var jobs = matchingPrompts.Select(CreateCompletionJob).ToList();

            // Ajouter des prompts qui ne matchent pas
            for (int i = 0; i < 20; i++)
            {
                jobs.Add(CreateCompletionJob($"No match for complex pattern {i}"));
            }

            // Act - Exécuter plusieurs fois pour une meilleure mesure
            const int iterations = 10;
            var hybridTimes = new List<TimeSpan>();
            var optimizedTimes = new List<TimeSpan>();

            for (int i = 0; i < iterations; i++)
            {
                var hybridResult = MeasurePerformance(hybridMatcher, jobs, settings);
                var optimizedResult = MeasurePerformance(optimizedMatcher, jobs, settings);

                hybridTimes.Add(hybridResult.TotalTime);
                optimizedTimes.Add(optimizedResult.TotalTime);
            }

            // Calculer les moyennes
            var avgHybridTime = TimeSpan.FromTicks((long)hybridTimes.Average(t => t.Ticks));
            var avgOptimizedTime = TimeSpan.FromTicks((long)optimizedTimes.Average(t => t.Ticks));

            // Assert - Pas d'assertions strictes, juste des logs
            _output.WriteLine($"Performance comparison with complex regex patterns (average of {iterations} iterations):");
            _output.WriteLine($"Hybrid: Avg Total={avgHybridTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"Optimized: Avg Total={avgOptimizedTime.TotalMilliseconds:F2}ms");

            // Calculer le ratio d'amélioration
            var hybridToOptimizedRatio = avgHybridTime.TotalMilliseconds / avgOptimizedTime.TotalMilliseconds;

            _output.WriteLine($"Optimized is {hybridToOptimizedRatio:F2}x faster than Hybrid with complex regex patterns");

            // Avec des regex complexes, l'amélioration devrait être mesurable
            Assert.True(avgOptimizedTime <= avgHybridTime, "Optimized should not be slower than Hybrid with complex regex patterns");
        }
    }
}
