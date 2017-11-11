using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared.Controls
{
    public class AwesomeTextBlock : TextBlock
    {
        public static readonly Uri AwesomeUri = new Uri("pack://application:,,,/FontAwesome.Net;component/Fonts/#FontAwesome", UriKind.RelativeOrAbsolute);
        public AwesomeTextBlock()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                FontFamily = new FontFamily("FontAwesome");
            else
            {
                var families = Fonts.GetFontFamilies(AwesomeUri);
                FontFamily = families.FirstOrDefault();
            }
        }
    }
}
