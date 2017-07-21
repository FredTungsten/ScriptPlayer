using System;
using System.Globalization;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class DistinctValueToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool) value)
                return parameter;
            return Binding.DoNothing;
        }
    }
}
