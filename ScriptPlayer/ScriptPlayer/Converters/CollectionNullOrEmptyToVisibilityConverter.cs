using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class CollectionNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public Visibility VisibilityWhenEmpty { get; set; }
        public Visibility VisibilityWhenNull { get; set; }
        public Visibility VisibilityOtherwise { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection collection = value as ICollection;
            if (collection == null)
                return VisibilityWhenNull;
            if (collection.Count == 0)
                return VisibilityWhenEmpty;

            return VisibilityOtherwise;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
