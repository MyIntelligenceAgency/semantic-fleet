// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion.PromptMatching
{
    public class RadixTreeTests
    {
        private readonly ITestOutputHelper _output;

        public RadixTreeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Constructor_CreatesEmptyRadixTree()
        {
            // Arrange & Act
            var radixTree = new RadixTree<string, char, int>();

            // Assert
            Assert.Equal(0, radixTree.Count);
        }

        [Fact]
        public void Add_NewKey_IncreasesCount()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();

            // Act
            radixTree.Add("key", 1);

            // Assert
            Assert.Equal(1, radixTree.Count);
        }

        [Fact]
        public void Add_DuplicateKey_UpdatesValueWithoutIncreasingCount()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("key", 1);

            // Act
            radixTree.Add("key", 2);

            // Assert
            Assert.Equal(1, radixTree.Count);
            radixTree.TryGetValue("key", out int value);
            Assert.Equal(2, value);
        }

        [Fact]
        public void Add_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => radixTree.Add(null!, 1));
        }

        [Fact]
        public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("key", 1);

            // Act
            var result = radixTree.TryGetValue("key", out int value);

            // Assert
            Assert.True(result);
            Assert.Equal(1, value);
        }

        [Fact]
        public void TryGetValue_NonExistingKey_ReturnsFalseAndDefaultValue()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();

            // Act
            var result = radixTree.TryGetValue("key", out int value);

            // Assert
            Assert.False(result);
            Assert.Equal(0, value);
        }

        [Fact]
        public void TryGetValue_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => radixTree.TryGetValue(null!, out int value));
        }

        [Fact]
        public void TryGetValueByPrefix_ExactMatch_ReturnsTrueAndValue()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("key", 1);

            // Act
            var result = radixTree.TryGetValueByPrefix("key", out int value);

            // Assert
            Assert.True(result);
            Assert.Equal(1, value);
        }

        [Fact]
        public void TryGetValueByPrefix_PrefixMatch_ReturnsTrueAndValue()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("key", 1);

            // Act
            var result = radixTree.TryGetValueByPrefix("keyboard", out int value);

            // Assert
            Assert.True(result);
            Assert.Equal(1, value);
        }

        [Fact]
        public void TryGetValueByPrefix_LongestPrefixMatch_ReturnsLongestMatchingValue()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("k", 1);
            radixTree.Add("ke", 2);
            radixTree.Add("key", 3);

            // Act
            var result = radixTree.TryGetValueByPrefix("keyboard", out int value);

            // Assert
            Assert.True(result);
            Assert.Equal(3, value);
        }

        [Fact]
        public void TryGetValueByPrefix_NoMatch_ReturnsFalseAndDefaultValue()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("key", 1);

            // Act
            var result = radixTree.TryGetValueByPrefix("value", out int value);

            // Assert
            Assert.False(result);
            Assert.Equal(0, value);
        }

        [Fact]
        public void TryGetValueByPrefix_NullPrefix_ThrowsArgumentNullException()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => radixTree.TryGetValueByPrefix(null!, out int value));
        }

        [Fact]
        public void Remove_ExistingKey_RemovesKeyAndReturnsTrue()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("key", 1);

            // Act
            var result = radixTree.Remove("key");

            // Assert
            Assert.True(result);
            Assert.Equal(0, radixTree.Count);
            Assert.False(radixTree.TryGetValue("key", out _));
        }

        [Fact]
        public void Remove_NonExistingKey_ReturnsFalse()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();

            // Act
            var result = radixTree.Remove("key");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Remove_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => radixTree.Remove(null!));
        }

        [Fact]
        public void Clear_NonEmptyRadixTree_RemovesAllEntries()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("key1", 1);
            radixTree.Add("key2", 2);

            // Act
            radixTree.Clear();

            // Assert
            Assert.Equal(0, radixTree.Count);
            Assert.False(radixTree.TryGetValue("key1", out _));
            Assert.False(radixTree.TryGetValue("key2", out _));
        }

        [Fact]
        public void RadixCompression_SharedPrefix_WorksCorrectly()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();

            // Act
            radixTree.Add("romane", 1);
            radixTree.Add("romanus", 2);
            radixTree.Add("romulus", 3);
            radixTree.Add("rubens", 4);
            radixTree.Add("ruber", 5);
            radixTree.Add("rubicon", 6);
            radixTree.Add("rubicundus", 7);

            // Assert
            Assert.Equal(7, radixTree.Count);

            // Verify all values can be retrieved
            Assert.True(radixTree.TryGetValue("romane", out int value1));
            Assert.Equal(1, value1);
            Assert.True(radixTree.TryGetValue("romanus", out int value2));
            Assert.Equal(2, value2);
            Assert.True(radixTree.TryGetValue("romulus", out int value3));
            Assert.Equal(3, value3);
            Assert.True(radixTree.TryGetValue("rubens", out int value4));
            Assert.Equal(4, value4);
            Assert.True(radixTree.TryGetValue("ruber", out int value5));
            Assert.Equal(5, value5);
            Assert.True(radixTree.TryGetValue("rubicon", out int value6));
            Assert.Equal(6, value6);
            Assert.True(radixTree.TryGetValue("rubicundus", out int value7));
            Assert.Equal(7, value7);

            // Verify prefix matching
            Assert.True(radixTree.TryGetValueByPrefix("rom", out int prefixValue1));
            Assert.True(radixTree.TryGetValueByPrefix("rub", out int prefixValue2));
            Assert.True(radixTree.TryGetValueByPrefix("rubic", out int prefixValue3));
            Assert.Equal(6, prefixValue3); // Should match "rubicon"
        }

        [Fact]
        public void RadixCompression_NodeSplitting_WorksCorrectly()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("test", 1);

            // Act - Add a key that shares a prefix with the existing key
            radixTree.Add("team", 2);

            // Assert
            Assert.Equal(2, radixTree.Count);
            Assert.True(radixTree.TryGetValue("test", out int value1));
            Assert.Equal(1, value1);
            Assert.True(radixTree.TryGetValue("team", out int value2));
            Assert.Equal(2, value2);

            // Verify prefix matching
            Assert.True(radixTree.TryGetValueByPrefix("te", out int prefixValue));
            // The value returned should be from either "test" or "team", depending on which was inserted last
            Assert.True(prefixValue == 1 || prefixValue == 2);
        }

        [Fact]
        public void RadixCompression_ExactPrefixMatch_WorksCorrectly()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("test", 1);

            // Act - Add a key that is a prefix of the existing key
            radixTree.Add("te", 2);

            // Assert
            Assert.Equal(2, radixTree.Count);
            Assert.True(radixTree.TryGetValue("test", out int value1));
            Assert.Equal(1, value1);
            Assert.True(radixTree.TryGetValue("te", out int value2));
            Assert.Equal(2, value2);

            // Verify prefix matching
            Assert.True(radixTree.TryGetValueByPrefix("testing", out int prefixValue));
            Assert.Equal(1, prefixValue); // Should match "test" as it's longer than "te"
        }

        [Fact]
        public void RadixCompression_RemoveWithSharedPrefix_WorksCorrectly()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>();
            radixTree.Add("romane", 1);
            radixTree.Add("romanus", 2);

            // Act
            var result = radixTree.Remove("romane");

            // Assert
            Assert.True(result);
            Assert.Equal(1, radixTree.Count);
            Assert.False(radixTree.TryGetValue("romane", out _));
            Assert.True(radixTree.TryGetValue("romanus", out int value));
            Assert.Equal(2, value);
        }

        // Définition d'un comparateur d'égalité pour les caractères insensible à la casse
        private class CharEqualityComparer : IEqualityComparer<char>
        {
            public bool Equals(char x, char y) => char.ToLowerInvariant(x) == char.ToLowerInvariant(y);
            public int GetHashCode(char obj) => char.ToLowerInvariant(obj).GetHashCode();
        }

        [Fact]
        public void CustomComparer_CaseInsensitive_WorksCorrectly()
        {
            // Arrange
            var radixTree = new RadixTree<string, char, int>(
                key => new List<char>(key.ToLowerInvariant()),
                new CharEqualityComparer());

            radixTree.Add("Test", 1);

            // Act & Assert
            Assert.True(radixTree.TryGetValue("TEST", out int value));
            Assert.Equal(1, value);

            // Prefix matching should also be case-insensitive
            Assert.True(radixTree.TryGetValueByPrefix("testing", out int prefixValue));
            Assert.Equal(1, prefixValue);
        }
    }
}
