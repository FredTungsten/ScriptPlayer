using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScriptPlayer.Shared.Subtitles;

namespace ScriptPlayer.Shared
{
    public class SubtitleDisplay : Control
    {
        public static readonly DependencyProperty TimeSourceProperty = DependencyProperty.Register(
            "TimeSource", typeof(TimeSource), typeof(SubtitleDisplay), new PropertyMetadata(default(TimeSource), OnTimeSourcePropertyChanged));

        private SubtitleHandler _handler;
        private string _text;

        private static void OnTimeSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SubtitleDisplay) d).TimeSourceChanged(e.OldValue as TimeSource, e.NewValue as TimeSource);
        }

        public void SetSubtitles(IEnumerable<SubtitleEntry> entries)
        {
            _handler.SetSubtitles(entries);
        }

        private void TimeSourceChanged(TimeSource oldTimeSource, TimeSource newTimeSource)
        {
            if (oldTimeSource != null)
                oldTimeSource.ProgressChanged -= TimeSource_ProgressChanged;

            if (newTimeSource != null)
            {
                newTimeSource.ProgressChanged += TimeSource_ProgressChanged;
                TimeSource_ProgressChanged(newTimeSource, newTimeSource.Progress);
            }
        }

        private void TimeSource_ProgressChanged(object sender, TimeSpan timeSpan)
        {
            List<SubtitleEntry> activeEntries = _handler.GetActiveEntries(timeSpan);
            string newText = "";
            foreach (SubtitleEntry entry in activeEntries)
                newText += entry.Markup;

            if (newText != _text)
            {
                _text = newText;
                InvalidateVisual();
            }
        }

        public TimeSource TimeSource
        {
            get => (TimeSource) GetValue(TimeSourceProperty);
            set => SetValue(TimeSourceProperty, value);
        }

        public SubtitleDisplay()
        {
            _handler = new SubtitleHandler();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (string.IsNullOrWhiteSpace(_text))
                return;

            NumberSubstitution numSub = new NumberSubstitution();
            FormattedText text = new FormattedText(_text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 12, Brushes.White, numSub, TextFormattingMode.Display, 96 );
            drawingContext.DrawText(text, new Point(0,0));
        }
    }
}
