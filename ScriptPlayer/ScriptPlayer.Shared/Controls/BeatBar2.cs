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
        public static readonly DependencyProperty FlashDurationProperty = DependencyProperty.Register(
            "FlashDuration", typeof(TimeSpan), typeof(BeatBar2), new PropertyMetadata(TimeSpan.FromMilliseconds(50)));

        public TimeSpan FlashDuration
        {
            get => (TimeSpan)GetValue(FlashDurationProperty);
            set => SetValue(FlashDurationProperty, value);
        }

        public static readonly DependencyProperty FlashAfterBeatProperty = DependencyProperty.Register(
            "FlashAfterBeat", typeof(bool), typeof(BeatBar2), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool FlashAfterBeat
        {
            get => (bool)GetValue(FlashAfterBeatProperty);
            set => SetValue(FlashAfterBeatProperty, value);
        }

        public static readonly DependencyProperty SoundAfterBeatProperty = DependencyProperty.Register(
            "SoundAfterBeat", typeof(bool), typeof(BeatBar2), new PropertyMetadata(default(bool)));

        public bool SoundAfterBeat
        {
            get => (bool) GetValue(SoundAfterBeatProperty);
            set => SetValue(SoundAfterBeatProperty, value);
        }

        public static readonly DependencyProperty SoundDelayProperty = DependencyProperty.Register(
            "SoundDelay", typeof(double), typeof(BeatBar2), new PropertyMetadata(default(double)));

        public double SoundDelay
        {
            get => (double) GetValue(SoundDelayProperty);
            set => SetValue(SoundDelayProperty, value);
        }

        public static readonly DependencyProperty BeatVolumeProperty = DependencyProperty.Register(
            "BeatVolume", typeof(double), typeof(BeatBar2), new PropertyMetadata(100.0, new PropertyChangedCallback(OnBeatVolumeChanged)));

        private static void OnBeatVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatBar2) d)._tick.Volume = (double) e.NewValue;
        }

        public double BeatVolume
        {
            get => (double) GetValue(BeatVolumeProperty);
            set => SetValue(BeatVolumeProperty, value);
        }

        public static readonly DependencyProperty HighlightBeatsProperty = DependencyProperty.Register(
            "HighlightBeats", typeof(bool), typeof(BeatBar2), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool HighlightBeats
        {
            get => (bool)GetValue(HighlightBeatsProperty);
            set => SetValue(HighlightBeatsProperty, value);
        }

        public static readonly DependencyProperty HighlightIntervalProperty = DependencyProperty.Register(
            "HighlightInterval", typeof(int), typeof(BeatBar2), new FrameworkPropertyMetadata(4, FrameworkPropertyMetadataOptions.AffectsRender));

        public int HighlightInterval
        {
            get => (int)GetValue(HighlightIntervalProperty);
            set => SetValue(HighlightIntervalProperty, value);
        }

        public static readonly DependencyProperty PreviewHighlightIntervalProperty = DependencyProperty.Register(
            "PreviewHighlightInterval", typeof(int), typeof(BeatBar2), new FrameworkPropertyMetadata(4, FrameworkPropertyMetadataOptions.AffectsRender));

        public int PreviewHighlightInterval
        {
            get => (int) GetValue(PreviewHighlightIntervalProperty);
            set => SetValue(PreviewHighlightIntervalProperty, value);
        }

        public static readonly DependencyProperty HighlightOffsetProperty = DependencyProperty.Register(
            "HighlightOffset", typeof(int), typeof(BeatBar2), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public int HighlightOffset
        {
            get => (int)GetValue(HighlightOffsetProperty);
            set => SetValue(HighlightOffsetProperty, value);
        }

        public static readonly DependencyProperty Marker1Property = DependencyProperty.Register(
            "Marker1", typeof(TimeSpan), typeof(BeatBar2), new FrameworkPropertyMetadata(TimeSpan.MinValue, FrameworkPropertyMetadataOptions.AffectsRender));

        public TimeSpan Marker1
        {
            get => (TimeSpan)GetValue(Marker1Property);
            set => SetValue(Marker1Property, value);
        }

        public static readonly DependencyProperty Marker2Property = DependencyProperty.Register(
            "Marker2", typeof(TimeSpan), typeof(BeatBar2), new FrameworkPropertyMetadata(TimeSpan.MinValue, FrameworkPropertyMetadataOptions.AffectsRender));

        public TimeSpan Marker2
        {
            get => (TimeSpan)GetValue(Marker2Property);
            set => SetValue(Marker2Property, value);
        }

        public event EventHandler<TimeSpan> TimeMouseDown;

        public event EventHandler<TimeSpan> TimeMouseRightDown;
        public event EventHandler<TimeSpan> TimeMouseRightMove;
        public event EventHandler<TimeSpan> TimeMouseRightUp;

        public static readonly DependencyProperty LineColorProperty = DependencyProperty.Register(
            "LineColor", typeof(Color), typeof(BeatBar2), new PropertyMetadata(Colors.Lime, OnVisualPropertyChanged));

        public Color LineColor
        {
            get => (Color)GetValue(LineColorProperty);
            set => SetValue(LineColorProperty, value);
        }

        public static readonly DependencyProperty HighlightColorProperty = DependencyProperty.Register(
            "HighlightColor", typeof(Color), typeof(BeatBar2), new PropertyMetadata(Colors.Cyan, OnVisualPropertyChanged));

        public Color HighlightColor
        {
            get => (Color)GetValue(HighlightColorProperty);
            set => SetValue(HighlightColorProperty, value);
        }

        public static readonly DependencyProperty LineWidthProperty = DependencyProperty.Register(
            "LineWidth", typeof(double), typeof(BeatBar2), new PropertyMetadata(5.0d, OnVisualPropertyChanged));

        public double LineWidth
        {
            get => (double)GetValue(LineWidthProperty);
            set => SetValue(LineWidthProperty, value);
        }

        public static readonly DependencyProperty BeatsProperty = DependencyProperty.Register(
            "Beats", typeof(BeatCollection), typeof(BeatBar2), new PropertyMetadata(default(BeatCollection), OnVisualPropertyChanged));

        public BeatCollection Beats
        {
            get => (BeatCollection)GetValue(BeatsProperty);
            set => SetValue(BeatsProperty, value);
        }

        public static readonly DependencyProperty PreviewBeatsProperty = DependencyProperty.Register(
            "PreviewBeats", typeof(BeatCollection), typeof(BeatBar2), new PropertyMetadata(default(BeatCollection), OnVisualPropertyChanged));

        public BeatCollection PreviewBeats
        {
            get => (BeatCollection) GetValue(PreviewBeatsProperty);
            set => SetValue(PreviewBeatsProperty, value);
        }

        public static readonly DependencyProperty TotalDisplayedDurationProperty = DependencyProperty.Register(
            "TotalDisplayedDuration", typeof(TimeSpan), typeof(BeatBar2), new PropertyMetadata(TimeSpan.FromSeconds(5), OnVisualPropertyChanged));

        public TimeSpan TotalDisplayedDuration
        {
            get => (TimeSpan)GetValue(TotalDisplayedDurationProperty);
            set => SetValue(TotalDisplayedDurationProperty, value);
        }

        public static readonly DependencyProperty MidpointProperty = DependencyProperty.Register(
            "Midpoint", typeof(double), typeof(BeatBar2), new PropertyMetadata(0.5d, OnVisualPropertyChanged));

        public double Midpoint
        {
            get => (double)GetValue(MidpointProperty);
            set => SetValue(MidpointProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(TimeSpan), typeof(BeatBar2), new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        public static readonly DependencyProperty SpeedRatioProperty = DependencyProperty.Register(
            "SpeedRatio", typeof(double), typeof(BeatBar2), new PropertyMetadata((double)1.0));

        public double SpeedRatio
        {
            get => (double) GetValue(SpeedRatioProperty);
            set => SetValue(SpeedRatioProperty, value);
        }

        private bool _rightdown;
        private double _lastMousePosition;

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatBar2)d).RefreshRightMove();
            ((BeatBar2)d).InvalidateVisual();
        }

        public TimeSpan Progress
        {
            get => (TimeSpan)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
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
            if (!_rightdown)
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
            if (_rightdown)
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

        private bool _wasActive;
        private readonly MetronomeTick _tick = new MetronomeTick();

        protected override void OnRender(DrawingContext drawingContext)
        {
            #region Background and Selection

            TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
            TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);

            List<TimeSpan> absoluteBeatPositions = Beats?.GetBeats(timeFrom, timeTo).ToList() ?? new List<TimeSpan>();
            List<TimeSpan> absolutePreviewPositions =
                PreviewBeats?.GetBeats(timeFrom, timeTo).ToList() ?? new List<TimeSpan>();

            //WTF weird ...
            TimeSpan soundFileDelay = TimeSpan.FromMilliseconds(85 * Math.Max(0, 2 - SpeedRatio)); // TimeSpan.FromMilliseconds(100 + SpeedRatio < 1 ? 50 : 0);
            
            TimeSpan delayedProgress = Progress.Subtract(TimeSpan.FromMilliseconds(SoundDelay));

            delayedProgress = delayedProgress.Subtract(soundFileDelay);
            
            bool isActive = absoluteBeatPositions.Any(b => delayedProgress >= b  && delayedProgress <= b.Add(FlashDuration));

            if (SoundAfterBeat)
            {
                if (isActive && !_wasActive)
                    _tick.Tick();
            }
            _wasActive = isActive;

            isActive &= FlashAfterBeat;

            Rect fullRect = new Rect(new Point(), new Size(ActualWidth, ActualHeight));
            drawingContext.PushClip(new RectangleGeometry(fullRect));
            drawingContext.DrawRectangle(isActive ? Brushes.White : Background, null, fullRect);

            if (Marker1 != TimeSpan.MinValue && Marker2 != TimeSpan.MinValue)
            {
                TimeSpan earlier = Marker1 < Marker2 ? Marker1 : Marker2;
                TimeSpan later = Marker1 > Marker2 ? Marker1 : Marker2;

                if (later >= timeFrom && earlier <= timeTo)
                {
                    double xFrom = XFromTime(earlier);
                    double xTo = XFromTime(later);

                    SolidColorBrush brush = new SolidColorBrush(Colors.Cyan) { Opacity = 0.3 };

                    drawingContext.DrawRectangle(brush, null, new Rect(new Point(xFrom, 0), new Point(xTo, ActualHeight)));
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

            drawingContext.DrawLine(new Pen(isActive ? Brushes.Black : Brushes.Red, 11), new Point(XFromTime(Progress), 0), new Point(Midpoint * ActualWidth, ActualHeight));

            #endregion

            #region Beats

            if (absoluteBeatPositions.Count > 0)
            {
                int startingIndex = Beats.IndexOf(absoluteBeatPositions.First());
                int index = startingIndex;

                foreach (TimeSpan timePos in absoluteBeatPositions)
                {
                    double pos = (timePos - timeFrom).Divide(timeTo - timeFrom);
                    bool isSelected = IsInSelectedRange(timePos);
                    int loopIndex = (index + HighlightOffset) % HighlightInterval;

                    Color outerColor = LineColor;

                    if (HighlightBeats && loopIndex == 0)
                    {
                        outerColor = HighlightColor;
                    }

                    Color innerColor = isSelected ? Colors.White : Colors.LightBlue;

                    DrawLine(drawingContext, outerColor, innerColor, new Point(pos * ActualWidth, -5),
                        new Point(pos * ActualWidth, ActualHeight + 5), LineWidth);

                    index++;
                }
            }

            if (absolutePreviewPositions.Count > 0)
            {
                int index = 0;

                foreach (TimeSpan timePos in absolutePreviewPositions)
                {
                    double pos = (timePos - timeFrom).Divide(timeTo - timeFrom);
                    int loopIndex = index % PreviewHighlightInterval;

                    Color outerColor = (loopIndex == 0) ? Colors.Red : Colors.LawnGreen;
                    Color innerColor = Colors.Yellow;

                    DrawLine(drawingContext, outerColor, innerColor, new Point(pos * ActualWidth, ActualHeight / 2.0), new Point(pos * ActualWidth, ActualHeight + 5), LineWidth);
                    index++;
                }
            }

            #endregion

            drawingContext.Pop();
        }

        private bool IsInSelectedRange(TimeSpan timePos)
        {
            if (Marker1 == TimeSpan.MinValue || Marker2 == TimeSpan.MinValue)
                return false;

            TimeSpan earlier = Marker1 < Marker2 ? Marker1 : Marker2;
            TimeSpan later = Marker1 > Marker2 ? Marker1 : Marker2;

            return timePos >= earlier && timePos <= later;
        }

        private bool IsTimeVisible(TimeSpan time)
        {
            if (time < Progress - TotalDisplayedDuration.Multiply(Midpoint))
                return false;
            if (time > Progress + TotalDisplayedDuration.Multiply(1 - Midpoint))
                return false;

            return true;
        }

        private void DrawLine(DrawingContext drawingContext, Color outer, Color inner, Point pFrom, Point pTo, double lineWidth = 3)
        {
            pFrom.X = Math.Round(pFrom.X);
            pTo.X = Math.Round(pTo.X);

            drawingContext.DrawLine(new Pen(new SolidColorBrush(outer) { Opacity = 0.8 }, lineWidth) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
            drawingContext.DrawLine(new Pen(new SolidColorBrush(inner) { Opacity = 1.0 }, 1) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
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
