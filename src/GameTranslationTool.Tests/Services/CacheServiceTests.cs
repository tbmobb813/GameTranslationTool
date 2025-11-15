using System;
using System.IO;
using GameTranslationTool.Services;
using Xunit;

namespace GameTranslationTool.Tests.Services
{
    public class CacheServiceTests : IDisposable
    {
        private readonly string _testCacheFile;
        private readonly CacheService _cacheService;

        public CacheServiceTests()
        {
            // Use a unique temp file for each test
            _testCacheFile = Path.Combine(Path.GetTempPath(), $"test_cache_{Guid.NewGuid()}.json");
            _cacheService = new CacheService(_testCacheFile);
        }

        public void Dispose()
        {
            // Cleanup temp file after each test
            if (File.Exists(_testCacheFile))
                File.Delete(_testCacheFile);
        }

        [Fact]
        public void CacheTranslation_StoresTranslation()
        {
            // Arrange
            var original = "Hello";
            var translated = "Hola";

            // Act
            _cacheService.CacheTranslation(original, translated);
            var result = _cacheService.GetCachedTranslation(original);

            // Assert
            Assert.Equal(translated, result);
        }

        [Fact]
        public void GetCachedTranslation_WithNonExistentKey_ReturnsNull()
        {
            // Act
            var result = _cacheService.GetCachedTranslation("NonExistentKey");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CacheTranslation_WithNullOriginal_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _cacheService.CacheTranslation(null!, "translated"));
        }

        [Fact]
        public void CacheTranslation_WithEmptyOriginal_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _cacheService.CacheTranslation("", "translated"));
        }

        [Fact]
        public void CacheTranslation_WithNullTranslated_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _cacheService.CacheTranslation("original", null!));
        }

        [Fact]
        public void GetCachedTranslation_WithNullKey_ReturnsNull()
        {
            // Act
            var result = _cacheService.GetCachedTranslation(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCachedTranslation_WithEmptyKey_ReturnsNull()
        {
            // Act
            var result = _cacheService.GetCachedTranslation("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SaveCache_CreatesFile()
        {
            // Arrange
            _cacheService.CacheTranslation("key1", "value1");

            // Act
            _cacheService.SaveCache();

            // Assert
            Assert.True(File.Exists(_testCacheFile));
        }

        [Fact]
        public void LoadCache_RestoresCachedData()
        {
            // Arrange
            _cacheService.CacheTranslation("key1", "value1");
            _cacheService.CacheTranslation("key2", "value2");
            _cacheService.SaveCache();

            // Create a new cache service instance
            var newCacheService = new CacheService(_testCacheFile);

            // Act
            newCacheService.LoadCache();

            // Assert
            Assert.Equal("value1", newCacheService.GetCachedTranslation("key1"));
            Assert.Equal("value2", newCacheService.GetCachedTranslation("key2"));
        }

        [Fact]
        public void LoadCache_WithNonExistentFile_DoesNotThrow()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");
            var cacheService = new CacheService(nonExistentFile);

            // Act & Assert (should not throw)
            cacheService.LoadCache();
        }

        [Fact]
        public void ClearCache_RemovesAllEntries()
        {
            // Arrange
            _cacheService.CacheTranslation("key1", "value1");
            _cacheService.CacheTranslation("key2", "value2");
            _cacheService.SaveCache();

            // Act
            _cacheService.ClearCache();

            // Assert
            Assert.Null(_cacheService.GetCachedTranslation("key1"));
            Assert.Null(_cacheService.GetCachedTranslation("key2"));
            Assert.Equal(0, _cacheService.GetCacheCount());
            Assert.False(File.Exists(_testCacheFile));
        }

        [Fact]
        public void GetCacheCount_ReturnsCorrectCount()
        {
            // Arrange
            _cacheService.CacheTranslation("key1", "value1");
            _cacheService.CacheTranslation("key2", "value2");
            _cacheService.CacheTranslation("key3", "value3");

            // Act
            var count = _cacheService.GetCacheCount();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void CacheTranslation_OverwritesExistingKey()
        {
            // Arrange
            var key = "testKey";
            _cacheService.CacheTranslation(key, "value1");

            // Act
            _cacheService.CacheTranslation(key, "value2");
            var result = _cacheService.GetCachedTranslation(key);

            // Assert
            Assert.Equal("value2", result);
            Assert.Equal(1, _cacheService.GetCacheCount());
        }

        [Fact]
        public void SaveAndLoad_PreservesMultipleEntries()
        {
            // Arrange
            for (int i = 0; i < 100; i++)
            {
                _cacheService.CacheTranslation($"key{i}", $"value{i}");
            }

            _cacheService.SaveCache();
            var newCacheService = new CacheService(_testCacheFile);

            // Act
            newCacheService.LoadCache();

            // Assert
            Assert.Equal(100, newCacheService.GetCacheCount());
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal($"value{i}", newCacheService.GetCachedTranslation($"key{i}"));
            }
        }
    }
}
