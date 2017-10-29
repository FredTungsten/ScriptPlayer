using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using System.Xml;

namespace ScriptPlayer.Shared
{
    public class VlcTimeSource : TimeSource, IDisposable
    {
        private readonly VlcConnectionSettings _connectionSettings;

        public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(
            "IsConnected", typeof(bool), typeof(VlcTimeSource), new PropertyMetadata(default(bool)));

        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

        public event EventHandler<string> FileOpened;

        private readonly Thread _clientLoop;
        private readonly ManualTimeSource _timeSource;

        private bool _running = true;
        private VlcStatus _previousStatus;

        public VlcTimeSource(ISampleClock clock, VlcConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;
            _previousStatus = new VlcStatus{IsValid = false};

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(200));
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
                    while (_running)
                    {
                        string status = Request("status.xml");
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
                VlcStatus newStatus = new VlcStatus(statusXml);

                if (newStatus.IsValid)
                {
                    if (!_previousStatus.IsValid || _previousStatus.Filename != newStatus.Filename)
                    {
                        FindFullFilename(newStatus.Filename);
                    }

                    if (!_previousStatus.IsValid || _previousStatus.PlaybackState != newStatus.PlaybackState)
                    {
                        if (newStatus.PlaybackState == VlcPlaybackState.Playing)
                            _timeSource.Play();
                        else
                            _timeSource.Pause();
                    }

                    if (!_previousStatus.IsValid || _previousStatus.Duration != newStatus.Duration)
                    {
                        _timeSource.SetDuration(newStatus.Duration);
                    }

                    if (!_previousStatus.IsValid || _previousStatus.Progress != newStatus.Progress)
                    {
                        _timeSource.SetPosition(newStatus.Progress);
                    }
                }

                _previousStatus = newStatus;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Couldn't interpret VLC Status: " + exception.Message);
            }
        }

        private void FindFullFilename(string newStatusFilename)
        {
            string playlist = Request("playlist.xml");

            if (String.IsNullOrWhiteSpace(playlist))
                return;

            string filename = FindFileByName(playlist, newStatusFilename);

            if(!String.IsNullOrWhiteSpace(filename))
                OnFileOpened(filename);
        }

        public string Request(string filename)
        {
            try
            {
                var client = new WebClient();

                string userName = "";
                string password = _connectionSettings.Password;
                string baseUrl = $"http://{_connectionSettings.IpAndPort}";

                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + ":" + password));
                client.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";

                return client.DownloadString(new Uri($"{baseUrl}/requests/{filename}"));
            }
            catch
            {
                return null;
            }
        }

        private string FindFileByName(string playlist, string newStatusFilename)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(playlist);

            XmlNodeList nodes = document.SelectNodes("//leaf");
            if (nodes == null)
                return null;

            string encodedFilename = null;

            foreach (XmlNode node in nodes)
            {
                if (node.Attributes == null) continue;
                if (node.Attributes["name"]?.InnerText != newStatusFilename) continue;

                encodedFilename = node.Attributes["uri"]?.InnerText;
                break;
            }

            if (string.IsNullOrWhiteSpace(encodedFilename))
                return null;

            if (encodedFilename.StartsWith("file:///"))
                encodedFilename = encodedFilename.Substring("file:///".Length);

            encodedFilename = HttpUtility.UrlDecode(encodedFilename);
            return encodedFilename;
        }

        public override void Play()
        {
            Request("status.xml?command=pl_forceresume");
        }

        public override void Pause()
        {
            Request("status.xml?command=pl_forcepause");
        }

        public override void SetPosition(TimeSpan position)
        {
            Request("status.xml?command=seek&val=" + (int) position.TotalSeconds);
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

        public override bool CanPlayPause => true;
        public override bool CanSeek => true;
        public override bool CanOpenMedia => false;
    }
}
