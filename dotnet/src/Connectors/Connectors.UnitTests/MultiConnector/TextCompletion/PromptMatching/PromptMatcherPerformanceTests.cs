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
    public class PromptMatcherPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PromptMatcherPerformanceTests(ITestOutputHelper output)
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
        public void ComparePerformance_SmallDataset()
        {
            // Arrange
            const int datasetSize = 100;
            var testData = GenerateTestData(datasetSize);

            // Créer les matchers
            var sequentialMatcher = new SequentialPromptMatcher();
            var radixTreeMatcher = new RadixTreePromptMatcher();
            var hybridMatcher = new HybridPromptMatcher();

            // Ajouter les données de test aux matchers
            var settings = new List<PromptMultiConnectorSettings>();
            foreach (var data in testData)
            {
                var (signature, promptSettings) = CreatePromptWithSettings(data.Item1);
                sequentialMatcher.AddPrompt(signature, promptSettings);
                radixTreeMatcher.AddPrompt(signature, promptSettings);
                hybridMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Créer les jobs de complétion pour les tests
            var jobs = testData.Select(data => CreateCompletionJob(data.Item2)).ToList();

            // Act
            var sequentialResult = MeasurePerformance(sequentialMatcher, jobs, settings);
            var radixTreeResult = MeasurePerformance(radixTreeMatcher, jobs, settings);
            var hybridResult = MeasurePerformance(hybridMatcher, jobs, settings);

            // Assert - Pas d'assertions strictes, juste des logs
            _output.WriteLine($"Performance comparison for {datasetSize} prompts:");
            _output.WriteLine($"Sequential: Total={sequentialResult.TotalTime.TotalMilliseconds:F2}ms, Avg={sequentialResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"RadixTree: Total={radixTreeResult.TotalTime.TotalMilliseconds:F2}ms, Avg={radixTreeResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"Hybrid: Total={hybridResult.TotalTime.TotalMilliseconds:F2}ms, Avg={hybridResult.AverageTime.TotalMilliseconds:F4}ms");

            // Calculer les ratios d'amélioration
            var sequentialToRadixRatio = sequentialResult.TotalTime.TotalMilliseconds / radixTreeResult.TotalTime.TotalMilliseconds;
            var sequentialToHybridRatio = sequentialResult.TotalTime.TotalMilliseconds / hybridResult.TotalTime.TotalMilliseconds;

            _output.WriteLine($"RadixTree is {sequentialToRadixRatio:F2}x faster than Sequential");
            _output.WriteLine($"Hybrid is {sequentialToHybridRatio:F2}x faster than Sequential");

            // Vérifier que les implémentations optimisées sont plus rapides
            Assert.True(radixTreeResult.TotalTime < sequentialResult.TotalTime, "RadixTree should be faster than Sequential");
        }

        [Fact]
        public void ComparePerformance_MediumDataset()
        {
            // Arrange
            const int datasetSize = 1000;
            var testData = GenerateTestData(datasetSize);

            // Créer les matchers
            var sequentialMatcher = new SequentialPromptMatcher();
            var radixTreeMatcher = new RadixTreePromptMatcher();
            var hybridMatcher = new HybridPromptMatcher();

            // Ajouter les données de test aux matchers
            var settings = new List<PromptMultiConnectorSettings>();
            foreach (var data in testData)
            {
                var (signature, promptSettings) = CreatePromptWithSettings(data.Item1);
                sequentialMatcher.AddPrompt(signature, promptSettings);
                radixTreeMatcher.AddPrompt(signature, promptSettings);
                hybridMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Créer les jobs de complétion pour les tests
            var jobs = testData.Select(data => CreateCompletionJob(data.Item2)).ToList();

            // Act
            var sequentialResult = MeasurePerformance(sequentialMatcher, jobs, settings);
            var radixTreeResult = MeasurePerformance(radixTreeMatcher, jobs, settings);
            var hybridResult = MeasurePerformance(hybridMatcher, jobs, settings);

            // Assert - Pas d'assertions strictes, juste des logs
            _output.WriteLine($"Performance comparison for {datasetSize} prompts:");
            _output.WriteLine($"Sequential: Total={sequentialResult.TotalTime.TotalMilliseconds:F2}ms, Avg={sequentialResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"RadixTree: Total={radixTreeResult.TotalTime.TotalMilliseconds:F2}ms, Avg={radixTreeResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"Hybrid: Total={hybridResult.TotalTime.TotalMilliseconds:F2}ms, Avg={hybridResult.AverageTime.TotalMilliseconds:F4}ms");

            // Calculer les ratios d'amélioration
            var sequentialToRadixRatio = sequentialResult.TotalTime.TotalMilliseconds / radixTreeResult.TotalTime.TotalMilliseconds;
            var sequentialToHybridRatio = sequentialResult.TotalTime.TotalMilliseconds / hybridResult.TotalTime.TotalMilliseconds;

            _output.WriteLine($"RadixTree is {sequentialToRadixRatio:F2}x faster than Sequential");
            _output.WriteLine($"Hybrid is {sequentialToHybridRatio:F2}x faster than Sequential");

            // Vérifier que les implémentations optimisées sont plus rapides
            Assert.True(radixTreeResult.TotalTime < sequentialResult.TotalTime, "RadixTree should be faster than Sequential");

            // Pour les datasets moyens, l'amélioration devrait être plus significative
            Assert.True(sequentialToRadixRatio > 2, "RadixTree should be at least 2x faster than Sequential for medium datasets");
        }

        [Fact]
        public void ComparePerformance_WithRegexPatterns()
        {
            // Arrange
            const int datasetSize = 100;
            var testData = GenerateTestData(datasetSize);

            // Créer les matchers
            var sequentialMatcher = new SequentialPromptMatcher();
            var radixTreeMatcher = new RadixTreePromptMatcher();
            var hybridMatcher = new HybridPromptMatcher();

            // Ajouter les données de test aux matchers
            var settings = new List<PromptMultiConnectorSettings>();

            // Ajouter des patterns normaux
            foreach (var data in testData.Take(datasetSize - 10))
            {
                var (signature, promptSettings) = CreatePromptWithSettings(data.Item1);
                sequentialMatcher.AddPrompt(signature, promptSettings);
                radixTreeMatcher.AddPrompt(signature, promptSettings);
                hybridMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Ajouter quelques patterns regex
            for (int i = 0; i < 10; i++)
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

                sequentialMatcher.AddPrompt(signature, promptSettings);
                // RadixTree ne peut pas gérer les regex directement
                hybridMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Créer les jobs de complétion pour les tests
            var normalJobs = testData.Take(datasetSize - 10).Select(data => CreateCompletionJob(data.Item2)).ToList();
            var regexJobs = Enumerable.Range(0, 10).Select(i => CreateCompletionJob($"Test Regex Pattern{i} with additional text")).ToList();
            var allJobs = normalJobs.Concat(regexJobs).ToList();

            // Act
            var sequentialResult = MeasurePerformance(sequentialMatcher, allJobs, settings);
            var radixTreeResult = MeasurePerformance(radixTreeMatcher, allJobs, settings);
            var hybridResult = MeasurePerformance(hybridMatcher, allJobs, settings);

            // Assert - Pas d'assertions strictes, juste des logs
            _output.WriteLine($"Performance comparison with regex patterns:");
            _output.WriteLine($"Sequential: Total={sequentialResult.TotalTime.TotalMilliseconds:F2}ms, Avg={sequentialResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"RadixTree: Total={radixTreeResult.TotalTime.TotalMilliseconds:F2}ms, Avg={radixTreeResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"Hybrid: Total={hybridResult.TotalTime.TotalMilliseconds:F2}ms, Avg={hybridResult.AverageTime.TotalMilliseconds:F4}ms");

            // Calculer les ratios d'amélioration
            var sequentialToHybridRatio = sequentialResult.TotalTime.TotalMilliseconds / hybridResult.TotalTime.TotalMilliseconds;

            _output.WriteLine($"Hybrid is {sequentialToHybridRatio:F2}x faster than Sequential with regex patterns");

            // Vérifier que l'implémentation hybride est plus rapide
            Assert.True(hybridResult.TotalTime < sequentialResult.TotalTime, "Hybrid should be faster than Sequential with regex patterns");
        }

        [Fact]
        public void ComparePerformance_WorstCaseScenario()
        {
            // Arrange - Créer un scénario où tous les prompts commencent par le même préfixe
            const int datasetSize = 100;
            var commonPrefix = "CommonPrefix";

            var testData = new List<(string, string)>();
            for (int i = 0; i < datasetSize; i++)
            {
                var prefix = commonPrefix + i;
                var fullPrompt = prefix + " with additional text";
                testData.Add((prefix, fullPrompt));
            }

            // Créer les matchers
            var sequentialMatcher = new SequentialPromptMatcher();
            var radixTreeMatcher = new RadixTreePromptMatcher();
            var hybridMatcher = new HybridPromptMatcher();

            // Ajouter les données de test aux matchers
            var settings = new List<PromptMultiConnectorSettings>();
            foreach (var data in testData)
            {
                var (signature, promptSettings) = CreatePromptWithSettings(data.Item1);
                sequentialMatcher.AddPrompt(signature, promptSettings);
                radixTreeMatcher.AddPrompt(signature, promptSettings);
                hybridMatcher.AddPrompt(signature, promptSettings);
                settings.Add(promptSettings);
            }

            // Créer les jobs de complétion pour les tests
            var jobs = testData.Select(data => CreateCompletionJob(data.Item2)).ToList();

            // Act
            var sequentialResult = MeasurePerformance(sequentialMatcher, jobs, settings);
            var radixTreeResult = MeasurePerformance(radixTreeMatcher, jobs, settings);
            var hybridResult = MeasurePerformance(hybridMatcher, jobs, settings);

            // Assert - Pas d'assertions strictes, juste des logs
            _output.WriteLine($"Performance comparison for worst case scenario (common prefix):");
            _output.WriteLine($"Sequential: Total={sequentialResult.TotalTime.TotalMilliseconds:F2}ms, Avg={sequentialResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"RadixTree: Total={radixTreeResult.TotalTime.TotalMilliseconds:F2}ms, Avg={radixTreeResult.AverageTime.TotalMilliseconds:F4}ms");
            _output.WriteLine($"Hybrid: Total={hybridResult.TotalTime.TotalMilliseconds:F2}ms, Avg={hybridResult.AverageTime.TotalMilliseconds:F4}ms");

            // Même dans le pire des cas, RadixTree devrait être plus rapide
            Assert.True(radixTreeResult.TotalTime < sequentialResult.TotalTime, "RadixTree should be faster than Sequential even in worst case");
        }
    }
}
