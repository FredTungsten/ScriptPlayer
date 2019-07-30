using System;
using System.Diagnostics;
//using System.Diagnostics;
//using System.Threading;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class MediaPlayerTimeSource : TimeSource, IDisposable
    {
        private MediaPlayer _player;
        private readonly ISampleClock _clock;
        private TimeSpan _timeWhenPaused;

        public MediaPlayerTimeSource(MediaPlayer player, ISampleClock clock)
        {
            SetPlayer(player);
            
            _clock = clock;
            _clock.Tick += ClockOnTick;
        }

        public void SetPlayer(MediaPlayer player)
        {
            _timeWhenPaused = TimeSpan.Zero;

            double playbackrate = 1.0;

            if (_player != null)
            {
                playbackrate = _player.SpeedRatio;
                _player.MediaOpened -= PlayerOnMediaOpened;
                _player.MediaEnded -= PlayerOnMediaEnded;
            }

            _player = player;

            if (_player != null)
            {
                _player.MediaOpened += PlayerOnMediaOpened;
                _player.MediaEnded += PlayerOnMediaEnded;
                _player.SpeedRatio = playbackrate;

                if(_player.NaturalDuration.HasTimeSpan)
                    OnOpened();

                if(IsPlaying)
                    _player.Play();
                else
                    _player.Pause();
            }

            IsConnected = _player != null;
        }

        private void PlayerOnMediaEnded(object sender, EventArgs eventArgs)
        {
            _timeWhenPaused = TimeSpan.Zero;

            //Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.PlayerOnMediaEnded, Setting IsPlaying to false");
            IsPlaying = false;
        }

        private void PlayerOnMediaOpened(object sender, EventArgs eventArgs)
        {
            OnOpened();
        }

        private void OnOpened()
        {
            _timeWhenPaused = TimeSpan.Zero;

            if (_player.NaturalDuration.HasTimeSpan)
                Duration = _player.NaturalDuration.TimeSpan;

            //Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.PlayerOnMediaOpened, Invoking Play");
            _player.Play();
            IsPlaying = true;
        }

        private void ClockOnTick(object sender, EventArgs eventArgs)
        {
            TimeSpan newPosition = _player.Position;

            if (newPosition < _timeWhenPaused)
            {
                Debug.WriteLine($"Time went backwards after pausing!: Pause-{_timeWhenPaused:g} / Prog-{Progress:g} / Now:{newPosition:g}");
                return;
            }

            Progress = newPosition;
        }
        
        public override void Play()
        {
            if (IsPlaying)
            {
                //Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.Play will be ignored (is already playing)");
                return;
            }

            //Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.Play, Setting IsPlaying to true");
            IsPlaying = true;
            _player.Play();
        }

        public override void Pause()
        {
            if (!IsPlaying)
            {
                //Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.Pause will be ignored (is already paused)");
                return;
            }

            _timeWhenPaused = Progress;
            //Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.Pause, Setting IsPlaying to false");
            IsPlaying = false;
            _player.Pause();
        }

        public override void SetPosition(TimeSpan position)
        {
            _player.Position = position;
            _timeWhenPaused = position;
        }

        public void Dispose()
        {
            _clock.Tick -= ClockOnTick;
            _player.Stop();
            _player.Close();
        }

        public override double PlaybackRate
        {
            get => _player.SpeedRatio;
            set
            {
                if (_player.SpeedRatio == value) return;
                _player.SpeedRatio = value;
                OnPropertyChanged();
                OnPlaybackRateChanged(_player.SpeedRatio);
            }
        }

        public override bool CanPlayPause => true;
        public override bool CanSeek => true;
        public override bool CanOpenMedia => true;
    }
}
