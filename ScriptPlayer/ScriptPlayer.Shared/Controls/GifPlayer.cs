using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScriptPlayer.Shared
{
    public class GifPlayer : Control
    {
        public event EventHandler FramesReady;

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(double), typeof(GifPlayer), new PropertyMetadata(default(double), OnProgressChanged));

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifPlayer)d).UpdateIndex();
        }

        private void UpdateIndex()
        {
            if (Frames == null) return;

            double actualProgress = Progress;
            actualProgress -= Math.Floor(actualProgress);

            int progress = (int)(actualProgress * Frames.Duration.TotalMilliseconds);
            int duration = 0;
            int index = 0;

            for (int i = 0; i < Frames.Count; i++)
            {
                duration += Frames[i].Delay;
                if (duration > progress)
                {
                    index = i;
                    break;
                }
            }

            if (index == _index) return;
            _index = index;

            if (_index < 0 || _index >= Frames.Count)
                throw new ArgumentException();

            InvalidateVisual();
        }

        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
            "Stretch", typeof(Stretch), typeof(GifPlayer), new PropertyMetadata(default(Stretch), OnStretchPropertyChanged));

        private static void OnStretchPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifPlayer)d).InvalidateMeasure();
            ((GifPlayer)d).InvalidateArrange();
            ((GifPlayer)d).InvalidateVisual();
        }

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public static readonly DependencyProperty FramesProperty = DependencyProperty.Register(
            "Frames", typeof(GifFrameCollection), typeof(GifPlayer), new PropertyMetadata(default(GifFrameCollection), OnFramesChanged));

        private static void OnFramesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifPlayer)d)._index = 0;
            ((GifPlayer)d).InvalidateMeasure();
            ((GifPlayer)d).InvalidateArrange();
            ((GifPlayer)d).InvalidateVisual();
            ((GifPlayer)d).OnFramesReady();
        }

        public GifFrameCollection Frames
        {
            get { return (GifFrameCollection)GetValue(FramesProperty); }
            set { SetValue(FramesProperty, value); }
        }

        private int _index;

        protected override Size MeasureOverride(Size constraint)
        {
            if (Frames == null)
            {
                if (IsUndefined(constraint))
                    return new Size(1, 1);
                return constraint;
            }

            if (!IsUndefined(constraint))
            {
                return GetScaledSize(constraint);
            }

            if (!IsUndefined(constraint.Width))
            {
                return new Size(constraint.Width, Frames.Height * (constraint.Width / Frames.Width));
            }

            if (!IsUndefined(constraint.Height))
            {
                return new Size(Frames.Width * (constraint.Height / Frames.Height), constraint.Height);
            }

            return new Size(Frames.Width, Frames.Height);
        }

        private bool IsUndefined(Size value)
        {
            return IsUndefined(value.Width) || IsUndefined(value.Height);
        }

        private bool IsUndefined(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect dimensions = new Rect(0, 0, ActualWidth, ActualHeight);
            if (Frames == null || Frames.Count == 0)
            {
                drawingContext.DrawRectangle(Brushes.Black, null, dimensions);
                return;
            }

            Size scaledSize = GetScaledSize(RenderSize);

            for (int index = 0; index <= _index; index++)
            {
                GifFrame frame = Frames[index];
                Rect final = new Rect(
                    frame.Left / (double)Frames.Width * scaledSize.Width,
                    frame.Top / (double)Frames.Height * scaledSize.Height,
                    frame.Width / (double)Frames.Width * scaledSize.Width,
                    frame.Height / (double)Frames.Height * scaledSize.Height);

                drawingContext.DrawImage(frame.Image, final);
            }
        }

        private Size GetScaledSize(Size constraint)
        {
            Size scaledSize;

            switch (Stretch)
            {
                case Stretch.None:
                    {
                        scaledSize = new Size(Frames.Width, Frames.Height);
                        break;
                    }
                case Stretch.Fill:
                    {
                        scaledSize = new Size(constraint.Width, constraint.Height);
                        break;
                    }
                case Stretch.Uniform:
                    {
                        double scale = Math.Min(constraint.Width / Frames.Width, constraint.Height / Frames.Height);
                        scaledSize = new Size(Frames.Width * scale, Frames.Height * scale);
                        break;
                    }
                case Stretch.UniformToFill:
                    {
                        double scale = Math.Max(constraint.Width / Frames.Width, constraint.Height / Frames.Height);
                        scaledSize = new Size(Frames.Width * scale, Frames.Height * scale);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return scaledSize;
        }

        public void Start(TimeSpan duration)
        {
            //BeginAnimation(ProgressProperty, new DoubleAnimation(0, 1, duration) { RepeatBehavior = RepeatBehavior.Forever });
        }

        public void Load(string filename)
        {
            new Thread(() => LoadAsync(filename)).Start();
        }

        private void LoadAsync(string filename)
        {
            GifFrameCollection collection = new GifFrameCollection();
            collection.Load(filename);

            Dispatcher.Invoke(() =>
            {
                Frames = collection;
            });
        }

        protected virtual void OnFramesReady()
        {
            var handler = FramesReady;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }

    public class GifFrame
    {
        public BitmapSource Image { get; set; }
        public UInt16 Left { get; set; }
        public UInt16 Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Delay { get; set; }
    }

    public class GifFrameCollection
    {
        private GifFrame[] _frames;
        private int _height;
        private int _width;

        public GifFrame this[int index]
        {
            get { return _frames[index]; }
        }

        public int Count
        {
            get { return _frames.Length; }
        }

        public int Height
        {
            get { return _height; }
        }

        public int Width
        {
            get { return _width; }
        }

        public TimeSpan Duration { get; set; }

        private static object _loadLocker = new object();

        private void Load(Stream stream)
        {
            GifBitmapDecoder decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            _width = decoder.Frames[0].PixelWidth;
            _height = decoder.Frames[0].PixelHeight;
            _frames = new GifFrame[decoder.Frames.Count];

            int duration = 0;

            for (int index = 0; index < decoder.Frames.Count; index++)
            {
                BitmapFrame frame = decoder.Frames[index].GetAsFrozen() as BitmapFrame;
                BitmapMetadata metadata = frame.Metadata as BitmapMetadata;
                int delay = (UInt16)metadata.GetQuery("/grctlext/Delay") * 10;

                if (delay < 60)
                    delay = 100;

                UInt16 left = (UInt16)metadata.GetQuery("/imgdesc/Left");
                UInt16 top = (UInt16)metadata.GetQuery("/imgdesc/Top");
                int width = frame.PixelWidth;
                int height = frame.PixelHeight;

                _frames[index] = new GifFrame
                {
                    Image = frame,
                    Height = height,
                    Left = left,
                    Top = top,
                    Width = width,
                    Delay = delay
                };

                duration += delay;
            }

            List<int> delays = _frames.Select(f => f.Delay).Distinct().ToList();
            if (delays.Count != 1)
            {
                Debug.WriteLine(String.Join(" ", delays));
            }

            Duration = TimeSpan.FromMilliseconds(duration);
        }

        public void Load(string filename)
        {
            lock (_loadLocker)
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    Load(stream);
                }
            }
        }
    }
}
