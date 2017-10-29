using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace ScriptPlayer.Shared
{
    public class WhirligigTimeSource : TimeSource, IDisposable
    {
        private readonly WhirligigConnectionSettings _connectionSettings;

        public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(
            "IsConnected", typeof(bool), typeof(WhirligigTimeSource), new PropertyMetadata(default(bool)));

        public bool IsConnected
        {
            get => (bool) GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

        public event EventHandler<string> FileOpened;

        private readonly Thread _clientLoop;
        private readonly ManualTimeSource _timeSource;

        private bool _running = true;
        private TcpClient _client;
        private TimeSpan _lastReceivedTimestamp = TimeSpan.MaxValue;

        public WhirligigTimeSource(ISampleClock clock, WhirligigConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(100));
            _timeSource.DurationChanged += TimeSourceOnDurationChanged;
            _timeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;

            _clientLoop = new Thread(ClientLoop);
            _clientLoop.Start();
        }

        private void TimeSourceOnProgressChanged(object sender, TimeSpan progress)
        {
            Progress = progress;
        }

        private void TimeSourceOnDurationChanged(object sender, TimeSpan duration)
        {
            Duration = duration;
        }

        private void TimeSourceOnIsPlayingChanged(object sender, bool isPlaying)
        {
            IsPlaying = isPlaying;
        }

        private void ClientLoop()
        {
            while (_running)
            {
                try
                {
                    _client = new TcpClient();
                    _client.Connect(_connectionSettings.ToEndpoint());

                    SetConnected(true);

                    using (NetworkStream stream = _client.GetStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                InterpretLine(line);
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                finally
                {
                    _client.Dispose();
                    _client = null;
                    SetConnected(false);
                }
            }
        }

        private void SetConnected(bool isConnected)
        {
            if (CheckAccess())
                IsConnected = isConnected;
            else
                Dispatcher.Invoke(() => { SetConnected(isConnected); });
        }

        private void InterpretLine(string line)
        {
            if (_timeSource.CheckAccess())
            {
                if (line.StartsWith("S"))
                {
                    _timeSource.Pause();
                }
                else if (line.StartsWith("C"))
                {
                    string file = line.Substring(2).Trim('\t', ' ', '\"');
                    Debug.WriteLine("Whirligig opened '{0}'", file);
                    OnFileOpened(file);
                }
                else if (line.StartsWith("P"))
                {
                    string timeStamp = line.Substring(2).Trim();
                    double seconds = double.Parse(timeStamp, CultureInfo.InvariantCulture);
                    TimeSpan position = TimeSpan.FromSeconds(seconds);

                    _timeSource.Play();

                    if (position == _lastReceivedTimestamp)
                        return;
                    _lastReceivedTimestamp = position;
                    _timeSource.SetPosition(position);
                }
            }
            else
            {
                _timeSource.Dispatcher.Invoke(() => InterpretLine(line));
            }
        }

        public override bool CanPlayPause => false;
        public override bool CanSeek => false;
        public override bool CanOpenMedia => false;

        public override void Play()
        {
            Debug.WriteLine("Can't play");
        }

        public override void Pause()
        {
            Debug.WriteLine("Can't pause");
        }

        public override void SetPosition(TimeSpan position)
        {
            Debug.WriteLine("Can't set position");
        }

        public void SetDuration(TimeSpan duration)
        {
            _timeSource.SetDuration(duration);
        }

        protected virtual void OnFileOpened(string e)
        {
            FileOpened?.Invoke(this, e);
        }

        public void Dispose()
        {
            _running = false;
            _client?.Dispose();
            _clientLoop?.Interrupt();
            _clientLoop?.Abort();
        }
    }
}