using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GameTranslationTool.Translation
{
    internal class GoogleTranslator
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GoogleTranslator()
        {
            // Assuming appsettings.json contains the key "GoogleTranslator:ApiKey"
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            // Retrieve the API key from configuration
            _apiKey = config["GoogleTranslator:ApiKey"];
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("Google API key is not configured.");
            }
            _httpClient = new HttpClient();
        }

        public async Task<string> TranslateAsync(string text, string fromLang, string toLang)
        {
            var url = $"https://translation.googleapis.com/language/translate/v2?key={_apiKey}";

            var requestBody = new
            {
                q = text,
                source = fromLang,
                target = toLang,
                format = "text"
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            // Error handling if response isn't successful
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error: {response.StatusCode}, Content: {errorContent}");
            }

            // Parse the response JSON
            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            // Try to extract the translation text
            if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                dataElement.TryGetProperty("translations", out JsonElement translationsElement) &&
                translationsElement.GetArrayLength() > 0)
            {
                var translationText = translationsElement[0].GetProperty("translatedText").GetString();
                return translationText ?? string.Empty;
            }
            else
            {
                throw new Exception("Invalid API response structure.");
            }
        }
    }
}
