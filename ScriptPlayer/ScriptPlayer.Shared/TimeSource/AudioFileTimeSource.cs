using System;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;

namespace ScriptPlayer.Shared
{
    public class AudioFileTimeSource : TimeSource, IDisposable
    {
        public override string Name => "Audio File";
        public override bool ShowBanner => true;
        public override string ConnectInstructions => "";

        private string _path;
        private WaveStream _rdr;
        private WaveStream _wavStream;
        private BlockAlignReductionStream _baStream;
        private WaveOut _waveOut;
        private bool _disposed;
        private bool _playing;
        private TimeSpan _position;

        public TimeSpan Delay { get; set; }

        /// <summary>
        /// mp3
        /// </summary>
        public AudioFileTimeSource(string path, string device)
        {
            _path = path;

            switch (Path.GetExtension(path).ToUpper().TrimStart('.'))
            {
                case "MP3":
                    _rdr = new Mp3FileReader(path);
                    break;
                case "WAV":
                    _rdr = new WaveFileReader(path);
                    break;
                default:
                    throw new ArgumentException($"Unsupported Audio file format", nameof(path));
            }
            
            _wavStream = WaveFormatConversionStream.CreatePcmStream(_rdr);
            _baStream = new BlockAlignReductionStream(_wavStream);

            _waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            _waveOut.DeviceNumber = GetDeviceNumber(device);
            _waveOut.DesiredLatency = 150;
            _waveOut.Init(_baStream);
        }

        private int GetDeviceNumber(string productName)
        {
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                if(capabilities.ProductName == productName)
                    return i;
            }

            return -1;
        }

        public override double PlaybackRate { get; set; }

        public override bool CanPlayPause => true;

        public override bool CanSeek => true;

        public override bool CanOpenMedia => true;

        public override void Play()
        {
            _playing = true;

            if (_position >= _rdr.TotalTime || _position <= TimeSpan.Zero)
            {
                return;
            }

            _waveOut.Play();
        }

        public override void Pause()
        {
            _playing = false;
            _waveOut.Pause();
        }

        public override void SetPosition(TimeSpan position)
        {
            Resync(position);
        }

        ~AudioFileTimeSource()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _rdr?.Dispose();
            _wavStream?.Dispose();
            _baStream?.Dispose();
            _waveOut?.Dispose();
        }

        public void Resync(TimeSpan timeSpan)
        {
            _position = timeSpan - Delay;

            if (_position >= _rdr.TotalTime)
            {
                Pause();
                return;
            }

            double maxDesync = TimeSpan.FromMilliseconds(_waveOut.DesiredLatency).TotalSeconds;
            TimeSpan currentPosition = _rdr.CurrentTime;
            TimeSpan desync = currentPosition - _position;
            if (Math.Abs(desync.TotalSeconds) > maxDesync)
            {
                Debug.WriteLine("Audio Desync - Resyncing");
                TimeSpan before = _rdr.CurrentTime;
                _rdr.CurrentTime = _position;
                TimeSpan after = _rdr.CurrentTime;
                Debug.WriteLine($"Before: {before:g} After: {after:g} Target: {_position:g}");
            }

            if (_playing && _position <= _rdr.TotalTime && _position >= TimeSpan.Zero)
            {
                Play();
            }
        }
    }
}
