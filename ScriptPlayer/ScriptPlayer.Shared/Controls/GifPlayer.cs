using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

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

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
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

            if (index == _index)
                return;

            _index = index;

            if (_index < 0 || _index >= Frames.Count)
                throw new ArgumentException();

            InvalidateVisual();
        }

        public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register(
            "AutoPlay", typeof(bool), typeof(GifPlayer), new PropertyMetadata(default(bool)));

        public bool AutoPlay
        {
            get => (bool)GetValue(AutoPlayProperty);
            set => SetValue(AutoPlayProperty, value);
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
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty FramesProperty = DependencyProperty.Register(
            "Frames", typeof(GifFrameCollection), typeof(GifPlayer), new PropertyMetadata(default(GifFrameCollection), OnFramesChanged));

        private static void OnFramesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifPlayer)d).FramesChanged();
        }

        private void FramesChanged()
        {
            _index = 0;
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
            OnFramesReady();

            if (AutoPlay)
                Start();
        }

        public GifFrameCollection Frames
        {
            get => (GifFrameCollection)GetValue(FramesProperty);
            set => SetValue(FramesProperty, value);
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

            drawingContext.DrawRectangle(Background, null, dimensions);

            if (Frames == null || Frames.Count == 0)
            {
                return;
            }

            Size scaledSize = GetScaledSize(RenderSize);
            Point offset = new Point((dimensions.Width - scaledSize.Width) / 2, (dimensions.Height - scaledSize.Height) / 2);

            GifFrame frame = Frames[_index];
            Rect final = new Rect(
                offset.X,
                offset.Y,
                scaledSize.Width,
                scaledSize.Height);

            drawingContext.DrawImage(frame.Image, final);
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

        public void Start()
        {
            Start(Frames.Duration);
        }

        public void Start(TimeSpan duration)
        {
            BeginAnimation(ProgressProperty, new DoubleAnimation(0, 1, duration) { RepeatBehavior = RepeatBehavior.Forever });
        }

        public void Stop()
        {
            BeginAnimation(ProgressProperty, null);
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
        public ushort Left { get; set; }
        public ushort Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Delay { get; set; }
    }

    public class GifFrameCollection
    {
        private GifFrame[] _frames;

        public GifFrame this[int index] => _frames[index];

        public int Count => _frames.Length;

        public int Height { get; private set; }

        public int Width { get; private set; }

        public TimeSpan Duration { get; set; }

        private static readonly object LoadLocker = new object();

        private void Load(Stream stream)
        {
            GifBitmapDecoder decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            Width = decoder.Frames[0].PixelWidth;
            Height = decoder.Frames[0].PixelHeight;
            _frames = new GifFrame[decoder.Frames.Count];

            int duration = 0;

            BitmapSource previousRenderResult = null;
            
            for (int index = 0; index < decoder.Frames.Count; index++)
            {
                BitmapFrame frame = decoder.Frames[index].GetAsFrozen() as BitmapFrame;
                BitmapMetadata metadata = frame.Metadata as BitmapMetadata;
                int delay = (ushort)metadata.GetQuery("/grctlext/Delay") * 10;
                ushort left = (ushort)metadata.GetQuery("/imgdesc/Left");
                ushort top = (ushort)metadata.GetQuery("/imgdesc/Top");
                int width = frame.PixelWidth;
                int height = frame.PixelHeight;

                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    if (previousRenderResult != null)
                        dc.DrawImage(previousRenderResult, new Rect(0, 0, Width, Height));
                    dc.DrawImage(frame, new Rect(left, top, width, height));
                }

                RenderTargetBitmap bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(drawingVisual);

                previousRenderResult = (BitmapSource)bitmap.GetAsFrozen();

                _frames[index] = new GifFrame
                {
                    Image = previousRenderResult,
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
                Debug.WriteLine(string.Join(" ", delays));
            }

            Duration = TimeSpan.FromMilliseconds(duration);
        }

        public void Load(string filename)
        {
            lock (LoadLocker)
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    Load(stream);
                }
            }
        }
    }
}
