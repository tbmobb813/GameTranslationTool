using System;
using System.Globalization;
using System.Windows.Data;

namespace GameTranslationTool
{
    /// <summary>
    /// Returns true if the bound string is non-empty, so we can highlight error rows.
    /// </summary>
    public class HasErrorConverter : IValueConverter
    {
        // value is your Error property (a string); return true if non-empty
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s && !string.IsNullOrWhiteSpace(s);

        // not needed for one-way binding
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
