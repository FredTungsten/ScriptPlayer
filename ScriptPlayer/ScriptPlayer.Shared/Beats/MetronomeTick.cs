using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;
using ScriptPlayer.Shared.Sounds;

namespace ScriptPlayer.Shared
{
    public class MetronomeTick : INotifyPropertyChanged
    {
        private MediaPlayer _player;
        private double _volume;

        public double Volume
        {
            get => _volume;
            set
            {
                if (value.Equals(_volume)) return;
                _volume = value;
                _player.Volume = _volume / 100.0;
                OnPropertyChanged();
            }
        }

        public MetronomeTick()
        {
            string file = GetAndPrepTickSound();

            _player = new MediaPlayer();
            _player.Volume = 1.0;
            _player.MediaOpened += PlayerOnMediaOpened;
            _player.MediaFailed += PlayerOnMediaFailed;
            _player.Stop();
            _player.Open(new Uri(file));
        }

        private string GetAndPrepTickSound()
        {
            var resStream = Application.GetResourceStream(SoundResources.GetResourceUri("2.wav"));
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VideoSync", "Audio");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string file = Path.Combine(dir, "Tick.wav");

            if (File.Exists(file))
                return file;

            using (FileStream f = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                resStream.Stream.CopyTo(f);
                f.Flush(true);
                f.Close();
            }

            return file;
        }

        private void PlayerOnMediaFailed(object sender, ExceptionEventArgs exceptionEventArgs)
        {
            
        }

        private void PlayerOnMediaOpened(object sender, EventArgs eventArgs)
        {
            _player.Stop();
        }

        public void Tick()
        {
            _player.Stop();
            _player.Play();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}