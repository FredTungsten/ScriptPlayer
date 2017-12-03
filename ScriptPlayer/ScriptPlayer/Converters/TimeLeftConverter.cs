using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class TimeLeftConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "- / -";

            if (!(values[0] is TimeSpan && values[1] is TimeSpan))
                return "- / -";

            TimeSpan progress = (TimeSpan)values[0];
            TimeSpan duration = (TimeSpan)values[1];
            TimeSpan timeLeft = duration - progress;
            return $"-{timeLeft:h\\:mm\\:ss} / {duration:h\\:mm\\:ss}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Enumerable.Repeat(DependencyProperty.UnsetValue, targetTypes.Length).ToArray();
        }
    }
}
