using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class BeatBar2 : Control
    {
        public static readonly DependencyProperty Marker1Property = DependencyProperty.Register(
            "Marker1", typeof(TimeSpan), typeof(BeatBar2), new PropertyMetadata(TimeSpan.MinValue, OnMarkerPropertyChanged));

        private static void OnMarkerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatBar2)d).InvalidateVisual();
        }

        public TimeSpan Marker1
        {
            get { return (TimeSpan) GetValue(Marker1Property); }
            set { SetValue(Marker1Property, value); }
        }

        public static readonly DependencyProperty Marker2Property = DependencyProperty.Register(
            "Marker2", typeof(TimeSpan), typeof(BeatBar2), new PropertyMetadata(TimeSpan.MinValue, OnMarkerPropertyChanged));

        public TimeSpan Marker2
        {
            get { return (TimeSpan) GetValue(Marker2Property); }
            set { SetValue(Marker2Property, value); }
        }

        public event EventHandler<TimeSpan> TimeMouseDown;

        public event EventHandler<TimeSpan> TimeMouseRightDown;
        public event EventHandler<TimeSpan> TimeMouseRightMove;
        public event EventHandler<TimeSpan> TimeMouseRightUp;

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
            "TotalDisplayedDuration", typeof(TimeSpan), typeof(BeatBar2), new PropertyMetadata(TimeSpan.FromSeconds(5), OnVisualPropertyChanged));

        public TimeSpan TotalDisplayedDuration
        {
            get { return (TimeSpan)GetValue(TotalDisplayedDurationProperty); }
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

        private bool _rightdown;
        private double _lastMousePosition;

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatBar2)d).RefreshRightMove();
            ((BeatBar2)d).InvalidateVisual();
            
        }

        public TimeSpan Progress
        {
            get { return (TimeSpan) GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            double x = e.GetPosition(this).X;
            TimeSpan position = TimeFromX(x);
            
            OnTimeMouseDown(position);
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            CaptureMouse();
            _rightdown = true;
            _lastMousePosition = e.GetPosition(this).X;
            OnTimeMouseRightDown(TimeFromX(_lastMousePosition));
            e.Handled = true;
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if(!_rightdown)
                base.OnPreviewMouseRightButtonUp(e);
            else
            {
                _rightdown = false;
                _lastMousePosition = e.GetPosition(this).X;
                ReleaseMouseCapture();
                OnTimeMouseRightUp(TimeFromX(_lastMousePosition));
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_rightdown)
            {
                _lastMousePosition = e.GetPosition(this).X;
                RefreshRightMove();
                e.Handled = true;
            }
        }

        private void RefreshRightMove()
        {
            if(_rightdown)
                OnTimeMouseRightMove(TimeFromX(_lastMousePosition));
            
        }

        private TimeSpan TimeFromX(double x)
        {
            double relativeProgress = (x / ActualWidth) - Midpoint;
            TimeSpan position = Progress + TotalDisplayedDuration.Multiply(relativeProgress);
            return position;
        }

        private double XFromTime(TimeSpan time)
        {
            TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
            TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);
            double relativePosition = (time - timeFrom).Divide(timeTo - timeFrom);
            double absolutePosition = relativePosition * ActualWidth;
            return absolutePosition;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            bool numbers = Keyboard.IsKeyToggled(Key.NumLock);

            #region Background

            Rect fullRect = new Rect(new Point(), new Size(ActualWidth, ActualHeight));
            drawingContext.PushClip(new RectangleGeometry(fullRect));
            drawingContext.DrawRectangle(Background, null, fullRect);

            #endregion

            #region Selection

            TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
            TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);

            if (Marker1 != TimeSpan.MinValue && Marker2 != TimeSpan.MinValue)
            {
                TimeSpan earlier = Marker1 < Marker2 ? Marker1 : Marker2;
                TimeSpan later = Marker1 > Marker2 ? Marker1 : Marker2;

                if (later >= timeFrom && earlier <= timeTo)
                {
                    double xFrom = XFromTime(earlier);
                    double xTo = XFromTime(later);

                    SolidColorBrush brush = new SolidColorBrush(Colors.Cyan){Opacity = 0.4};

                    drawingContext.DrawRectangle(brush, null, new Rect(new Point(xFrom,0), new Point(xTo, ActualHeight)));
                }
            }

            if (Marker1 != TimeSpan.MinValue && IsTimeVisible(Marker1))
            {
                double x = XFromTime(Marker1);
                drawingContext.DrawLine(new Pen(Brushes.Cyan, 1), new Point(x, 0), new Point(x, ActualHeight));
            }

            if (Marker2 != TimeSpan.MinValue && IsTimeVisible(Marker2))
            {
                double x = XFromTime(Marker2);
                drawingContext.DrawLine(new Pen(Brushes.Cyan, 1), new Point(x, 0), new Point(x, ActualHeight));
            }

            #endregion

            #region MidPoint

            drawingContext.DrawLine(new Pen(Brushes.Red, 11), new Point(Midpoint * ActualWidth, 0), new Point(Midpoint * ActualWidth, ActualHeight));

            #endregion

            #region Beats

            if (Beats != null)
            {
                List<TimeSpan> absoluteBeatPositions = Beats.GetBeats(timeFrom, timeTo).ToList();

                if(absoluteBeatPositions.Count > 0)
                {
                    int startingIndex = Beats.IndexOf(absoluteBeatPositions.First());
                    int index = startingIndex;

                    List<double> relativeBeatPositions = absoluteBeatPositions.Select(b => (b - timeFrom).Divide(timeTo - timeFrom)).ToList();

                    foreach (double pos in relativeBeatPositions)
                    {
                        int loopIndex = index % 8;

                        DrawLine(drawingContext, loopIndex == 0 ? Colors.Red : LineColor, new Point(pos * ActualWidth, -5),
                            new Point(pos * ActualWidth, ActualHeight + 5), LineWidth);

                        index++;
                    }
                }
            }

            #endregion

            drawingContext.Pop();
        }

        private bool IsTimeVisible(TimeSpan time)
        {
            if (time < Progress - TotalDisplayedDuration.Multiply(Midpoint))
                return false;
            if (time > Progress + TotalDisplayedDuration.Multiply(1 - Midpoint))
                return false;

            return true;
        }

        private void DrawLine(DrawingContext drawingContext, Color primary, Point pFrom, Point pTo, double lineWidth = 4)
        {
            drawingContext.DrawLine(new Pen(new SolidColorBrush(primary) { Opacity = 0.5 }, lineWidth) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Colors.White) { Opacity = 1.0 }, 2) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
        }

        protected virtual void OnTimeMouseDown(TimeSpan e)
        {
            TimeMouseDown?.Invoke(this, e);
        }

        protected virtual void OnTimeMouseRightDown(TimeSpan e)
        {
            TimeMouseRightDown?.Invoke(this, e);
        }

        protected virtual void OnTimeMouseRightUp(TimeSpan e)
        {
            TimeMouseRightUp?.Invoke(this, e);
        }

        protected virtual void OnTimeMouseRightMove(TimeSpan e)
        {
            TimeMouseRightMove?.Invoke(this, e);
        }
    }
}
