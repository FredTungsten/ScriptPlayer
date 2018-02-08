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

        private bool _running = true;
        private static ManualResetEvent allDone = new ManualResetEvent(false);


        private TimeSpan _lastReceivedTimestamp = TimeSpan.MaxValue;

        public SamsungVrTimeSource(ISampleClock clock, SamsungVrConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(100));
            _timeSource.DurationChanged += TimeSourceOnDurationChanged;
            _timeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;

            _clientLoop = new Thread(ClientLoop);
            _clientLoop.Start();
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
            UdpClient socketv = new UdpClient(endpoint);

            try
            {
                while (_running)
                {
                    allDone.Reset();
                    socketv.BeginReceive(OnUdpData, socketv);
                    allDone.WaitOne();
                    SetConnected(true);
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
                socketv.Dispose();
                socketv = null;
                SetConnected(false);
            }
        }

        private void OnUdpData(IAsyncResult result)
        {
            allDone.Set();

            try
            {
                UdpClient socketv = result.AsyncState as UdpClient;
                IPEndPoint source = new IPEndPoint(IPAddress.Any, 5000);

                byte[] datagram = socketv.EndReceive(result, ref source);
                string message = Encoding.UTF8.GetString(datagram);
                JObject data = JObject.Parse(message);

                InterpretData(data);
                Console.WriteLine("Got '" + message + "' from " + source);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Couldn't interpret Samsung VR Status: " + e.Message);
            }

        }
        private void SetConnected(bool isConnected)
        {
            if (CheckAccess())
                IsConnected = isConnected;
            else
                Dispatcher.Invoke(() => { SetConnected(isConnected); });
        }

        private void InterpretData(JObject data)
        {
            if (!_timeSource.CheckAccess())
            {
                _timeSource.Dispatcher.Invoke(() => InterpretData(data));
                return;
            }

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
                        /*int res1 = line.IndexOf("title") + 8;
                        int res2 = line.IndexOf("looping") - 3;
                        string resFile = line.Substring(res1, res2 - res1);

                        string file = resFile + ".funscript";
                        Debug.WriteLine("Samsung VR opened '{0}'", file);
                        OnFileOpened(file);
                        _timeSource.Play();*/



                        // Appending ".funscript" shouldn't be necessary - we expect it to be a video so the filename should be fine.
                        // Maybe we can get the full path from "url"?
                        string title = data["data"]["title"].Value<string>();
                        string filename = title + ".mp4";


                        OnFileOpened(filename);

                        break;
                    }
                case "seekTo":
                    {
                        /*string timeStamp = line.Substring(24, line.Length - 26);
                        double seconds = double.Parse(timeStamp, CultureInfo.InvariantCulture);

                            TimeSpan position = TimeSpan.FromMilliseconds(seconds);
                        if (position == _lastReceivedTimestamp)
                            return;
                        _lastReceivedTimestamp = position;
                        _timeSource.SetPosition(position);*/

                        double miliseconds = data["data"].Value<double>();
                        TimeSpan position = TimeSpan.FromMilliseconds(miliseconds);

                        if (position == _lastReceivedTimestamp)
                            return;

                        _lastReceivedTimestamp = position;
                        _timeSource.SetPosition(position);
                        break;
                    }
            }
        }

        public override bool CanPlayPause => true;
        public override bool CanSeek => true;
        public override bool CanOpenMedia => false;

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

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.EnableBroadcast = true;
                socket.Connect(IPAddress.Broadcast, 5000);
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