using System;
using ScriptPlayer.Shared.Beats;

namespace ScriptPlayer.Shared
{
    public class Ticker
    {
        public double SoundDelay { get; set; }

        public double Volume
        {
            get => _tick.Volume;
            set => _tick.Volume = value;
        }

        private readonly MetronomeTick _tick = new MetronomeTick();

        private ITickSource _tickSource;
        private TimeSpan _previousCheck;
        private TimeSource _timeSource;

        public void SetTimeSource(TimeSource source)
        {
            if(_timeSource != null)
                _timeSource.ProgressChanged -= TimeSourceOnProgressChanged;

            _timeSource = source;

            if (_timeSource != null)
                _timeSource.ProgressChanged += TimeSourceOnProgressChanged;
        }

        private void TimeSourceOnProgressChanged(object sender, TimeSpan timeSpan)
        {
            Check(timeSpan);
        }

        public void SetTickSource(ITickSource tickSource)
        {
            _tickSource = tickSource;
        }

        public void Check(TimeSpan time)
        {
            if (_tickSource == null)
                return;

            if (_timeSource == null)
                return;

            //WTF weird ...
            TimeSpan soundFileDelay = TimeSpan.FromMilliseconds(85 * Math.Max(0, 2 - _timeSource.PlaybackRate));
            TimeSpan adjustedProgress = time.Subtract(TimeSpan.FromMilliseconds(SoundDelay));
            adjustedProgress = adjustedProgress.Subtract(soundFileDelay);

            if (adjustedProgress > _previousCheck)
            {
                var tickType = _tickSource.DetermineTick(new TimeFrame(_previousCheck, adjustedProgress));
                if (tickType != TickType.None)
                    _tick.Tick();
            }

            _previousCheck = adjustedProgress;
        }
    }

    public interface ITickSource
    {
        TickType DetermineTick(TimeFrame within);
    }

    public enum TickType
    {
        None,
        Major,
        Minor
    }
}
