using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class TimeScale : Control
    {
        public static readonly DependencyProperty IntervallProperty = DependencyProperty.Register(
            "Intervall", typeof(TimeSpan), typeof(TimeScale), new PropertyMetadata(TimeSpan.FromSeconds(1)));

        private bool _down;
        private TimeSpan _downPos;
        private TimeSpan _upPos;

        public TimeSpan Intervall
        {
            get { return (TimeSpan) GetValue(IntervallProperty); }
            set { SetValue(IntervallProperty, value); }
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(new Point(0,0), new Size(ActualWidth, ActualHeight)));
            TimeSpan span = TimePanel.GetDuration(this);
            TimeSpan handled = TimeSpan.Zero;

            while (handled <= span)
            {
                double x = TimeSpanToPosition(handled);
                drawingContext.DrawLine(new Pen(Brushes.White, 1), new Point(x,0),new Point(x,ActualHeight));
                handled = handled.Add(Intervall);
            }

            if (_downPos != _upPos)
            {
                double xFrom = TimeSpanToPosition(_downPos);
                double xTo = TimeSpanToPosition(_upPos);

                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(70,255,0,0)), new Pen(Brushes.Red,1), new Rect(new Point(xFrom,0), new Point(xTo, ActualHeight)));
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
            _down = true;
            CaptureMouse();
            _downPos = PositionToTimeSpan(e.GetPosition(this).X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_down) return;
            e.Handled = true;

            _upPos = PositionToTimeSpan(e.GetPosition(this).X);
            InvalidateVisual();
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!_down) return;
            e.Handled = true;
            _down = false;
            ReleaseMouseCapture();
            _upPos = PositionToTimeSpan(e.GetPosition(this).X);
        }

        private TimeSpan PositionToTimeSpan(double position)
        {
            TimeSpan duration = TimePanel.GetDuration(this);
            TimeSpan start = TimePanel.GetDuration(this);

            double relativePosition = position / ActualWidth;

            return start + duration.Multiply(relativePosition);
        }

        private double TimeSpanToPosition(TimeSpan position)
        {
            TimeSpan duration = TimePanel.GetDuration(this);
            TimeSpan start = TimePanel.GetDuration(this);

            TimeSpan relativePosition = position - start;
            double relativePositionX = relativePosition.Divide(duration);

            return relativePositionX * ActualWidth;
        }
    }
}
