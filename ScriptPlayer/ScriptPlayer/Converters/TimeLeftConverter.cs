using System;
using System.Globalization;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class TimeLeftConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan progress = (TimeSpan)values[0];
            TimeSpan duration = (TimeSpan)values[1];
            TimeSpan timeLeft = duration - progress;
            return $"-{timeLeft:h\\:mm\\:ss} / {duration:h\\:mm\\:ss}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
