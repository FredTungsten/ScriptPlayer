using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class BeatBar2 : TimelineBaseControl
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

        public static readonly DependencyProperty SoundDelayProperty = DependencyProperty.Register(
            "SoundDelay", typeof(double), typeof(BeatBar2), new PropertyMetadata(default(double)));

        public double SoundDelay
        {
            get => (double) GetValue(SoundDelayProperty);
            set => SetValue(SoundDelayProperty, value);
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

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatBar2)d).VisualPropertyChanged();
        }
        
        protected override void Render(TimeBasedRenderContext context)
        {
            #region Background and Selection
            
            List<TimeSpan> absoluteBeatPositions = Beats?.GetBeats(context.TimeFrame.From.Add(TimeSpan.FromSeconds(-1)), context.TimeFrame.To.Add(TimeSpan.FromSeconds(1))).ToList() ?? new List<TimeSpan>();
            List<TimeSpan> absolutePreviewPositions =
                PreviewBeats?.GetBeats(context.TimeFrame.From, context.TimeFrame.To).ToList() ?? new List<TimeSpan>();

            context.DrawingContext.DrawRectangle(Background, null, context.FullRect);

            if (Marker1 != TimeSpan.MinValue && Marker2 != TimeSpan.MinValue)
            {
                TimeSpan earlier = Marker1 < Marker2 ? Marker1 : Marker2;
                TimeSpan later = Marker1 > Marker2 ? Marker1 : Marker2;

                if (later >= context.TimeFrame.From && earlier <= context.TimeFrame.To)
                {
                    double xFrom = XFromTime(earlier);
                    double xTo = XFromTime(later);

                    SolidColorBrush brush = new SolidColorBrush(Colors.Cyan) { Opacity = 0.3 };

                    context.DrawingContext.DrawRectangle(brush, null, new Rect(new Point(xFrom, 0), new Point(xTo, ActualHeight)));
                }
            }

            if (Marker1 != TimeSpan.MinValue && IsTimeVisible(Marker1))
            {
                double x = XFromTime(Marker1);
                context.DrawingContext.DrawLine(new Pen(Brushes.Cyan, 1), new Point(x, 0), new Point(x, ActualHeight));
            }

            if (Marker2 != TimeSpan.MinValue && IsTimeVisible(Marker2))
            {
                double x = XFromTime(Marker2);
                context.DrawingContext.DrawLine(new Pen(Brushes.Cyan, 1), new Point(x, 0), new Point(x, ActualHeight));
            }

            #endregion

            #region MidPoint

            context.DrawingContext.DrawLine(new Pen(Brushes.Red, 11), new Point(XFromTime(TimeFrameContext.Progress), 0), new Point(TimeFrameContext.Midpoint * ActualWidth, ActualHeight));

            #endregion

            #region Beats

            if (absoluteBeatPositions.Count > 0)
            {
                int startingIndex = Beats.IndexOf(absoluteBeatPositions.First());
                int index = startingIndex;

                foreach (TimeSpan timePos in absoluteBeatPositions)
                {
                    double posX = context.GetAbsoluteXPosition(timePos);
                    bool isSelected = IsInSelectedRange(timePos);
                    int loopIndex = (index + HighlightOffset) % HighlightInterval;

                    Color outerColor = LineColor;

                    if (HighlightBeats && loopIndex == 0)
                    {
                        outerColor = HighlightColor;
                    }

                    Color innerColor = isSelected ? Colors.White : Colors.LightBlue;

                    DrawLine(context.DrawingContext, outerColor, innerColor, new Point(posX, -5),
                        new Point(posX, ActualHeight + 5), LineWidth);

                    index++;
                }
            }

            if (absolutePreviewPositions.Count > 0)
            {
                int index = 0;

                foreach (TimeSpan timePos in absolutePreviewPositions)
                {
                    double posX = context.GetAbsoluteXPosition(timePos);
                    int loopIndex = index % PreviewHighlightInterval;

                    Color outerColor = (loopIndex == 0) ? Colors.Red : Colors.LawnGreen;
                    Color innerColor = Colors.Yellow;

                    DrawLine(context.DrawingContext, outerColor, innerColor, new Point(posX, ActualHeight / 2.0), new Point(posX, ActualHeight + 5), LineWidth);
                    index++;
                }
            }

            #endregion
        }

        private bool IsInSelectedRange(TimeSpan timePos)
        {
            if (Marker1 == TimeSpan.MinValue || Marker2 == TimeSpan.MinValue)
                return false;

            TimeSpan earlier = Marker1 < Marker2 ? Marker1 : Marker2;
            TimeSpan later = Marker1 > Marker2 ? Marker1 : Marker2;

            return timePos >= earlier && timePos <= later;
        }

        private void DrawLine(DrawingContext drawingContext, Color outer, Color inner, Point pFrom, Point pTo, double lineWidth = 3)
        {
            pFrom.X = Math.Round(pFrom.X);
            pTo.X = Math.Round(pTo.X);

            drawingContext.DrawLine(new Pen(new SolidColorBrush(outer) { Opacity = 0.8 }, lineWidth) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
            drawingContext.DrawLine(new Pen(new SolidColorBrush(inner) { Opacity = 1.0 }, 1) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
        }
    }
}
