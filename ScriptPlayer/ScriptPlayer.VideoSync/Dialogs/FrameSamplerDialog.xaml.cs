using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Video.FFMPEG;
using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync
{
    /// <summary>
    /// Interaction logic for FrameSamplerDialog.xaml
    /// </summary>
    public partial class FrameSamplerDialog : Window
    {
        private readonly string _videoFile;
        private readonly Int32Rect _captureRect;

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
            "Image", typeof(ImageSource), typeof(FrameSamplerDialog), new PropertyMetadata(default(ImageSource)));

        public ImageSource Image
        {
            get { return (ImageSource) GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(FrameCaptureCollection), typeof(FrameSamplerDialog), new PropertyMetadata(default(FrameCaptureCollection)));

        public FrameCaptureCollection Result
        {
            get { return (FrameCaptureCollection) GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        

        private Thread _t;
        private bool _running;
        private bool _isClosing;
        private TimeSpan _backupDuration;

        public FrameSamplerDialog(string videoFile, Int32Rect captureRect, TimeSpan backupDuration)
        {
            TaskbarItemInfo = new TaskbarItemInfo
            {
                ProgressState = TaskbarItemProgressState.Normal
            };
            _backupDuration = backupDuration;
            _videoFile = videoFile;   
            _captureRect = captureRect;
            InitializeComponent();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            _running = false;
        }

        private void Start()
        {
            _running = true;
            _t = new Thread(() => ProcessVideo(_videoFile));
            _t.Start();
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private void ProcessVideo(string fileName)
        {
            VideoFileReader reader = new VideoFileReader();
            reader.Open(fileName);
            
            System.Drawing.Color[] samples = new System.Drawing.Color[_captureRect.Width * _captureRect.Height];

            
            FrameCaptureCollection frameSamples = new FrameCaptureCollection();

            bool indeterminate = reader.FrameCount <= 0;

            if (!indeterminate)
            {
                frameSamples.TotalFramesInVideo = (int) reader.FrameCount;
                frameSamples.DurationNumerator = reader.FrameCount * reader.FrameRate.Denominator;
                frameSamples.DurationDenominator = reader.FrameRate.Numerator;
            }

            //List<byte> allAudio = new List<byte>();
            
            frameSamples.VideoFile = _videoFile;
            frameSamples.CaptureRect = _captureRect;

            long frame = 0;

            DateTime start = DateTime.Now;

            int preview = 0;

            Dictionary<long, BitmapSource> thumbnails = new Dictionary<long, BitmapSource>();

            do
            {
                //List<byte> audio = new List<byte>();
                var current = reader.ReadVideoFrame();
                if (current == null) break;
                frame++;

                //allAudio.AddRange(allAudio);


                for (int i = 0; i < samples.Length; i++)
                    samples[i] = current.GetPixel(_captureRect.X + i % _captureRect.Width,
                        _captureRect.Y + i / _captureRect.Width);

                frameSamples.Add(new FrameCapture(frame, samples));

                if (frame % 25 == 0)
                {
                    preview++;
                    

                    var hBitmap = current.GetHbitmap();

                    var capture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(current.Width, current.Height));

                    capture.Freeze();

                    if (preview % 10 == 0)
                    {
                        BitmapSource thumbnail = Resize(capture, 200, 130);
                        thumbnails.Add(frame, thumbnail);
                    }

                    DeleteObject(hBitmap);

                    double progressValue;
                    string progressText;

                    if (!indeterminate)
                    {
                        progressValue = (double) frame / reader.FrameCount;
                        progressText = $"{frame} / {reader.FrameCount} ({progressValue:P})";

                        TimeSpan elapsed = DateTime.Now - start;
                        TimeSpan averagePerFrame = elapsed.Divide(frame);
                        long left = Math.Max(0, reader.FrameCount - frame - 1);
                        TimeSpan timeLeft = averagePerFrame.Multiply(left);

                        progressText += $" ETA {timeLeft:mm\\:ss}";
                    }
                    else
                    {
                        progressValue = 0.0;
                        progressText = $"{frame} / Unknown";
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (_isClosing)
                            return;

                        Image = capture;
                        txtProgress.Text = progressText;
                        TaskbarItemInfo.ProgressValue = progressValue;
                        proTotal.Value = progressValue;

                        if (indeterminate)
                        {
                            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                            proTotal.IsIndeterminate = true;
                        }
                    });
                }

                current.Dispose();
            } while (_running);

            //byte[] a = allAudio.ToArray();

            //Signal s = new Signal(a, 1, a.Length, reader.SampleRate, SampleFormat.Format8BitUnsigned);

            //for (int index = 0; index < frameSamples.Count; index++)
            //{
            //    var frameSample = frameSamples[index];
            //    frameSample.AudioLevel = s.GetSample(0, (int) frameSample.FrameIndex);
            //}

            long framesSampled = frame;
            long expectedFrames = frameSamples.TotalFramesInVideo;
            long sampledFrames = frameSamples.Count;

            if (frameSamples.TotalFramesInVideo == 0)
            {
                frameSamples.TotalFramesInVideo = (int)frame;
                frameSamples.DurationNumerator = frame * reader.FrameRate.Denominator;
                frameSamples.DurationDenominator = reader.FrameRate.Numerator;
            }


            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isClosing)
                    return;

                Thumbnails = thumbnails;
                Result = frameSamples;
                DialogResult = true;
            }));
        }

        private BitmapSource Resize(BitmapSource image, int maxWidth, int maxHeight)
        {
            double scale = Math.Min(maxWidth / image.Width, maxHeight / image.Height);

            int width = (int) (image.Width * scale);
            int height = (int) (image.Height * scale);

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96,96, PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(image, new Rect(0, 0, width, height));
            }
            bitmap.Render(drawingVisual);
            bitmap.Freeze();

            return bitmap;
        }

        public Dictionary<long, BitmapSource> Thumbnails { get; set; }

        private void FrameSamplerDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
            _running = false;
            _t.Join(1000);
        }
    }
}
