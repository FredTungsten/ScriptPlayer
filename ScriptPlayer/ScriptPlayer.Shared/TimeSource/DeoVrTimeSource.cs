using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace ScriptPlayer.Shared
{
    public class DeoVrTimeSource : TimeSource, IDisposable
    {
        private const bool UseLittleEndian = false;

        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(3.0);
        private static readonly TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(1.0);
        private static readonly TimeSpan PingDelay = TimeSpan.FromSeconds(1.0);

        private readonly BlockingQueue<DeoVrApiData> _sendQueue = new BlockingQueue<DeoVrApiData>();
        private readonly ManualTimeSource _timeSource;

        private Thread _sendThread;
        private Thread _receiveThread;
        private TcpClient _client;
        private bool _connected;
        private DeoVrApiData _previousData;
        private DeoVrConnectionSettings _connectionSettings;

        public event EventHandler<string> FileOpened;

        public override double PlaybackRate { get; set; }

        public override bool CanPlayPause => true;

        public override bool CanSeek => true;

        public override bool CanOpenMedia => true;

        public override void Play()
        {
            Send(new DeoVrApiData
            {
                PlayerState = DeoVrPlayerState.Play
            });
        }

        public override void Pause()
        {
            Send(new DeoVrApiData
            {
                PlayerState = DeoVrPlayerState.Pause
            });
        }

        public override void SetPosition(TimeSpan position)
        {
            Send(new DeoVrApiData
            {
                CurrentTime = (float)position.TotalSeconds
            });
        }

        public DeoVrTimeSource(ISampleClock clock, DeoVrConnectionSettings connectionSettings)
        {
            // Manual TimeSource that will interpolate Timestamps between updates

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(100));
            _timeSource.DurationChanged += TimeSourceOnDurationChanged;
            _timeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;
            _timeSource.PlaybackRateChanged += TimeSourceOnPlaybackRateChanged;

            _connectionSettings = connectionSettings;
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

            _client = new TcpClient(_connectionSettings.Address, _connectionSettings.Port);
            Stream stream = _client.GetStream();
            _connected = true;

            _sendThread = new Thread(SendLoop);
            _sendThread.Start(stream);

            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.Start(stream);
        }

        public void Send(DeoVrApiData newData)
        {
            if (!_connected)
                return;

            _sendQueue.Enqueue(newData);
        }

        public void Disconnect()
        {
            _connected = false;

            if (_client != null)
            {
                _client.Close();
                _client.Dispose();
                _client = null;
            }

            SetConnected(false);
            
            if (_sendThread != null)
            {
                if (!_sendThread.Join(DisconnectTimeout))
                    _sendThread.Abort();
                _sendThread = null;
            }

            if (_sendThread != null)
            {
                if (!_receiveThread.Join(DisconnectTimeout))
                    _receiveThread.Abort();
                _receiveThread = null;
            }

            _sendQueue.Clear();
            _previousData = null;
        }


        private void ReceiveLoop(object arg)
        {
            Stream stream = (Stream)arg;
            stream.ReadTimeout = (int) PingDelay.TotalMilliseconds;

            try
            {
                DateTime lastReceiveTime = DateTime.UtcNow;
                byte[] buffer = new byte[1024 * 8];
                int bufferPosition = 0;

                while (_connected)
                {
                    if (DateTime.UtcNow - lastReceiveTime >= ConnectTimeout)
                        throw new Exception($"Connection timeout! (>= {ConnectTimeout.TotalSeconds:F2}s)");

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

                    if (messageLength == 0)
                    {
                        // Ping
                        lastReceiveTime = DateTime.UtcNow;
                        continue;
                    }

                    string jsonMsg = Encoding.UTF8.GetString(buffer, headerLength, messageLength);
                    DeoVrApiData data = JsonConvert.DeserializeObject<DeoVrApiData>(jsonMsg);

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

        private void InterpretData(DeoVrApiData data)
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
                _timeSource.PlaybackRate = (float)data.PlaybackSpeed;

            if (data.Duration != null)
                _timeSource.SetDuration(TimeSpan.FromSeconds((float) data.Duration));

            if(data.CurrentTime != null)
                _timeSource.SetPosition(TimeSpan.FromSeconds((float) data.CurrentTime));

            if (data.PlayerState != null)
            {
                bool isPlaying = (data.PlayerState == DeoVrPlayerState.Play);
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
                    DeoVrApiData data = _sendQueue.Dequeue(PingDelay);
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

        private static void SendData(DeoVrApiData data, Stream stream)
        {
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

        protected virtual void OnFileOpened(string e)
        {
            FileOpened?.Invoke(this, e);
        }

        public void Dispose()
        {
            _client?.Dispose();
            Disconnect();
        }
    }


    public class DeoVrConnectionSettings
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }

    [Serializable]
    public class DeoVrApiData
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
        public DeoVrPlayerState? PlayerState;
    }

    public enum DeoVrPlayerState
    {
        Play = 0,
        Pause = 1
    }
}
