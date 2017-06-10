using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class BeatLine : Control
    {
        public static readonly DependencyProperty BeatDefinitionProperty = DependencyProperty.Register(
            "BeatDefinition", typeof(BeatDefinition), typeof(BeatLine), new PropertyMetadata(default(BeatDefinition), OnVisualPropertyChanged));

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatLine)d).InvalidateVisual();
        }

        public BeatDefinition BeatDefinition
        {
            get { return (BeatDefinition) GetValue(BeatDefinitionProperty); }
            set { SetValue(BeatDefinitionProperty, value); }
        }

        public static readonly DependencyProperty PatternDurationProperty = DependencyProperty.Register(
            "PatternDuration", typeof(TimeSpan), typeof(BeatLine), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan PatternDuration
        {
            get { return (TimeSpan) GetValue(PatternDurationProperty); }
            set { SetValue(PatternDurationProperty, value); }
        }

        public bool TimeLock
        {
            get { return _timeLock; }
            set
            {
                _timeLock = value;
                if (_timeLock)
                {
                    EnsureParent();
                    if (_parent == null) return;

                    TimeSpan duration = TimePanel.GetDuration(_parent);

                    if (duration == TimeSpan.Zero) return;

                    _patternRepeats = duration.Divide(PatternDuration);
                }
                else
                {
                    EnsureParent();
                    if (_parent == null) return;

                    TimeSpan duration = TimePanel.GetDuration(_parent);

                    if (duration == TimeSpan.Zero) return;

                    PatternDuration = duration.Divide(_patternRepeats);
                }
                InvalidateVisual();
            }
        }

        public double _patternRepeats;

        public BeatLine()
        {
            ClipToBounds = true;
            PatternDuration = TimeSpan.FromSeconds(1);
        }

        public List<TimeSpan> GetBeats()
        {
            List<TimeSpan> result = new List<TimeSpan>();

            TimeSpan threshold = TimeSpan.FromMilliseconds(10);

            TimeSpan position = TimePanel.GetPosition(_parent);
            TimeSpan duration = TimePanel.GetDuration(_parent);

            TimeSpan currentPosition = position;
            TimeSpan endposition = position + duration;

            var pattern = BeatDefinition.Pattern;

            TimeSpan beatLength = PatternDuration.Divide(pattern.Length);

            while (endposition - currentPosition > threshold)
            {
                foreach (bool isactive in pattern)
                {
                    if(isactive)
                        result.Add(currentPosition);
                    currentPosition += beatLength;

                    if (endposition - currentPosition <= threshold)
                        break;
                }
            }

            return result;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0,0,ActualWidth,ActualHeight));

            if (BeatDefinition == null) return;
            if (PatternDuration == TimeSpan.Zero) return;
            if (BeatDefinition.Pattern == null) return;
            if (BeatDefinition.Pattern.Length == 0) return;

            EnsureParent();
            if (_parent == null) return;

            TimeSpan duration = TimePanel.GetDuration(_parent);

            if (duration <= TimeSpan.Zero) return;

            TimeSpan handledDuration = TimeSpan.Zero;
            double widthPerBeat;

            if (!TimeLock)
            {
                widthPerBeat = ActualWidth / duration.Divide(PatternDuration) / BeatDefinition.Pattern.Length;
            }
            else
            {
                widthPerBeat = ActualWidth / (BeatDefinition.Pattern.Length * _patternRepeats);
                PatternDuration = duration.Divide(_patternRepeats);
            }

            double x = 0;
            bool on = false;

            double widthPerCylce = ActualWidth / duration.Divide(PatternDuration);

            var typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal,
                FontStretches.Normal);

            int i = 1;
            while (handledDuration < duration)
            {
                on ^= true;

                if(on)
                    drawingContext.DrawRectangle(Brushes.White,null, new Rect(x,0,widthPerCylce,ActualHeight));

                var text = new FormattedText(i.ToString("D"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12.0, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display, 96);

                drawingContext.DrawText(text, new Point(x,0));

                handledDuration += PatternDuration;
                x += widthPerCylce;
                i++;
            }

            handledDuration = TimeSpan.Zero;

            x = 0;
            while (handledDuration < duration)
            {
                foreach (bool isbeat in BeatDefinition.Pattern)
                {
                    if(isbeat)
                        drawingContext.DrawLine(new Pen(Brushes.Lime, 4), new Point(x,0),new Point(x, ActualHeight));

                    x += widthPerBeat;
                }

                handledDuration += PatternDuration;
            }

            drawingContext.DrawEllipse(TimeLock?Brushes.Green:Brushes.Red,null, new Point(6,6),3,3 );
        }

        private DependencyObject _parent;
        private bool _timeLock;

        private void EnsureParent()
        {
            if (_parent != null) return;

            do
            {
                _parent = VisualTreeHelper.GetParent(_parent ?? this);
            } while (_parent != null && !(_parent is BeatContainer));
        }

        public void SetBeatSegment(BeatSegment summary)
        {
            _timeLock = summary.TimeLocked;

            if (_timeLock)
                _patternRepeats = summary.Duration / (double)summary.PatternDuration;

            PatternDuration = TimeSpan.FromTicks(summary.PatternDuration);
            BeatDefinition = summary.Beat;
        }

        public void SetBeatDuration(TimeSpan beatDuration)
        {
            EnsureParent();
            if (_parent == null) return;

            TimeSpan duration = TimePanel.GetDuration(_parent);

            if (duration <= TimeSpan.Zero) return;

            _timeLock = false;
            _patternRepeats = duration.Divide(beatDuration);
            PatternDuration = beatDuration;

            InvalidateVisual();
        }

        public BeatSegment GetBeatSegment()
        {
            BeatSegment summary = new BeatSegment();
            summary.Beat = BeatDefinition;
            summary.PatternDuration = PatternDuration.Ticks;
            summary.TimeLocked = TimeLock;

            return summary;
        }

        public void Snap()
        {
            EnsureParent();
            if (_parent == null) return;

            TimeSpan duration = TimePanel.GetDuration(_parent);

            if (duration <= TimeSpan.Zero) return;

            if(!TimeLock)
                _patternRepeats = duration.Divide(PatternDuration);

            int repeats = (int) Math.Round(_patternRepeats);
            if (repeats <= 0)
                repeats = 1;

            duration = PatternDuration.Multiply(repeats);
            _patternRepeats = repeats;

            TimePanel.SetDuration(_parent, duration);
        }
    }
}
