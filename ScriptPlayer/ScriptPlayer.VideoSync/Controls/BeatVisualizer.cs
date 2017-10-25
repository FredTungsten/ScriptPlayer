using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.VideoSync.Controls
{
    public class BeatVisualizer : Control
    {
        static BeatVisualizer()
        {
            ForegroundProperty.OverrideMetadata(typeof(BeatVisualizer), new FrameworkPropertyMetadata(Brushes.Magenta));
        }

        public static readonly DependencyProperty PatternProperty = DependencyProperty.Register(
            "Pattern", typeof(object), typeof(BeatVisualizer), new FrameworkPropertyMetadata(default(object), FrameworkPropertyMetadataOptions.AffectsRender));

        public object Pattern
        {
            get { return (object) GetValue(PatternProperty); }
            set { SetValue(PatternProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            bool[] pattern = Pattern as bool[];

            drawingContext.DrawLine(new Pen(Foreground,1), new Point(0, ActualHeight / 2), new Point(ActualWidth, ActualHeight / 2));

            if (pattern == null || pattern.Length == 0) return;

            int len = pattern.Length;
            double beatRadius = 4;
            double x = beatRadius / 2.0;
            double lenPerBeat = (ActualWidth - beatRadius) / (len - 1);

            for (int i = 0; i < len; i++)
            {
                bool active = pattern[i];
                Brush color = (i > 0 && i < len - 1) ? Foreground : Brushes.DarkGray;

                if(active)
                    drawingContext.DrawEllipse(color, null, new Point(x + lenPerBeat * i, ActualHeight / 2.0),beatRadius,beatRadius);
            }
        }
    }
}
