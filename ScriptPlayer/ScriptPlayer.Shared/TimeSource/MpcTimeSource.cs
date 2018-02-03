using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using HtmlAgilityPack;

namespace ScriptPlayer.Shared
{
    public class MpcTimeSource : TimeSource, IDisposable
    {
        private MpcConnectionSettings _connectionSettings;

        public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(
            "IsConnected", typeof(bool), typeof(MpcTimeSource), new PropertyMetadata(default(bool)));

        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

        public event EventHandler<string> FileOpened;

        private readonly Thread _clientLoop;
        private readonly ManualTimeSource _timeSource;

        private bool _running = true;
        private MpcStatus _previousStatus;

        public MpcTimeSource(ISampleClock clock, MpcConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;
            _previousStatus = new MpcStatus { IsValid = false };

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(20));
            _timeSource.DurationChanged += TimeSourceOnDurationChanged;
            _timeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;

            _clientLoop = new Thread(ClientLoop);
            _clientLoop.Start();
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

        private void InterpretStatus(string statusXml)
        {
            if (!_timeSource.CheckAccess())
            {
                _timeSource.Dispatcher.Invoke(() => InterpretStatus(statusXml));
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

        public override void Play()
        {
            //Request("status.xml?command=pl_forceresume");
        }

        public override void Pause()
        {
            //Request("status.xml?command=pl_forcepause");
        }

        public override void SetPosition(TimeSpan position)
        {
            //Request("status.xml?command=seek&val=" + (int)position.TotalSeconds);
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
            _clientLoop?.Interrupt();
            _clientLoop?.Abort();
        }

        public override bool CanPlayPause => false;
        public override bool CanSeek => false;
        public override bool CanOpenMedia => false;
    }

    public enum MpcCommandCodes
    {
        Play = 887,
        Pause = 888
    }

    public enum MpcPlaybackState
    {
        Stopped = 0,
        Paused = 1,
        Playing = 2
    }

    public class MpcStatus
    {
        public bool IsValid { get; set; }

        public string FilePath { get; set; }

        public string File { get; set; }

        public string FileDir { get; set; }

        public MpcPlaybackState State { get; set; }

        public long Position { get; set; }

        public long Duration { get; set; }

        public int VolumeLevel { get; set; }

        public MpcStatus()
        {
            IsValid = false;
        }

        public MpcStatus(string statusHtml)
        {
            try
            {
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(statusHtml);

                File = document.GetElementbyId("file").InnerText;
                FilePath = document.GetElementbyId("filepath").InnerText;
                FileDir = document.GetElementbyId("filedir").InnerText;
                State = (MpcPlaybackState)int.Parse(document.GetElementbyId("State").InnerText);
                Position = long.Parse(document.GetElementbyId("position").InnerText);
                Duration = long.Parse(document.GetElementbyId("duration").InnerText);
                VolumeLevel = int.Parse(document.GetElementbyId("volumelevel").InnerText);
                IsValid = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Couldn't load MPC-HC status page! " + e.Message);
                IsValid = false;
            }
        }
    }

    public class MpcConnectionSettings
    {
        public string IpAndPort { get; set; }

        // http://localhost:13579/variables.html
    }
}
