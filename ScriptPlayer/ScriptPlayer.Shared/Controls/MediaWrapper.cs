using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ScriptPlayer.Shared.Controls
{
    public class MediaWrapper : DependencyObject, IDisposable
    {
        public event EventHandler MediaEnded;
        public event EventHandler MediaOpened;

        public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register(
            "Resolution", typeof(Resolution), typeof(MediaWrapper), new PropertyMetadata(default(Resolution)));

        public Resolution Resolution
        {
            get { return (Resolution) GetValue(ResolutionProperty); }
            set { SetValue(ResolutionProperty, value); }
        }

        public static readonly DependencyProperty ActualResolutionProperty = DependencyProperty.Register(
            "ActualResolution", typeof(Resolution), typeof(MediaWrapper), new PropertyMetadata(default(Resolution)));

        public Resolution ActualResolution
        {
            get { return (Resolution) GetValue(ActualResolutionProperty); }
            set { SetValue(ActualResolutionProperty, value); }
        }

        public static readonly DependencyProperty VideoBrushProperty = DependencyProperty.Register(
            "VideoBrush", typeof(Brush), typeof(MediaWrapper), new PropertyMetadata(default(Brush)));

        public Brush VideoBrush
        {
            get { return (Brush) GetValue(VideoBrushProperty); }
            set { SetValue(VideoBrushProperty, value); }
        }

        public static readonly DependencyProperty SideBySideProperty = DependencyProperty.Register(
            "SideBySide", typeof(bool), typeof(MediaWrapper), new PropertyMetadata(default(bool), OnSideBySidePropertyChanged));

        private static void OnSideBySidePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaWrapper)d).OnSideBySideChanged();
        }

        private void OnSideBySideChanged()
        {
            UpdateBrush();
            UpdateResolution();
        }

        public bool SideBySide
        {
            get { return (bool) GetValue(SideBySideProperty); }
            set { SetValue(SideBySideProperty, value); }
        }

        public static readonly DependencyProperty EmptyBrushProperty = DependencyProperty.Register(
            "EmptyBrush", typeof(Brush), typeof(MediaWrapper), new PropertyMetadata(default(Brush)));

        public Brush EmptyBrush
        {
            get { return (Brush) GetValue(EmptyBrushProperty); }
            set { SetValue(EmptyBrushProperty, value); }
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(MediaWrapper), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Duration
        {
            get { return (TimeSpan) GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public double Volume
        {
            get => _player.Volume;
            set => _player.Volume = value;
        }

        public TimeSpan Position
        {
            get => _player.Position;
            set => _player.Position = value;
        }

        public double SpeedRatio
        {
            get => _player.SpeedRatio;
            set => _player.SpeedRatio = value;
        }

        public string LoadedMedia { get; private set; }

        private readonly MediaPlayer _player;

        public MediaWrapper()
        {
            _player = new MediaPlayer();
            _player.MediaOpened += MediaOpenedSuccess;
            _player.MediaFailed += MediaOpenedFailure;
            _player.MediaEnded += PlayerOnMediaEnded;
            _player.ScrubbingEnabled = true;
            
            EmptyBrush = new SolidColorBrush(Colors.Black);
            Resolution = new Resolution();
        }

        private void PlayerOnMediaEnded(object o, EventArgs eventArgs)
        {
            OnMediaEnded();
        }

        private void MediaOpenedSuccess(object sender, EventArgs e)
        {
            UpdateAll();
            OnMediaOpened();
        }

        private void UpdateAll()
        {
            UpdateActualResolution();
            UpdateBrush();
            UpdateDuration();
        }

        private void UpdateDuration()
        {
            if (_player.NaturalDuration.HasTimeSpan)
                Duration = _player.NaturalDuration.TimeSpan;
            else
                Duration = TimeSpan.Zero;
        }

        private void MediaOpenedFailure(object sender, ExceptionEventArgs e)
        {
            Debug.WriteLine("Media Open Failed with Exception: " + e.ErrorException.Message);
            UpdateAll();
        }

        private void UpdateBrush()
        {
            if (!_player.HasVideo)
            {
                VideoBrush = EmptyBrush;
            }

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

        public async Task Seek(string filename, TimeSpan position)
        {
            bool fileChanged = false;

            if (filename != LoadedMedia)
            {
                fileChanged = true;
                await OpenAndWaitFor(filename);
            }

            if(position != TimeSpan.Zero || fileChanged)
                await PlayAndSeek(position);
        }

        public async Task PlayAndSeek(TimeSpan position)
        {
            TimeSpan maxSeekDelay = TimeSpan.FromSeconds(1.0);
            TimeSpan maxPlayDelay = TimeSpan.FromSeconds(0.1);

            DateTime start = DateTime.Now;
            DateTime startPlay = DateTime.Now;

            Debug.WriteLine("Starting Player ... ");

            _player.Play();
            while (_player.Position == TimeSpan.Zero && DateTime.Now - startPlay <= maxPlayDelay)
                await Task.Delay(10);

            Debug.WriteLine("Started Player in " + (DateTime.Now - startPlay));

            Debug.WriteLine("Seeking " + position);
            _player.Position = position;

            DateTime startSeek = DateTime.Now;
            while (_player.Position <= position && DateTime.Now - startSeek <= maxSeekDelay)
            {
                await Task.Delay(10);
            }

            Debug.WriteLine("Seeked in " + (DateTime.Now - startSeek));
            Debug.WriteLine("Play and Seek done in " + (DateTime.Now - start));
        }

        public async Task OpenAndWaitFor(string filename)
        {
            ManualResetEvent loadEvent = new ManualResetEvent(false);

            void Success(object sender, EventArgs args)
            {
                OnMediaSuccessfullyLoaded(filename);
                loadEvent.Set();
            }

            void Failure(object sender, ExceptionEventArgs args)
            {
                loadEvent.Set();
            }

            _player.MediaOpened += Success;
            _player.MediaFailed += Failure;

            try
            {
                if (filename == LoadedMedia)
                {
                    Debug.WriteLine("File already loaded: " + filename);
                    return;
                }

                _player.Open(new Uri(filename, UriKind.Absolute));
                _player.Play();

                bool timeout = !await Task.Run(() => loadEvent.WaitOne(TimeSpan.FromSeconds(5)));

                if (timeout)
                {
                    Debug.WriteLine("Timeout while opening " + filename);
                }
            }
            finally
            {
                _player.MediaOpened -= Success;
                _player.MediaFailed -= Failure;
            }
        }

        private void OnMediaSuccessfullyLoaded(string filename)
        {
            LoadedMedia = filename;
        }

        private void UpdateActualResolution()
        {
            if (_player.HasVideo)
            {
                ActualResolution = new Resolution(_player.NaturalVideoWidth, _player.NaturalVideoHeight);
            }
            else
            {
                ActualResolution = new Resolution();
            }

            UpdateResolution();
        }

        private void UpdateResolution()
        {
            if (!SideBySide)
                Resolution = ActualResolution;
            else
                Resolution = new Resolution(ActualResolution.Horizontal / 2, ActualResolution.Vertical);
        }

        public void Dispose()
        {
            _player.Stop();
            _player.Close();
        }

        protected virtual void OnMediaEnded()
        {
            MediaEnded?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMediaOpened()
        {
            MediaOpened?.Invoke(this, EventArgs.Empty);
        }

        public void SetTimeSource(MediaPlayerTimeSource timeSource)
        {
            timeSource.SetPlayer(_player);
        }

        public TimeSource CreateTimeSource(ISampleClock sampleClock)
        {
            return new MediaPlayerTimeSource(_player, sampleClock);
        }

        public void Pause()
        {
            _player.Pause();
        }
    }
}
