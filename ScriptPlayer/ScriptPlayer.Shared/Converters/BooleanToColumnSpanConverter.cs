using System;
using System.Globalization;
using System.Windows.Data;

namespace ScriptPlayer.Shared.Converters
{
    public class BooleanToColumnSpanConverter : IValueConverter
    {
        public int OnTrue { get; set; } = int.MaxValue;
        public int OnFalse { get; set; } = 1;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) 
                return OnFalse;

            return System.Convert.ToBoolean(value) ? OnTrue : OnFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
