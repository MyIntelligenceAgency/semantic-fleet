// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion.PromptMatching
{
    public class HybridDictionaryTests
    {
        private readonly ITestOutputHelper _output;

        public HybridDictionaryTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Constructor_WithDefaultParameters_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var dictionary = new HybridDictionary<string, int>();

            // Assert
            Assert.Equal(0, dictionary.Count);
        }

        [Fact]
        public void Constructor_WithCustomThreshold_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var dictionary = new HybridDictionary<string, int>(5);

            // Assert
            Assert.Equal(0, dictionary.Count);
        }

        [Fact]
        public void Constructor_WithCustomComparer_CreatesEmptyDictionary()
        {
            // Arrange & Act
            var dictionary = new HybridDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Assert
            Assert.Equal(0, dictionary.Count);
        }

        [Fact]
        public void Add_NewKey_IncreasesCount()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();

            // Act
            dictionary.Add("key1", 1);

            // Assert
            Assert.Equal(1, dictionary.Count);
        }

        [Fact]
        public void Add_DuplicateKey_ThrowsArgumentException()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => dictionary.Add("key1", 2));
        }

        [Fact]
        public void Indexer_GetExistingKey_ReturnsValue()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);

            // Act
            var value = dictionary["key1"];

            // Assert
            Assert.Equal(1, value);
        }

        [Fact]
        public void Indexer_GetNonExistingKey_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => dictionary["key1"]);
        }

        [Fact]
        public void Indexer_SetExistingKey_UpdatesValue()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);

            // Act
            dictionary["key1"] = 2;

            // Assert
            Assert.Equal(2, dictionary["key1"]);
        }

        [Fact]
        public void Indexer_SetNewKey_AddsKeyValue()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();

            // Act
            dictionary["key1"] = 1;

            // Assert
            Assert.Equal(1, dictionary["key1"]);
            Assert.Equal(1, dictionary.Count);
        }

        [Fact]
        public void ContainsKey_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);

            // Act
            var result = dictionary.ContainsKey("key1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ContainsKey_NonExistingKey_ReturnsFalse()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();

            // Act
            var result = dictionary.ContainsKey("key1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);

            // Act
            var result = dictionary.TryGetValue("key1", out var value);

            // Assert
            Assert.True(result);
            Assert.Equal(1, value);
        }

        [Fact]
        public void TryGetValue_NonExistingKey_ReturnsFalseAndDefaultValue()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();

            // Act
            var result = dictionary.TryGetValue("key1", out var value);

            // Assert
            Assert.False(result);
            Assert.Equal(0, value);
        }

        [Fact]
        public void Remove_ExistingKey_RemovesKeyAndReturnsTrue()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);

            // Act
            var result = dictionary.Remove("key1");

            // Assert
            Assert.True(result);
            Assert.Equal(0, dictionary.Count);
        }

        [Fact]
        public void Remove_NonExistingKey_ReturnsFalse()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();

            // Act
            var result = dictionary.Remove("key1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Clear_NonEmptyDictionary_RemovesAllEntries()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);
            dictionary.Add("key2", 2);

            // Act
            dictionary.Clear();

            // Assert
            Assert.Equal(0, dictionary.Count);
        }

        [Fact]
        public void Keys_NonEmptyDictionary_ReturnsAllKeys()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);
            dictionary.Add("key2", 2);

            // Act
            var keys = dictionary.Keys.ToList();

            // Assert
            Assert.Equal(2, keys.Count);
            Assert.Contains("key1", keys);
            Assert.Contains("key2", keys);
        }

        [Fact]
        public void Values_NonEmptyDictionary_ReturnsAllValues()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>();
            dictionary.Add("key1", 1);
            dictionary.Add("key2", 2);

            // Act
            var values = dictionary.Values.ToList();

            // Assert
            Assert.Equal(2, values.Count);
            Assert.Contains(1, values);
            Assert.Contains(2, values);
        }

        [Fact]
        public void ConversionToDictionary_ExceedingThreshold_WorksCorrectly()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>(3); // Threshold of 3

            // Act - Add more items than the threshold
            dictionary.Add("key1", 1);
            dictionary.Add("key2", 2);
            dictionary.Add("key3", 3);
            dictionary.Add("key4", 4); // This should trigger conversion to Dictionary

            // Assert - All operations should still work correctly
            Assert.Equal(4, dictionary.Count);
            Assert.Equal(4, dictionary["key4"]);
            Assert.True(dictionary.ContainsKey("key1"));

            dictionary.Remove("key1");
            Assert.Equal(3, dictionary.Count);
            Assert.False(dictionary.ContainsKey("key1"));
        }

        [Fact]
        public void CustomComparer_CaseInsensitive_WorksCorrectly()
        {
            // Arrange
            var dictionary = new HybridDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            dictionary.Add("key", 1);

            // Act & Assert
            Assert.True(dictionary.ContainsKey("KEY"));
            Assert.Equal(1, dictionary["Key"]);

            // Update using different case
            dictionary["KEY"] = 2;
            Assert.Equal(2, dictionary["key"]);

            // Remove using different case
            Assert.True(dictionary.Remove("Key"));
            Assert.Equal(0, dictionary.Count);
        }
    }
}
