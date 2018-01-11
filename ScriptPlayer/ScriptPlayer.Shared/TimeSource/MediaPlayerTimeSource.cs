using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class MediaPlayerTimeSource : TimeSource, IDisposable
    {
        private MediaPlayer _player;
        private readonly ISampleClock _clock;

        public MediaPlayerTimeSource(MediaPlayer player, ISampleClock clock)
        {
            SetPlayer(player);
            
            _clock = clock;
            _clock.Tick += ClockOnTick;
        }

        public void SetPlayer(MediaPlayer player)
        {
            if (_player != null)
            {
                _player.MediaOpened -= PlayerOnMediaOpened;
                _player.MediaEnded -= PlayerOnMediaEnded;
            }

            _player = player;

            if (_player != null)
            {
                _player.MediaOpened += PlayerOnMediaOpened;
                _player.MediaEnded += PlayerOnMediaEnded;

                if(_player.NaturalDuration.HasTimeSpan)
                    OnOpened();
            }
        }

        private void PlayerOnMediaEnded(object sender, EventArgs eventArgs)
        {
            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.PlayerOnMediaEnded, Setting IsPlaying to false");
            IsPlaying = false;
        }

        private void PlayerOnMediaOpened(object sender, EventArgs eventArgs)
        {
            OnOpened();
        }

        private void OnOpened()
        {
            if (_player.NaturalDuration.HasTimeSpan)
                Duration = _player.NaturalDuration.TimeSpan;

            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.PlayerOnMediaOpened, Invoking Play");
            _player.Play();
            IsPlaying = true;
        }

        private void ClockOnTick(object sender, EventArgs eventArgs)
        {
            Progress = _player.Position;
        }
        
        public override void Play()
        {
            if (IsPlaying)
            {
                Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.Play will be ignored (is already playing)");
                return;
            }

            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.Play, Setting IsPlaying to true");
            IsPlaying = true;
            _player.Play();
        }

        public override void Pause()
        {
            if (!IsPlaying)
            {
                Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.Pause will be ignored (is already paused)");
                return;
            }

            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: MediaPlayerTimeSource.Pause, Setting IsPlaying to false");
            IsPlaying = false;
            _player.Pause();
        }

        public override void SetPosition(TimeSpan position)
        {
            _player.Position = position;
        }

        public void Dispose()
        {
            _clock.Tick -= ClockOnTick;
            _player.Stop();
            _player.Close();
        }

        public override bool CanPlayPause => true;
        public override bool CanSeek => true;
        public override bool CanOpenMedia => true;
    }
}
