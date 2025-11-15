using GameTranslationTool.MVVM;

namespace GameTranslationTool.Models
{
    public class DialogEntry : ViewModelBase
    {
        private string _id = string.Empty;
        private string _text = string.Empty;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }
    }
}
