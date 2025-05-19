using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace GameTranslationTool.Translation
{
    public class MicrosoftTranslatorService : ITranslator
    {
        private readonly string _apiKey;
        private readonly string _region;
        private readonly HttpClient _client;

        public MicrosoftTranslatorService(string apiKey, string region)
        {
            _apiKey = apiKey;
            _region = region;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", _region);
        }

        public async Task<string> TranslateAsync(string text, string fromLang, string toLang)
        {
            try
            {
                var endpoint = $"https://{_region}.api.cognitive.microsofttranslator.com/";
                var url = $"{endpoint}translate?api-version=3.0&from={fromLang}&to={toLang}";
                var body = new[] { new { Text = text } };

                var response = await _client.PostAsync(url,
                    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement[0]
                          .GetProperty("translations")[0]
                          .GetProperty("text").GetString() ?? text;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Microsoft translation failed for text: {Text}", text);
                return text; // fallback
            }
        }
    }
}
