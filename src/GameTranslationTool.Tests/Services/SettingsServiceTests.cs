using System;
using System.IO;
using GameTranslationTool.Models;
using GameTranslationTool.Services;
using Xunit;

namespace GameTranslationTool.Tests.Services
{
    public class SettingsServiceTests : IDisposable
    {
        private readonly string _testSettingsFile;
        private readonly SettingsService _settingsService;

        public SettingsServiceTests()
        {
            // Use a unique temp file for each test
            _testSettingsFile = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid()}.json");
            _settingsService = new SettingsService(_testSettingsFile);
        }

        public void Dispose()
        {
            // Cleanup temp file after each test
            if (File.Exists(_testSettingsFile))
                File.Delete(_testSettingsFile);
        }

        [Fact]
        public void Constructor_WithNullPath_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SettingsService(null!));
        }

        [Fact]
        public void Constructor_WithEmptyPath_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new SettingsService(""));
        }

        [Fact]
        public void GetSettingsPath_ReturnsCorrectPath()
        {
            // Act
            var path = _settingsService.GetSettingsPath();

            // Assert
            Assert.Equal(_testSettingsFile, path);
        }

        [Fact]
        public void LoadSettings_WithNonExistentFile_ReturnsDefaultSettings()
        {
            // Act
            var settings = _settingsService.LoadSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal(string.Empty, settings.ApiKey);
            Assert.Equal(string.Empty, settings.Region);
            Assert.Equal(string.Empty, settings.GoogleApiKey);
        }

        [Fact]
        public void SaveSettings_CreatesFile()
        {
            // Arrange
            var settings = new TranslationSettings
            {
                ApiKey = "test-api-key",
                Region = "eastus",
                GoogleApiKey = "test-google-key"
            };

            // Act
            _settingsService.SaveSettings(settings);

            // Assert
            Assert.True(File.Exists(_testSettingsFile));
        }

        [Fact]
        public void SaveSettings_WithNullSettings_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _settingsService.SaveSettings(null!));
        }

        [Fact]
        public void SaveAndLoad_PreservesSettings()
        {
            // Arrange
            var originalSettings = new TranslationSettings
            {
                ApiKey = "my-api-key",
                Region = "westus",
                GoogleApiKey = "my-google-key"
            };

            // Act
            _settingsService.SaveSettings(originalSettings);
            var loadedSettings = _settingsService.LoadSettings();

            // Assert
            Assert.Equal(originalSettings.ApiKey, loadedSettings.ApiKey);
            Assert.Equal(originalSettings.Region, loadedSettings.Region);
            Assert.Equal(originalSettings.GoogleApiKey, loadedSettings.GoogleApiKey);
        }

        [Fact]
        public void SaveSettings_OverwritesExistingFile()
        {
            // Arrange
            var settings1 = new TranslationSettings { ApiKey = "key1", Region = "region1" };
            var settings2 = new TranslationSettings { ApiKey = "key2", Region = "region2" };

            // Act
            _settingsService.SaveSettings(settings1);
            _settingsService.SaveSettings(settings2);
            var loadedSettings = _settingsService.LoadSettings();

            // Assert
            Assert.Equal("key2", loadedSettings.ApiKey);
            Assert.Equal("region2", loadedSettings.Region);
        }

        [Fact]
        public void LoadSettings_WithCorruptedFile_ReturnsDefaultSettings()
        {
            // Arrange - Create a corrupted JSON file
            File.WriteAllText(_testSettingsFile, "{ invalid json content }");

            // Act
            var settings = _settingsService.LoadSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal(string.Empty, settings.ApiKey);
        }

        [Fact]
        public void SaveSettings_CreatesIndentedJson()
        {
            // Arrange
            var settings = new TranslationSettings
            {
                ApiKey = "test-key",
                Region = "test-region"
            };

            // Act
            _settingsService.SaveSettings(settings);
            var fileContent = File.ReadAllText(_testSettingsFile);

            // Assert
            Assert.Contains("\n", fileContent); // Indented JSON should have newlines
            Assert.Contains("  ", fileContent); // Indented JSON should have spaces
        }

        [Fact]
        public void SaveSettings_WithEmptyStrings_SavesCorrectly()
        {
            // Arrange
            var settings = new TranslationSettings
            {
                ApiKey = "",
                Region = "",
                GoogleApiKey = ""
            };

            // Act
            _settingsService.SaveSettings(settings);
            var loadedSettings = _settingsService.LoadSettings();

            // Assert
            Assert.Equal("", loadedSettings.ApiKey);
            Assert.Equal("", loadedSettings.Region);
            Assert.Equal("", loadedSettings.GoogleApiKey);
        }
    }
}
