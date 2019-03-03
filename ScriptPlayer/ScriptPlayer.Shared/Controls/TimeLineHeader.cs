using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public enum MarkerMode
    {
        Center,
        OutOfBoundsRecenter,
        OutOfBoundsJump,
        None
    }

    public class TimeLineHeader : Control
    {
        public static readonly DependencyProperty ShowMarkerProperty = DependencyProperty.Register(
            "ShowMarker", typeof(bool), typeof(TimeLineHeader), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowMarker
        {
            get { return (bool)GetValue(ShowMarkerProperty); }
            set { SetValue(ShowMarkerProperty, value); }
        }

        public static readonly DependencyProperty MarkerModeProperty = DependencyProperty.Register(
            "MarkerMode", typeof(MarkerMode), typeof(TimeLineHeader), new PropertyMetadata(MarkerMode.Center, OnMarkerModePropertyChanged));

        private static void OnMarkerModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineHeader header = (TimeLineHeader)d;
            header.RecheckMarker();
            header.InvalidateVisual();
        }

        public MarkerMode MarkerMode
        {
            get { return (MarkerMode)GetValue(MarkerModeProperty); }
            set { SetValue(MarkerModeProperty, value); }
        }

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
            switch (MarkerMode)
            {
                case MarkerMode.Center:
                    Position = Marker;
                    break;
                case MarkerMode.OutOfBoundsRecenter:
                    if (Marker < Offset || Marker > Offset + ViewPort)
                    {
                        Position = Marker;
                    }
                    break;
                case MarkerMode.OutOfBoundsJump:
                    if (Marker < Offset)
                    {
                        Position = Marker + Offset;
                    }
                    else if (Marker > Offset + ViewPort)
                    {
                        Position = Marker;
                    }
                    break;
                case MarkerMode.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public TimeSpan Marker
        {
            get { return (TimeSpan)GetValue(MarkerProperty); }
            set { SetValue(MarkerProperty, value); }
        }

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            "Position", typeof(TimeSpan), typeof(TimeLineHeader), new PropertyMetadata(default(TimeSpan), OnPositionPropertyChanged));

        private static void OnPositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineHeader header = (TimeLineHeader)d;
            header.Offset = header.Position - header.ViewPort.Divide(2);
            header.InvalidateVisual();
        }

        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
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
            get { return (TimeSpan)GetValue(ViewPortProperty); }
            set { SetValue(ViewPortProperty, value); }
        }

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof(TimeSpan), typeof(TimeLineHeader), new PropertyMetadata(default(TimeSpan)));

        private bool _down;
        private TimeSpan _downTime;
        private Point _downPosition;
        private TimeSpan _downCenter;
        private static readonly Typeface _typeface;
        private TimeSpan _lastRender;

        public TimeSpan Offset
        {
            get => (TimeSpan)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        static TimeLineHeader()
        {
            BackgroundProperty.OverrideMetadata(typeof(TimeLineHeader), new FrameworkPropertyMetadata(Brushes.Black));

            //Cache the typeface (not much but should help a little)
            _typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Dirty hack, should remove later (whenever or never)
            if (((UIElement) this.Parent).Visibility != Visibility.Visible)
                return;

            //var diffToLast = Math.Abs(TimeSpanToPosition(_lastRender));
            //Debug.WriteLine(diffToLast);
            //if (diffToLast < 10)
            //    return;
            
            //_lastRender = Offset;

            Rect rectAll = new Rect(0, 0, ActualWidth, ActualHeight);
            drawingContext.DrawRectangle(Background, null, rectAll);

            TimeDivision scale = GetTimeScale(ViewPort, ActualWidth);

            TimeSpan earliestFull = RoundDown(Offset, scale.MajorScale);

            TimeSpan currentPosition = earliestFull;

            if (ShowMarker)
            {

                double marker = TimeSpanToPosition(Marker);

                drawingContext.DrawLine(new Pen(Brushes.Red, 1), new Point(marker, 0), new Point(marker, ActualHeight));
            }

            while (currentPosition <= Offset + ViewPort)
            {
                double x = TimeSpanToPosition(currentPosition);

                drawingContext.DrawLine(new Pen(Brushes.White, 1), new Point(x, 32), new Point(x, 64));
                
                FormattedText text = new FormattedText(GetText(currentPosition), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, 10, Brushes.White, 96);

                drawingContext.DrawText(text, new Point(x - text.Width / 2, text.Height));

                for (int i = 1; i < scale.Subdivisions; i++)
                {
                    TimeSpan smallTicks = currentPosition + scale.MajorScale.Divide(scale.Subdivisions).Multiply(i);
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
            int full = (int)position.Divide(scale);
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

            e.Handled = true;
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
