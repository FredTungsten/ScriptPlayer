using System;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class MediaPlayerTimeSource : TimeSource
    {
        private readonly MediaPlayer _player;
        private readonly ISampleClock _clock;

        public MediaPlayerTimeSource(MediaPlayer player, ISampleClock clock)
        {
            _player = player;
            _clock = clock;
            _clock.Tick += ClockOnTick;
        }

        private void ClockOnTick(object sender, EventArgs eventArgs)
        {
            Progress = _player.Position;
        }
    }
}
