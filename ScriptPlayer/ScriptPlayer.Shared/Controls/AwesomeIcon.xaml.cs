using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ScriptPlayer.Shared.Controls
{
    /// <summary>
    /// Interaction logic for AwesomeIcon.xaml
    /// </summary>
    public partial class AwesomeIcon : UserControl
    {
        static AwesomeIcon()
        {
            
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            "Icon", typeof(AwesomePath), typeof(AwesomeIcon), new PropertyMetadata(AwesomePath.Adjust_Solid));

        public AwesomePath Icon
        {
            get => (AwesomePath) GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public AwesomeIcon()
        {
            InitializeComponent();
        }
    }

    public class AwesomePathToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AwesomePaths.GetPath((AwesomePath) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
