using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.Shared.Converters
{
    public class ConversionModeToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((ConversionMode)value)
            {
                case ConversionMode.UpOrDown:
                    return "Up / Down";
                case ConversionMode.DownFast:
                    return "Down (Fast)";
                case ConversionMode.DownCenter:
                    return "Down (Centered)";
                case ConversionMode.UpFast:
                    return "Up (Fast)";
                case ConversionMode.UpCenter:
                    return "Up (Centered)";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
