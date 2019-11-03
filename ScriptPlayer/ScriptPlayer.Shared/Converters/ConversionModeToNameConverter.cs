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
            if (!(value is ConversionMode))
                return "Unknown";

            switch ((ConversionMode)value)
            {
                case ConversionMode.UpOrDown:
                    return "Up / Down";
                case ConversionMode.UpDownFast:
                    return "Up / Down (Fast)";
                case ConversionMode.DownFast:
                    return "Down (Fast)";
                case ConversionMode.DownCenter:
                    return "Down (Centered)";
                case ConversionMode.UpFast:
                    return "Up (Fast)";
                case ConversionMode.UpCenter:
                    return "Up (Centered)";
                case ConversionMode.DownFastSlow:
                    return "Down (Fast, Slow)";
                case ConversionMode.DownSlowFast:
                    return "Down (Slow, Fast)";
                case ConversionMode.UpFastSlow:
                    return "Up (Fast, Slow)";
                case ConversionMode.UpSlowFast:
                    return "Up (Slow, Fast)";
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
