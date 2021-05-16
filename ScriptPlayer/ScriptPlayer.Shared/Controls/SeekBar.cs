using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ScriptPlayer.Shared.Classes;
using ScriptPlayer.Shared.Converters;

namespace ScriptPlayer.Shared
{
    public class SeekBar : Control
    {
        public delegate void ClickedEventHandler(object sender, double relative, TimeSpan absolute, int downMoveUp);

        public static readonly DependencyProperty OverlayProperty = DependencyProperty.Register(
            "Overlay", typeof(Brush), typeof(SeekBar),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OverlayOpacityProperty = DependencyProperty.Register(
            "OverlayOpacity", typeof(Brush), typeof(SeekBar),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OverlayGeometryProperty = DependencyProperty.Register(
            "OverlayGeometry", typeof(Geometry), typeof(SeekBar), new FrameworkPropertyMetadata(default(Geometry), FrameworkPropertyMetadataOptions.AffectsRender));

        public Geometry OverlayGeometry
        {
            get { return (Geometry) GetValue(OverlayGeometryProperty); }
            set { SetValue(OverlayGeometryProperty, value); }
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(TimeSpan), typeof(SeekBar),
            new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(SeekBar),
            new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        public static readonly DependencyProperty HoverPositionProperty = DependencyProperty.Register(
            "HoverPosition", typeof(TimeSpan), typeof(SeekBar), new PropertyMetadata(default(TimeSpan)));

        public static readonly DependencyProperty ThumbnailsProperty = DependencyProperty.Register(
            "Thumbnails", typeof(VideoThumbnailCollection), typeof(SeekBar), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnThumbnailsPropertyChanged));

        private static void OnThumbnailsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SeekBar) d).ThumbnailsChanged();
        }

        private void ThumbnailsChanged()
        {
            if (_img != null)
                _img.Thumbnails = Thumbnails;
        }

        public static readonly DependencyProperty HighlightRangeProperty = DependencyProperty.Register(
            "HighlightRange", typeof(Section), typeof(SeekBar), new FrameworkPropertyMetadata(default(Section), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PositionsProperty = DependencyProperty.Register(
            "Positions", typeof(PositionCollection), typeof(SeekBar), new FrameworkPropertyMetadata(default(PositionCollection), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ThumbnailModesProperty = DependencyProperty.Register(
            "ThumbnailModes", typeof(ThumbnailModes), typeof(SeekBar), new PropertyMetadata(ThumbnailModes.One));

        public ThumbnailModes ThumbnailModes
        {
            get => (ThumbnailModes) GetValue(ThumbnailModesProperty);
            set => SetValue(ThumbnailModesProperty, value);
        }

        public PositionCollection Positions
        {
            get => (PositionCollection) GetValue(PositionsProperty);
            set => SetValue(PositionsProperty, value);
        }

        public Section HighlightRange
        {
            get => (Section) GetValue(HighlightRangeProperty);
            set => SetValue(HighlightRangeProperty, value);
        }

        public VideoThumbnailCollection Thumbnails
        {
            get => (VideoThumbnailCollection) GetValue(ThumbnailsProperty);
            set => SetValue(ThumbnailsProperty, value);
        }

        public TimeSpan HoverPosition
        {
            get => (TimeSpan) GetValue(HoverPositionProperty);
            set => SetValue(HoverPositionProperty, value);
        }

        private bool _down;
        private Popup _popup;
        private TextBlock _txt;
        private StackPanel _stack;
        private ThumbnailPreview _img;
        private PositionBar _pos;
        private double _calculatedHorizontalOffset;
        private Window _window;

        static SeekBar()
        {
            BackgroundProperty.OverrideMetadata(typeof(SeekBar),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
        }

        public SeekBar()
        {
            ClipToBounds = true;
            InitializePopup();
        }

        public Brush Overlay
        {
            get => (Brush) GetValue(OverlayProperty);
            set => SetValue(OverlayProperty, value);
        }

        public Brush OverlayOpacity
        {
            get => (Brush) GetValue(OverlayOpacityProperty);
            set => SetValue(OverlayOpacityProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan) GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public TimeSpan Progress
        {
            get => (TimeSpan) GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public event ClickedEventHandler Seek;

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SeekBar) d).InvalidateVisual();
        }

        private void InitializePopup()
        {
            _stack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            _img = new ThumbnailPreview
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Thumbnails = Thumbnails
            };

            _txt = new TextBlock
            {
                Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _pos = new PositionBar
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 20,
                TotalDisplayedDuration = TimeSpan.FromSeconds(16),
                DrawCircles = false,
                DrawLines = false,
                DrawZero = true,
                Background = Brushes.Black
            };

            BindPositions(_pos);
            BindText(_txt);

            _stack.Children.Add(_img);
            _stack.Children.Add(_pos);
            _stack.Children.Add(_txt);

            Border border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                Child = _stack
            };

            _popup = new Popup
            {
                IsOpen = false,
                Placement = PlacementMode.Custom,
                CustomPopupPlacementCallback = CustomPopupPlacementCallback,
                PlacementTarget = this,
                Child = border
            };
        }

        private void BindPositions(PositionBar pos)
        {
            BindingOperations.SetBinding(pos, PositionBar.PositionsProperty, new Binding("Positions") {Source = this});
        }

        private void BindText(TextBlock txt)
        {
            MultiBinding binding = new MultiBinding{ Converter = new SeekBarPositionConverter()};
            binding.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(ProgressProperty) });
            binding.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(DurationProperty) });
            binding.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(HoverPositionProperty) });

            BindingOperations.SetBinding(txt, TextBlock.TextProperty, binding);
        }

        protected override void OnRender(DrawingContext dc)
        {
            Rect rect = new Rect(new Point(0, 0), new Size(ActualWidth, ActualHeight));

            dc.PushClip(new RectangleGeometry(rect));

            dc.DrawRectangle(Background, null, rect);

            if (HighlightRange != null && !HighlightRange.IsEmpty)
            {
                double x1 = Math.Min(1, Math.Max(0, HighlightRange.Start.Divide(Duration))) * ActualWidth;
                double x2 = Math.Min(1, Math.Max(0, HighlightRange.End.Divide(Duration))) * ActualWidth;

                Color inRangeBackgroundColor = Color.FromArgb(255, 35, 35, 35);
                Brush inRangeBrush = new SolidColorBrush(inRangeBackgroundColor);

                dc.DrawRectangle(inRangeBrush, null, new Rect(new Point(x1, 0), new Point(x2, ActualHeight)));
            }

            dc.PushOpacityMask(OverlayOpacity);

            bool overlayGeometry = false;

            if (OverlayGeometry != null)
            {
                Geometry geo = OverlayGeometry.Clone();
                geo.Transform = new ScaleTransform(ActualWidth, ActualHeight);

                overlayGeometry = true;
                dc.PushClip(geo);
            }


            dc.DrawRectangle(Overlay, null, rect);

            if(overlayGeometry)
                dc.Pop();

            dc.Pop();

            if (Duration == TimeSpan.Zero) return;

            double linePosition = Progress.Divide(Duration) * ActualWidth;

            linePosition = RoundLinePosition(linePosition);

            dc.DrawLine(new Pen(Brushes.Black, 3), new Point(linePosition, 0), new Point(linePosition, ActualHeight));
            dc.DrawLine(new Pen(Brushes.White, 1), new Point(linePosition, 0), new Point(linePosition, ActualHeight));

            double outOfRangeOpacity = 0.5;

            if (HighlightRange != null && !HighlightRange.IsEmpty)
            {
                double x1 = Math.Min(1, Math.Max(0, HighlightRange.Start.Divide(Duration))) * ActualWidth;
                double x2 = Math.Min(1, Math.Max(0, HighlightRange.End.Divide(Duration))) * ActualWidth;

                Color outOfRangeOverlayColor = Color.FromArgb((byte) (255 * outOfRangeOpacity), 0, 0, 0);
                Brush outOfRangeBrush = new SolidColorBrush(outOfRangeOverlayColor);

                if (x1 > 0)
                    dc.DrawRectangle(outOfRangeBrush, null, new Rect(0, 0, x1, ActualHeight));

                if(x2 < ActualWidth)
                    dc.DrawRectangle(outOfRangeBrush, null, new Rect(x2, 0, ActualWidth - x2, ActualHeight));
            }

            dc.Pop();
        }

        private double RoundLinePosition(double linePosition)
        {
            return Math.Round(linePosition - 0.5) + 0.5;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (Duration == TimeSpan.Zero) return;
            if (!_down) return;

            ReleaseMouseCapture();
            _down = false;
            Point p = e.GetPosition(this);

            if (IsMouseOver)
                UpdatePopup(p);
            else
                ClosePopup();

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
            if (Duration == TimeSpan.Zero)
                return;

            if (_window == null)
                _window = Window.GetWindow(this);

            double relativePosition = GetRelativePosition(point.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            HoverPosition = absolutePosition;

            if (Thumbnails != null)
            {
                _img.TimeStamp = absolutePosition;
                _img.Visibility = Visibility.Visible;
            }
            else
            {
                _img.Visibility = Visibility.Collapsed;
            }

            if (Positions != null)
            {
                _pos.Progress = absolutePosition;
                _pos.Visibility = Visibility.Visible;   
            }
            else
            {
                _pos.Visibility = Visibility.Collapsed;
            }

            _calculatedHorizontalOffset = relativePosition * ActualWidth -
                                          ((FrameworkElement)_popup.Child).ActualWidth / 2.0;

            _popup.VerticalOffset = 1 - _popup.VerticalOffset;

            _popup.IsOpen = true;
        }

        private CustomPopupPlacement[] CustomPopupPlacementCallback(Size popupSize, Size targetSize, Point offset)
        {
            //Debug.WriteLine($"Size: {popupSize.Width:f0} x {popupSize.Height:f0}, offset = {offset.X:f0}/{offset.Y:f0}");
            //Debug.WriteLine($"x = {offset.X} - {offset.X + popupSize.Width}");

            double verticalOffset = -popupSize.Height - offset.Y;
            double horizontalOffset = _calculatedHorizontalOffset - offset.X;

            double left = _window.TranslatePoint(new Point(0, 0), this).X;
            double right = _window.TranslatePoint(new Point(_window.ActualWidth, 0), this).X - popupSize.Width;

            horizontalOffset = Math.Min(right, Math.Max(left, horizontalOffset));

            return new[]
            {
                new CustomPopupPlacement(new Point(horizontalOffset, verticalOffset), PopupPrimaryAxis.Horizontal), 
            };
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

    public enum ThumbnailModes
    {
        One,
        Many
    }
}