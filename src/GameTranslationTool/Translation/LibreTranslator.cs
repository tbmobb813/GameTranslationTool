using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace GameTranslationTool.Translation
{
    public static class LibreTranslator
    {
        private static readonly HttpClient _httpClient = new();

        public static async Task<string> TranslateAsync(string input, string fromLang = "en", string toLang = "es")
        {
            try
            {
                var request = new
                {
                    q = input,
                    source = fromLang,
                    target = toLang,
                    format = "text"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://libretranslate.com/translate", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<LibreResponse>(responseBody);
                return result?.TranslatedText ?? input;
            }
            catch (Exception ex)
            {
                Log.Error("LibreTranslate failed: {Message}", ex.Message);
                return input;
            }
        }

            // Inside your LibreTranslator class
            private class LibreResponse
            {
                [JsonPropertyName("translatedText")]
                public string? TranslatedText { get; set; }
            }
        
    }
}
