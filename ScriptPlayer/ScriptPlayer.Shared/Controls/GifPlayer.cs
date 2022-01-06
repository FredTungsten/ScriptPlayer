using System;
using System.Diagnostics;
using System.IO;
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
        public static readonly DependencyProperty ShowProgressProperty = DependencyProperty.Register(
            "ShowProgress", typeof(bool), typeof(GifPlayer), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowProgress
        {
            get => (bool)GetValue(ShowProgressProperty);
            set => SetValue(ShowProgressProperty, value);
        }

        public static readonly DependencyProperty ProgressHeightProperty = DependencyProperty.Register(
            "ProgressHeight", typeof(double), typeof(GifPlayer), new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public double ProgressHeight
        {
            get => (double)GetValue(ProgressHeightProperty);
            set => SetValue(ProgressHeightProperty, value);
        }

        public static readonly DependencyProperty ProgressBackgroundProperty = DependencyProperty.Register(
            "ProgressBackground", typeof(Brush), typeof(GifPlayer), new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush ProgressBackground
        {
            get => (Brush)GetValue(ProgressBackgroundProperty);
            set => SetValue(ProgressBackgroundProperty, value);
        }

        public static readonly DependencyProperty ProgressForegroundProperty = DependencyProperty.Register(
            "ProgressForeground", typeof(Brush), typeof(GifPlayer), new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush ProgressForeground
        {
            get => (Brush)GetValue(ProgressForegroundProperty);
            set => SetValue(ProgressForegroundProperty, value);
        }

        public static readonly DependencyProperty AutoSizeProperty = DependencyProperty.Register(
            "AutoSize", typeof(bool), typeof(GifPlayer), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool AutoSize
        {
            get => (bool)GetValue(AutoSizeProperty);
            set => SetValue(AutoSizeProperty, value);
        }

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
                var frame = Frames[i];
                if (frame?.CompleteImage == null)
                    break;

                index = i;

                duration += frame.Delay;
                if (duration > progress)
                    break;
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
            GifPlayer player = (GifPlayer)d;
            player.Dispatcher.Invoke(() =>
                {
                    player.FramesChanged((GifFrameCollection)e.OldValue, (GifFrameCollection)e.NewValue);
                });
        }

        private void FramesChanged(GifFrameCollection oldValue, GifFrameCollection newValue)
        {
            if (oldValue != null)
            {
                oldValue.LoadStateChanged -= Frames_LoadStateChanged;
            }

            if (newValue != null)
            {
                newValue.LoadStateChanged += Frames_LoadStateChanged;
            }

            _index = 0;
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
            OnFramesReady();
        }

        private void Frames_LoadStateChanged(object sender, LoadStates state)
        {
            if (Dispatcher.CheckAccess())
                UpdateState(state);
            else
                Dispatcher.Invoke(() => { UpdateState(state); });
        }

        private void UpdateState(LoadStates state)
        {
            switch (state)
            {
                case LoadStates.None:
                    break;
                case LoadStates.BasicInformation:
                {
                    InvalidateMeasure();
                    InvalidateArrange();
                    break;
                }
                case LoadStates.FrameMetadata:
                {
                    break;
                }
                //case LoadStates.FirstFrame:
                //{
                //    InvalidateVisual();
                //    break;
                //}
                //case LoadStates.Complete:
                //{
                //    if (AutoPlay)
                //        Start();
                //    break;
                //}
                case LoadStates.FirstFrame:
                {
                    InvalidateVisual();
                    if (AutoPlay)
                        Start();
                    break;
                }
                case LoadStates.Complete:
                {
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public GifFrameCollection Frames
        {
            get => (GifFrameCollection)GetValue(FramesProperty);
            set => SetValue(FramesProperty, value);
        }

        private int _index;
        private Thread _loaderThread;

        protected override Size MeasureOverride(Size constraint)
        {
            if (!FramesValid())
            {
                if (IsUndefined(constraint) || AutoSize)
                    return new Size(1, 1);
                return constraint;
            }

            if (AutoSize)
            {
                return new Size(Frames.Width, Frames.Height + ActualProgressHeight);
            }

            if (!IsUndefined(constraint))
            {
                Size constraintWithoutProgress = new Size(constraint.Width, constraint.Height - ActualProgressHeight);
                Size scaledSize = GetScaledSize(constraintWithoutProgress);
                return new Size(scaledSize.Width, scaledSize.Height + ActualProgressHeight);
            }

            if (!IsUndefined(constraint.Width))
            {
                return new Size(constraint.Width, Frames.Height * (constraint.Width / Frames.Width) + ActualProgressHeight);
            }

            if (!IsUndefined(constraint.Height))
            {
                return new Size(Frames.Width * ((constraint.Height - ActualProgressHeight) / Frames.Height), constraint.Height);
            }

            return new Size(Frames.Width, Frames.Height + ActualProgressHeight);
        }

        public double ActualProgressHeight => ShowProgress ? ProgressHeight : 0;

        private bool IsUndefined(Size value)
        {
            return IsUndefined(value.Width) || IsUndefined(value.Height);
        }

        private bool IsUndefined(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }

        private bool FramesValid()
        {
            if (Frames == null) return false;
            if (Frames.LoadState < LoadStates.BasicInformation) return false;
            if (Frames.Count == 0) return false;

            return true;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect frameDimension = new Rect(0, 0, ActualWidth, ActualHeight - ActualProgressHeight);
            Rect totalDimension = new Rect(0, 0, ActualWidth, ActualHeight);

            drawingContext.DrawRectangle(Background, null, totalDimension);

            double fillRatio = 0;

            if (FramesValid() && Frames.LoadState >= LoadStates.FirstFrame)
            {
                fillRatio = _index / (Frames.Count - 1.0);

                Size scaledSize = GetScaledSize(RenderSize);
                Point offset = new Point((frameDimension.Width - scaledSize.Width) / 2,
                    (frameDimension.Height - scaledSize.Height) / 2);

                GifFrame frame = Frames[_index];
                Rect final = new Rect(
                    offset.X,
                    offset.Y,
                    scaledSize.Width,
                    scaledSize.Height);

                drawingContext.DrawImage(frame.CompleteImage, final);
            }

            if (ShowProgress)
            {
                Rect progressBackgroundDimension = new Rect(0, ActualHeight - ActualProgressHeight, ActualWidth,
                    ProgressHeight);
                Rect progressForegroundDimension = new Rect(0, ActualHeight - ActualProgressHeight,
                    ActualWidth * fillRatio, ProgressHeight);

                drawingContext.DrawRectangle(ProgressBackground, null, progressBackgroundDimension);
                drawingContext.DrawRectangle(ProgressForeground, null, progressForegroundDimension);
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

        public void Start()
        {
            if (Frames != null)
                Start(Frames.Duration);
        }

        private void Start(TimeSpan duration)
        {
            BeginAnimation(ProgressProperty, new DoubleAnimation(0, 1, duration) { RepeatBehavior = RepeatBehavior.Forever });
        }

        public void Stop()
        {
            BeginAnimation(ProgressProperty, null);
            Frames?.CancelLoad();
        }

        public void Close()
        {
            Stop();
            Frames = null;

            InvalidateMeasure();
        }

        public void Load(string filename)
        {
            GifFrameCollection collection = new GifFrameCollection();
            Frames = collection;

            _loaderThread = new Thread(() => { collection.Load(filename); });
            _loaderThread.Start();
        }

        protected virtual void OnFramesReady()
        {
            FramesReady?.Invoke(this, EventArgs.Empty);
        }
    }

    public class GifFrame
    {
        public BitmapSource CompleteImage { get; set; }
        public ushort Left { get; set; }
        public ushort Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Delay { get; set; }
        public Rect PartialRect => new Rect(Left, Top, Width, Height);
        public byte Disposal { get; set; }
    }

    public enum LoadStates
    {
        None = 0,
        BasicInformation = 1,
        FrameMetadata = 2,
        FirstFrame = 3,
        Complete = 4
    }

    public class GifFrameCollection
    {
        public event EventHandler<LoadStates> LoadStateChanged;

        private GifFrame[] _frames;
        private LoadStates _loadState = LoadStates.None;

        public GifFrame this[int index]
        {
            get
            {
                if (_frames == null)
                    return null;

                if (_frames.Length <= index)
                    return null;

                return _frames[index];
            }
        }

        public int Count => _frames?.Length ?? 0;

        public int Height { get; private set; }

        public int Width { get; private set; }

        public TimeSpan Duration { get; set; }

        public LoadStates LoadState
        {
            get => _loadState;
            set
            {
                if (value == _loadState)
                    return;
                _loadState = value;
                OnLoadStateChanged(value);
            }
        }


        private CancellationTokenSource _source;

        private readonly object _loadLocker = new object();

        private readonly object _cancelLocker = new object();


        private void Load(Stream stream, CancellationToken sourceToken)
        {
            DateTime start = DateTime.Now;

            GifBitmapDecoder decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);

            Width = decoder.Frames[0].PixelWidth;
            Height = decoder.Frames[0].PixelHeight;
            var frameRect = new Rect(0, 0, Width, Height);
            _frames = new GifFrame[decoder.Frames.Count];

            LoadState = LoadStates.BasicInformation;
            Debug.WriteLine($"Basic Decode done after {(DateTime.Now - start).TotalMilliseconds:f2}");

            int totalDuration = 0;

            for (int index = 0; index < decoder.Frames.Count; index++)
            {
                BitmapFrame frame = decoder.Frames[index];
                BitmapMetadata metadata = frame.Metadata as BitmapMetadata;
                int frameDelay = (ushort)metadata.GetQuery("/grctlext/Delay") * 10;
                //byte disposal = (byte) metadata.GetQuery("/grctlext/Disposal"); //seems to be mostly "combine" 
                ushort left = (ushort)metadata.GetQuery("/imgdesc/Left");
                ushort top = (ushort)metadata.GetQuery("/imgdesc/Top");
                int width = frame.PixelWidth;
                int height = frame.PixelHeight;

                _frames[index] = new GifFrame
                {
                    CompleteImage = null,
                    Height = height,
                    Left = left,
                    Top = top,
                    Width = width,
                    Delay = frameDelay,
                    //Disposal = disposal
                };

                totalDuration += frameDelay;
            }
            
            Duration = TimeSpan.FromMilliseconds(totalDuration);

            LoadState = LoadStates.FrameMetadata;
            //Debug.WriteLine($"Frame metadata done after {(DateTime.Now - start).TotalMilliseconds:f2}");

            BitmapSource previousRenderResult = null;

            for (int index = 0; index < decoder.Frames.Count; index++)
            {
                if (sourceToken.IsCancellationRequested)
                    return;

                BitmapFrame frame = decoder.Frames[index];

                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    if (previousRenderResult != null)
                        dc.DrawImage(previousRenderResult, frameRect);

                    dc.DrawImage(frame, _frames[index].PartialRect);
                }

                RenderTargetBitmap bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(drawingVisual);
                bitmap.Freeze();

                previousRenderResult = bitmap;
                
                _frames[index].CompleteImage = previousRenderResult;

                
                if (index != 0)
                    continue;

                LoadState = LoadStates.FirstFrame;
                //Debug.WriteLine($"First Frame done after {(DateTime.Now - start).TotalMilliseconds:f2}");
            }

            LoadState = LoadStates.Complete;
            //Debug.WriteLine($"All Decode done after {(DateTime.Now - start).TotalMilliseconds:f2}");
        }

        public void CancelLoad()
        {
            lock (_cancelLocker)
            {
                if (_source == null)
                    return;

                //Debug.WriteLine("CANCEL LOAD");
                _source.Cancel(false);
                _source = null;
            }
        }

        public void Load(string filename)
        {
            lock (_loadLocker)
            {
                CancelLoad();

                try
                {
                    CancellationToken token;
                    lock (_cancelLocker)
                    {
                        _source = new CancellationTokenSource();
                        token = _source.Token;
                    }

                    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        Load(stream, token);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GifPlayer.Load: " + ex.Message);
                }
                finally
                {
                    lock (_cancelLocker)
                        _source = null;
                }
            }
        }

        protected virtual void OnLoadStateChanged(LoadStates e)
        {
            LoadStateChanged?.Invoke(this, e);
        }
    }
}
