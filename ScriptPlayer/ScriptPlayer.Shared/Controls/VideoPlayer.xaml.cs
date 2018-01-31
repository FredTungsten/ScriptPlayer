using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ScriptPlayer.Shared
{
    /// <summary>
    ///     Interaction logic for VideoPlayer.xaml
    /// </summary>
    public partial class VideoPlayer : UserControl, IDisposable
    {
        public delegate void VideoMouseEventHAndler(object sender, int x, int y);

        public static readonly DependencyProperty SpeedRatioProperty = DependencyProperty.Register(
            "SpeedRatio", typeof(double), typeof(VideoPlayer), new PropertyMetadata(1.0, OnSpeedPropertyChanged));

        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
            "Volume", typeof(double), typeof(VideoPlayer), new PropertyMetadata(100.0, OnVolumePropertyChanged));

        public static readonly DependencyProperty MainVolumeProperty = DependencyProperty.Register(
            "MainVolume", typeof(double), typeof(VideoPlayer), new PropertyMetadata(1.0, OnMainVolumePropertyChanged));

        public static readonly DependencyProperty StandByVolumeProperty = DependencyProperty.Register(
            "StandByVolume", typeof(double), typeof(VideoPlayer),
            new PropertyMetadata(0.0, OnStandByVolumePropertyChanged));

        public static readonly DependencyProperty HideMouseProperty = DependencyProperty.Register(
            "HideMouse", typeof(bool), typeof(VideoPlayer),
            new PropertyMetadata(default(bool), OnHideMousePropertyChanged));

        public static readonly DependencyProperty SampleRectProperty = DependencyProperty.Register(
            "SampleRect", typeof(Rect), typeof(VideoPlayer),
            new PropertyMetadata(Rect.Empty, OnSampleRectPropertyChanged));

        public static readonly DependencyProperty DisplayedWidthProperty = DependencyProperty.Register(
            "DisplayedWidth", typeof(double), typeof(VideoPlayer), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty DisplayedHeightProperty = DependencyProperty.Register(
            "DisplayedHeight", typeof(double), typeof(VideoPlayer), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty ResolutionProperty =
            DependencyProperty.Register("Resolution", typeof(Resolution), typeof(VideoPlayer),
                new PropertyMetadata(new Resolution(0, 0)));

        public static readonly DependencyProperty StandByBrushProperty = DependencyProperty.Register(
            "StandByBrush", typeof(Brush), typeof(VideoPlayer), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty VideoBrushProperty = DependencyProperty.Register(
            "VideoBrush", typeof(Brush), typeof(VideoPlayer), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(VideoPlayer), new PropertyMetadata(default(TimeSpan)));

        public static readonly DependencyProperty TimeSourceProperty = DependencyProperty.Register(
            "TimeSource", typeof(TimeSource), typeof(VideoPlayer), new PropertyMetadata(default(TimeSource)));

        public static readonly DependencyProperty OpenedFileProperty = DependencyProperty.Register(
            "OpenedFile", typeof(string), typeof(VideoPlayer), new PropertyMetadata(default(string)));

        private readonly MouseHider _mouseHider;
        private bool _down;
        private Point _offset;

        private MediaPlayer _player;

        private double _scale;
        private bool _sideBySide;
        private MediaPlayer _standByPlayer;

        private ulong loadIteration = 0;

        public VideoPlayer()
        {
            _mouseHider = new MouseHider(this);

            InitializeComponent();
            InitializePlayer();
        }

        public double SpeedRatio
        {
            get => (double) GetValue(SpeedRatioProperty);
            set => SetValue(SpeedRatioProperty, value);
        }

        public double Volume
        {
            get => (double) GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }

        public double MainVolume
        {
            get => (double) GetValue(MainVolumeProperty);
            set => SetValue(MainVolumeProperty, value);
        }

        public double StandByVolume
        {
            get => (double) GetValue(StandByVolumeProperty);
            set => SetValue(StandByVolumeProperty, value);
        }

        public bool HideMouse
        {
            get => (bool) GetValue(HideMouseProperty);
            set => SetValue(HideMouseProperty, value);
        }

        public Rect SampleRect
        {
            get => (Rect) GetValue(SampleRectProperty);
            set => SetValue(SampleRectProperty, value);
        }

        public double DisplayedWidth
        {
            get => (double) GetValue(DisplayedWidthProperty);
            set => SetValue(DisplayedWidthProperty, value);
        }

        public double DisplayedHeight
        {
            get => (double) GetValue(DisplayedHeightProperty);
            set => SetValue(DisplayedHeightProperty, value);
        }

        public Resolution Resolution
        {
            get => (Resolution) GetValue(ResolutionProperty);
            set => SetValue(ResolutionProperty, value);
        }

        public Brush StandByBrush
        {
            get => (Brush) GetValue(StandByBrushProperty);
            set => SetValue(StandByBrushProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan) GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        protected Resolution ActualResolution { get; set; }

        public TimeSource TimeSource
        {
            get => (TimeSource) GetValue(TimeSourceProperty);
            set => SetValue(TimeSourceProperty, value);
        }

        public string OpenedFile
        {
            get => (string) GetValue(OpenedFileProperty);
            set => SetValue(OpenedFileProperty, value);
        }

        public Brush VideoBrush
        {
            get => (Brush) GetValue(VideoBrushProperty);
            set => SetValue(VideoBrushProperty, value);
        }

        public bool SideBySide
        {
            get => _sideBySide;
            set
            {
                _sideBySide = value;
                UpdateVideoBrush();
                UpdateStandByBrush();
                UpdateResolution();
            }
        }

        public void Dispose()
        {
            _player.Stop();
            _player.Close();
            _standByPlayer.Stop();
            _standByPlayer.Close();
        }

        private static void OnSpeedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer) d).OnSpeedChanged();
        }

        private void OnSpeedChanged()
        {
            _player.SpeedRatio = SpeedRatio;
        }

        private static void OnVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer) d).OnVolumeChanged();
        }

        private void OnVolumeChanged()
        {
            OnMainVolumeChanged();
            OnStandByVolumeChanged();
        }

        private static void OnMainVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer) d).OnMainVolumeChanged();
        }

        private void OnMainVolumeChanged()
        {
            _player.Volume = MainVolume * Volume / 100.0;
        }

        private static void OnStandByVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer) d).OnStandByVolumeChanged();
        }

        private void OnStandByVolumeChanged()
        {
            _standByPlayer.Volume = StandByVolume * Volume / 100.0;
        }

        private static void OnHideMousePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer) d).UpdateMouseHider();
        }

        private void UpdateMouseHider()
        {
            _mouseHider.IsEnabled = HideMouse && TimeSource.IsPlaying;
            _mouseHider.ResetTimer();
        }

        private static void OnSampleRectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer) d).RefreshRect();
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

        public event EventHandler MediaOpened;

        public event VideoMouseEventHAndler VideoMouseDown;
        public event VideoMouseEventHAndler VideoMouseUp;

        private void SetIsPlaying(bool isPlaying)
        {
            SystemIdlePreventer.Prevent(isPlaying);
            CommandManager.InvalidateRequerySuggested();
            UpdateMouseHider();
        }

        public async void Open(string filename)
        {
            await Open(filename, TimeSpan.Zero, TimeSpan.Zero);
        }

        public async Task<bool> Open(string filename, TimeSpan startAt, TimeSpan duration)
        {
            if (!File.Exists(filename)) return false;

            return await AddSeekEntry(new SeekEntry
            {
                FileName = filename,
                Position = startAt,
                Duration = duration
            });
        }

        private async Task<bool> AddSeekEntry(SeekEntry seekEntry)
        {
            return await ProcessSeekEntry(seekEntry);
        }

        private readonly SemaphoreSlim _seekSemaphore = new SemaphoreSlim(1, 1);
        private uint _seekPriority = 0;

        private async Task<bool> ProcessSeekEntry(SeekEntry seekEntry)
        {
            try
            {
                uint myPriority = ++_seekPriority;

                await _seekSemaphore.WaitAsync();
                IsSeeking = true;

                if (_seekPriority != myPriority)
                    return false;

                if (seekEntry.FileName != OpenedFile)
                {
                    await ProcessFileSeekEntry(seekEntry);
                }
                else
                {
                    await ProcessPositionSeekEntry(seekEntry);
                }

                return true;
            }
            finally
            {
                IsSeeking = false;
                _seekSemaphore.Release(1);
            }
        }

        private async Task ProcessPositionSeekEntry(SeekEntry seekEntry)
        {
            ulong iteration = ++loadIteration;

            TimeSpan position = ClampTimestamp(seekEntry.Position);
            TimeSpan diff = position - _player.Position;

            if (position > _player.Position && diff < TimeSpan.FromSeconds(4))
                return;

            await CrossFade(position, seekEntry.Duration);

            if (iteration != loadIteration)
                return;
        }

        private async Task ProcessFileSeekEntry(SeekEntry seekEntry)
        {
            ulong iteration = ++loadIteration;

            OpenedFile = seekEntry.FileName;

            SetEventSource(_standByPlayer);

            await OpenAndWaitFor(_standByPlayer, seekEntry.FileName);

            if (iteration != loadIteration) return;

            await CrossFade(seekEntry.Position, seekEntry.Duration);

            if (iteration != loadIteration) return;

            await OpenAndWaitFor(_standByPlayer, seekEntry.FileName);
        }

        private void SetEventSource(MediaPlayer player)
        {
            _player.MediaOpened -= PlayerOnMediaOpened;
            _player.MediaEnded -= PlayerOnMediaEnded;
            _standByPlayer.MediaOpened -= PlayerOnMediaOpened;
            _standByPlayer.MediaEnded -= PlayerOnMediaEnded;

            if (player != null)
            {
                player.MediaOpened += PlayerOnMediaOpened;
                player.MediaEnded += PlayerOnMediaEnded;
            }
        }

        public bool IsSeeking { get; set; }

        public async Task OpenAndWaitFor(MediaPlayer player, string filename)
        {
            ManualResetEvent loadEvent = new ManualResetEvent(false);

            void Success(object sender, EventArgs args)
            {
                loadEvent.Set();
            }

            void Failure(object sender, ExceptionEventArgs args)
            {
                loadEvent.Set();
            }

            player.MediaOpened += Success;
            player.MediaFailed += Failure;

            player.Open(new Uri(filename, UriKind.Absolute));

            await Task.Run(() => loadEvent.WaitOne());

            player.MediaOpened -= Success;
            player.MediaFailed -= Failure;
        }

        private void SwapPlayers()
        {
            MediaPlayer tmp = _player;
            _player = _standByPlayer;
            _standByPlayer = tmp;

            UpdateVideoBrush();
            UpdateStandByBrush();
        }

        public void SetPrimaryPlayer(MediaPlayer player)
        {
            ((MediaPlayerTimeSource) TimeSource).SetPlayer(player);
        }

        private void InitializePlayer()
        {
            _player = new MediaPlayer {ScrubbingEnabled = true};
            _player.Volume = MainVolume;

            _standByPlayer = new MediaPlayer {ScrubbingEnabled = true};
            _standByPlayer.Volume = StandByVolume;

            TimeSource = new MediaPlayerTimeSource(_player,
                new DispatcherClock(Dispatcher, TimeSpan.FromMilliseconds(10)));

            TimeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;

            UpdateVideoBrush();
            UpdateStandByBrush();
        }

        private void TimeSourceOnIsPlayingChanged(object sender, bool isPlaying)
        {

            if (isPlaying)
            {
                Debug.WriteLine("now playing");
            }

            SetIsPlaying(isPlaying);
        }

        private void UpdateVideoBrush()
        {
            if (_player == null)
                return;

            Rect rect = new Rect(0, 0, 1, 1);

            if (SideBySide)
                rect = new Rect(0, 0, 0.5, 1);

            VideoDrawing videoDrawing = new VideoDrawing
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

            VideoDrawing videoDrawing = new VideoDrawing
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

            /*if (ReferenceEquals(player, _player))
            {
                Duration = player.NaturalDuration.TimeSpan;
                ActualResolution = new Resolution(player.NaturalVideoWidth, player.NaturalVideoHeight);
                UpdateResolution();
                OnMediaOpened();
            }
            else if (ReferenceEquals(player, _standByPlayer))
            {*/


            if(player.NaturalDuration.HasTimeSpan)
                Duration = player.NaturalDuration.TimeSpan;

            if (player.HasVideo)
            {
                ActualResolution = new Resolution(player.NaturalVideoWidth, player.NaturalVideoHeight);
                UpdateResolution();
            }

            OnMediaOpened();

            //}
        }

        private void UpdateResolution()
        {
            Resolution = !SideBySide
                ? ActualResolution
                : new Resolution(ActualResolution.Horizontal / 2, ActualResolution.Vertical);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 1) return;

            ((IInputElement) sender).CaptureMouse();
            _down = true;

            ClampPosition(e.GetPosition((IInputElement)sender), out int x, out int y);
            OnVideoMouseDown(x, y);
        }

        private void ClampPosition(Point point, out int x, out int y)
        {
            x = (int) Math.Max(0, Math.Min(Resolution.Horizontal, Math.Round(point.X)));
            y = (int) Math.Max(0, Math.Min(Resolution.Vertical, Math.Round(point.Y)));
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((IInputElement) sender).ReleaseMouseCapture();
            if (!_down)
                return;

            _down = false;

            ClampPosition(e.GetPosition((IInputElement)sender), out int x, out int y);
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

        public async Task<bool> SoftSeek(TimeSpan position, TimeSpan duration)
        {
            return await AddSeekEntry(new SeekEntry
            {
                FileName = OpenedFile,
                Position = position,
                Duration = duration
            });
        }

        private async Task CrossFade(TimeSpan position, TimeSpan duration)
        {
            TimeSpan seekDelay = TimeSpan.FromSeconds(0.5);
            TimeSpan playDelay = TimeSpan.FromSeconds(0.05);
            TimeSpan fadeDuration = duration;

            _standByPlayer.Position = position.Subtract(fadeDuration);

            await Task.Delay(seekDelay);

            _standByPlayer.Play();

            await Task.Delay(playDelay);

            SetPrimaryPlayer(_standByPlayer);

            Fade(fadeDuration);
            await Task.Delay(fadeDuration);

            SwapPlayers();

            CancelAnimations();

            OnVolumeChanged();
            OnStandByVolumeChanged();

            _standByPlayer.Pause();
            //_player.Play();

            Border.Opacity = 1;
        }

        private void Fade(TimeSpan duration)
        {
            Storyboard storyboardFadeOut = new Storyboard();

            DoubleAnimation volumeFadeOutAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                From = 1,
                To = 0,
                EasingFunction = new CubicEase {EasingMode = EasingMode.EaseInOut}
            };

            DoubleAnimation volumeFadeInAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                From = 0,
                To = 1,
                EasingFunction = new CubicEase {EasingMode = EasingMode.EaseInOut}
            };

            DoubleAnimation opacityFadeOutAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                To = 0,
                EasingFunction = new CubicEase {EasingMode = EasingMode.EaseInOut}
            };

            storyboardFadeOut.Children.Add(volumeFadeOutAnimation);
            storyboardFadeOut.Children.Add(volumeFadeInAnimation);
            storyboardFadeOut.Children.Add(opacityFadeOutAnimation);


            Storyboard.SetTarget(volumeFadeOutAnimation, this);
            Storyboard.SetTargetProperty(volumeFadeOutAnimation, new PropertyPath(MainVolumeProperty));

            Storyboard.SetTarget(volumeFadeInAnimation, this);
            Storyboard.SetTargetProperty(volumeFadeInAnimation, new PropertyPath(StandByVolumeProperty));

            Storyboard.SetTarget(opacityFadeOutAnimation, Border);
            Storyboard.SetTargetProperty(opacityFadeOutAnimation, new PropertyPath(OpacityProperty));

            storyboardFadeOut.Begin();
        }

        private void CancelAnimations()
        {
            BeginAnimation(MainVolumeProperty, null);
            BeginAnimation(StandByVolumeProperty, null);
            Border.BeginAnimation(OpacityProperty, null);
        }

        protected virtual void OnMediaOpened()
        {
            MediaOpened?.Invoke(this, EventArgs.Empty);
        }

        private void Viewbox_LayoutUpdated(object sender, EventArgs e)
        {
            try
            {
                GeneralTransform transform = Border.TransformToAncestor(this);

                Point p1 = transform.Transform(new Point(0, 0));
                Point p2 = transform.Transform(new Point(Border.ActualWidth, Border.ActualHeight));

                DisplayedWidth = Math.Abs(p1.X - p2.X);
                DisplayedHeight = Math.Abs(p1.Y - p2.Y);
            }
            catch
            {
                // ignored
            }
        }

        public TimeSpan GetPosition()
        {
            return _player.Position;
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

    public class SeekEntry
    {
        public string FileName { get; set; }
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }
    }
}