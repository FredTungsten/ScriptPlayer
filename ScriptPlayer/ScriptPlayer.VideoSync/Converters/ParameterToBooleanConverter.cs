using System;
using System.Globalization;
using System.Windows.Data;

namespace ScriptPlayer.VideoSync.Converters
{
    public class ParameterToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter)??false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(!(value is bool))
                return Binding.DoNothing;

            if ((bool) value)
                return parameter;

            return Binding.DoNothing;
        }
    }
}
