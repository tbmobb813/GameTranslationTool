namespace GameTranslationTool.Constants
{
    /// <summary>
    /// Translation API providers
    /// </summary>
    public enum TranslationProvider
    {
        MicrosoftTranslator,
        GoogleTranslate
    }

    /// <summary>
    /// Helper class for TranslationProvider enum
    /// </summary>
    public static class TranslationProviderExtensions
    {
        public static string ToDisplayName(this TranslationProvider provider)
        {
            return provider switch
            {
                TranslationProvider.MicrosoftTranslator => "Microsoft Translator",
                TranslationProvider.GoogleTranslate => "Google Translate",
                _ => provider.ToString()
            };
        }

        public static TranslationProvider FromDisplayName(string displayName)
        {
            return displayName switch
            {
                "Microsoft Translator" => TranslationProvider.MicrosoftTranslator,
                "Google Translate" => TranslationProvider.GoogleTranslate,
                _ => TranslationProvider.GoogleTranslate
            };
        }
    }
}
