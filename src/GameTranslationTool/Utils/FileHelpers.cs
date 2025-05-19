using System;
using Serilog;
using System.IO;
using System.Linq;

namespace GameTranslationTool.Utils
{
    public static class FileHelpers
    {
        public static bool IsTranslatableFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            var textExtensions = new[] { ".txt", ".xml", ".ini", ".csv", ".json", ".po", ".yaml", ".yml" };
            var maybeBinaryWithText = new[] { ".bin", ".dat", ".pak" };

            if (textExtensions.Contains(ext)) return true;
            if (maybeBinaryWithText.Contains(ext)) return true;

            try
            {
                var content = File.ReadAllText(filePath);
                return content.Any(c => char.IsLetterOrDigit(c));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to read file '{filePath}': {ex.Message}");
            }

            return false;
        }
    }
}
