// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.AI;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion.PromptMatching
{
    public class PromptMatchersTests
    {
        private readonly ITestOutputHelper _output;

        public PromptMatchersTests(ITestOutputHelper output)
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

        #region SequentialPromptMatcher Tests

        [Fact]
        public void SequentialPromptMatcher_Constructor_CreatesEmptyMatcher()
        {
            // Arrange & Act
            var matcher = new SequentialPromptMatcher();

            // Assert
            Assert.Equal(0, matcher.Count);
        }

        [Fact]
        public void SequentialPromptMatcher_AddPrompt_IncreasesCount()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (signature, settings) = CreatePromptWithSettings("Hello");

            // Act
            matcher.AddPrompt(signature, settings);

            // Assert
            Assert.Equal(1, matcher.Count);
        }

        [Fact]
        public void SequentialPromptMatcher_AddPrompt_NullSignature_ThrowsArgumentNullException()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (_, settings) = CreatePromptWithSettings("Hello");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => matcher.AddPrompt(null!, settings));
        }

        [Fact]
        public void SequentialPromptMatcher_AddPrompt_NullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (signature, _) = CreatePromptWithSettings("Hello");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => matcher.AddPrompt(signature, null!));
        }

        [Fact]
        public void SequentialPromptMatcher_AddPrompt_DuplicateSignature_UpdatesSettings()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (signature, settings1) = CreatePromptWithSettings("Hello");
            matcher.AddPrompt(signature, settings1);

            // Create new settings with the same signature
            var settings2 = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = "Updated",
                    Instances = { "Updated instance" }
                }
            };

            // Act
            matcher.AddPrompt(signature, settings2);

            // Assert
            Assert.Equal(1, matcher.Count);
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello world"), new List<PromptMultiConnectorSettings>());
            Assert.Equal("Updated", result?.PromptType.PromptName);
        }

        [Fact]
        public void SequentialPromptMatcher_RemovePrompt_ExistingPrompt_RemovesPromptAndReturnsTrue()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (signature, settings) = CreatePromptWithSettings("Hello");
            matcher.AddPrompt(signature, settings);

            // Act
            var result = matcher.RemovePrompt(signature);

            // Assert
            Assert.True(result);
            Assert.Equal(0, matcher.Count);
        }

        [Fact]
        public void SequentialPromptMatcher_RemovePrompt_NonExistingPrompt_ReturnsFalse()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (signature, _) = CreatePromptWithSettings("Hello");

            // Act
            var result = matcher.RemovePrompt(signature);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SequentialPromptMatcher_RemovePrompt_NullSignature_ThrowsArgumentNullException()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => matcher.RemovePrompt(null!));
        }

        [Fact]
        public void SequentialPromptMatcher_Clear_RemovesAllPrompts()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (signature1, settings1) = CreatePromptWithSettings("Hello");
            var (signature2, settings2) = CreatePromptWithSettings("World");
            matcher.AddPrompt(signature1, settings1);
            matcher.AddPrompt(signature2, settings2);

            // Act
            matcher.Clear();

            // Assert
            Assert.Equal(0, matcher.Count);
        }

        [Fact]
        public void SequentialPromptMatcher_MatchPromptSettings_MatchingPrompt_ReturnsSettings()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (signature, settings) = CreatePromptWithSettings("Hello");
            matcher.AddPrompt(signature, settings);

            // Act
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello world"), new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings, result);
        }

        [Fact]
        public void SequentialPromptMatcher_MatchPromptSettings_NonMatchingPrompt_ReturnsNull()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (signature, settings) = CreatePromptWithSettings("Hello");
            matcher.AddPrompt(signature, settings);

            // Act
            var result = matcher.MatchPromptSettings(CreateCompletionJob("World"), new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SequentialPromptMatcher_MatchPromptSettings_FallbackToProvidedSettings()
        {
            // Arrange
            var matcher = new SequentialPromptMatcher();
            var (_, settings) = CreatePromptWithSettings("Hello");
            var providedSettings = new List<PromptMultiConnectorSettings> { settings };

            // Act
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello world"), providedSettings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings, result);
        }

        #endregion

        #region RadixTreePromptMatcher Tests

        [Fact]
        public void RadixTreePromptMatcher_Constructor_CreatesEmptyMatcher()
        {
            // Arrange & Act
            var matcher = new RadixTreePromptMatcher();

            // Assert
            Assert.Equal(0, matcher.Count);
        }

        [Fact]
        public void RadixTreePromptMatcher_AddPrompt_IncreasesCount()
        {
            // Arrange
            var matcher = new RadixTreePromptMatcher();
            var (signature, settings) = CreatePromptWithSettings("Hello");

            // Act
            matcher.AddPrompt(signature, settings);

            // Assert
            Assert.Equal(1, matcher.Count);
        }

        [Fact]
        public void RadixTreePromptMatcher_MatchPromptSettings_MatchingPrompt_ReturnsSettings()
        {
            // Arrange
            var matcher = new RadixTreePromptMatcher();
            var (signature, settings) = CreatePromptWithSettings("Hello");
            matcher.AddPrompt(signature, settings);

            // Act
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello world"), new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings, result);
        }

        [Fact]
        public void RadixTreePromptMatcher_MatchPromptSettings_PrefixMatch_ReturnsSettings()
        {
            // Arrange
            var matcher = new RadixTreePromptMatcher();
            var (signature1, settings1) = CreatePromptWithSettings("Hello");
            var (signature2, settings2) = CreatePromptWithSettings("Hello world");
            matcher.AddPrompt(signature1, settings1);
            matcher.AddPrompt(signature2, settings2);

            // Act - Should match the longer prefix
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello world and more"), new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings2, result);
        }

        #endregion

        #region HybridPromptMatcher Tests

        [Fact]
        public void HybridPromptMatcher_Constructor_CreatesEmptyMatcher()
        {
            // Arrange & Act
            var matcher = new HybridPromptMatcher();

            // Assert
            Assert.Equal(0, matcher.Count);
        }

        [Fact]
        public void HybridPromptMatcher_AddPrompt_SimpleString_UsesRadixTree()
        {
            // Arrange
            var matcher = new HybridPromptMatcher();
            var (signature, settings) = CreatePromptWithSettings("Hello");

            // Act
            matcher.AddPrompt(signature, settings);

            // Assert
            Assert.Equal(1, matcher.Count);
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello world"), new List<PromptMultiConnectorSettings>());
            Assert.NotNull(result);
            Assert.Equal(settings, result);
        }

        [Fact]
        public void HybridPromptMatcher_AddPrompt_RegexPattern_UsesRegex()
        {
            // Arrange
            var matcher = new HybridPromptMatcher();
            var signature = new PromptSignature
            {
                PromptStart = "Hello.*world",
                RequestSettings = new AIRequestSettings()
            };

            var settings = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature,
                    PromptName = "regex_pattern",
                    Instances = { "Hello beautiful world" }
                }
            };

            // Act
            matcher.AddPrompt(signature, settings);

            // Assert
            Assert.Equal(1, matcher.Count);
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello beautiful world"), new List<PromptMultiConnectorSettings>());
            Assert.NotNull(result);
            Assert.Equal(settings, result);
        }

        [Fact]
        public void HybridPromptMatcher_MatchPromptSettings_TriesRadixTreeFirst()
        {
            // Arrange
            var matcher = new HybridPromptMatcher();

            // Add a simple string pattern
            var (signature1, settings1) = CreatePromptWithSettings("Hello");
            matcher.AddPrompt(signature1, settings1);

            // Add a regex pattern that would also match
            var signature2 = new PromptSignature
            {
                PromptStart = "H.*o",
                RequestSettings = new AIRequestSettings()
            };
            var settings2 = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature2,
                    PromptName = "regex_pattern",
                    Instances = { "Hello" }
                }
            };
            matcher.AddPrompt(signature2, settings2);

            // Act - Should match the RadixTree entry first
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello world"), new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings1, result);
        }

        [Fact]
        public void HybridPromptMatcher_MatchPromptSettings_FallsBackToRegex()
        {
            // Arrange
            var matcher = new HybridPromptMatcher();

            // Add a simple string pattern that won't match
            var (signature1, settings1) = CreatePromptWithSettings("Bonjour");
            matcher.AddPrompt(signature1, settings1);

            // Add a regex pattern that will match
            var signature2 = new PromptSignature
            {
                PromptStart = "H.*o",
                RequestSettings = new AIRequestSettings()
            };
            var settings2 = new PromptMultiConnectorSettings
            {
                PromptType = new PromptType
                {
                    Signature = signature2,
                    PromptName = "regex_pattern",
                    Instances = { "Hello" }
                }
            };
            matcher.AddPrompt(signature2, settings2);

            // Act - Should fall back to regex
            var result = matcher.MatchPromptSettings(CreateCompletionJob("Hello world"), new List<PromptMultiConnectorSettings>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings2, result);
        }

        #endregion
    }
}
