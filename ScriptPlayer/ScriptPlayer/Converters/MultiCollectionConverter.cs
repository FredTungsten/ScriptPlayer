using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class MultiCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            List<object> result = new List<object>();

            foreach (object value in values)
            {
                if (value == null)
                    continue;

                if(value is IEnumerable enumerable)
                    result.AddRange(enumerable.Cast<object>());
                else
                    result.Add(value);
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Enumerable.Repeat(Binding.DoNothing, targetTypes.Length).ToArray();
        }
    }

    public class OneToManyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new object[] {value};
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
