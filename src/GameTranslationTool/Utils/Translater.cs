using System;
using System.IO;

namespace GameTranslationTool.Utils
{
    public static class Translator
    {
        // Simulate translation by appending [TRANSLATED] to each line
        public static string TranslateText(string input)
        {
            // Split the input text into lines
            var lines = input.Split([Environment.NewLine], StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                // Simulate translation
                lines[i] += " [TRANSLATED]";
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static void TranslateFile(string inputPath, string outputPath)
        {
            try
            {
                string originalText = File.ReadAllText(inputPath);
                string translatedText = TranslateText(originalText);
                File.WriteAllText(outputPath, translatedText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error translating file '{inputPath}': {ex.Message}");
            }
        }
    }
}
