using System;

namespace ScriptPlayer.Shared
{
    public class Ticker
    {
        public double Volume
        {
            get => _tick.Volume;
            set => _tick.Volume = value;
        }

        private readonly MetronomeTick _tick = new MetronomeTick();

        private ITickSource _source;

        public void SetSource(ITickSource source)
        {
            if(_source != null)
                _source.Tick -= SourceOnTick;

            _source = source;

            if (_source != null)
                _source.Tick += SourceOnTick;
        }

        private void SourceOnTick(object sender, EventArgs eventArgs)
        {
            _tick.Tick();
        }
    }

    public interface ITickSource
    {
        event EventHandler Tick;

        double SoundDelay { get; set; }
    }
}
