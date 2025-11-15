using GameTranslationTool.Models;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Service for managing application settings
    /// </summary>
    public interface ISettingsService
    {
        TranslationSettings LoadSettings();
        void SaveSettings(TranslationSettings settings);
        string GetSettingsPath();
    }
}
