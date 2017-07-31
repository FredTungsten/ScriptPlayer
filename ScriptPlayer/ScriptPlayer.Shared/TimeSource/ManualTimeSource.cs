using System;

namespace ScriptPlayer.Shared
{
    public class ManualTimeSource : TimeSource
    {
        private ISampleClock _clock;
        private TimeSpan _lastProgress;
        private DateTime _lastCheckpoint;

        public ManualTimeSource(ISampleClock clock)
        {
            _clock = clock;
            _clock.Tick += ClockOnTick;
        }

        public override void TogglePlayback()
        {
            if (IsPlaying)
                Pause();
            else
                Play();
        }

        public void SetDuration(TimeSpan duration)
        {
            Duration = duration;
        }

        public override void SetPosition(TimeSpan position)
        {
            _lastCheckpoint = DateTime.Now;
            _lastProgress = position;
            Progress = position;
        }

        public override void Play()
        {
            if (IsPlaying)
                return;

            RefreshProgress();
            IsPlaying = true;
        }

        public override void Pause()
        {
            if (!IsPlaying)
                return;

            RefreshProgress();
            IsPlaying = false;
        }

        private void ClockOnTick(object sender, EventArgs eventArgs)
        {
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            if (IsPlaying)
            {
                DateTime now = DateTime.Now;
                TimeSpan elapsed = now - _lastCheckpoint;
                _lastProgress += elapsed;
                _lastCheckpoint = now;
                Progress = _lastProgress;
            }
            else
            {
                _lastCheckpoint = DateTime.Now;
            }
        }
    }
}