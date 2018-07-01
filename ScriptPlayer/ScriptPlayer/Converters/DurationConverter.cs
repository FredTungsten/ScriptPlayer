using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class DurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan)
                return ((TimeSpan) value).ToString(@"h\:mm\:ss");

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
