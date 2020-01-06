using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class AwesomeTextBlock : TextBlock
    {
        static AwesomeTextBlock()
        {
            FontFamilyProperty.OverrideMetadata(typeof(AwesomeTextBlock), new FrameworkPropertyMetadata(GetFontAwesome()));
        }

        private static FontFamily GetFontAwesome()
        {
            return new FontFamily(FontsUri, FontNames);
        }

        public static readonly Uri FontsUri = new Uri("pack://application:,,,/ScriptPlayer.Shared;component/Fonts/", UriKind.RelativeOrAbsolute);
        public static readonly string FontNames = "Font Awesome 5 Free, ./#Font Awesome 5 Free, ./#Font Awesome 5 Brands";
        
        public static FontFamily AwesomeFont => GetFontAwesome();
    }
}
