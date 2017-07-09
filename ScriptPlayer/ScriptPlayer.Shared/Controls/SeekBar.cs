using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class SeekBar : Control
    {

        public delegate void ClickedEventHandler(object sender, double relative, TimeSpan absolute, int downMoveUp);

        public event ClickedEventHandler Seek;

        static SeekBar()
        {
            BackgroundProperty.OverrideMetadata(typeof(SeekBar), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
        }


        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(TimeSpan), typeof(SeekBar), new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(SeekBar), new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        private bool _down;

        public TimeSpan Duration
        {
            get { return (TimeSpan) GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SeekBar)d).InvalidateVisual();
        }

        public TimeSpan Progress
        {
            get { return (TimeSpan) GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        private Popup _popup;
        private TextBox _txt;

        public SeekBar()
        {
            InitializePopup();
        }

        private void InitializePopup()
        {
            _txt = new TextBox
            {
                Padding = new Thickness(3)
            };

            Border border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                Child = _txt
            };

            _popup = new Popup
            {
                IsOpen = false,
                Placement = PlacementMode.Top,
                PlacementTarget = this,
                Child = border
            };
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.Black, null, new Rect(new Point(0, 0), new Size(ActualWidth, ActualHeight)));
            dc.DrawRectangle(Background, null, new Rect(new Point(0,0), new Size(ActualWidth, ActualHeight)));

            if (Duration == TimeSpan.Zero) return;

            double linePosition = Progress.Divide(Duration) * ActualWidth;

            dc.DrawRectangle(new LinearGradientBrush(HeatMapGenerator.GradientsSmoothFromColors(0.5, Color.FromArgb(0,0,0,0), Color.FromArgb(150,0,0,0), Color.FromArgb(0, 0, 0, 0)), new Point(0,0), new Point(1,0)), null, new Rect(linePosition-10,0,20, ActualHeight));

            dc.DrawLine(new Pen(Brushes.White, 1), new Point(linePosition,0), new Point(linePosition, ActualHeight));
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (Duration == TimeSpan.Zero) return;
            if (!_down) return;

            ReleaseMouseCapture();
            _down = false;
            Point p = e.GetPosition(this);

            if (IsMouseOver)
            {
                UpdatePopup(p);
            }
            else
            {
                ClosePopup();
            }

            double relativePosition = GetRelativePosition(p.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            OnSeek(relativePosition, absolutePosition, 2);
        }

        private void ClosePopup()
        {
            _popup.IsOpen = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point p = e.GetPosition(this);
            UpdatePopup(p);

            if (!_down) return;
            if (Duration == TimeSpan.Zero) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            double relativePosition = GetRelativePosition(p.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            OnSeek(relativePosition, absolutePosition, 1);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            UpdatePopup(e.GetPosition(this));
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (_down)
                return;

            ClosePopup();
        }

        private void UpdatePopup(Point point)
        {
            if (Duration == TimeSpan.Zero) return;

            double relativePosition = GetRelativePosition(point.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            if(Duration >= TimeSpan.FromHours(1))
                _txt.Text = $"{absolutePosition.Hours:00}:{absolutePosition.Minutes:00}:{absolutePosition.Seconds:00}";
            else
                _txt.Text = $"{absolutePosition.Minutes:00}:{absolutePosition.Seconds:00}";

            _popup.IsOpen = true;
            _popup.HorizontalOffset = relativePosition * ActualWidth - ((FrameworkElement)_popup.Child).ActualWidth / 2.0;
        }

        private double GetRelativePosition(double x)
        {
            double progress = x / Math.Max(1, ActualWidth);
            progress = Math.Min(1.0, Math.Max(0, progress));
            return progress;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (Duration == TimeSpan.Zero) return;

            _down = true;
            CaptureMouse();

            Point p = e.GetPosition(this);
            UpdatePopup(p);

            double relativePosition = GetRelativePosition(p.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            OnSeek(relativePosition, absolutePosition, 0);
        }

        protected virtual void OnSeek(double relative, TimeSpan absolute, int downMoveUp)
        {
            Seek?.Invoke(this, relative, absolute, downMoveUp);
        }
    }
}
