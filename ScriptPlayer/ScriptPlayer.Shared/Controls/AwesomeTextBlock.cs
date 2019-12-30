using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared.Controls
{
    public class AwesomeTextBlock : TextBlock
    {
        //public static readonly Uri AwesomeUri = new Uri("pack://application:,,,/FontAwesome.Net;component/Fonts/#FontAwesome", UriKind.RelativeOrAbsolute);

        public static readonly Uri AwesomeUri = new Uri("pack://application:,,,/ScriptPlayer.Shared;component/Fonts/#Font Awesome 5 Free Regular", UriKind.RelativeOrAbsolute);
        private static FontFamily _awesomeFont;

        public static FontFamily AwesomeFont
        {
            get
            {
                if (_awesomeFont == null)
                {
                    var families = Fonts.GetFontFamilies(AwesomeUri);
                    _awesomeFont = families.FirstOrDefault();
                }

                return _awesomeFont;
            }
        }

        public AwesomeTextBlock()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                FontFamily = new FontFamily("Font Awesome 5 Free Regular");
            }
            else
            {
                FontFamily = AwesomeFont;
            }
        }
    }
}
