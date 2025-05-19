using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameTranslationTool
{
    public class DialogEntry : INotifyPropertyChanged
    {
        private string _id = "";
        private string _text = "";

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

}
