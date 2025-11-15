using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace GameTranslationTool.Translation
{
    public static class FakeTranslator
    {
        /// <summary>
        /// Translates a single text string using the configured translation engine.
        /// </summary>
        /// <param name="input">The original text to translate.</param>
        /// <param name="fromLang">Source language code (e.g., "en").</param>
        /// <param name="toLang">Target language code (e.g., "es").</param>
        /// <returns>The translated text, or original if an error occurs.</returns>
        public static async Task<string> TranslateTextAsync(string input, string fromLang = "en", string toLang = "es")
        {
            try
            {
                // Call the real translator (LibreTranslate)
                return await LibreTranslator.TranslateAsync(input, fromLang, toLang);
            }
            catch (Exception ex)
            {
                Log.Error("Translation error: {Message}", ex.Message);
                return input; // Fallback to original text on error
            }
        }

        /// <summary>
        /// Reads a file, translates each line, and writes the translated lines to the output file.
        /// </summary>
        public static async Task TranslateFileAsync(string inputPath, string outputPath, string fromLang = "en", string toLang = "es")
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(inputPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = await TranslateTextAsync(lines[i], fromLang, toLang);
                    // Throttle if needed to avoid rate limits
                    await Task.Delay(100);
                }

                await File.WriteAllLinesAsync(outputPath, lines);
                Log.Information("File translated: {Path}", inputPath);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to translate file: {Path}. Error: {Message}", inputPath, ex.Message);
            }
        }
    }
}
