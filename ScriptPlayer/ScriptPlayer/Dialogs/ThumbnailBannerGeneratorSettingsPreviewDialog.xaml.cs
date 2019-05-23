using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for ThumbnailBannerGeneratorSettingsPreviewDialog.xaml
    /// </summary>
    public partial class ThumbnailBannerGeneratorSettingsPreviewDialog : Window
    {
        public static readonly DependencyProperty ThumbnailBannerProperty = DependencyProperty.Register(
            "ThumbnailBanner", typeof(ImageSource), typeof(ThumbnailBannerGeneratorSettingsPreviewDialog), new PropertyMetadata(default(ImageSource)));

        public ImageSource ThumbnailBanner
        {
            get => (ImageSource)GetValue(ThumbnailBannerProperty);
            set => SetValue(ThumbnailBannerProperty, value);
        }

        public ThumbnailBannerGeneratorSettingsPreviewDialog(ThumbnailBannerGeneratorSettings settings)
        {
            InitializeComponent();
            ThumbnailBanner = ThumbnailBannerGenerator.CreatePreview(settings);
        }
    }

    public class ThumbnailBannerGeneratorData
    {
        public ThumbnailBannerGeneratorSettings Settings { get; set; }

        public ThumbnailBannerGeneratorImage[] Images { get; set; }
    }

    public class ThumbnailBannerGeneratorImage
    {
        public ImageSource Image { get; set; }
        public TimeSpan Position { get; set; }
    }


    public class ThumbnailBannerGenerator
    {
        public static ImageSource CreatePreview(ThumbnailBannerGeneratorSettings settings)
        {
            ThumbnailBannerGeneratorData data = new ThumbnailBannerGeneratorData
            {
                Settings = settings,
                Images = new ThumbnailBannerGeneratorImage[settings.Rows * settings.Columns]
            };

            for (int i = 0; i < data.Images.Length; i++)
                data.Images[i] = new ThumbnailBannerGeneratorImage
                {
                    Image = CreateImage(800, 600, Brushes.DimGray),
                    Position = TimeSpan.FromMinutes(i)
                };

            return CreateBanner(data);
        }

        private static ImageSource CreateImage(int width, int height, Brush fill)
        {
            DrawingVisual background = new DrawingVisual();
            using (DrawingContext dc = background.RenderOpen())
            {
                dc.DrawRectangle(fill, null, new Rect(0, 0, width, height));
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(background);
            return bitmap;
        }

        private static ImageSource CreateBanner(ThumbnailBannerGeneratorData data)
        {
            Brush backgroundBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
            Brush shadowBrush = Brushes.Black;
            Brush borderBrush = Brushes.Black;

            double shadowOpacity = 0.5;
            double textOpacity = 0.7;

            int shadowOffset = 4;
            int headerHeight = 80;
            int horizontalSpacing = 8;
            int verticalSpacing = 8;

            int positionTextOutlineWidth = 4;
            int textSpacing = 5;
            int fontSize = 16;
            int borderWidth = 1;

            string font = "Arial";

            // ------------------------------

            int horizontalImageResolution = ((BitmapSource)data.Images[0].Image).PixelWidth;
            int verticalImageResolution = ((BitmapSource)data.Images[0].Image).PixelHeight;

            int imageWidth = (data.Settings.TotalWidth - (data.Settings.Columns + 1) * horizontalSpacing) / data.Settings.Columns;
            int imageHeight = (int)Math.Round((imageWidth / (double)horizontalImageResolution) * verticalImageResolution);

            int width = data.Settings.TotalWidth;
            int height = headerHeight + (data.Settings.Rows + 1) * verticalSpacing + data.Settings.Rows * imageHeight;

            Typeface typeFace = new Typeface(new FontFamily(font), new FontStyle(), FontWeights.Bold, FontStretches.Normal);

            DrawingVisual image = new DrawingVisual();
            using (DrawingContext dc = image.RenderOpen())
            {
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.LowQuality);

                dc.DrawRectangle(backgroundBrush, null, new Rect(0, 0, width, height));

                for (int column = 0; column < data.Settings.Columns; column++)
                {
                    for (int row = 0; row < data.Settings.Rows; row++)
                    {
                        int index = row * data.Settings.Columns + column;
                        int x = horizontalSpacing + column * (horizontalSpacing + imageWidth);
                        int y = headerHeight + verticalSpacing + row * (verticalSpacing + imageHeight);

                        Rect imageRect = new Rect(x, y, imageWidth, imageHeight);

                        Rect borderRect = imageRect;
                        borderRect.X -= borderWidth;
                        borderRect.Y -= borderWidth;
                        borderRect.Width += 2 * borderWidth;
                        borderRect.Height += 2 * borderWidth;

                        Rect shadowRect = borderRect;
                        shadowRect.Offset(shadowOffset, shadowOffset);

                        dc.PushOpacity(shadowOpacity);
                        dc.DrawRectangle(shadowBrush, null, shadowRect);
                        dc.Pop();

                        dc.DrawRectangle(borderBrush, null, borderRect);
                        dc.DrawImage(data.Images[index].Image, imageRect);

                        FormattedText text = new FormattedText(data.Images[index].Position.ToString("mm\\:ss"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, 
                            typeFace, fontSize, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display, 96);

                        Point origin = new Point(x + imageWidth - textSpacing - text.Width, y + imageHeight - textSpacing - text.Height);
                        
                        Geometry textGeometry = text.BuildGeometry(origin);
                        Geometry textOutline = textGeometry.GetWidenedPathGeometry(new Pen(Brushes.Black, positionTextOutlineWidth));

                        dc.PushOpacity(textOpacity);

                        dc.DrawGeometry(Brushes.Black, null, textOutline);
                        dc.DrawGeometry(Brushes.White, null, textGeometry);

                        dc.Pop();
                    }
                }
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(image);
            return bitmap;
        }
    }
}
