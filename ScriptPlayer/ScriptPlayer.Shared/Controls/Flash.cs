using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ScriptPlayer.Shared
{
    public class Flash : Control
    {
        public static readonly DependencyProperty FadeTimeProperty = DependencyProperty.Register(
            "FadeTime", typeof(TimeSpan), typeof(Flash), new PropertyMetadata(TimeSpan.FromMilliseconds(100)));

        public TimeSpan FadeTime
        {
            get { return (TimeSpan) GetValue(FadeTimeProperty); }
            set { SetValue(FadeTimeProperty, value); }
        }

        public static readonly DependencyProperty ActiveColorProperty = DependencyProperty.Register(
            "ActiveColor", typeof(Color), typeof(Flash), new PropertyMetadata(Colors.Transparent, OnActiveColorPropertyChanged));

        public Color ActiveColor
        {
            get { return (Color) GetValue(ActiveColorProperty); }
            set { SetValue(ActiveColorProperty, value); }
        }
        private static void OnActiveColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Flash)d).InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(new SolidColorBrush(ActiveColor),null, new Rect(0,0,ActualWidth,ActualHeight));
        }

        public void Now()
        {
            BeginAnimation(ActiveColorProperty, new ColorAnimation(Colors.White, Color.FromArgb(0,255,255,255), FadeTime));
        }
    }
}
