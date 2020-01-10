using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.Controls
{
    public class CutoutPreview : Control
    {
        public static readonly DependencyProperty CutoutProperty = DependencyProperty.Register(
            "Cutout", typeof(Rect), typeof(CutoutPreview), new FrameworkPropertyMetadata(default(Rect), FrameworkPropertyMetadataOptions.AffectsRender));

        public Rect Cutout
        {
            get => (Rect) GetValue(CutoutProperty);
            set => SetValue(CutoutProperty, value);
        }

        public static readonly DependencyProperty CutoutTypeProperty = DependencyProperty.Register(
            "CutoutType", typeof(CutoutType), typeof(CutoutPreview), new FrameworkPropertyMetadata(default(CutoutType), FrameworkPropertyMetadataOptions.AffectsRender));

        private static readonly BitmapImage PreviewImage;

        public CutoutType CutoutType
        {
            get => (CutoutType) GetValue(CutoutTypeProperty);
            set => SetValue(CutoutTypeProperty, value);
        }

        static CutoutPreview()
        {
            BackgroundProperty.OverrideMetadata(typeof(CutoutPreview), new FrameworkPropertyMetadata(Brushes.Black));
            BorderBrushProperty.OverrideMetadata(typeof(CutoutPreview), new FrameworkPropertyMetadata(Brushes.DimGray));

            PreviewImage = new BitmapImage(new Uri("pack://application:,,,/ScriptPlayer;component/Images/VideoPreview.png"));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect rectAll = new Rect(new Point(0,0), new Size(ActualWidth, ActualHeight));
            drawingContext.DrawRectangle(Background, new Pen(BorderBrush, 1), rectAll);

            Size imgSize = new Size(PreviewImage.PixelWidth, PreviewImage.PixelHeight);

            Size drawSize = ResizeHelper.StretchSize(Stretch.Uniform, imgSize, rectAll.Size);
            Rect displayRect = ResizeHelper.CenterInRect(drawSize, rectAll);

            drawingContext.DrawImage(PreviewImage, displayRect);

            if (!Cutout.IsEmpty)
            {
                Pen cutoutPen = new Pen(Brushes.Red, 1);

                Rect cutoutRect = ResizeHelper.ReduceRectangle(displayRect, Cutout);

                CombinedGeometry outerRect = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(rectAll), new RectangleGeometry(cutoutRect));

                drawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(60,0,0,0)),null, outerRect);
                drawingContext.DrawRectangle(null, cutoutPen, cutoutRect);
            }
        }
    }

    public enum CutoutType
    {
        [XmlEnum("Cutout")]
        Cutout,

        [XmlEnum("Blackout")]
        Blackout
    }
}
