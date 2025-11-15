using System.Collections.Generic;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Service for managing translation cache
    /// </summary>
    public interface ICacheService
    {
        string? GetCachedTranslation(string originalText);
        void CacheTranslation(string originalText, string translatedText);
        void LoadCache();
        void SaveCache();
        void ClearCache();
        int GetCacheCount();
    }
}
