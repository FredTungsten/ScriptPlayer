using System;
using System.Media;
using System.Windows;
using ScriptPlayer.Shared.Sounds;

namespace ScriptPlayer.Shared
{
    public class MetronomeAudio
    {
        private BeatTimeline _timeline;

        private ProgressChangedEventArgs _previousProgress;
        private SoundPlayer _player;

        public MetronomeAudio()
        {
            var resStream = Application.GetResourceStream(SoundResources.GetResourceUri("2.wav"));
            _player = new SoundPlayer(resStream.Stream);
        }

        public BeatTimeline Timeline
        {
            set
            {
                if (_timeline != null)
                    _timeline.ProgressChanged -= ValueOnProgressChanged;

                _timeline = value;

                if (_timeline != null)
                    _timeline.ProgressChanged += ValueOnProgressChanged;
            }
        }

        private void ValueOnProgressChanged(object sender, ProgressChangedEventArgs eventArgs)
        {
            if (_previousProgress != null)
            {
                if (Math.Abs(_previousProgress.Progress - eventArgs.Progress) > double.Epsilon)
                {
                    if (eventArgs.BeatProgress < _previousProgress.BeatProgress)
                        _player.Play();
                }
            }

            _previousProgress = eventArgs;
        }
    }
}
