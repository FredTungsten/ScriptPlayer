using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.Shared.Converters
{
    public class ConversionModeToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((BeatsToFunScriptConverter.ConversionMode) value)
            {
                case BeatsToFunScriptConverter.ConversionMode.UpOrDown:
                    return "Up / Down";
                case BeatsToFunScriptConverter.ConversionMode.DownFast:
                    return "Down (Fast)";
                case BeatsToFunScriptConverter.ConversionMode.DownCenter:
                    return "Down (Centered)";
                case BeatsToFunScriptConverter.ConversionMode.UpFast:
                    return "Up (Fast)";
                case BeatsToFunScriptConverter.ConversionMode.UpCenter:
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
