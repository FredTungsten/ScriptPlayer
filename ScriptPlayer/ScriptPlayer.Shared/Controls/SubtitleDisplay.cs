using System;
using System.Collections.Generic;

using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ScriptPlayer.Shared.Subtitles;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace ScriptPlayer.Shared
{
    public class SubtitleDisplay : Control
    {
        public static readonly DependencyProperty TimeSourceProperty = DependencyProperty.Register(
            "TimeSource", typeof(TimeSource), typeof(SubtitleDisplay), new PropertyMetadata(default(TimeSource), OnTimeSourcePropertyChanged));

        private SubtitleHandler _handler;
        private string _text;
        private List<SubtitleEntry> _entries;

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
                _entries = activeEntries;
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

            double fontSize = this.ActualHeight * 0.06;
            double borderSize = fontSize * 0.05;

            NumberSubstitution numSub = new NumberSubstitution();
            foreach (SubtitleEntry entry in _entries)
            {
                FormattedText text = new FormattedText(entry.Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), fontSize, Brushes.White, numSub, TextFormattingMode.Display, 96);

                text.MaxTextWidth = this.ActualWidth;

                Geometry g = text.BuildGeometry(new Point(0, 0));

                Point offset = new Point((this.ActualWidth - g.Bounds.Width) / 2, this.ActualHeight * 0.9 - g.Bounds.Height);

                drawingContext.PushTransform(new TranslateTransform(offset.X, offset.Y));

                drawingContext.DrawGeometry(Brushes.Black, new Pen(Brushes.Black, borderSize * 2), g);
                drawingContext.DrawGeometry(Brushes.White, null, g);

                drawingContext.Pop();

                //drawingContext.DrawText(text, new Point(0, 0));
            }
        }
    }
}
