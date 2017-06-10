using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Accord.Video.FFMPEG;

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

        public FrameSamplerDialog(string videoFile, Int32Rect captureRect)
        {
            TaskbarItemInfo = new TaskbarItemInfo()
            {
                ProgressState = TaskbarItemProgressState.Normal
            };
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
            frameSamples.TotalFramesInVideo = (int) reader.FrameCount;
            frameSamples.VideoFile = _videoFile;
            frameSamples.CaptureRect = _captureRect;

            long frame = 0;

            do
            {
                var current = reader.ReadVideoFrame();
                if (current == null) break;
                frame++;

                for (int i = 0; i < samples.Length; i++)
                    samples[i] = current.GetPixel(_captureRect.X + i % _captureRect.Width,
                        _captureRect.Y + i / _captureRect.Width);

                frameSamples.Add(new FrameCapture(frame, samples));

                if (frame % 25 == 0)
                {
                    var hBitmap = current.GetHbitmap();

                    var capture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(current.Width, current.Height));

                    capture.Freeze();

                    DeleteObject(hBitmap);

                    var progress01 = (double) frame / reader.FrameCount;

                    string progress = $"{frame} / {reader.FrameCount} ({progress01:P})";

                    Dispatcher.Invoke(() =>
                    {
                        if (_isClosing)
                            return;

                        Image = capture;
                        txtProgress.Text = progress;
                        TaskbarItemInfo.ProgressValue = progress01;
                    });
                }

                current.Dispose();
            } while (_running);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isClosing)
                    return;

                Result = frameSamples;
                DialogResult = true;
            }));
        }

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
