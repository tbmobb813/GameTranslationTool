using System;
using Serilog;
using System.Collections.Generic;
using System.IO;

namespace GameTranslationTool.Translation
{
    public static class TranslationProcessor
    {
        public static void ProcessFiles(List<string> files, string outputFolder, string translatedFolder)
        {
            foreach (var file in files)
            {
                string inputPath = Path.Combine(outputFolder, file);
                string outputPath = Path.Combine(translatedFolder, file);

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                    string originalText = File.ReadAllText(inputPath);

                    string translatedText = DummyTranslate(originalText); // Replace later with ML/LLM

                    File.WriteAllText(outputPath, translatedText);

                    Console.WriteLine($"Translated: {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to translate {file}: {ex.Message}");
                }
            }
        }

        private static string DummyTranslate(string input)
        {
            // Simple mock "translation"
            return "[TRANSLATED] " + input;
        }
    }
}
