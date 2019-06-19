using System;
using System.Globalization;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    /// <summary>
    /// Expects 2 Values, 1st = Boolean, AlternateValue = parameter
    /// </summary>
    public class AlternativeValueConverter : IMultiValueConverter
    {
        public bool UseAlternateValueOnTrue { get; set; } = true;

        public object AlternateValue { get; set; } = "Auto";

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return Binding.DoNothing;

            if (!(values[1] is bool condition))
                return Binding.DoNothing;

            object value = values[0];

            return System.Convert.ChangeType((object)(condition == UseAlternateValueOnTrue ? AlternateValue : value), targetType);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (Equals(value, AlternateValue))
                return new[]
                {
                    Binding.DoNothing,
                    Binding.DoNothing
                };

            return new[]
            {
                System.Convert.ChangeType(value, targetTypes[0]),
                Binding.DoNothing
            };
        }
    }
}
