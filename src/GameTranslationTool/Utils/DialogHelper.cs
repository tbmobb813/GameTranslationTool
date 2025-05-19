using System.Windows;

namespace GameTranslationTool.Utils
{
    public static class DialogHelper
    {
        public static void Info(string message, string title = "Info")
        {
            // Use WPF MessageBox explicitly to avoid ambiguity
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Warn(string message, string title = "Warning")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void Error(string message, string title = "Error")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static bool Confirm(string message, string title = "Confirm", MessageBoxImage icon = MessageBoxImage.Question)
        {
            var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, icon);
            return result == MessageBoxResult.Yes;
        }
    }
}
