using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync.Controls
{
    public class SampleRectZoom : Control
    {
        public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register(
            "Player", typeof(VideoPlayer), typeof(SampleRectZoom), new PropertyMetadata(default(VideoPlayer), OnVideoPlayerPropertyChanged));

        private static void OnVideoPlayerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SampleRectZoom)d).OnVideoPlayerChanged();
        }

        public VideoPlayer Player
        {
            get => (VideoPlayer)GetValue(PlayerProperty);
            set => SetValue(PlayerProperty, value);
        }

        public static readonly DependencyProperty SampleRectProperty = DependencyProperty.Register(
            "SampleRect", typeof(Rect), typeof(SampleRectZoom), new FrameworkPropertyMetadata(default(Rect), FrameworkPropertyMetadataOptions.AffectsRender));

        public Rect SampleRect
        {
            get => (Rect)GetValue(SampleRectProperty);
            set => SetValue(SampleRectProperty, value);
        }

        public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register(
            "Resolution", typeof(Resolution), typeof(SampleRectZoom), new FrameworkPropertyMetadata(default(Resolution), FrameworkPropertyMetadataOptions.AffectsRender));

        public Resolution Resolution
        {
            get => (Resolution)GetValue(ResolutionProperty);
            set => SetValue(ResolutionProperty, value);
        }

        public static readonly DependencyProperty VideoBrushProperty = DependencyProperty.Register(
            "VideoBrush", typeof(Brush), typeof(SampleRectZoom), new FrameworkPropertyMetadata(default(Brush), FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush VideoBrush
        {
            get => (Brush)GetValue(VideoBrushProperty);
            set => SetValue(VideoBrushProperty, value);
        }

        public SampleRectZoom()
        {
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            ClipToBounds = true;
        }

        private void OnVideoPlayerChanged()
        {
            if (Player != null)
            {
                BindingOperations.SetBinding(this, SampleRectProperty,
                    new Binding { Source = Player, Path = new PropertyPath(VideoPlayer.SampleRectProperty), Mode = BindingMode.OneWay });
                BindingOperations.SetBinding(this, VideoBrushProperty,
                    new Binding { Source = Player, Path = new PropertyPath(VideoPlayer.VideoBrushProperty), Mode = BindingMode.OneWay });

                BindingOperations.SetBinding(this, ResolutionProperty,
                    new Binding
                    {
                        Source = Player,
                        Path = new PropertyPath(VideoPlayer.ResolutionProperty),
                        Mode = BindingMode.OneWay
                    });
            }
            else
            {
                BindingOperations.ClearBinding(this, ResolutionProperty);
                BindingOperations.ClearBinding(this, SampleRectProperty);
                BindingOperations.ClearBinding(this, VideoBrushProperty);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.Black, null, new Rect(new Size(ActualWidth, ActualHeight)));

            if (VideoBrush == null) return;
            if (SampleRect.IsEmpty) return;
            if (Resolution.ToSize().IsEmpty) return;

            double zoom = Math.Min(ActualWidth / SampleRect.Width, ActualHeight / SampleRect.Height);

            dc.PushClip(new RectangleGeometry(new Rect(new Size(SampleRect.Width * zoom, SampleRect.Height * zoom))));

            dc.PushTransform(new TranslateTransform(-SampleRect.Left, -SampleRect.Top));

            dc.PushTransform(new ScaleTransform(zoom, zoom, SampleRect.Left, SampleRect.Top));

            //dc.PushClip(new RectangleGeometry(new Rect(SampleRect.Left * zoom, SampleRect.Top * zoom, SampleRect.Width * zoom, SampleRect.Height * zoom)));

            //dc.PushClip(new RectangleGeometry(new Rect(SampleRect.TopLeft, new Size(SampleRect.Width * zoom, SampleRect.Height * zoom))));
            dc.DrawRectangle(VideoBrush, null, new Rect(Resolution.ToSize()));

            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
    }
}
