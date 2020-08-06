using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Accord.Extensions.Imaging;
using Accord.Video.FFMPEG;
using DotImaging;
using PointF = DotImaging.Primitives2D.PointF;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for VisualTrackerDialog.xaml
    /// </summary>
    public partial class VisualTrackerDialog : Window
    {
        private readonly string _filename;
        private readonly TimeSpan _timeBegin;
        private readonly TimeSpan _timeEnd;
        private readonly Rectangle _startLocation;

        private readonly PyrLKStorage<Gray<float>> _lkStorage = new PyrLKStorage<Gray<float>>(1);

        private Gray<float>[,] _prevIm;
        private List<PointF> _oldPositions;

        private const int WinSize = 40;

        public VisualTrackerDialog(string filename, TimeSpan timeBegin, TimeSpan timeEnd, Rectangle startLocation)
        {
            Loaded += OnLoaded;

            _filename = filename;
            _timeBegin = timeBegin;
            _timeEnd = timeEnd;
            _startLocation = startLocation;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            new Thread(ProcessFrames).Start();
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private void ProcessFrames()
        {
            VideoFileReader reader = new VideoFileReader();
            reader.Open(_filename);

            Dispatcher.Invoke(() =>
            {
                Preview.Width = reader.Width;
                Preview.Height = reader.Height;
            });

            long frameBegin = (long)(reader.FrameRate.Value * _timeBegin.TotalSeconds);
            long frameEnd = (long)(reader.FrameRate.Value * _timeEnd.TotalSeconds);

            _oldPositions = new List<PointF>{ new PointF(_startLocation.X, _startLocation.Y)};

            for (long frame = 0; frame <= frameEnd; frame++)
            {
                Bitmap bitmap = reader.ReadVideoFrame();
                if (bitmap == null)
                    return;

                if (frame >= frameBegin)
                {
                    var im = bitmap.ToBgr();
                    var gray = im.ToGray().Cast<float>();

                    //long start = DateTime.Now.Ticks;

                    if (_prevIm == null)
                    {
                        _prevIm = gray;
                    }

                    ProcessImage(_prevIm, gray, _oldPositions, out List<PointF> newPositions);

                    _prevIm = gray;
                    _oldPositions = newPositions;

                    //long end = DateTime.Now.Ticks;
                    //long elapsedMs = (end - start) / TimeSpan.TicksPerMillisecond;
                }

                if (frame < frameBegin && frame % 5 != 0)
                    continue;

                var hBitmap = bitmap.GetHbitmap();

                var capture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));

                capture.Freeze();

                DeleteObject(hBitmap);
                bitmap.Dispose();

                Dispatcher.Invoke(() =>
                {
                    Preview.Background = new ImageBrush(capture);

                    if (_oldPositions.Count > 0)
                    {
                        Center.Margin = new Thickness(_oldPositions[0].X - 10, _oldPositions[0].Y - 10, 0, 0);
                        Center.Width = 20;
                        Center.Height = 20;
                        Center.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Center.Visibility = Visibility.Collapsed;
                    }
                });
            }
        }

        private void ProcessImage(Gray<float>[,] prevIm, Gray<float>[,] currIm, List<PointF> oldPositions, out List<PointF> newPositions)
        {
            _lkStorage.Process(prevIm, currIm);

            PyrLKOpticalFlow<Gray<float>>.EstimateFlow(_lkStorage, oldPositions.ToArray(), out PointF[] currFeatures, out KLTFeatureStatus[] featureStatus, WinSize);

            newPositions = new List<PointF>();
            for (int i = 0; i < currFeatures.Length; i++)
            {
                if (featureStatus[i] == KLTFeatureStatus.Success)
                    newPositions.Add(currFeatures[i]);

                Console.WriteLine(featureStatus[i]);
            }
        }
    }
}
