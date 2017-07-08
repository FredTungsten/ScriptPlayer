using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    /// <summary>
    /// Interaction logic for RangeSlider.xaml
    /// </summary>
    public partial class RangeSlider : UserControl
    {
        public static readonly DependencyProperty InsideBrushProperty = DependencyProperty.Register(
            "InsideBrush", typeof(Brush), typeof(RangeSlider), new PropertyMetadata(Brushes.Lime));

        public Brush InsideBrush
        {
            get { return (Brush) GetValue(InsideBrushProperty); }
            set { SetValue(InsideBrushProperty, value); }
        }

        public static readonly DependencyProperty OutsideBrushProperty = DependencyProperty.Register(
            "OutsideBrush", typeof(Brush), typeof(RangeSlider), new PropertyMetadata(Brushes.Red));

        public Brush OutsideBrush
        {
            get { return (Brush) GetValue(OutsideBrushProperty); }
            set { SetValue(OutsideBrushProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0, OnMinimumPropertyChanged));

        private static void OnMinimumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RangeSlider)d).OnMinimumChanged();
        }

        private void OnMinimumChanged()
        {
            RecalculateThumbPositions();
        }

        public double Minimum
        {
            get { return (double) GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(double), typeof(RangeSlider), new PropertyMetadata(1.0, OnMaximumPropertyChanged));

        private static void OnMaximumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RangeSlider)d).OnMaximumChanged();
        }

        private void OnMaximumChanged()
        {
            RecalculateThumbPositions();
        }

        public double Maximum
        {
            get { return (double) GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(
            "LowerValue", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.25, OnLowerValuePropertyChanged, CoerceLowerValue));

        private static object CoerceLowerValue(DependencyObject d, object basevalue)
        {
            if (!((RangeSlider) d).IsLoaded)
                return basevalue;

            double value = (double)basevalue;
            value = Math.Min((double)d.GetValue(MaximumProperty), value);
            value = Math.Max((double)d.GetValue(MinimumProperty), value);
            value = Math.Min((double)d.GetValue(UpperValueProperty), value);
            return value;
        }

        private static void OnLowerValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RangeSlider)d).OnLowerValueChanged();
        }

        private void OnLowerValueChanged()
        {
            RecalculateThumbPositions();
        }

        public double LowerValue
        {
            get { return (double) GetValue(LowerValueProperty); }
            set { SetValue(LowerValueProperty, value); }
        }

        public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(
            "UpperValue", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.75, OnUpperValuePropertyChanged, CoerceUpperValue));

        private double _startValue;
        private Point _startPosition;

        private static object CoerceUpperValue(DependencyObject d, object basevalue)
        {
            if (!((RangeSlider)d).IsLoaded)
                return basevalue;

            double value = (double) basevalue;
            value = Math.Min((double)d.GetValue(MaximumProperty), value);
            value = Math.Max((double)d.GetValue(MinimumProperty), value);
            value = Math.Max((double)d.GetValue(LowerValueProperty), value);
            return value;
        }

        private static void OnUpperValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RangeSlider) d).OnUpperValueChanged();
        }

        private void OnUpperValueChanged()
        {
            RecalculateThumbPositions();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            RecalculateThumbPositions();
        }

        private void RecalculateThumbPositions()
        {
            if (ActualWidth <= 16) return;

            double trackWidth = ActualWidth - 8.0 * 2;

            double lowerValue = GetRelativeValue(LowerValue, Minimum, Maximum);
            double rightEdgeOfLowerThumb = trackWidth * lowerValue + 8;
            double leftMarginOfLowerThumb = rightEdgeOfLowerThumb - thumbLower.ActualWidth;
            Thickness m = thumbLower.Margin;
            thumbLower.Margin = new Thickness(leftMarginOfLowerThumb, m.Top, m.Right, m.Bottom);

            double upperValue = GetRelativeValue(UpperValue, Minimum, Maximum);
            double leftEdgeOfUpperThumb = trackWidth * upperValue + 8;
            m = thumbUpper.Margin;
            thumbUpper.Margin = new Thickness(leftEdgeOfUpperThumb, m.Top, m.Right, m.Bottom);

            RectLeft.Width = rightEdgeOfLowerThumb - 8;
            RectRight.Width = ActualWidth - leftEdgeOfUpperThumb - 8;

            RectCenter.Width = leftEdgeOfUpperThumb - rightEdgeOfLowerThumb;
            RectCenter.Margin = new Thickness(rightEdgeOfLowerThumb,8,0,8);
        }

        private double GetRelativeValue(double value, double minimum, double maximum)
        {
            return (value - minimum) / (maximum - minimum);
        }

        public double UpperValue
        {
            get { return (double) GetValue(UpperValueProperty); }
            set { SetValue(UpperValueProperty, value); }
        }

        public RangeSlider()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            LowerValue = (double)CoerceLowerValue(this, LowerValue);
            UpperValue = (double)CoerceUpperValue(this, UpperValue);

            RecalculateThumbPositions();
        }

        private void Thumb_OnDragStarted(object sender, DragStartedEventArgs e)
        {
            _startValue = ReferenceEquals(sender, thumbLower) ? LowerValue : UpperValue;
            _startPosition = Mouse.GetPosition(this);
        }

        private void Thumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            Point position = Mouse.GetPosition(this);
            double horizontalChange = position.X - _startPosition.X;

            double delta = (horizontalChange / (ActualWidth - 8.0 * 2)) * (Maximum - Minimum);
            if (ReferenceEquals(sender, thumbLower))
                LowerValue = (double)CoerceLowerValue(this, _startValue + delta);
            else
                UpperValue = (double)CoerceUpperValue(this, _startValue + delta);
        }

        private void Thumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            Point position = Mouse.GetPosition(this);
            double horizontalChange = position.X - _startPosition.X;

            double delta = (horizontalChange / (ActualWidth - 8.0 * 2)) * (Maximum - Minimum);
            if (ReferenceEquals(sender, thumbLower))
                LowerValue = (double) CoerceLowerValue(this, _startValue + delta);
            else
                UpperValue = (double) CoerceUpperValue(this, _startValue + delta);
        }
    }
}
