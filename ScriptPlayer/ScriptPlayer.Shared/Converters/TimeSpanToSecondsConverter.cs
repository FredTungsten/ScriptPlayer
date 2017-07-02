using System;
using System.Globalization;
using System.Windows.Data;

namespace ScriptPlayer.Shared.Converters
{
    public class TimeSpanToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType);   
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType);
        }

        private object Convert(object value, Type targetType)
        {
            if(value == null)
                throw new ArgumentNullException(nameof(value));

            if (targetType == typeof(TimeSpan))
            {
                if (value is TimeSpan)
                    return value;
                return TimeSpan.FromSeconds((double) value);
            }

            if (targetType == typeof(double))
            {
                if (value is double)
                    return value;

                return ((TimeSpan)value).TotalSeconds;
            }

            throw new ArgumentException($"Can't convert value of type {value.GetType().Name} to type {targetType.Name}");
        }
    }
}
