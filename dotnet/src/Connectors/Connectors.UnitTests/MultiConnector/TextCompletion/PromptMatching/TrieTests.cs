// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptMatching;
using Xunit;
using Xunit.Abstractions;

namespace SemanticKernel.Connectors.UnitTests.MultiConnector.TextCompletion.PromptMatching
{
    public class TrieTests
    {
        private readonly ITestOutputHelper _output;

        public TrieTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Constructor_CreatesEmptyTrie()
        {
            // Arrange & Act
            var trie = new Trie<string, char, int>();

            // Assert
            Assert.Equal(0, trie.Count);
        }

        [Fact]
        public void Add_NewKey_IncreasesCount()
        {
            // Arrange
            var trie = new Trie<string, char, int>();

            // Act
            trie.Add("key", 1);

            // Assert
            Assert.Equal(1, trie.Count);
        }

        [Fact]
        public void Add_DuplicateKey_UpdatesValueWithoutIncreasingCount()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("key", 1);

            // Act
            trie.Add("key", 2);

            // Assert
            Assert.Equal(1, trie.Count);
            trie.TryGetValue("key", out int value);
            Assert.Equal(2, value);
        }

        [Fact]
        public void Add_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var trie = new Trie<string, char, int>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => trie.Add(null!, 1));
        }

        [Fact]
        public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("key", 1);

            // Act
            var result = trie.TryGetValue("key", out int value);

            // Assert
            Assert.True(result);
            Assert.Equal(1, value);
        }

        [Fact]
        public void TryGetValue_NonExistingKey_ReturnsFalseAndDefaultValue()
        {
            // Arrange
            var trie = new Trie<string, char, int>();

            // Act
            var result = trie.TryGetValue("key", out int value);

            // Assert
            Assert.False(result);
            Assert.Equal(0, value);
        }

        [Fact]
        public void TryGetValue_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var trie = new Trie<string, char, int>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => trie.TryGetValue(null!, out int value));
        }

        [Fact]
        public void TryGetValueByPrefix_ExactMatch_ReturnsTrueAndValue()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("key", 1);

            // Act
            var result = trie.TryGetValueByPrefix("key", out int value);

            // Assert
            Assert.True(result);
            Assert.Equal(1, value);
        }

        [Fact]
        public void TryGetValueByPrefix_PrefixMatch_ReturnsTrueAndValue()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("key", 1);

            // Act
            var result = trie.TryGetValueByPrefix("keyboard", out int value);

            // Assert
            Assert.True(result);
            Assert.Equal(1, value);
        }

        [Fact]
        public void TryGetValueByPrefix_LongestPrefixMatch_ReturnsLongestMatchingValue()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("k", 1);
            trie.Add("ke", 2);
            trie.Add("key", 3);

            // Act
            var result = trie.TryGetValueByPrefix("keyboard", out int value);

            // Assert
            Assert.True(result);
            Assert.Equal(3, value);
        }

        [Fact]
        public void TryGetValueByPrefix_NoMatch_ReturnsFalseAndDefaultValue()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("key", 1);

            // Act
            var result = trie.TryGetValueByPrefix("value", out int value);

            // Assert
            Assert.False(result);
            Assert.Equal(0, value);
        }

        [Fact]
        public void TryGetValueByPrefix_NullPrefix_ThrowsArgumentNullException()
        {
            // Arrange
            var trie = new Trie<string, char, int>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => trie.TryGetValueByPrefix(null!, out int value));
        }

        [Fact]
        public void Remove_ExistingKey_RemovesKeyAndReturnsTrue()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("key", 1);

            // Act
            var result = trie.Remove("key");

            // Assert
            Assert.True(result);
            Assert.Equal(0, trie.Count);
            Assert.False(trie.TryGetValue("key", out _));
        }

        [Fact]
        public void Remove_NonExistingKey_ReturnsFalse()
        {
            // Arrange
            var trie = new Trie<string, char, int>();

            // Act
            var result = trie.Remove("key");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Remove_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            var trie = new Trie<string, char, int>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => trie.Remove(null!));
        }

        [Fact]
        public void Remove_SharedPrefix_OnlyRemovesSpecifiedKey()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("key", 1);
            trie.Add("keyboard", 2);

            // Act
            var result = trie.Remove("key");

            // Assert
            Assert.True(result);
            Assert.Equal(1, trie.Count);
            Assert.False(trie.TryGetValue("key", out _));
            Assert.True(trie.TryGetValue("keyboard", out int value));
            Assert.Equal(2, value);
        }

        [Fact]
        public void Clear_NonEmptyTrie_RemovesAllEntries()
        {
            // Arrange
            var trie = new Trie<string, char, int>();
            trie.Add("key1", 1);
            trie.Add("key2", 2);

            // Act
            trie.Clear();

            // Assert
            Assert.Equal(0, trie.Count);
            Assert.False(trie.TryGetValue("key1", out _));
            Assert.False(trie.TryGetValue("key2", out _));
        }

        [Fact]
        public void MultipleOperations_WorkCorrectly()
        {
            // Arrange
            var trie = new Trie<string, char, int>();

            // Act & Assert - Add and verify
            trie.Add("hello", 1);
            trie.Add("help", 2);
            trie.Add("world", 3);
            Assert.Equal(3, trie.Count);

            // Verify values
            Assert.True(trie.TryGetValue("hello", out int value1));
            Assert.Equal(1, value1);
            Assert.True(trie.TryGetValue("help", out int value2));
            Assert.Equal(2, value2);
            Assert.True(trie.TryGetValue("world", out int value3));
            Assert.Equal(3, value3);

            // Update a value
            trie.Add("hello", 10);
            Assert.True(trie.TryGetValue("hello", out int updatedValue));
            Assert.Equal(10, updatedValue);
            Assert.Equal(3, trie.Count);

            // Remove a value
            Assert.True(trie.Remove("help"));
            Assert.Equal(2, trie.Count);
            Assert.False(trie.TryGetValue("help", out _));

            // Prefix matching
            Assert.True(trie.TryGetValueByPrefix("hel", out int prefixValue));
            Assert.Equal(10, prefixValue); // Should match "hello"
            Assert.True(trie.TryGetValueByPrefix("wor", out int prefixValue2));
            Assert.Equal(3, prefixValue2); // Should match "world"
        }

        [Fact]
        public void CustomEnumerable_WorksCorrectly()
        {
            // Arrange
            var trie = new Trie<List<int>, int, string>();
            var list1 = new List<int> { 1, 2, 3 };
            var list2 = new List<int> { 4, 5, 6 };

            // Act
            trie.Add(list1, "list1");
            trie.Add(list2, "list2");

            // Assert
            Assert.Equal(2, trie.Count);
            Assert.True(trie.TryGetValue(list1, out string value1));
            Assert.Equal("list1", value1);
            Assert.True(trie.TryGetValue(list2, out string value2));
            Assert.Equal("list2", value2);
        }
    }
}
