using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScriptPlayer.Shared.Converters
{
    public class PositionToMarginConverter : IValueConverter
    {
        public Thickness MarginBottom { get; set; }
        public Thickness MarginTop { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double position = (double)value;
            double top = position / 99.0;
            double bottom = 1 - top;

            return new Thickness(
                MarginBottom.Left * bottom + MarginTop.Left * top,
                MarginBottom.Top * bottom + MarginTop.Top * top,
                MarginBottom.Right * bottom + MarginTop.Right * top,
                MarginBottom.Bottom * bottom + MarginTop.Bottom * top
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
