using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class TimeLineHeader : Control
    {
        public static readonly DependencyProperty MarkerProperty = DependencyProperty.Register(
            "Marker", typeof(TimeSpan), typeof(TimeLineHeader), new PropertyMetadata(default(TimeSpan), OnMarkerPositionChanged));

        private static void OnMarkerPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineHeader header = (TimeLineHeader)d;
            header.RecheckMarker();
            header.InvalidateVisual();
        }

        private void RecheckMarker()
        {
            if (Marker < Offset || Marker > Offset + ViewPort)
            {
                Position = Marker;
            }
        }

        public TimeSpan Marker
        {
            get { return (TimeSpan) GetValue(MarkerProperty); }
            set { SetValue(MarkerProperty, value); }
        }

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            "Position", typeof(TimeSpan), typeof(TimeLineHeader), new PropertyMetadata(default(TimeSpan), OnPositionPropertyChanged));

        private static void OnPositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineHeader header = (TimeLineHeader) d;
            header.Offset = header.Position - header.ViewPort.Divide(2);
            header.InvalidateVisual();
        }

        public TimeSpan Position
        {
            get { return (TimeSpan) GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly DependencyProperty ViewPortProperty = DependencyProperty.Register(
            "ViewPort", typeof(TimeSpan), typeof(TimeLineHeader), new PropertyMetadata(TimeSpan.FromSeconds(20), OnViewPortPropertyChanged));

        private static void OnViewPortPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineHeader header = (TimeLineHeader)d;
            header.Offset = header.Position - header.ViewPort.Divide(2);
            header.InvalidateVisual();
        }

        public TimeSpan ViewPort
        {
            get { return (TimeSpan) GetValue(ViewPortProperty); }
            set { SetValue(ViewPortProperty, value); }
        }

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof(TimeSpan), typeof(TimeLineHeader), new PropertyMetadata(default(TimeSpan)));

        private bool _down;
        private TimeSpan _downTime;
        private Point _downPosition;
        private TimeSpan _downCenter;

        public TimeSpan Offset
        {
            get { return (TimeSpan) GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect rectAll = new Rect(0,0, ActualWidth, ActualHeight);
            drawingContext.DrawRectangle(Brushes.Black, null, rectAll);

            TimeDivision scale = GetTimeScale(ViewPort, ActualWidth);

            TimeSpan earliestFull = RoundDown(Offset, scale.MajorScale);

            TimeSpan currentPosition = earliestFull;

            Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            double marker = TimeSpanToPosition(Marker);

            drawingContext.DrawLine(new Pen(Brushes.Red,1), new Point(marker, 0), new Point(marker, ActualHeight));

            while (currentPosition <= Offset + ViewPort)
            {
                double x = TimeSpanToPosition(currentPosition);

                drawingContext.DrawLine(new Pen(Brushes.White,1), new Point(x,32),new Point(x,64));

                

                FormattedText text = new FormattedText(GetText(currentPosition), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, Brushes.White);

                drawingContext.DrawText(text, new Point(x - text.Width / 2, text.Height));

                for (int i = 1; i < scale.Subdivisions; i++)
                {
                    TimeSpan smallTicks = currentPosition + TimeSpanExtensions.Divide((TimeSpan) scale.MajorScale, (double) scale.Subdivisions).Multiply(i);
                    double x2 = TimeSpanToPosition(smallTicks);
                    drawingContext.DrawLine(new Pen(Brushes.White, 1), new Point(x2, 48), new Point(x2, 64));
                }

                currentPosition += scale.MajorScale;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                CaptureMouse();
                e.Handled = true;

                _down = true;
                _downPosition = e.GetPosition(this);
                _downTime = PositionToTimeSpan(_downPosition);
                _downCenter = Position;
            }
            else
            {
                base.OnMouseDown(e);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_down)
            {
                Point position = e.GetPosition(this);
                double diff = position.X - _downPosition.X;

                TimeSpan delta = LengthToTimeSpan(diff);
                Position = _downCenter - delta;
            }
            else
            {
                base.OnMouseMove(e);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_down && e.ChangedButton == MouseButton.Middle)
            {
                ReleaseMouseCapture();
                e.Handled = true;

                _down = false;
            }
            else
            {
                base.OnMouseLeftButtonUp(e);
            }
        }

        private TimeSpan LengthToTimeSpan(double length)
        {
            return ViewPort.Multiply(length / ActualWidth);
        }

        private TimeSpan PositionToTimeSpan(Point position)
        {
            return Offset + LengthToTimeSpan(position.X);
        }

        private string GetText(TimeSpan timestamp)
        {
            return $"{timestamp.Hours:00}:{timestamp.Minutes:00}:{timestamp.Seconds:00}.{timestamp.Milliseconds:000}";
        }

        private double TimeSpanToPosition(TimeSpan timespan)
        {
            double positionRelative = (timespan - Offset).Divide(ViewPort);
            return positionRelative * ActualWidth;
        }

        private TimeSpan RoundDown(TimeSpan position, TimeSpan scale)
        {
            int full = (int) position.Divide(scale);
            if (position < TimeSpan.Zero)
                full--;

            return scale.Multiply(full);
        }

        private static TimeDivision GetTimeScale(TimeSpan viewPort, double width)
        {
            double minSizePerMajorUnit = 100.0;
            double units = width / minSizePerMajorUnit;
            TimeSpan minTimeSpan = viewPort.Divide(units);
            TimeSpan actualUnitTimeSpan = RoundUp(minTimeSpan);

            TimeDivision result = new TimeDivision();
            result.MajorScale = actualUnitTimeSpan;
            if (result.MajorScale >= TimeSpan.FromMinutes(1))
                result.Subdivisions = 6;
            else
                result.Subdivisions = 10;

            return result;
        }

        private static TimeSpan RoundUp(TimeSpan minTimeSpan)
        {
            TimeSpan[] recommendet = 
            {
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10)
            };

            foreach (var timeSpan in recommendet)
            {
                if (timeSpan >= minTimeSpan)
                    return timeSpan;
            }

            return recommendet.Last();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                DecreaseViewPort();
            else
                IncreaseViewPort();
        }

        private void IncreaseViewPort()
        {
            ViewPort = ViewPort.Multiply(1.5);
        }

        private void DecreaseViewPort()
        {
            ViewPort = ViewPort.Divide(1.5);
        }
    }

    public class TimeDivision
    {
        public TimeSpan MajorScale { get; set; }
        public int Subdivisions { get; set; }
    }
}
