using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Converters
{
    public class DeviceToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Device device = (Device) value;

            if (device.Name.Contains("Launch"))
                return DeviceImages.Launch;

            if (device.Name.Contains("Hush"))
                return DeviceImages.Hush;

            if (device.Name.Contains("Nora"))
                return DeviceImages.Nora;

            if (device.Name.Contains("Gamepad"))
                return DeviceImages.Controller;

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public static class DeviceImages
    {
        public static Uri Launch = UriFromName("Launch");
        public static Uri Hush = UriFromName("Hush");
        public static Uri Nora = UriFromName("Nora");
        public static Uri Controller = UriFromName("Controller");

        private static Uri UriFromName(string name)
        {
            return new Uri($"pack://application:,,,/ScriptPlayer.Shared;component/Images/Devices/{name}.png");
        }
    }
}
