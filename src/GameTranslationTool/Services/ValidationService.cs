using System;
using System.IO;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Provides input validation for the application
    /// </summary>
    public static class ValidationService
    {
        public static bool IsValidFilePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                _ = Path.GetFullPath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidDirectoryPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                _ = Path.GetFullPath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool FileExists(string? path)
        {
            return IsValidFilePath(path) && File.Exists(path);
        }

        public static bool DirectoryExists(string? path)
        {
            return IsValidDirectoryPath(path) && Directory.Exists(path);
        }

        public static bool IsValidApiKey(string? apiKey)
        {
            return !string.IsNullOrWhiteSpace(apiKey) && apiKey.Length >= 10;
        }

        public static bool IsValidLanguageCode(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return false;

            return languageCode.Length == 2 && languageCode == languageCode.ToLower();
        }

        public static bool IsValidRegion(string? region)
        {
            return !string.IsNullOrWhiteSpace(region) && region.Length >= 2;
        }

        public static bool IsPositiveInteger(string? text, out int value)
        {
            value = 0;
            return int.TryParse(text, out value) && value > 0;
        }
    }
}
