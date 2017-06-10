using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class ColorSampleBar : Control
    {
        public static readonly DependencyProperty TimeSourceProperty = DependencyProperty.Register(
            "TimeSource", typeof(TimeSource), typeof(ColorSampleBar), new PropertyMetadata(default(TimeSource), OnTimeSourcePropertyChanged));

        private static void OnTimeSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorSampleBar)d).OnTimeSourceChanged(e.OldValue as TimeSource, e.NewValue as TimeSource);
        }

        private void OnTimeSourceChanged(TimeSource oldSource, TimeSource newSource)
        {
            if (oldSource != null)
                newSource.ProgressChanged -= SourceOnProgressChanged;

            if (newSource != null)
                newSource.ProgressChanged += SourceOnProgressChanged;
        }

        public TimeSource TimeSource
        {
            get { return (TimeSource)GetValue(TimeSourceProperty); }
            set { SetValue(TimeSourceProperty, value); }
        }

        public static readonly DependencyProperty SamplerProperty = DependencyProperty.Register(
            "Sampler", typeof(ColorSampler), typeof(ColorSampleBar), new PropertyMetadata(default(ColorSampler)));

        public ColorSampler Sampler
        {
            get { return (ColorSampler)GetValue(SamplerProperty); }
            set { SetValue(SamplerProperty, value); }
        }

        public static readonly DependencyProperty SamplesProperty = DependencyProperty.Register(
            "Samples", typeof(int), typeof(ColorSampleBar), new PropertyMetadata((int)50));

        public static readonly DependencyProperty SampleConditionProperty = DependencyProperty.Register(
            "SampleCondition", typeof(SampleCondition), typeof(ColorSampleBar), new PropertyMetadata(default(SampleCondition), OnSampleConditionPropertyChanged));

        private static void OnSampleConditionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorSampleBar)d).InvalidateVisual();
        }

        public SampleCondition SampleCondition
        {
            get { return (SampleCondition)GetValue(SampleConditionProperty); }
            set { SetValue(SampleConditionProperty, value); }
        }

        private List<Color> _colors;
        private bool _down;
        private Point _mousePos;
        private Popup _tooltip;
        private int _segment;
        private ColorSummary _preview;

        public int Samples
        {
            get { return (int)GetValue(SamplesProperty); }
            set { SetValue(SamplesProperty, value); }
        }

        private void SourceOnProgressChanged(object sender, TimeSpan progress)
        {
            if (Sampler == null) return;

            _colors = Sampler.GetColors(progress, Samples);
            InvalidateVisual();
        }

        public ColorSampleBar()
        {
            _preview = new ColorSummary();
            _preview.Background = Brushes.White;
            _tooltip = new Popup();
            _tooltip.PlacementTarget = this;
            _tooltip.Placement = PlacementMode.MousePoint;
            _tooltip.Child = _preview;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _down = true;
            CaptureMouse();

            UpdateMouse(e.GetPosition(this));
            InvalidateVisual();
        }

        private void UpdateMouse(Point position)
        {
            _mousePos = position;
        }

        private void UpdateToolTip()
        {
            _preview.Color = _colors[(_colors.Count - 1) - _segment];
            _tooltip.IsOpen = true;
            _tooltip.HorizontalOffset = 1;
            _tooltip.HorizontalOffset = 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_down)
            {
                base.OnMouseMove(e);
                return;
            }

            UpdateMouse(e.GetPosition(this));
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!_down)
            {
                base.OnMouseLeftButtonUp(e);
                return;
            }

            _down = false;
            _tooltip.IsOpen = false;

            ReleaseMouseCapture();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var rect = new Rect(0, 0, ActualWidth, ActualHeight);
            if (_colors == null)
            {
                drawingContext.DrawRectangle(Brushes.Black, null, rect);

            }
            else
            {
                var colors = _colors.ToArray();
                Array.Reverse(colors);
                var background = new LinearGradientBrush(HeatMapGenerator.GradientsSharpFromColors(colors),
                    new Point(0, 0), new Point(1, 0));
                drawingContext.DrawRectangle(background, null, rect);

                var conditionRect = new Rect(0, ActualHeight - 8, ActualWidth, 8);

                if (SampleCondition == null)
                {
                    drawingContext.DrawRectangle(Brushes.Black, null, conditionRect);
                }
                else
                {
                    var samples = new Color[colors.Length];

                    for (int i = 0; i < samples.Length; i++)
                    {
                        bool isOk = SampleCondition.CheckSample(new[] { colors[i].R, colors[i].G, colors[i].B });
                        samples[i] = isOk ? Colors.Lime : Colors.Red;
                    }

                    var results = new LinearGradientBrush(HeatMapGenerator.GradientsSharpFromColors(samples),
                        new Point(0, 0), new Point(1, 0));

                    drawingContext.DrawRectangle(results, null, conditionRect);
                }

                if (_down)
                {
                    _segment = (int)(_mousePos.X / ActualWidth * _colors.Count);
                    _segment = Math.Max(0, Math.Min(_colors.Count - 1, _segment));

                    double segmentWidth = ActualWidth / _colors.Count;

                    var selectRect = new Rect(segmentWidth * _segment, 0, segmentWidth, ActualHeight);
                    drawingContext.DrawRectangle(null, new Pen(Brushes.Red, 1), selectRect);

                    UpdateToolTip();
                }
            }
        }
    }
}
