using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScriptPlayer.Shared
{

    /// <summary>
    /// Interaction logic for VideoPlayer.xaml
    /// </summary>
    public partial class VideoPlayer : UserControl, IDisposable
    {
        public static readonly DependencyProperty FadeOutOpacityProperty = DependencyProperty.Register(
            "FadeOutOpacity", typeof(double), typeof(VideoPlayer), new PropertyMetadata(0.5));

        public double FadeOutOpacity
        {
            get { return (double)GetValue(FadeOutOpacityProperty); }
            set { SetValue(FadeOutOpacityProperty, value); }
        }

        public static readonly DependencyProperty SpeedRatioProperty = DependencyProperty.Register(
            "SpeedRatio", typeof(double), typeof(VideoPlayer), new PropertyMetadata(1.0, OnSpeedPropertyChanged));

        private static void OnSpeedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).OnSpeedChanged();
        }

        private void OnSpeedChanged()
        {
            _player.SpeedRatio = SpeedRatio;
        }

        public double SpeedRatio
        {
            get { return (double)GetValue(SpeedRatioProperty); }
            set { SetValue(SpeedRatioProperty, value); }
        }

        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
            "Volume", typeof(double), typeof(VideoPlayer), new PropertyMetadata(100.0, OnVolumePropertyChanged));

        public static readonly DependencyProperty StandByVolumeProperty = DependencyProperty.Register(
            "StandByVolume", typeof(double), typeof(VideoPlayer), new PropertyMetadata(0.0, OnStandByVolumePropertyChanged));

        private static void OnStandByVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).OnStandByVolumeChanged();
        }

        public double StandByVolume
        {
            get { return (double) GetValue(StandByVolumeProperty); }
            set { SetValue(StandByVolumeProperty, value); }
        }

        private static void OnVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).OnVolumeChanged();
        }

        private void OnVolumeChanged()
        {
            _player.Volume = Volume / 100.0;
        }

        private void OnStandByVolumeChanged()
        {
            _standByPlayer.Volume = StandByVolume / 100.0;
        }

        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public static readonly DependencyProperty HideMouseProperty = DependencyProperty.Register(
            "HideMouse", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(default(bool), OnHideMousePropertyChanged));

        private static void OnHideMousePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).UpdateMouseHider();
        }

        private void UpdateMouseHider()
        {
            _mouseHider.IsEnabled = HideMouse && TimeSource.IsPlaying;
            _mouseHider.ResetTimer();
        }

        private readonly MouseHider _mouseHider;

        public bool HideMouse
        {
            get { return (bool)GetValue(HideMouseProperty); }
            set { SetValue(HideMouseProperty, value); }
        }

        public static readonly DependencyProperty SampleRectProperty = DependencyProperty.Register(
            "SampleRect", typeof(Rect), typeof(VideoPlayer), new PropertyMetadata(Rect.Empty, OnSampleRectPropertyChanged));

        private static void OnSampleRectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).RefreshRect();
        }

        public static readonly DependencyProperty SoftSeekFreezeFrameProperty = DependencyProperty.Register(
            "SoftSeekFreezeFrame", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(default(bool)));

        public bool SoftSeekFreezeFrame
        {
            get { return (bool)GetValue(SoftSeekFreezeFrameProperty); }
            set { SetValue(SoftSeekFreezeFrameProperty, value); }
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

        public static readonly DependencyProperty StandByBrushProperty = DependencyProperty.Register(
            "StandByBrush", typeof(Brush), typeof(VideoPlayer), new PropertyMetadata(default(Brush)));

        public Brush StandByBrush
        {
            get { return (Brush) GetValue(StandByBrushProperty); }
            set { SetValue(StandByBrushProperty, value); }
        }

        public static readonly DependencyProperty VideoBrushProperty = DependencyProperty.Register(
            "VideoBrush", typeof(Brush), typeof(VideoPlayer), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(VideoPlayer), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        protected Resolution ActualResolution { get; set; }

        public static readonly DependencyProperty TimeSourceProperty = DependencyProperty.Register(
            "TimeSource", typeof(TimeSource), typeof(VideoPlayer), new PropertyMetadata(default(TimeSource)));

        public TimeSource TimeSource
        {
            get { return (TimeSource)GetValue(TimeSourceProperty); }
            set { SetValue(TimeSourceProperty, value); }
        }

        public static readonly DependencyProperty OpenedFileProperty = DependencyProperty.Register(
            "OpenedFile", typeof(string), typeof(VideoPlayer), new PropertyMetadata(default(string)));

        public string OpenedFile
        {
            get { return (string)GetValue(OpenedFileProperty); }
            set { SetValue(OpenedFileProperty, value); }
        }

        public Brush VideoBrush
        {
            get { return (Brush)GetValue(VideoBrushProperty); }
            set { SetValue(VideoBrushProperty, value); }
        }

        private MediaPlayer _player;
        private MediaPlayer _standByPlayer;
        private bool _down;

        private double _scale;
        private Point _offset;
        private bool _sideBySide;
        private bool _animationCanceled;

        public bool SideBySide
        {
            get { return _sideBySide; }
            set
            {
                _sideBySide = value;
                UpdateVideoBrush();
                UpdateStandByBrush();
                UpdateResolution();
            }
        }

        public VideoPlayer()
        {
            _mouseHider = new MouseHider(this);

            InitializeComponent();
            InitializePlayer();
        }

        private void SetIsPlaying(bool isPlaying)
        {
            SystemIdlePreventer.Prevent(isPlaying);
            CommandManager.InvalidateRequerySuggested();
            UpdateMouseHider();
        }

        public async void Open(string filename)
        {
            await Open(filename, TimeSpan.Zero);
        }

        public async Task Open(string filename, TimeSpan startAt)
        {
            if (!File.Exists(filename)) return;

            OpenedFile = filename;

            /*
            _player.Open(new Uri(filename, UriKind.Absolute));
            _standByPlayer.Open(new Uri(filename, UriKind.Absolute));
            // */

            //*
            await OpenAndWaitFor(_standByPlayer, filename);

            await CrossFade(startAt);

            await OpenAndWaitFor(_standByPlayer, filename);
            // */
        }

        public async Task OpenAndWaitFor(MediaPlayer player, string filename)
        {
            ManualResetEvent loadEvent = new ManualResetEvent(false);
            EventHandler success = (sender, args) => { loadEvent.Set(); };
            EventHandler<ExceptionEventArgs> failure = (sender, args) => { loadEvent.Set(); };

            player.MediaOpened += success;
            player.MediaFailed += failure;

            player.Open(new Uri(filename, UriKind.Absolute));

            await Task.Run(() => loadEvent.WaitOne());

            player.MediaOpened -= success;
            player.MediaFailed -= failure;
        }

        private void SwapPlayers()
        {
            MediaPlayer tmp = _player;
            _player = _standByPlayer;
            _standByPlayer = tmp;

            ((MediaPlayerTimeSource)TimeSource).SetPlayer(_player);
            UpdateVideoBrush();
            UpdateStandByBrush();
        }

        private void InitializePlayer()
        {
            _player = new MediaPlayer { ScrubbingEnabled = true };
            _player.MediaOpened += PlayerOnMediaOpened;
            _player.MediaEnded += PlayerOnMediaEnded;
            _player.Volume = Volume;

            _standByPlayer = new MediaPlayer { ScrubbingEnabled = true };
            _standByPlayer.MediaOpened += PlayerOnMediaOpened;
            _standByPlayer.MediaEnded += PlayerOnMediaEnded;
            _standByPlayer.Volume = StandByVolume;

            TimeSource = new MediaPlayerTimeSource(_player, new DispatcherClock(Dispatcher, TimeSpan.FromMilliseconds(10)));

            TimeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;

            UpdateVideoBrush();
            UpdateStandByBrush();
        }

        private void TimeSourceOnIsPlayingChanged(object sender, bool isPlaying)
        {
            SetIsPlaying(isPlaying);
        }

        private void UpdateVideoBrush()
        {
            if (_player == null)
                return;

            Rect rect = new Rect(0, 0, 1, 1);

            if (SideBySide)
                rect = new Rect(0, 0, 0.5, 1);

            var videoDrawing = new VideoDrawing
            {
                Player = _player,
                Rect = rect
            };

            VideoBrush = new DrawingBrush(videoDrawing)
            {
                Stretch = Stretch.Fill,
                Viewbox = rect
            };
        }

        private void UpdateStandByBrush()
        {
            if (_standByPlayer == null)
                return;

            Rect rect = new Rect(0, 0, 1, 1);

            if (SideBySide)
                rect = new Rect(0, 0, 0.5, 1);

            var videoDrawing = new VideoDrawing
            {
                Player = _standByPlayer,
                Rect = rect
            };

            StandByBrush = new DrawingBrush(videoDrawing)
            {
                Stretch = Stretch.Fill,
                Viewbox = rect
            };
        }

        private void PlayerOnMediaEnded(object sender, EventArgs eventArgs)
        {
            MediaPlayer player = sender as MediaPlayer;
            if (player == null) return;

            if (ReferenceEquals(player, _player))
            {
                SetIsPlaying(false);
                OnMediaEnded();
            }
            else if (ReferenceEquals(player, _standByPlayer))
            {

            }
        }

        private void PlayerOnMediaOpened(object sender, EventArgs e)
        {
            MediaPlayer player = sender as MediaPlayer;
            if (player == null) return;

            if (ReferenceEquals(player, _player))
            {
                Duration = player.NaturalDuration.TimeSpan;
                ActualResolution = new Resolution(player.NaturalVideoWidth, player.NaturalVideoHeight);
                UpdateResolution();
                OnMediaOpened();
            }
            else if (ReferenceEquals(player, _standByPlayer))
            {
                //_standByPlayer.Pause();

                Duration = player.NaturalDuration.TimeSpan;
                ActualResolution = new Resolution(player.NaturalVideoWidth, player.NaturalVideoHeight);
                UpdateResolution();
                OnMediaOpened();
            }
        }

        private void UpdateResolution()
        {
            Resolution = !SideBySide ? ActualResolution : new Resolution(ActualResolution.Horizontal / 2, ActualResolution.Vertical);
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

        public void SetPosition(TimeSpan position, bool cancelAnimation = true)
        {
            if (cancelAnimation)
                CancelAnimations();

            position = ClampTimestamp(position);
            _player.Position = position;
        }

        private TimeSpan ClampTimestamp(TimeSpan position)
        {
            if (position < TimeSpan.Zero)
                return TimeSpan.Zero;

            if (position > _player.NaturalDuration)
                return _player.NaturalDuration.TimeSpan;

            return position;
        }

        public async Task SoftSeek(TimeSpan position, bool skipFadeOut = false)
        {
            position = ClampTimestamp(position);
            TimeSpan diff = position - _player.Position;

            if (diff < TimeSpan.FromSeconds(4))
            {
                SetPosition(position);
                return;
            }

            if(skipFadeOut)
                await Transistion(position, skipFadeOut);
            else
                await CrossFade(position);
        }

        private async Task CrossFade(TimeSpan position)
        {
            TimeSpan seekDelay = TimeSpan.FromSeconds(0.5);
            TimeSpan playDelay = TimeSpan.FromSeconds(0.05);
            TimeSpan fadeDuration = TimeSpan.FromSeconds(3);

            _standByPlayer.Position = position.Subtract(fadeDuration);

            BackgroundBorder.Background = StandByBrush;
            BackgroundBorder.Opacity = 1.0;
            double vol = Volume;
            
            await Task.Delay(seekDelay);

            _standByPlayer.Play();
            await Task.Delay(playDelay);

            Fade(fadeDuration);
            await Task.Delay(fadeDuration);
            
            SwapPlayers();

            CancelAnimations();

            OnVolumeChanged();
            OnStandByVolumeChanged();

            _standByPlayer.Pause();
            _player.Play();

            Border.Opacity = 1;
        }

        private void Fade(TimeSpan duration)
        {
            Storyboard storyboardFadeOut = new Storyboard();

            DoubleAnimation volumeFadeOutAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                To = 0,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation volumeFadeInAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                From = 0,
                To = Volume,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation opacityFadeOutAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                To = 0,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            storyboardFadeOut.Children.Add(volumeFadeOutAnimation);
            storyboardFadeOut.Children.Add(volumeFadeInAnimation);
            storyboardFadeOut.Children.Add(opacityFadeOutAnimation);


            Storyboard.SetTarget(volumeFadeOutAnimation, this);
            Storyboard.SetTargetProperty(volumeFadeOutAnimation, new PropertyPath(VolumeProperty));

            Storyboard.SetTarget(volumeFadeInAnimation, this);
            Storyboard.SetTargetProperty(volumeFadeInAnimation, new PropertyPath(StandByVolumeProperty));

            Storyboard.SetTarget(opacityFadeOutAnimation, Border);
            Storyboard.SetTargetProperty(opacityFadeOutAnimation, new PropertyPath(OpacityProperty));

            storyboardFadeOut.Begin();
        }

        private async Task Transistion(TimeSpan position, bool skipFadeOut)
        {
            _animationCanceled = false;
            TimeSpan fadeOutDuration = TimeSpan.FromSeconds(1);
            TimeSpan fadeInDuration = TimeSpan.FromSeconds(1);

            if (!skipFadeOut)
            {
                await FadeOutAndCapture(fadeOutDuration);
            }

            if (_animationCanceled) return;
            SetPosition(position, false);

            if (_animationCanceled) return;
            await FadeInAndRelease(fadeInDuration);
        }

        private async Task FadeInAndRelease(TimeSpan fadeInDuration)
        {
            FadeIn(fadeInDuration);
            if (_animationCanceled) return;
            await Task.Delay(fadeInDuration);
            if (_animationCanceled) return;
            BackgroundBorder.Background = new SolidColorBrush(Colors.Black);
        }

        private async Task FadeOutAndCapture(TimeSpan fadeOutDuration)
        {
            FadeOut(fadeOutDuration);
            if (_animationCanceled) return;
            await Task.Delay(fadeOutDuration);
            if (_animationCanceled) return;

            if (SoftSeekFreezeFrame)
                CaptureBackground();
        }

        private void CaptureBackground()
        {
            int w = Resolution.Horizontal;
            int h = Resolution.Vertical;

            if (w * h > 0)
            {
                RenderTargetBitmap bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                DrawingVisual visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.DrawRectangle(VideoBrush, null, new Rect(0, 0, w, h));
                }
                bitmap.Render(visual);

                BackgroundBorder.Background = new ImageBrush(bitmap);
            }
        }

        

        private void FadeOut(TimeSpan duration)
        {
            Storyboard storyboardFadeOut = new Storyboard();

            DoubleAnimation volumeFadeOutAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                To = 0,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            double opacity = SoftSeekFreezeFrame ? FadeOutOpacity : 0.0;

            DoubleAnimation opacityFadeOutAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                To = opacity,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            storyboardFadeOut.Children.Add(volumeFadeOutAnimation);
            storyboardFadeOut.Children.Add(opacityFadeOutAnimation);

            Storyboard.SetTarget(volumeFadeOutAnimation, this);
            Storyboard.SetTarget(opacityFadeOutAnimation, Border);
            Storyboard.SetTargetProperty(volumeFadeOutAnimation, new PropertyPath(VolumeProperty));
            Storyboard.SetTargetProperty(opacityFadeOutAnimation, new PropertyPath(OpacityProperty));

            storyboardFadeOut.Begin();
        }

        private void CancelAnimations()
        {
            _animationCanceled = true;
            BeginAnimation(VolumeProperty, null);
            BeginAnimation(StandByVolumeProperty, null);
            Border.BeginAnimation(OpacityProperty, null);
        }

        private void FadeIn(TimeSpan duration)
        {
            Storyboard storyboardFadeIn = new Storyboard();

            DoubleAnimation volumeFadeInAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.Stop,
                From = 0,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation opacityFadeInAnimation = new DoubleAnimation()
            {
                Duration = duration,
                FillBehavior = FillBehavior.Stop,
                From = 0,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            storyboardFadeIn.Children.Add(volumeFadeInAnimation);
            storyboardFadeIn.Children.Add(opacityFadeInAnimation);

            Storyboard.SetTarget(volumeFadeInAnimation, this);
            Storyboard.SetTarget(opacityFadeInAnimation, Border);
            Storyboard.SetTargetProperty(volumeFadeInAnimation, new PropertyPath(VolumeProperty));
            Storyboard.SetTargetProperty(opacityFadeInAnimation, new PropertyPath(OpacityProperty));

            storyboardFadeIn.Begin();
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

            BackgroundBorder.RenderTransform = g;
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
}
