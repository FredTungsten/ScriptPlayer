using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace ScriptPlayer.Shared
{
    public class WhirligigTimeSource : TimeSource, IDisposable
    {
        private WhirligigConnectionSettings _connectionSettings;

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
            _timeSource.PlaybackRateChanged += TimeSourceOnPlaybackRateChanged;

            _clientLoop = new Thread(ClientLoop);
            _clientLoop.Start();
        }

        private void TimeSourceOnPlaybackRateChanged(object sender, double d)
        {
            OnPlaybackRateChanged(d);
        }

        public void UpdateConnectionSettings(WhirligigConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;
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

                    if(_running)
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
            if(!line.StartsWith("S") && !line.StartsWith("P"))
                Debug.WriteLine("Whirligig: " + line);

            if (_timeSource.CheckAccess())
            {
                if (line.StartsWith("S"))
                {
                    _timeSource.Pause();
                }
                else if (line.StartsWith("C"))
                {
                    string file = line.Substring(2).Trim('\t', ' ', '\"');
                    Debug.WriteLine($"Whirligig opened '{file}'");
                    OnFileOpened(file);
                }
                else if (line.StartsWith("P"))
                {
                    string timeStamp = line.Substring(2).Trim();
                    double seconds = ParseWhirligigTimestap(timeStamp);
                    TimeSpan position = TimeSpan.FromSeconds(seconds);

                    _timeSource.Play();

                    if (position == _lastReceivedTimestamp)
                        return;
                    _lastReceivedTimestamp = position;
                    _timeSource.SetPosition(position);
                }
                else if (line.StartsWith("dometype")) { }
                else if (line.StartsWith("duration"))
                {
                    string timeStamp = line.Substring(10).Trim();
                    double seconds = ParseWhirligigTimestap(timeStamp);
                    _timeSource.SetDuration(TimeSpan.FromSeconds(seconds));
                }
                else
                {
                    Debug.WriteLine("Unknown Parameter: " + line);
                }

                //Unknown Parameter: dometype = 6180
                //Unknown Parameter: duration = 1389.141
            }
            else
            {
                _timeSource.Dispatcher.Invoke(() => InterpretLine(line));
            }
        }

        private static readonly CultureInfo[] Cultures;

        static WhirligigTimeSource()
        {
            Cultures = new[]
            {
                CultureInfo.InvariantCulture,
                CultureInfo.InstalledUICulture,
                CultureInfo.CurrentCulture,
                CultureInfo.CurrentUICulture,
                CultureInfo.DefaultThreadCurrentCulture,
                CultureInfo.DefaultThreadCurrentUICulture,
            }.Distinct().ToArray();
        }

        private static double ParseWhirligigTimestap(string timeStamp)
        {
            List<double> potentialValues = new List<double>();

            foreach (CultureInfo culture in Cultures)
            {
                if (double.TryParse(timeStamp, NumberStyles.AllowDecimalPoint, culture, out double value))
                {
                    if(value > 0 && ! potentialValues.Contains(value))
                        potentialValues.Add(value);
                }
            }

            if (potentialValues.Count == 0)
                return 0;
            if (potentialValues.Count == 1)
                return potentialValues[0];

            return potentialValues.Min();
        }

        public override double PlaybackRate
        {
            get => _timeSource.PlaybackRate;
            set => _timeSource.PlaybackRate = value;
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
            IsConnected = false;
            _client?.Dispose();

            if (_clientLoop == null)
                return;

            _clientLoop?.Interrupt();
            if (!_clientLoop.Join(500))
            {
                _clientLoop?.Abort();
            }

            _client = null;
        }
    }
}