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
using ScriptPlayer.Shared.Controls;

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

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(VideoPlayer), new PropertyMetadata(default(TimeSpan)));

        public static readonly DependencyProperty TimeSourceProperty = DependencyProperty.Register(
            "TimeSource", typeof(TimeSource), typeof(VideoPlayer), new PropertyMetadata(default(TimeSource)));

        public static readonly DependencyProperty OpenedFileProperty = DependencyProperty.Register(
            "OpenedFile", typeof(string), typeof(VideoPlayer), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty RotateProperty = DependencyProperty.Register(
            "Rotate", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(default(bool), OnRotatePropertyChanged));


        public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register(
            "Player", typeof(MediaWrapper), typeof(VideoPlayer), new PropertyMetadata(default(MediaWrapper)));

        public MediaWrapper Player
        {
            get { return (MediaWrapper) GetValue(PlayerProperty); }
            set { SetValue(PlayerProperty, value); }
        }

        public static readonly DependencyProperty StandByPlayerProperty = DependencyProperty.Register(
            "StandByPlayer", typeof(MediaWrapper), typeof(VideoPlayer), new PropertyMetadata(default(MediaWrapper)));

        public MediaWrapper StandByPlayer
        {
            get { return (MediaWrapper) GetValue(StandByPlayerProperty); }
            set { SetValue(StandByPlayerProperty, value); }
        }

        private static void OnRotatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).RotateChanged();
        }

        private void RotateChanged()
        {
            Border.LayoutTransform = Rotate ? new RotateTransform(90) : null;
            BackgroundBorder.LayoutTransform = Rotate ? new RotateTransform(90) : null;
        }

        public bool Rotate
        {
            get => (bool)GetValue(RotateProperty);
            set => SetValue(RotateProperty, value);
        }

        private readonly MouseHider _mouseHider;
        private bool _down;
        private Point _offset;

        private double _scale;
        private bool _sideBySide;

        private readonly SemaphoreSlim _seekSemaphore = new SemaphoreSlim(1, 1);
        private ulong _seekPriority;
        private ulong _loadIteration;

        public bool IsSeeking { get; set; }

        public VideoPlayer()
        {
            _mouseHider = new MouseHider(this);

            InitializeComponent();
            InitializePlayer();
        }

        public double SpeedRatio
        {
            get => (double)GetValue(SpeedRatioProperty);
            set => SetValue(SpeedRatioProperty, value);
        }

        public double Volume
        {
            get => (double)GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }

        public double MainVolume
        {
            get => (double)GetValue(MainVolumeProperty);
            set => SetValue(MainVolumeProperty, value);
        }

        public double StandByVolume
        {
            get => (double)GetValue(StandByVolumeProperty);
            set => SetValue(StandByVolumeProperty, value);
        }

        public bool HideMouse
        {
            get => (bool)GetValue(HideMouseProperty);
            set => SetValue(HideMouseProperty, value);
        }

        public Rect SampleRect
        {
            get => (Rect)GetValue(SampleRectProperty);
            set => SetValue(SampleRectProperty, value);
        }

        public double DisplayedWidth
        {
            get => (double)GetValue(DisplayedWidthProperty);
            set => SetValue(DisplayedWidthProperty, value);
        }

        public double DisplayedHeight
        {
            get => (double)GetValue(DisplayedHeightProperty);
            set => SetValue(DisplayedHeightProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        protected Resolution ActualResolution { get; set; }

        protected Resolution ActualStandByResolution { get; set; }

        public TimeSource TimeSource
        {
            get => (TimeSource)GetValue(TimeSourceProperty);
            set => SetValue(TimeSourceProperty, value);
        }

        public string OpenedFile
        {
            get => (string)GetValue(OpenedFileProperty);
            set => SetValue(OpenedFileProperty, value);
        }

        public bool SideBySide
        {
            get => _sideBySide;
            set
            {
                _sideBySide = value;
                Player.SideBySide = value;
                StandByPlayer.SideBySide = value;
            }
        }

        public void Dispose()
        {
            Player.Dispose();
            StandByPlayer.Dispose();
        }

        private static void OnSpeedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).OnSpeedChanged();
        }

        private void OnSpeedChanged()
        {
            Player.SpeedRatio = SpeedRatio;
        }

        private static void OnVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).OnVolumeChanged();
        }

        private void OnVolumeChanged()
        {
            OnMainVolumeChanged();
            OnStandByVolumeChanged();
        }

        private static void OnMainVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).OnMainVolumeChanged();
        }

        private void OnMainVolumeChanged()
        {
            Player.Volume = MainVolume * Volume / 100.0;
        }

        private static void OnStandByVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).OnStandByVolumeChanged();
        }

        private void OnStandByVolumeChanged()
        {
            StandByPlayer.Volume = StandByVolume * Volume / 100.0;
        }

        private static void OnHideMousePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VideoPlayer)d).UpdateMouseHider();
        }

        private void UpdateMouseHider()
        {
            _mouseHider.IsEnabled = HideMouse && TimeSource.IsPlaying;
            _mouseHider.ResetTimer();
        }

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

        public event EventHandler MediaOpened;

        public event VideoMouseEventHAndler VideoMouseDown;
        public event VideoMouseEventHAndler VideoMouseUp;

        private void SetIsPlaying(bool isPlaying)
        {
            SystemIdlePreventer.Prevent(isPlaying);
            CommandManager.InvalidateRequerySuggested();
            UpdateMouseHider();
        }

        public void Open(string filename)
        {
            Open(filename, TimeSpan.Zero, TimeSpan.Zero);
        }

        public async void Open(string filename, TimeSpan startAt, TimeSpan duration)
        {
            if (!File.Exists(filename)) return;

            await AddSeekEntry(new SeekEntry
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

        private async Task<bool> ProcessSeekEntry(SeekEntry seekEntry)
        {
            seekEntry.Priority = ++_seekPriority;
            
            try
            {
                await _seekSemaphore.WaitAsync();
                IsSeeking = true;

                if (_seekPriority != seekEntry.Priority)
                    //Since awaiting the Semaphore, another entry was added to the queue --> Skip this one
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
                if (seekEntry.Priority == _seekPriority)
                    IsSeeking = false;

                _seekSemaphore.Release(1);
            }
        }

        private async Task ProcessPositionSeekEntry(SeekEntry seekEntry)
        {
            ++_loadIteration;

            TimeSpan position = ClampTimestamp(seekEntry.Position);

            if (seekEntry.Duration == TimeSpan.Zero)
            {
                Player.Position = position;
            }
            else
            {
                TimeSpan diff = position - Player.Position;

                if (position > Player.Position && diff < TimeSpan.FromSeconds(4))
                    return;

                await CrossFade(position, seekEntry.Duration, seekEntry.Priority);
            }
        }

        private async Task ProcessFileSeekEntry(SeekEntry seekEntry)
        {
            ulong iteration = ++_loadIteration;

            if (seekEntry.Duration == TimeSpan.Zero)
            {
                SetPrimaryPlayer(Player);
                SetEventSource(Player);
                await Player.Seek(seekEntry.FileName, seekEntry.Position);

                //await Player.OpenAndWaitFor(seekEntry.FileName);
            }
            else
            {
                OpenedFile = seekEntry.FileName;

                SetEventSource(StandByPlayer);

                await StandByPlayer.OpenAndWaitFor(seekEntry.FileName);

                if (iteration != _loadIteration) return;

                await CrossFade(seekEntry.Position, seekEntry.Duration);

                if (iteration != _loadIteration) return;

                await StandByPlayer.OpenAndWaitFor(seekEntry.FileName);
            }
        }

        private void SetEventSource(MediaWrapper player)
        {
            Player.MediaOpened -= PlayerOnMediaOpened;
            Player.MediaEnded -= PlayerOnMediaEnded;
            StandByPlayer.MediaOpened -= PlayerOnMediaOpened;
            StandByPlayer.MediaEnded -= PlayerOnMediaEnded;

            if (player != null)
            {
                player.MediaOpened += PlayerOnMediaOpened;
                player.MediaEnded += PlayerOnMediaEnded;
            }
        }

        private void SwapPlayers()
        {
            Debug.WriteLine("Swapping Players");
            MediaWrapper tmp = Player;
            Player = StandByPlayer;
            StandByPlayer = tmp;
        }

        public void SetPrimaryPlayer(MediaWrapper player)
        {
            player.SetTimeSource(((MediaPlayerTimeSource)TimeSource));
        }

        private void InitializePlayer()
        {
            Player = new MediaWrapper
            {
                Volume = MainVolume,
                EmptyBrush = new SolidColorBrush(Colors.Red)
            };

            StandByPlayer = new MediaWrapper
            {
                Volume = StandByVolume,
                EmptyBrush = new SolidColorBrush(Colors.Blue)
            };

            TimeSource = Player.CreateTimeSource(new DispatcherClock(Dispatcher, TimeSpan.FromMilliseconds(10)));
            TimeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
        }

        private void TimeSourceOnIsPlayingChanged(object sender, bool isPlaying)
        {
            if (isPlaying)
            {
                Debug.WriteLine("now playing");
            }

            SetIsPlaying(isPlaying);
        }

        private void PlayerOnMediaEnded(object sender, EventArgs eventArgs)
        {
            MediaWrapper player = sender as MediaWrapper;
            if (player == null) return;

            if (ReferenceEquals(player, Player))
            {
                SetIsPlaying(false);
                OnMediaEnded();
            }
            else if (ReferenceEquals(player, StandByPlayer))
            {
            }
        }

        private void PlayerOnMediaOpened(object sender, EventArgs e)
        {
            if (!(sender is MediaWrapper player)) return;

            Duration = player.Duration;

            Debug.WriteLine("PlayerOnMediaOpened");

            OnMediaOpened();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 1) return;

            ((IInputElement)sender).CaptureMouse();
            _down = true;

            ClampPosition(e.GetPosition((IInputElement)sender), out int x, out int y);
            OnVideoMouseDown(x, y);
        }

        private void ClampPosition(Point point, out int x, out int y)
        {
            x = (int)Math.Max(0, Math.Min(Player.Resolution.Horizontal, Math.Round(point.X)));
            y = (int)Math.Max(0, Math.Min(Player.Resolution.Vertical, Math.Round(point.Y)));
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((IInputElement)sender).ReleaseMouseCapture();
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
            Player.Position = position;
        }

        private TimeSpan ClampTimestamp(TimeSpan position)
        {
            if (position < TimeSpan.Zero)
                return TimeSpan.Zero;

            if (position > Player.Duration)
                return Player.Duration;

            return position;
        }

        public async void SoftSeek(TimeSpan position, TimeSpan duration)
        {
            await AddSeekEntry(new SeekEntry
            {
                FileName = OpenedFile,
                Position = position,
                Duration = duration
            });
        }

        private async Task CrossFade(TimeSpan position, TimeSpan duration, ulong priority = 0)
        {
            await StandByPlayer.PlayAndSeek(position - duration.Divide(2.0));

            SetPrimaryPlayer(StandByPlayer);

            //This would be a good spot to disable IsSeeking
            // -----------
            if (priority == _seekPriority)
                IsSeeking = false;
            // -----------

            Fade(duration);
            await Task.Delay(duration);

            SwapPlayers();

            CancelAnimations();

            OnVolumeChanged();
            OnStandByVolumeChanged();

            StandByPlayer.Pause();
            ForegroundLayer.Opacity = 1;
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
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation volumeFadeInAnimation = new DoubleAnimation
            {
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
                From = 0,
                To = 1,
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
            Storyboard.SetTargetProperty(volumeFadeOutAnimation, new PropertyPath(MainVolumeProperty));

            Storyboard.SetTarget(volumeFadeInAnimation, this);
            Storyboard.SetTargetProperty(volumeFadeInAnimation, new PropertyPath(StandByVolumeProperty));

            Storyboard.SetTarget(opacityFadeOutAnimation, ForegroundLayer);
            Storyboard.SetTargetProperty(opacityFadeOutAnimation, new PropertyPath(OpacityProperty));

            storyboardFadeOut.Begin();
        }

        private void CancelAnimations()
        {
            BeginAnimation(MainVolumeProperty, null);
            BeginAnimation(StandByVolumeProperty, null);
            ForegroundLayer.BeginAnimation(OpacityProperty, null);
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
            return Player.Position;
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
        public ulong Priority { get; set; }
    }
}