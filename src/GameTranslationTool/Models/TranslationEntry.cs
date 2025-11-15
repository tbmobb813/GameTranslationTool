using GameTranslationTool.MVVM;

namespace GameTranslationTool.Models
{
    public class TranslationEntry : ViewModelBase
    {
        private string _original = string.Empty;
        private string _translated = string.Empty;
        private string _error = string.Empty;

        public string Original
        {
            get => _original;
            set => SetProperty(ref _original, value);
        }

        public string Translated
        {
            get => _translated;
            set => SetProperty(ref _translated, value);
        }

        public string Error
        {
            get => _error;
            set => SetProperty(ref _error, value);
        }
    }
}
