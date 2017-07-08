using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ScriptPlayer.Shared
{

    /// <summary>
    /// Interaction logic for VideoPlayer.xaml
    /// </summary>
    public partial class VideoPlayer : UserControl, IDisposable
    {
        public static readonly DependencyProperty SpeedRatioProperty = DependencyProperty.Register(
            "SpeedRatio", typeof(double), typeof(VideoPlayer), new PropertyMetadata(1.0, OnSpeedPropertyChanged));

        private static void OnSpeedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer) d).OnSpeedChanged();
        }

        private void OnSpeedChanged()
        {
            _player.SpeedRatio = SpeedRatio;
        }

        public double SpeedRatio
        {
            get { return (double) GetValue(SpeedRatioProperty); }
            set { SetValue(SpeedRatioProperty, value); }
        }

        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
            "Volume", typeof(double), typeof(VideoPlayer), new PropertyMetadata(100.0, OnVolumePropertyChanged));

        private static void OnVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer) d).OnVolumeChanged();
        }

        private void OnVolumeChanged()
        {
            _player.Volume = Volume / 100.0;
        }

        public double Volume
        {
            get { return (double) GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public static readonly DependencyProperty HideMouseProperty = DependencyProperty.Register(
            "HideMouse", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(default(bool), OnHideMousePropertyChanged));

        private static void OnHideMousePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).UpdateMouse();
        }

        private void UpdateMouse()
        {
            SetMouse(!HideMouse);
        }

        private void SetMouse(bool visible)
        {
            Cursor = !visible ? Cursors.None : Cursors.Arrow;
        }

        public bool HideMouse
        {
            get { return (bool)GetValue(HideMouseProperty); }
            set { SetValue(HideMouseProperty, value); }
        }
        public bool IsPlaying => _isPlaying;

        public static readonly DependencyProperty SampleRectProperty = DependencyProperty.Register(
            "SampleRect", typeof(Rect), typeof(VideoPlayer), new PropertyMetadata(Rect.Empty, OnSampleRectPropertyChanged));

        private static void OnSampleRectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).RefreshRect();
        }

        public event EventHandler MediaEnded; 

        private void RefreshRect()
        {
            if (SampleRect.IsEmpty)
            {
                rectSample.Visibility = Visibility.Collapsed;
                return;
            }

            rectSample.Visibility = Visibility.Visible;
            rectSample.Width = SampleRect.Width;
            rectSample.Height = SampleRect.Height;
            rectSample.Margin = new Thickness(SampleRect.Left, SampleRect.Top, 0, 0);
        }

        public Rect SampleRect
        {
            get { return (Rect)GetValue(SampleRectProperty); }
            set { SetValue(SampleRectProperty, value); }
        }

        public static readonly DependencyProperty DisplayedWidthProperty = DependencyProperty.Register(
            "DisplayedWidth", typeof(double), typeof(VideoPlayer), new PropertyMetadata(default(double)));

        public double DisplayedWidth
        {
            get { return (double)GetValue(DisplayedWidthProperty); }
            set { SetValue(DisplayedWidthProperty, value); }
        }

        public static readonly DependencyProperty DisplayedHeightProperty = DependencyProperty.Register(
            "DisplayedHeight", typeof(double), typeof(VideoPlayer), new PropertyMetadata(default(double)));

        public double DisplayedHeight
        {
            get { return (double)GetValue(DisplayedHeightProperty); }
            set { SetValue(DisplayedHeightProperty, value); }
        }

        public event EventHandler MediaOpened;

        public delegate void VideoMouseEventHAndler(object sender, int x, int y);

        public event VideoMouseEventHAndler VideoMouseDown;
        public event VideoMouseEventHAndler VideoMouseUp;

        public Resolution Resolution
        {
            get { return (Resolution)GetValue(ResolutionProperty); }
            set { SetValue(ResolutionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Resolution.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResolutionProperty =
            DependencyProperty.Register("Resolution", typeof(Resolution), typeof(VideoPlayer), new PropertyMetadata(new Resolution(0, 0)));

        public static readonly DependencyProperty VideoBrushProperty = DependencyProperty.Register(
            "VideoBrush", typeof(Brush), typeof(VideoPlayer), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(VideoPlayer), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public static readonly DependencyProperty TimeSourceProperty = DependencyProperty.Register(
            "TimeSource", typeof(TimeSource), typeof(VideoPlayer), new PropertyMetadata(default(TimeSource)));

        public TimeSource TimeSource
        {
            get { return (TimeSource)GetValue(TimeSourceProperty); }
            set { SetValue(TimeSourceProperty, value); }
        }

        public static readonly DependencyProperty OpenFileProperty = DependencyProperty.Register(
            "OpenFile", typeof(string), typeof(VideoPlayer), new PropertyMetadata(default(string)));

        public string OpenFile
        {
            get { return (string)GetValue(OpenFileProperty); }
            set { SetValue(OpenFileProperty, value); }
        }

        public Brush VideoBrush
        {
            get { return (Brush)GetValue(VideoBrushProperty); }
            set { SetValue(VideoBrushProperty, value); }
        }

        private MediaPlayer _player;
        private bool _down;
        private bool _isPlaying;
        private DispatcherTimer _mouseTimer;
        private double _scale;
        private Point _offset;

        public VideoPlayer()
        {
            _mouseTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Normal, MouseTimerOnElapsed, Dispatcher.CurrentDispatcher);
            InitializeComponent();
            InitializePlayer();
        }

        private void MouseTimerOnElapsed(object sender, EventArgs eventArgs)
        {
            _mouseTimer.Stop();
            if (IsPlaying)
                SetMouse(false);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            ResetHideMouseTimer();
            base.OnMouseMove(e);
        }

        private void ResetHideMouseTimer()
        {
            if (HideMouse)
            {
                SetMouse(true);
                _mouseTimer.Stop();
                _mouseTimer.Start();
            }
        }

        public void Play()
        {
            SystemIdelPreventer.Prevent(true);
            _player.Play();
            _isPlaying = true;
            //Fadeout.Animate("4");

            ResetHideMouseTimer();
        }

        public void Pause()
        {
            SystemIdelPreventer.Prevent(false);
            _player.Pause();
            _isPlaying = false;
            //Fadeout.Animate(";");
        }

        public void Open(string filename)
        {
            OpenFile = filename;
            _player.Open(new Uri(filename, UriKind.Absolute));
        }

        private void InitializePlayer()
        {
            _player = new MediaPlayer();
            _player.ScrubbingEnabled = true;
            _player.MediaOpened += PlayerOnMediaOpened;
            _player.MediaEnded += PlayerOnMediaEnded;

            TimeSource = new MediaPlayerTimeSource(_player,
                new DispatcherClock(Dispatcher, TimeSpan.FromMilliseconds(10)));

            VideoBrush = new DrawingBrush(new VideoDrawing { Player = _player, Rect = new Rect(0, 0, 1, 1) }) { Stretch = Stretch.Fill };
        }

        private void PlayerOnMediaEnded(object sender, EventArgs eventArgs)
        {
            OnMediaEnded();
        }

        private void PlayerOnMediaOpened(object sender, EventArgs e)
        {
            Duration = _player.NaturalDuration.TimeSpan;
            Resolution = new Resolution(_player.NaturalVideoWidth, _player.NaturalVideoHeight);
            OnMediaOpened();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                ((IInputElement)sender).CaptureMouse();
                _down = true;

                int x, y;
                ClampPosition(e.GetPosition((IInputElement)sender), out x, out y);
                OnVideoMouseDown(x, y);
            }
        }

        private void ClampPosition(Point point, out int x, out int y)
        {
            x = (int)Math.Max(0, Math.Min(Resolution.Horizontal, Math.Round(point.X)));
            y = (int)Math.Max(0, Math.Min(Resolution.Vertical, Math.Round(point.Y)));
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((IInputElement)sender).ReleaseMouseCapture();
            if (!_down)
                return;

            _down = false;

            int x, y;
            ClampPosition(e.GetPosition((IInputElement)sender), out x, out y);
            OnVideoMouseUp(x, y);
        }

        protected virtual void OnVideoMouseDown(int x, int y)
        {
            VideoMouseDown?.Invoke(this, x, y);
        }

        protected virtual void OnVideoMouseUp(int x, int y)
        {
            VideoMouseUp?.Invoke(this, x, y);
        }

        public void SetPosition(TimeSpan position)
        {
            if (position < TimeSpan.Zero)
                position = TimeSpan.Zero;

            if (position > _player.NaturalDuration)
                position = _player.NaturalDuration.TimeSpan;

            _player.Position = position;
        }

        protected virtual void OnMediaOpened()
        {
            MediaOpened?.Invoke(this, EventArgs.Empty);
        }

        private void Viewbox_LayoutUpdated(object sender, EventArgs e)
        {
            try
            {
                var transform = Border.TransformToAncestor(this);

                Point p1 = transform.Transform(new Point(0, 0));
                Point p2 = transform.Transform(new Point(Border.ActualWidth, Border.ActualHeight));

                DisplayedWidth = Math.Abs(p1.X - p2.X);
                DisplayedHeight = Math.Abs(p1.Y - p2.Y);
            }
            catch { }
        }

        public TimeSpan GetPosition()
        {
            return _player.Position;
        }

        public void Dispose()
        {
            _player.Stop();
        }

        public void ResetTransform()
        {
            _scale = 1;
            _offset = new Point(0, 0);
            Border.RenderTransformOrigin = new Point(0.5, 0.5);
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            TransformGroup g = new TransformGroup();
            g.Children.Add(new ScaleTransform(_scale, _scale));
            g.Children.Add(new TranslateTransform(_offset.X, _offset.Y));

            Border.RenderTransform = g;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ResetTransform();
        }

        public void Move(Point delta)
        {
            _offset = new Point(_offset.X + delta.X, _offset.Y + delta.Y);
            ApplyTransform();
        }

        public void ChangeZoom(double delta)
        {
            _scale = Math.Min(10, Math.Max(0.1, _scale + delta));
            ApplyTransform();
        }

        protected virtual void OnMediaEnded()
        {
            MediaEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    public struct Resolution
    {
        public int Horizontal { get; set; }
        public int Vertical { get; set; }

        public Resolution(int horizonal, int vertical)
        {
            Horizontal = horizonal;
            Vertical = vertical;
        }
    }
}
