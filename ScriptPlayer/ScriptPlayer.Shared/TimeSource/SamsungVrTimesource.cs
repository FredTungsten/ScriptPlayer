using System;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace ScriptPlayer.Shared
{
    public class SamsungVrTimeSource : TimeSource, IDisposable
    {
        private SamsungVrConnectionSettings _connectionSettings;

        public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(
            "IsConnected", typeof(bool), typeof(SamsungVrTimeSource), new PropertyMetadata(default(bool)));

        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

        public event EventHandler<string> FileOpened;

        private readonly Thread _clientLoop;
        private readonly ManualTimeSource _timeSource;
        private readonly ManualResetEvent _allDone = new ManualResetEvent(false);
        private bool _running = true;


        private TimeSpan _lastReceivedTimestamp = TimeSpan.MaxValue;

        public SamsungVrTimeSource(ISampleClock clock, SamsungVrConnectionSettings connectionSettings)
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

        public void UpdateConnectionSettings(SamsungVrConnectionSettings connectionSettings)
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
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, _connectionSettings.UdpPort);
            using (UdpClient socketv = new UdpClient(endpoint))
            {
                try
                {
                    while (_running)
                    {
                        _allDone.Reset();
                        socketv.BeginReceive(OnUdpData, socketv);
                        _allDone.WaitOne();
                        SetConnected(true);
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (ThreadInterruptedException)
                {
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

        private void OnUdpData(IAsyncResult result)
        {
            _allDone.Set();

            try
            {
                UdpClient socketv = (UdpClient)result.AsyncState;
                IPEndPoint source = new IPEndPoint(IPAddress.Any, _connectionSettings.UdpPort);

                byte[] datagram = socketv.EndReceive(result, ref source);
                string message = Encoding.UTF8.GetString(datagram);
                if(CouldBeJsonObject(message))
                    if(!message.Contains("\"headpos2\"")) // unfortunately that command contains invalid Json
                        InterpretMessage(message, source);
                else
                    Debug.WriteLine($"Udp Message wasn't Json, will be ignored: '{message}'");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Couldn't interpret Samsung VR Status: {e.Message}");
            }

        }
        private void SetConnected(bool isConnected)
        {
            if (CheckAccess())
                IsConnected = isConnected;
            else
                Dispatcher.Invoke(() => { SetConnected(isConnected); });
        }

        private static bool CouldBeJsonObject(string input)
        {
            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}");
        }

        private void InterpretMessage(string message, IPEndPoint source)
        {
            if (!_timeSource.CheckAccess())
            {
                _timeSource.Dispatcher.Invoke(() => InterpretMessage(message, source));
                return;
            }

            

            JObject data = JObject.Parse(message);

            bool outputCommand = true;
            string command = data["cmd"].Value<string>();

            switch (command)
            {
                case "pause":
                    {
                        _timeSource.Pause();
                        break;
                    }
                case "play":
                    {
                        _timeSource.Play();
                        break;
                    }
                case "stop":
                    {
                        // Not sure if this will be useful:
                        // Format of local files: {"cmd":"stop", "data":"/storage/emulated/0/Download/my_video.mp4"}
                        // string filename = data["data"].Value<string>().Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).Last();

                        _timeSource.Pause();
                        _timeSource.SetPosition(TimeSpan.Zero);
                        break;
                    }
                case "load":
                    {
                        string title = data["data"]["title"].Value<string>();
                        string filename = title + ".mp4";


                        OnFileOpened(filename);
                        _timeSource.Play();

                        break;
                    }
                case "seekTo":
                    {
                        double miliseconds = data["data"].Value<double>();
                        TimeSpan position = TimeSpan.FromMilliseconds(miliseconds);

                        if (position == _lastReceivedTimestamp)
                            return;

                        _lastReceivedTimestamp = position;
                        _timeSource.SetPosition(position);
                        break;
                    }
                case "headpos2":
                    {
                        outputCommand = false;
                        break;
                    }
            }

            if (outputCommand)
                Debug.WriteLine("Got '" + message + "' from " + source);
        }

        public override bool CanPlayPause => true;
        public override bool CanSeek => true;
        public override bool CanOpenMedia => false;

        public override double PlaybackRate
        {
            get => _timeSource.PlaybackRate;
            set => _timeSource.PlaybackRate = value;
        }

        public override void Play()
        {
            SendUdpDatagram("play");
        }

        public override void Pause()
        {
            SendUdpDatagram("pause");
        }

        public override void SetPosition(TimeSpan position)
        {
            SendUdpDatagram($"seek:{(int)position.TotalMilliseconds}");
        }

        private void SendUdpDatagram(string command)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(command);

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {EnableBroadcast = true};
                socket.Connect(IPAddress.Broadcast, _connectionSettings.UdpPort);
                socket.Send(data);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Couln't send command '{command}': {e.Message}");
            }
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
    }
}