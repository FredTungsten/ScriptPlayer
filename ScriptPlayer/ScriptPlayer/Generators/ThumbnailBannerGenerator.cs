using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.Generators
{
    public class ThumbnailBannerGenerator : FfmpegGenerator<ThumbnailBannerGeneratorSettings>
    {
        private bool _canceled;
        private FrameConverterWrapper _wrapper;

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

        public ThumbnailBannerGenerator(string ffmpegExePath) : base(ffmpegExePath)
        {
        }

        protected override string ProcessingType => "Thumbnail Banner";

        protected override void ProcessInternal(ThumbnailBannerGeneratorSettings settings, GeneratorEntry entry)
        {
            try
            {
                entry.State = JobStates.Processing;

                VideoInfoWrapper infoWrapper = new VideoInfoWrapper(FfmpegExePath);
                infoWrapper.DebugOutput = false;
                infoWrapper.VideoFile = settings.VideoFile;
                infoWrapper.Execute();

                if(!infoWrapper.IsGoodEnough())
                    infoWrapper.DumpInfo();

                TimeSpan duration = MediaHelper.GetDuration(settings.VideoFile).Value;

                TimeSpan intervall = duration.Divide(settings.Columns * settings.Rows + 1);

                _wrapper = new FrameConverterWrapper(FfmpegExePath)
                {
                    Width = 800,
                    Intervall = intervall.TotalSeconds
                };

                _wrapper.ProgressChanged += (s, progress) => { entry.Update(null, progress); };

                entry.Update("Extracting Frames", 0);

                _wrapper.VideoFile = settings.VideoFile;
                _wrapper.GenerateRandomOutputPath();
                string tempPath = _wrapper.OutputDirectory;
                _wrapper.Execute();

                if (_canceled)
                    return;

                entry.Update("Saving Thumbnails", 1);

                List<ThumbnailBannerGeneratorImage> images = new List<ThumbnailBannerGeneratorImage>();
                List<string> usedFiles = new List<string>();

                foreach (string file in Directory.EnumerateFiles(tempPath))
                {
                    string number = Path.GetFileNameWithoutExtension(file);
                    int index = int.Parse(number);

                    TimeSpan position = intervall.Multiply(index + 1);

                    var frame = new BitmapImage();
                    frame.BeginInit();
                    frame.CacheOption = BitmapCacheOption.OnLoad;
                    frame.UriSource = new Uri(file, UriKind.Absolute);
                    frame.EndInit();

                    images.Add(new ThumbnailBannerGeneratorImage
                    {
                        Image = frame,
                        Position = position
                    });
                    usedFiles.Add(file);
                }

                ThumbnailBannerGeneratorData data = new ThumbnailBannerGeneratorData();
                data.Settings = settings;
                data.Images = images.ToArray();

                var result = CreateBanner(data);
                
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)result));
                using(FileStream stream = new FileStream(settings.OutputFile, FileMode.Create))
                    encoder.Save(stream);

                foreach (string tempFile in usedFiles)
                    File.Delete(tempFile);

                Directory.Delete(tempPath);

                entry.DoneType = JobDoneTypes.Success;
                entry.Update("Done", 1);
            }
            catch (Exception)
            {
                entry.DoneType = JobDoneTypes.Failure;
            }
            finally
            {
                entry.State = JobStates.Done;

                if (_canceled)
                    entry.DoneType = JobDoneTypes.Cancelled;
            }
        }

        public override void Cancel()
        {
            _canceled = true;
            _wrapper?.Cancel();
        }
    }
}