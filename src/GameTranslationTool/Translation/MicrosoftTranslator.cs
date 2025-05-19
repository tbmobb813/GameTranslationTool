using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace GameTranslationTool.Translation
{
    public static class MicrosoftTranslator
    {
        private static readonly HttpClient _httpClient = new();

        public static string Translate(string input, string fromLang, string toLang, string apiKey, string endpoint)
        {
            try
            {
                string route = $"/translate?api-version=3.0&from={fromLang}&to={toLang}";
                var uri = new Uri(new Uri(endpoint), route);

                var body = new object[] { new { Text = input } };
                var requestBody = JsonSerializer.Serialize(body);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "YOUR_REGION"); // Replace with your Azure region (e.g., "eastus")

                var response = _httpClient.Send(request);

                System.Diagnostics.Debug.WriteLine($"Calling: {uri}");
                System.Diagnostics.Debug.WriteLine($"Key: {apiKey}"); // Don't do this in production — only for debug!

                var jsonResponse = response.Content.ReadAsStringAsync().Result;

                System.Diagnostics.Debug.WriteLine($"Response: {jsonResponse}");

                var results = JsonSerializer.Deserialize<List<TranslationResult>>(jsonResponse);
                return results?[0].Translations[0].Text ?? input;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Translation failed: {ex.Message}");
                return input;
            }
        }

        private class TranslationResult
        {
            public List<Translation> Translations { get; set; } = new();
        }

        private class Translation
        {
            public string Text { get; set; } = string.Empty;
        }
    }
}
