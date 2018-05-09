using System.Media;
using System.Windows;
using ScriptPlayer.Shared.Sounds;

namespace ScriptPlayer.Shared
{
    public class MetronomeTick
    {
        private SoundPlayer _player;

        public MetronomeTick()
        {
            var resStream = Application.GetResourceStream(SoundResources.GetResourceUri("2.wav"));
            _player = new SoundPlayer(resStream.Stream);
            _player.Load();
        }

        public void Tick()
        {
            _player.Play();
        }
    }
}