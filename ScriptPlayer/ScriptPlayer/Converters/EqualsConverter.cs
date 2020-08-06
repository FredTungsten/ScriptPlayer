using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class EqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values[0].ToString() == values[1].ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Enumerable.Repeat(DependencyProperty.UnsetValue, targetTypes.Length).ToArray();
        }
    }
}
