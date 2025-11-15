using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace GameTranslationTool.Translation
{
    public class GoogleTranslatorService : ITranslator
    {
        private readonly string _apiKey;
        private static readonly HttpClient _client = new HttpClient();

        public GoogleTranslatorService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string> TranslateAsync(string text, string fromLang, string toLang)
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return text;

            try
            {
                var url = $"https://translation.googleapis.com/language/translate/v2?key={_apiKey}";
                var body = new { q = text, source = fromLang, target = toLang, format = "text" };

                var response = await _client.PostAsync(url,
                    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("data")
                          .GetProperty("translations")[0]
                          .GetProperty("translatedText").GetString() ?? text;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Google translation failed for text: {Text}", text);
                return text;
            }
        }
    }
}
