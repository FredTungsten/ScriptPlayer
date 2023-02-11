using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared
{
    public class HereSphereTimeSource : TimeSource, IDisposable
    {
        public override string Name => "HereSphere";
        public override bool ShowBanner => true;
        public override string ConnectInstructions => "Not connected.\r\nOpen settings menu in selection scene or file browser. Toggle switch 'Enable remote control'.";

        public const string DefaultEndpoint = "localhost:23554";

        private const bool UseLittleEndian = true;

        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(3.0);
        private static readonly TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(1.0);
        private static readonly TimeSpan PingDelay = TimeSpan.FromSeconds(1.0);

        private readonly BlockingQueue<HereSphereApiData> _sendQueue = new BlockingQueue<HereSphereApiData>();
        private readonly ManualTimeSource _timeSource;

        private Thread _sendThread;
        private Thread _receiveThread;
        private TcpClient _client;
        private bool _connected;
        private HereSphereApiData _previousData;
        private SimpleTcpConnectionSettings _connectionSettings;
        private bool _disposed;

        public override double PlaybackRate
        {
            get => _timeSource.PlaybackRate;
            set => _timeSource.SetPlaybackRate(value);
        }

        public override bool CanPlayPause => true;

        public override bool CanSeek => true;

        public override bool CanOpenMedia => false;

        public override void Play()
        {
            Send(new HereSphereApiData
            {
                PlayerState = HereSpherePlayerState.Play
            });
        }

        public override void Pause()
        {
            Send(new HereSphereApiData
            {
                PlayerState = HereSpherePlayerState.Pause
            });
        }

        public override void SetPosition(TimeSpan position)
        {
            Send(new HereSphereApiData
            {
                CurrentTime = (float)position.TotalSeconds
            });
        }

        public HereSphereTimeSource(ISampleClock clock, SimpleTcpConnectionSettings connectionSettings)
        {
            // Manual TimeSource that will interpolate Timestamps between updates

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(100));
            _timeSource.DurationChanged += TimeSourceOnDurationChanged;
            _timeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;
            _timeSource.PlaybackRateChanged += TimeSourceOnPlaybackRateChanged;

            _connectionSettings = connectionSettings;

            new Thread(KeepConnecting).Start();
        }

        private void KeepConnecting()
        {
            while (!_disposed)
            {
                if (!_connected)
                {
                    try
                    {
                        _connectionSettings.GetParameters(out string host, out int port);
                        Connect(host, port);
                    }
                    catch
                    {
                        //
                    }
                }

                Thread.Sleep(2000);
            }
        }

        private void TimeSourceOnPlaybackRateChanged(object sender, double d)
        {
            OnPlaybackRateChanged(d);
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

        public void Connect(string hostname, int port)
        {
            if (_sendThread != null)
                Disconnect();

            if (string.IsNullOrEmpty(hostname))
                throw new Exception("Empty host name!");

            if (port <= 0 || port > 65535)
                throw new Exception("Invalid port!");

            _client = new TcpClient();
            _client.Connect(hostname, port);
            Stream stream = _client.GetStream();
            _connected = true;
            SetConnected(true);

            _sendThread = new Thread(SendLoop);
            _sendThread.Start(stream);

            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.Start(stream);
        }

        public void Send(HereSphereApiData newData)
        {
            if (!_connected)
                return;

            _sendQueue.Enqueue(newData);
        }

        public void Disconnect()
        {
            _connected = false;

            SetConnected(false);
            
            if (_sendThread != null)
            {
                try
                {
                    if (!_sendThread.Join(DisconnectTimeout))
                        _sendThread.Abort();
                    _sendThread = null;
                }
                catch
                {
                    //
                }
            }

            if (_sendThread != null)
            {
                try
                {
                    if (!_receiveThread.Join(DisconnectTimeout))
                        _receiveThread.Abort();
                    _receiveThread = null;
                }
                catch
                {
                    //
                }
            }

            if (_client != null)
            {
                try
                {
                    _client.Close();
                    _client.Dispose();
                    _client = null;
                }
                catch
                {
                    //
                }
            }

            _sendQueue.Clear();
            _previousData = null;
        }


        private void ReceiveLoop(object arg)
        {
            Stream stream = (Stream)arg;
            stream.ReadTimeout = int.MaxValue; //(int) PingDelay.TotalMilliseconds;

            try
            {
                DateTime lastReceiveTime = DateTime.UtcNow;
                byte[] buffer = new byte[1024 * 8];
                
                while (_connected)
                {
                    int bufferPosition = 0;

                    if (DateTime.UtcNow - lastReceiveTime >= ConnectTimeout)
                    {
                        Debug.WriteLine("Connection Timeout?");
                        //throw new Exception($"Connection timeout! (>= {ConnectTimeout.TotalSeconds:F2}s)");
                    }

                    const int headerLength = 4;

                    while (bufferPosition < headerLength)
                    {
                        int actuallyRead = stream.Read(buffer, bufferPosition, headerLength);
                        if (actuallyRead == 0)
                            return; // Socket Closed

                        bufferPosition += actuallyRead;
                    }

                    int messageLength = ReadInt32(buffer, 0);

                    while (bufferPosition < headerLength + messageLength)
                    {
                        int actuallyRead = stream.Read(buffer, bufferPosition, headerLength);
                        if (actuallyRead == 0)
                            return; // Socket Closed

                        bufferPosition += actuallyRead;
                    }

                    lastReceiveTime = DateTime.UtcNow;

                    if (messageLength == 0)
                    {
                        // Ping
                        continue;
                    }

                    string jsonMsg = Encoding.UTF8.GetString(buffer, headerLength, messageLength);
                    HereSphereApiData data = JsonConvert.DeserializeObject<HereSphereApiData>(jsonMsg);

                    if (data == null)
                        continue;

                    InterpretData(data);
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
                Disconnect();
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private void InterpretData(HereSphereApiData data)
        {
            if (data == null)
                return;

            if (!_timeSource.CheckAccess())
            {
                _timeSource.Dispatcher.Invoke(() => InterpretData(data));
                return;
            }

            if (!string.IsNullOrEmpty(data.Path))
            {
                if (!String.Equals(data.Path, _previousData?.Path, StringComparison.InvariantCultureIgnoreCase))
                {
                    OnFileOpened(data.Path);
                }
            }

            if (data.PlaybackSpeed != null)
            {
                //TODO doesn't work yet because of broken Two-Way Binding
                _timeSource.SetPlaybackRate((float)data.PlaybackSpeed);
            }

            if (data.Duration != null)
            {
                _timeSource.SetDuration(TimeSpan.FromSeconds((float) data.Duration));
            }

            if (data.CurrentTime != null)
            {
                _timeSource.SetPosition(TimeSpan.FromSeconds((float) data.CurrentTime));
                Debug.WriteLine("New Position: " + data.CurrentTime);
            }

            if (data.PlayerState != null)
            {
                Debug.WriteLine("New PlayerState: " + data.PlayerState);

                bool isPlaying = (data.PlayerState == HereSpherePlayerState.Play);
                if (_timeSource.IsPlaying != isPlaying)
                {
                    if (isPlaying)
                        _timeSource.Play();
                    else
                        _timeSource.Pause();
                }
            }
        }

        // This might seem excessive, but BitConverter is not consistent across different architectures.
        // With this Implementation the result should always be the same.

        private static int ReadInt32(byte[] buffer, int offset)
        {
            byte[] data = new byte[4];
            Array.Copy(buffer, 0, data, offset, 4);

            if(BitConverter.IsLittleEndian != UseLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToInt32(data, 0);
        }

        private static void WriteInt32(int value, byte[] buffer, int offset)
        {
            byte[] data = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian != UseLittleEndian)
                Array.Reverse(data);

            Array.Copy(data, 0, buffer, offset, 4);
        }

        private void SendLoop(object arg)
        {
            Stream stream = (Stream) arg;

            try
            {
                while (_connected)
                {
                    HereSphereApiData data = _sendQueue.Dequeue(PingDelay);
                    Debug.WriteLine("Sending data to HereSphere");
                    SendData(data, stream);
                }

                _sendThread = null;
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private void SendData(HereSphereApiData data, Stream stream)
        {
            if (_disposed || !_connected)
                return;

            byte[] result;

            if (data != null)
            {
                string jsonString = JsonConvert.SerializeObject(data);
                int stringLength = Encoding.UTF8.GetByteCount(jsonString);

                result = new byte[4 + stringLength];
                WriteInt32(stringLength, result, 0);
                Encoding.UTF8.GetBytes(jsonString, 0, jsonString.Length, result, 4);
            }
            else
            {
                result = new byte[4];
            }

            stream.Write(result, 0, result.Length);
            stream.Flush();
        }

        private void SetConnected(bool isConnected)
        {
            if (CheckAccess())
                IsConnected = isConnected;
            else
                Dispatcher.Invoke(() => { SetConnected(isConnected); });
        }

        public void Dispose()
        {
            _disposed = true;
            Disconnect();
        }
    }

    [Serializable]
    public class HereSphereApiData
    {
        [JsonProperty("path")]
        public string Path;

        [JsonProperty("duration")]
        public float? Duration;

        [JsonProperty("currentTime")]
        public float? CurrentTime;

        [JsonProperty("playbackSpeed")]
        public float? PlaybackSpeed;

        [JsonProperty("playerState")]
        public HereSpherePlayerState? PlayerState;
    }

    public enum HereSpherePlayerState
    {
        Play = 0,
        Pause = 1
    }
}
