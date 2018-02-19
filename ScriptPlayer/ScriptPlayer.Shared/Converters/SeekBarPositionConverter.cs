using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ScriptPlayer.Shared.Converters
{
    public class SeekBarPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan progress = (TimeSpan)values[0];
            TimeSpan duration = (TimeSpan)values[1];
            TimeSpan hoverposition = (TimeSpan)values[2];

            TimeSpan timeLeft = hoverposition - progress;
            TimeSpan timeLeftAbs = timeLeft.Abs();

            string prefix = timeLeft < TimeSpan.Zero ? "-" : "+";

            string format = duration >= TimeSpan.FromHours(1) ? "hh\\:mm\\:ss" : "mm\\:ss";

            string result = hoverposition.ToString(format) + " (" + prefix + timeLeftAbs.ToString(format) + ")";
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Enumerable.Repeat(DependencyProperty.UnsetValue, targetTypes.Length).ToArray();
        }
    }
}
