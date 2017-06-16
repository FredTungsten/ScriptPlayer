using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Accord.Extensions.Imaging;
using Accord.Imaging;
using Accord.Video.FFMPEG;
using Accord.Vision.Tracking;
using DotImaging;
using Point = System.Windows.Point;
using Size = System.Drawing.Size;

using Accord.Extensions;
using Accord.Imaging.Filters;
using DotImaging.Primitives2D;
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
        private readonly System.Drawing.Rectangle _startLocation;

        public VisualTrackerDialog(string filename, TimeSpan timeBegin, TimeSpan timeEnd, System.Drawing.Rectangle startLocation)
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

            PointF position;
            oldPositions = new List<PointF>{ new PointF(_startLocation.X, _startLocation.Y)};

            for (long frame = 0; frame <= frameEnd; frame++)
            {
                try
                {
                    Bitmap bitmap = reader.ReadVideoFrame();
                    if (bitmap == null)
                        return;

                    if (frame >= frameBegin)
                    {
                        var im = bitmap.ToBgr();
                        var gray = im.ToGray().Cast<float>();

                        long start = DateTime.Now.Ticks;

                        List<PointF> newPositions;

                        if (prevIm == null)
                        {
                            prevIm = gray;
                        }

                        processImage(prevIm, gray, this.oldPositions, out newPositions);

                        prevIm = gray;
                        oldPositions = newPositions;

                        long end = DateTime.Now.Ticks;
                        long elapsedMs = (end - start) / TimeSpan.TicksPerMillisecond;
                    }

                    if (frame >= frameBegin || frame % 5 == 0)
                    {
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

                            if (oldPositions.Count > 0)
                            {
                                Center.Margin = new Thickness(oldPositions[0].X - 10, oldPositions[0].Y - 10, 0, 0);
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
                finally
                {
                    
                }
            }
        }

    
        PyrLKStorage<Gray<float>> lkStorage = new PyrLKStorage<Gray<float>>(pyrLevels: 1);
        int winSize = 40;

        private void processImage(Gray<float>[,] prevIm, Gray<float>[,] currIm, List<PointF> oldPositions, out List<PointF> newPositions)
        {
            lkStorage.Process(prevIm, currIm);

            PointF[] currFeatures;
            KLTFeatureStatus[] featureStatus;

            PyrLKOpticalFlow<Gray<float>>.EstimateFlow(lkStorage, oldPositions.ToArray(), out currFeatures, out featureStatus, winSize);

            newPositions = new List<PointF>();
            for (int i = 0; i < currFeatures.Length; i++)
            {
                if (featureStatus[i] == KLTFeatureStatus.Success)
                    newPositions.Add(currFeatures[i]);

                Console.WriteLine(featureStatus[i]);
            }
        }

        Gray<float>[,] prevIm = null;
        List<PointF> oldPositions = null;
        Bgr<byte>[,] frame = null;
    }
}
