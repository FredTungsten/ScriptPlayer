using System;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.IO;


/*
 * This is based on SamsungVrTimeSource 
 */


namespace ScriptPlayer.Shared
{
    public class GoProVrPlayerTimeSource : TimeSource, IDisposable
    {
        public override string Name => "GoPro VR Player";
        public override bool ShowBanner => true;
        public override string ConnectInstructions => "Not connected.\r\nStart GoPro VR Player and go to File -> Preferences -> PRIMARY/SECONDARY and set: \r\n Communication mode:primary \r\n IP address: localhost \r\n IP Packet format: JSON";

        private GoProVrPlayerConnectionSettings _connectionSettings;

        private readonly Thread _clientLoop;
        private readonly ManualTimeSource _timeSource;
        private readonly ManualResetEvent _allDone = new ManualResetEvent(false);
        private bool _running = true;

        public int currentState = 1;
        public string openedFilename = "";

        public long lastPositionreceived = 0;


        private TimeSpan _lastReceivedTimestamp = TimeSpan.MaxValue;

        public GoProVrPlayerTimeSource(ISampleClock clock, GoProVrPlayerConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(100));
            _timeSource.DurationChanged += TimeSourceOnDurationChanged;
            _timeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;
            _timeSource.PlaybackRateChanged += TimeSourceOnPlaybackRateChanged;
			Console.WriteLine("Hello GoPro VR Player!");
            _clientLoop = new Thread(ClientLoop);
            _clientLoop.Start();
        }

        private void TimeSourceOnPlaybackRateChanged(object sender, double d)
        {
            OnPlaybackRateChanged(d);
        }

        public void UpdateConnectionSettings(GoProVrPlayerConnectionSettings connectionSettings)
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
			Console.WriteLine("ClientLoop gopro");
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
                    if (_running)
                        SetConnected(false);
                }
            }
        }

        private void OnUdpData(IAsyncResult result)
        {
			//Console.WriteLine("OnUdpData");
            _allDone.Set();

            try
            {
                UdpClient socketv = (UdpClient)result.AsyncState;
                IPEndPoint source = new IPEndPoint(IPAddress.Any, _connectionSettings.UdpPort);

                byte[] datagram = socketv.EndReceive(result, ref source);
                string message = Encoding.UTF8.GetString(datagram);
				//Console.WriteLine(message);
                if(CouldBeJsonObject(message))
                    if(!message.Contains("\"headpos2\"")) // unfortunately that command contains invalid Json
                        InterpretMessage(message, source);
                else
                    Debug.WriteLine($"Udp Message wasn't Json, will be ignored: '{message}'");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Couldn't interpret GoPro VR Player Status: {e.Message}");
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
			//Console.WriteLine("InterpretMessage");
            if (!_timeSource.CheckAccess())
            {
                _timeSource.Dispatcher.Invoke(() => InterpretMessage(message, source));
                return;
            }

            
			
            JObject data = JObject.Parse(message);
			
			//Console.WriteLine(data);

            /*
             * Example of json data sent by Go ProVR Player
             {
              "fov": 90,
              "id": "ked",
              "pitch": 2.4684922695159912,
              "position": 14506,
              "roll": 0,
              "state": 1,
              "stereoMode": 0,
              "url": "file:///C:/Users/MyUser/Downloads/test.mp4",
              "yaw": -0.18873780965805054
             }

             Does not send commands, only information of playback
             I have to build the commands based on that info.
             - field "state": 0 = initial mode (no opened file), 1 = playing, 2 = paused
             - field "position": current position in miliseconds
             - field "url": path of the file opened

             */

            //bool outputCommand = true;
            string filenamepath = data["url"].Value<string>();
            //string command = data["cmd"].Value<string>();
            string command = data["fov"].Value<string>();
			int receivedState = data["state"].Value<int>();
            string filenameReveiced = Path.GetFileName(filenamepath);
           
            //Console.WriteLine("receivedState: " + receivedState);
            //Console.WriteLine(filename1);
            //Console.WriteLine(filename2);
            long currentPositionReceived = data["position"].Value<long>();

            //build commands
            if (receivedState == 1 && currentState == 0) //if go from initial state to playing
            {
                command = "load";
                Console.WriteLine("LOAD!!!!!!!!!!!!");
            }

            if(receivedState == 1 && currentState == 2) //if is in 'pause' and receive 'playing'
            {
                command = "play";
                Console.WriteLine("PLAY!!!!!!!!!!");
            }

            if (receivedState == 2 && currentState == 1)  //if is 'playing' and receive 'pause'
            {
                command = "pause";
                Console.WriteLine("PAUSE  !!!!!!!!!!");
            }
            currentState = receivedState;

            if(filenameReveiced != openedFilename) //if filename changed
            {
                command = "load";
                Console.WriteLine("LOAD!!!!!!!!!!!!");
            }

            if (currentPositionReceived < this.lastPositionreceived) //if backward
            {
                command = "seekTo";
                Console.WriteLine("SEEKTO!!!!!!!!!!!!");
            }

            if (currentPositionReceived > this.lastPositionreceived) //if forward
            {
                if(currentPositionReceived - this.lastPositionreceived > 600) /* if forward more than 600ms (test optimal values) */
                {
                    command = "seekTo";
                    Console.WriteLine(currentPositionReceived - this.lastPositionreceived);
                    Console.WriteLine("SEEKTO!!!!!!!!!!!!");
                }

            }


            switch (command)
            {
                case "pause":
                    {
                        _timeSource.Pause();
                        break;
                    }
                case "play":
                    {
                        Console.WriteLine("option play!!!!!");
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
                        //string title = data["data"]["title"].Value<string>();
                        //string filename = title + ".mp4";
                        Console.WriteLine("load file: "+ filenameReveiced);
                        openedFilename = filenameReveiced;
                        OnFileOpened(filenameReveiced);
                        _timeSource.Play();

                        break;
                    }
                case "seekTo":
                    {
                        //double miliseconds = data["data"].Value<double>();
                        //TimeSpan position = TimeSpan.FromMilliseconds(miliseconds);

                        TimeSpan position = TimeSpan.FromMilliseconds(currentPositionReceived);
                        this.lastPositionreceived = currentPositionReceived;

                        if (position == _lastReceivedTimestamp)
                            return;

                        _lastReceivedTimestamp = position;
                        _timeSource.SetPosition(position);
                        break;
                    }
                /*case "headpos2":
                    {
                        outputCommand = false;
                        break;
                    }
                */
            }

            this.lastPositionreceived = currentPositionReceived;

            //if (outputCommand)
            //    Debug.WriteLine("Got '" + message + "' from " + source);
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
            //Console.WriteLine("Send play!!!!!");
            SendUdpDatagram("play");
        }

        public override void Pause()
        {
            //Console.WriteLine("Send pause!!!!!");
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

        public void Dispose()
        {
            _running = false;
            _clientLoop?.Interrupt();
            _clientLoop?.Abort();
        }
    }
}