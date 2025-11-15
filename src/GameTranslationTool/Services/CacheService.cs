using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Serilog;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Manages translation caching to avoid redundant API calls
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly Dictionary<string, string> _cache = [];
        private readonly string _cacheFilePath;

        public CacheService()
        {
            _cacheFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "translation_cache.json");
        }

        public CacheService(string cacheFilePath)
        {
            if (string.IsNullOrWhiteSpace(cacheFilePath))
                throw new ArgumentException("Cache file path cannot be null or empty", nameof(cacheFilePath));

            _cacheFilePath = cacheFilePath;
        }

        public string? GetCachedTranslation(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return null;

            return _cache.TryGetValue(originalText, out var cached) ? cached : null;
        }

        public void CacheTranslation(string originalText, string translatedText)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                throw new ArgumentException("Original text cannot be null or empty", nameof(originalText));

            if (string.IsNullOrWhiteSpace(translatedText))
                throw new ArgumentException("Translated text cannot be null or empty", nameof(translatedText));

            _cache[originalText] = translatedText;
        }

        public void LoadCache()
        {
            if (!File.Exists(_cacheFilePath))
            {
                Log.Information("Cache file not found at {Path}", _cacheFilePath);
                return;
            }

            try
            {
                var json = File.ReadAllText(_cacheFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (dict != null)
                {
                    _cache.Clear();
                    foreach (var kv in dict)
                        _cache[kv.Key] = kv.Value;

                    Log.Information("Loaded {Count} cached translations from {Path}",
                        _cache.Count, _cacheFilePath);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load translation cache from {Path}", _cacheFilePath);
            }
        }

        public void SaveCache()
        {
            try
            {
                var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_cacheFilePath, json);
                Log.Information("Saved {Count} cached translations to {Path}",
                    _cache.Count, _cacheFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save translation cache to {Path}", _cacheFilePath);
                throw;
            }
        }

        public void ClearCache()
        {
            _cache.Clear();

            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
                Log.Information("Translation cache cleared and file deleted");
            }
        }

        public int GetCacheCount() => _cache.Count;
    }
}
