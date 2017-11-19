using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool allTrue = true;

            foreach (object value in values)
            {
                if (value is bool b)
                    allTrue &= b;
                else
                    return false;
            }

            return allTrue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Enumerable.Repeat(DependencyProperty.UnsetValue, targetTypes.Length).ToArray();
        }
    }
}
