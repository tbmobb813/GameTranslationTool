using System;
using System.IO;
using System.Text.Json;
using GameTranslationTool.Models;
using Serilog;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Manages application settings persistence
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;

        public SettingsService()
        {
            _settingsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "translator_settings.json");
        }

        public SettingsService(string settingsPath)
        {
            if (string.IsNullOrWhiteSpace(settingsPath))
                throw new ArgumentException("Settings path cannot be null or empty", nameof(settingsPath));

            _settingsPath = settingsPath;
        }

        public string GetSettingsPath() => _settingsPath;

        public TranslationSettings LoadSettings()
        {
            if (!File.Exists(_settingsPath))
            {
                Log.Information("Settings file not found, creating default settings");
                return new TranslationSettings();
            }

            try
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<TranslationSettings>(json);

                if (settings == null)
                {
                    Log.Warning("Failed to deserialize settings, using defaults");
                    return new TranslationSettings();
                }

                Log.Information("Settings loaded successfully from {Path}", _settingsPath);
                return settings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading settings from {Path}, using defaults", _settingsPath);
                return new TranslationSettings();
            }
        }

        public void SaveSettings(TranslationSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_settingsPath, json);
                Log.Information("Settings saved successfully to {Path}", _settingsPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving settings to {Path}", _settingsPath);
                throw;
            }
        }
    }
}
