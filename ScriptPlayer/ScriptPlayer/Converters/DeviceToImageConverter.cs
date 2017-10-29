using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Converters
{
    public class DeviceToImageConverter : IValueConverter
    {
        //TODO Add more graphics
        /*
         "Youcups Device ({friendlyNames[aInterface.Name]})"
                "Youcups"
            "Warrior II"

             $"Vibratissimo Device ({aInterface.Name})",
            "Vibratissimo"


            $"WeVibe Device ({aInterface.Name})",
            "4 Plus",
            "Ditto",
            "Nova",
            "Pivot",
            "Wish",
            "Verge",

            $"Kiiroo {aInterface.Name}",
             "ONYX", "PEARL" 
 
             "CycSA" "Vorze A10 Cyclone"
 
             "Fleshlight Launch"
 
             $"MagicMotion Device ({aInterface.Name})",
             "Smart Mini Vibe",
 
            $"Lovense Device ({friendlyNames[aInterface.Name]})",
 
            // Nora
            "LVS-A011", "LVS-C011",

            // Max
            "LVS-B011",

             // Ambi
            "LVS-L009",

            // Edge
            "LVS-P36",

             // Edge
            "LVS-Domi37",

            private static Dictionary<string, string> friendlyNames = new Dictionary<string, string>()
            {
                { "LVS-A011", "Nora" },
                { "LVS-C011", "Nora" },
                { "LVS-B011", "Max" },
                { "LVS-L009", "Ambi" },
                { "LVS-S001", "Lush" },
                { "LVS-Z001", "Hush" },
                { "LVS_Z001", "Hush Prototype" },
                { "LVS-P36", "Edge" },
                { "LVS-Z36", "Hush" },
                { "LVS-Domi37", "Domi" },
            };
         */

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
