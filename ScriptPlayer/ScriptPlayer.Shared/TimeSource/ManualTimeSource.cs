using System;
using System.Diagnostics;

namespace ScriptPlayer.Shared
{
    public class ManualTimeSource : TimeSource
    {
        private readonly ISampleClock _clock;
        private readonly object _clocklock = new object();

        private TimeSpan _lastProgress;
        private DateTime _lastCheckpoint;
        private TimeSpan _maxOffset;

        public override bool CanPlayPause => true;
        public override bool CanSeek => true;
        public override bool CanOpenMedia => true;

        public ManualTimeSource(ISampleClock clock) : this(clock, TimeSpan.FromMilliseconds(30))
        { }

        public ManualTimeSource(ISampleClock clock, TimeSpan maxOffset)
        {
            _maxOffset = maxOffset;
            _clock = clock;
            _clock.Tick += ClockOnTick;
        }

        public void SetDuration(TimeSpan duration)
        {
            Duration = duration;
        }

        public override void SetPosition(TimeSpan position)
        {
            //Calculate Expected Position and Compare:

            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - _lastCheckpoint;
            TimeSpan expected = _lastProgress + elapsed;

            TimeSpan diff = expected - position;

            //Debug.WriteLine("Time Offset: " + diff.TotalMilliseconds.ToString("f2") + " ms [" + position.ToString("h\\:mm\\:ss\\.fff") + "]");

            if (Math.Abs(diff.TotalMilliseconds) < Math.Abs(_maxOffset.TotalMilliseconds))
                return;

            lock (_clocklock)
            {
                Debug.WriteLine("Offset too high ({0}), adjusting ...", diff);
                _lastCheckpoint = DateTime.Now;
                _lastProgress = position;
                Progress = position;
            }
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
                lock (_clocklock)
                {
                    DateTime now = DateTime.Now;
                    TimeSpan elapsed = now - _lastCheckpoint;
                    _lastProgress += elapsed;
                    _lastCheckpoint = now;
                    Progress = _lastProgress;
                }
            }
            else
            {
                _lastCheckpoint = DateTime.Now;
            }
        }
    }
}