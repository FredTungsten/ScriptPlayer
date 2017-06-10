using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class BeatBar2 : Control
    {
        public static readonly DependencyProperty LineColorProperty = DependencyProperty.Register(
            "LineColor", typeof(Color), typeof(BeatBar2), new PropertyMetadata(Colors.Lime, OnVisualPropertyChanged));

        public Color LineColor
        {
            get { return (Color) GetValue(LineColorProperty); }
            set { SetValue(LineColorProperty, value); }
        }

        public static readonly DependencyProperty LineWidthProperty = DependencyProperty.Register(
            "LineWidth", typeof(double), typeof(BeatBar2), new PropertyMetadata(4.0d, OnVisualPropertyChanged));

        public double LineWidth
        {
            get { return (double) GetValue(LineWidthProperty); }
            set { SetValue(LineWidthProperty, value); }
        }

        public static readonly DependencyProperty BeatsProperty = DependencyProperty.Register(
            "Beats", typeof(BeatCollection), typeof(BeatBar2), new PropertyMetadata(default(BeatCollection), OnVisualPropertyChanged));

        public BeatCollection Beats
        {
            get { return (BeatCollection) GetValue(BeatsProperty); }
            set { SetValue(BeatsProperty, value); }
        }

        public static readonly DependencyProperty TotalDisplayedDurationProperty = DependencyProperty.Register(
            "TotalDisplayedDuration", typeof(double), typeof(BeatBar2), new PropertyMetadata(5.0d, OnVisualPropertyChanged));

        public double TotalDisplayedDuration
        {
            get { return (double)GetValue(TotalDisplayedDurationProperty); }
            set { SetValue(TotalDisplayedDurationProperty, value); }
        }

        public static readonly DependencyProperty MidpointProperty = DependencyProperty.Register(
            "Midpoint", typeof(double), typeof(BeatBar2), new PropertyMetadata(0.5d, OnVisualPropertyChanged));

        public double Midpoint
        {
            get { return (double)GetValue(MidpointProperty); }
            set { SetValue(MidpointProperty, value); }
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(TimeSpan), typeof(BeatBar2), new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatBar2)d).InvalidateVisual();
        }

        public TimeSpan Progress
        {
            get { return (TimeSpan) GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect fullRect = new Rect(new Point(), new Size(ActualWidth, ActualHeight));

            drawingContext.PushClip(new RectangleGeometry(fullRect));

            drawingContext.DrawRectangle(Background, null, fullRect);

            drawingContext.DrawLine(new Pen(Brushes.Red, 11), new Point(Midpoint * ActualWidth, 0), new Point(Midpoint * ActualWidth, ActualHeight));

            if (Beats != null)
            {
                double timeFrom = Progress.TotalSeconds - Midpoint * TotalDisplayedDuration;
                double timeTo = Progress.TotalSeconds + (1 - Midpoint) * TotalDisplayedDuration;

                List<TimeSpan> absoluteBeatPositions = Beats.GetBeats(TimeSpan.FromSeconds(timeFrom), TimeSpan.FromSeconds(timeTo)).ToList();

                IEnumerable<double> relativeBeatPositions =
                    absoluteBeatPositions.Select(b => (b.TotalSeconds - timeFrom) / (timeTo - timeFrom));

                const double safeSpace = 30;
                double y = ActualHeight / 2.0;

                DrawLine(drawingContext, Colors.Red, new Point(-safeSpace, y),
                    new Point(ActualWidth + safeSpace, y));

                foreach (double pos in relativeBeatPositions)
                {
                    DrawLine(drawingContext, LineColor, new Point(pos * ActualWidth, -5), new Point(pos * ActualWidth, ActualHeight + 5), LineWidth);
                }
            }

            drawingContext.Pop();
        }

        private void DrawLine(DrawingContext drawingContext, Color primary, Point pFrom, Point pTo, double lineWidth = 4)
        {
            drawingContext.DrawLine(new Pen(new SolidColorBrush(primary) { Opacity = 0.5 }, lineWidth) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Colors.White) { Opacity = 1.0 }, 2) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
        }
    }
}
