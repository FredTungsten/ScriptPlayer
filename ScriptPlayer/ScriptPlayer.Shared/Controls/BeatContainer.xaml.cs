using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    /// <summary>
    /// Interaction logic for BeatContainer.xaml
    /// </summary>
    public partial class BeatContainer : UserControl
    {
        public static readonly DependencyProperty PrimaryColorProperty = DependencyProperty.Register(
            "PrimaryColor", typeof(Color), typeof(BeatContainer), new PropertyMetadata(Colors.Red, OnPrimaryColorPropertyChanged));

        private static void OnPrimaryColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatContainer) d).RefreshColors();
        }

        public static readonly DependencyProperty PrimaryBrushProperty = DependencyProperty.Register(
            "PrimaryBrush", typeof(Brush), typeof(BeatContainer), new PropertyMetadata(default(Brush)));

        public Brush PrimaryBrush
        {
            get { return (Brush) GetValue(PrimaryBrushProperty); }
            set { SetValue(PrimaryBrushProperty, value); }
        }

        private void RefreshColors()
        {
            PrimaryBrush = new SolidColorBrush(PrimaryColor);
        }

        public Color PrimaryColor
        {
            get { return (Color) GetValue(PrimaryColorProperty); }
            set { SetValue(PrimaryColorProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title", typeof(string), typeof(BeatContainer), new PropertyMetadata(default(string)));

        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty SegmentProperty = DependencyProperty.Register(
            "Segment", typeof(BeatSegment), typeof(BeatContainer), new PropertyMetadata(default(BeatSegment), OnBeatSegmentPropertyChanged));

        private static void OnBeatSegmentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatContainer)d).SetBeatSegment(e.NewValue as BeatSegment);
        }

        public BeatSegment Segment
        {
            get { return (BeatSegment) GetValue(SegmentProperty); }
            set { SetValue(SegmentProperty, value); }
        }

        public BeatContainer()
        {
            InitializeComponent();

            SetBeat(new BeatDefinition { Pattern = new[] {true, false, true, false, true, false, true, false}});
        }

        private void BeatContainer_OnLoaded(object sender, RoutedEventArgs e)
        {
            RefreshColors();
        }

        public BeatSegment GetBeatSegment()
        {
            BeatSegment summary = BeatLine.GetBeatSegment();
            summary.Duration = TimePanel.GetDuration(this).Ticks;
            summary.Position = TimePanel.GetPosition(this).Ticks;

            return summary;
        }

        public void SetBeatSegment(BeatSegment summary)
        {
            TimePanel.SetPosition(this, TimeSpan.FromTicks(summary.Position));
            TimePanel.SetDuration(this, TimeSpan.FromTicks(summary.Duration));

            BeatLine.SetBeatSegment(summary);
        }

        public void SetBeat(BeatDefinition beatDefinition)
        {
            BeatLine.BeatDefinition = beatDefinition;
        }

        public void SnapDuration()
        {
            BeatLine.Snap();
        }

        public IEnumerable<TimeSpan> GetBeats()
        {
            return BeatLine.GetBeats();
        }

        public void SetBeatDuration(TimeSpan duration)
        {
            BeatLine.SetBeatDuration(duration);
        }
    }
}
