using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScriptPlayer.Shared.Classes;

namespace ScriptPlayer.Shared
{
    public class ThumbnailPreview : Control
    {
        public static readonly DependencyProperty ThumbnailsProperty = DependencyProperty.Register(
            "Thumbnails", typeof(VideoThumbnailCollection), typeof(ThumbnailPreview), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnThumbnailsPropertyChanged));

        private static void OnThumbnailsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ThumbnailPreview) d).ThumbnailsChanged();
        }

        private void ThumbnailsChanged()
        {
            if (AutoSize)
            {
                if (Thumbnails == null || Thumbnails.Count == 0)
                {
                    Width = 0;
                    Height = 0;
                }
                else
                {
                    var first = Thumbnails.Get(0).Thumbnail;
                    Width = first.PixelWidth;
                    Height = first.PixelHeight;
                }
            }
        }

        public VideoThumbnailCollection Thumbnails
        {
            get => (VideoThumbnailCollection) GetValue(ThumbnailsProperty);
            set => SetValue(ThumbnailsProperty, value);
        }

        public static readonly DependencyProperty TimeStampProperty = DependencyProperty.Register(
            "TimeStamp", typeof(TimeSpan), typeof(ThumbnailPreview), new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.AffectsRender));

        public TimeSpan TimeStamp
        {
            get => (TimeSpan) GetValue(TimeStampProperty);
            set => SetValue(TimeStampProperty, value);
        }

        public static readonly DependencyProperty ThumbnailModeProperty = DependencyProperty.Register(
            "ThumbnailMode", typeof(ThumbnailModes), typeof(ThumbnailPreview), new FrameworkPropertyMetadata(ThumbnailModes.One, FrameworkPropertyMetadataOptions.AffectsRender));

        public ThumbnailModes ThumbnailMode
        {
            get => (ThumbnailModes) GetValue(ThumbnailModeProperty);
            set => SetValue(ThumbnailModeProperty, value);
        }

        public static readonly DependencyProperty AutoSizeProperty = DependencyProperty.Register(
            "AutoSize", typeof(bool), typeof(ThumbnailPreview), new PropertyMetadata(true));

        public bool AutoSize
        {
            get => (bool) GetValue(AutoSizeProperty);
            set => SetValue(AutoSizeProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect rect = new Rect(0,0, ActualWidth, ActualHeight);
            
            drawingContext.DrawRectangle(Brushes.Black, null, rect);

            if (Thumbnails == null)
                return;

            BitmapSource image = Thumbnails.Get(TimeStamp)?.Thumbnail;

            if (image == null)
                return;

            Size destSize = StretchSize(Stretch.Uniform, new Size(image.Width, image.Height), rect.Size);
            Rect destPos = CenterInRect(destSize, rect);

            drawingContext.DrawImage(image, destPos);
        }

        private Rect CenterInRect(Size destSize, Rect rect)
        {
            double offsetX = (rect.Width - destSize.Width) / 2;
            double offsetY = (rect.Height - destSize.Height) / 2;

            return new Rect(new Point(offsetX, offsetY), destSize);
        }

        private Size StretchSize(Stretch stretchMode, Size imageSize, Size destSize)
        {
            double ratio = imageSize.Width / imageSize.Height;

            double resultWidth = destSize.Width;
            double resultHeight = destSize.Height;

            if (ratio * destSize.Height > destSize.Width)
            {
                resultHeight = destSize.Width / ratio;
            }
            else
            {
                resultWidth = destSize.Height * ratio;
            }

            return new Size(resultWidth, resultHeight);
        }
    }
}
