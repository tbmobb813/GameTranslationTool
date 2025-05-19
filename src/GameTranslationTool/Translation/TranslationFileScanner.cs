using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameTranslationTool.ISO;

namespace GameTranslationTool.Translation
{
    public static class TranslationFileScanner
    {
        public static List<string> FindTranslatableFiles(string rootFolder)
        {
            var allFiles = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories);

            var translatableFiles = allFiles
                .Where(file => IsTranslatableFile(file)) // Updated to call a new method
                .ToList(); // Convert IEnumerable<string> to List<string>

            return translatableFiles;
        }

        private static bool IsTranslatableFile(string filePath)
        {
            // Implement logic to determine if the file is translatable
            // For example, check file extensions or other criteria
            return filePath.EndsWith(".txt") || filePath.EndsWith(".xml");
        }
    }
}
