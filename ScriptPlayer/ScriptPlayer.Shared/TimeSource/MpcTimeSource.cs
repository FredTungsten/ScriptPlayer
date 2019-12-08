using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace ScriptPlayer.Shared
{
    public class MpcTimeSource : TimeSource, IDisposable
    {
        private MpcConnectionSettings _connectionSettings;

        public event EventHandler<string> FileOpened;

        private readonly Thread _clientLoop;
        private readonly ManualTimeSource _timeSource;

        private bool _running = true;
        private MpcStatus _previousStatus;

        public MpcTimeSource(ISampleClock clock, MpcConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;
            _previousStatus = new MpcStatus { IsValid = false };

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(35));
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

        public void UpdateConnectionSettings(MpcConnectionSettings connectionSettings)
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
                    while (_running)
                    {
                        string status = Request("variables.html");
                        if (string.IsNullOrWhiteSpace(status))
                        {
                            SetConnected(false);
                        }
                        else
                        {
                            InterpretStatus(status);
                            SetConnected(true);
                        }
                        Thread.Sleep(5);
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
            {
                try
                {
                    Dispatcher.Invoke(() => { SetConnected(isConnected); });
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception in MpcTimeSource.SetConnected: " + e.Message);
                }
            }
        }

        private void InterpretStatus(string statusXml)
        {
            if (!_timeSource.CheckAccess())
            {
                try
                {
                    _timeSource.Dispatcher.Invoke(() => InterpretStatus(statusXml));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception in McpTimeSource.InterpretStatus: " + e.Message);
                }
                return;
            }

            try
            {
                MpcStatus newStatus = new MpcStatus(statusXml);

                if (newStatus.IsValid)
                {
                    if (!_previousStatus.IsValid || _previousStatus.FilePath != newStatus.FilePath)
                    {
                        OnFileOpened(newStatus.FilePath);
                    }

                    if (!_previousStatus.IsValid || _previousStatus.State != newStatus.State)
                    {
                        if (newStatus.State == MpcPlaybackState.Playing)
                            _timeSource.Play();
                        else
                            _timeSource.Pause();
                    }

                    if (!_previousStatus.IsValid || _previousStatus.Duration != newStatus.Duration)
                    {
                        _timeSource.SetDuration(TimeSpan.FromMilliseconds(newStatus.Duration));
                    }

                    if (!_previousStatus.IsValid || _previousStatus.Position != newStatus.Position)
                    {
                        _timeSource.SetPosition(TimeSpan.FromMilliseconds(newStatus.Position));
                    }
                }

                _previousStatus = newStatus;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Couldn't interpret MPC Status: " + exception.Message);
            }
        }

        public string Request(string filename)
        {
            try
            {
                var client = new WebClient();
                return client.DownloadString(new Uri($"http://{_connectionSettings.IpAndPort}/{filename}"));
            }
            catch
            {
                return null;
            }
        }

        private void PostCommand(MpcCommandCodes commandCode, params string[] keyValuePairs)
        {
            try
            {
                WebClient client = new WebClient();
                Uri uri = new Uri($"http://{_connectionSettings.IpAndPort}/command.html");
                NameValueCollection values =
                    new NameValueCollection {{"wm_command", ((int) commandCode).ToString("D")}};

                for (int i = 0; i < keyValuePairs.Length / 2; i++)
                    values.Add(keyValuePairs[i * 2], keyValuePairs[i * 2 + 1]);

                client.UploadValues(uri, values);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception in MpcTimeSource.PostCommand: " + e.Message);
            }
        }

        public override void Play()
        {
            PostCommand(MpcCommandCodes.Play);
        }

        public override void Pause()
        {
            PostCommand(MpcCommandCodes.Pause);
        }

        public override void SetPosition(TimeSpan position)
        {
            PostCommand(MpcCommandCodes.Custom, "position", position.ToString("hh\\:mm\\:ss"));
        }

        public void SetDuration(TimeSpan duration)
        {
            _timeSource.SetDuration(duration);
        }

        protected virtual void OnFileOpened(string e)
        {
            FileOpened?.Invoke(this, e);
        }

        public override double PlaybackRate
        {
            get => _timeSource.PlaybackRate;
            set => _timeSource.PlaybackRate = value;
        }

        public void Dispose()
        {
            _running = false;
            _clientLoop?.Interrupt();
            _clientLoop?.Abort();
        }

        public override bool CanPlayPause => true;
        public override bool CanSeek => true;
        public override bool CanOpenMedia => false;
    }
}
