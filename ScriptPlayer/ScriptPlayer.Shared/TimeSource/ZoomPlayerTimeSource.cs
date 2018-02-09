using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace ScriptPlayer.Shared
{
    public class ZoomPlayerTimeSource : TimeSource, IDisposable
    {
        private ZoomPlayerConnectionSettings _connectionSettings;

        public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(
            "IsConnected", typeof(bool), typeof(ZoomPlayerTimeSource), new PropertyMetadata(default(bool)));

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

        public ZoomPlayerTimeSource(ISampleClock clock, ZoomPlayerConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;

            _timeSource = new ManualTimeSource(clock, TimeSpan.FromMilliseconds(100));
            _timeSource.DurationChanged += TimeSourceOnDurationChanged;
            _timeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            _timeSource.ProgressChanged += TimeSourceOnProgressChanged;

            _clientLoop = new Thread(ClientLoop);
            _clientLoop.Start();
        }

        public void UpdateConnectionSettings(ZoomPlayerConnectionSettings connectionSettings)
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

                    SendCommand(ZoomPlayerCommandCodes.RequestPlayingFileName, "");
                    SendCommand(ZoomPlayerCommandCodes.SendTimelineUpdate, "1");
                    SendCommand(ZoomPlayerCommandCodes.SendTimelineUpdate, "2");

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
                    Thread.Sleep(1000);
                }
                finally
                {
                    DispatchPause();
                    _client.Dispose();
                    _client = null;
                    SetConnected(false);
                }
            }
        }

        private void DispatchPause()
        {
            if (CheckAccess())
                _timeSource.Pause();
            else
            {
                try
                {
                    Dispatcher.Invoke(DispatchPause);
                }
                catch
                {

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
                catch
                {
                    
                }
            }
        }

        private void InterpretLine(string line)
        {
            if (_timeSource.CheckAccess())
            {
                Debug.WriteLine(line);

                ZoomPlayerMessageCodes commandCode = (ZoomPlayerMessageCodes)int.Parse(line.Substring(0, 4));
                string parameter = line.Substring(4).Trim();

                switch (commandCode)
                {
                    case ZoomPlayerMessageCodes.StateChanged:
                        ZoomPlayerPlaybackStates state = (ZoomPlayerPlaybackStates) int.Parse(parameter);
                        if(state == ZoomPlayerPlaybackStates.Playing)
                            _timeSource.Play();
                        else
                            _timeSource.Pause();

                        break;
                    case ZoomPlayerMessageCodes.PositionUpdate:
                        string[] parts = parameter.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).ToArray();

                        string[] timeFormats = {"hh\\:mm\\:ss", "mm\\:ss"};

                        TimeSpan position = TimeSpan.ParseExact(parts[0], timeFormats, CultureInfo.InvariantCulture);
                        TimeSpan duration = TimeSpan.ParseExact(parts[1], timeFormats, CultureInfo.InvariantCulture);

                        _timeSource.SetDuration(duration);
                        _timeSource.SetPosition(position);
                        break;
                    case ZoomPlayerMessageCodes.CurrentlyLoadedFile:
                        if(!String.IsNullOrWhiteSpace(parameter))
                            OnFileOpened(parameter);
                        break;
                }
            }
            else
            {
                _timeSource.Dispatcher.Invoke(() => InterpretLine(line));
            }
        }

        public override bool CanPlayPause => true;
        public override bool CanSeek => true;
        public override bool CanOpenMedia => false;

        public override void Play()
        {
            SendCommand(ZoomPlayerCommandCodes.CallFunction, "fnPlay");
        }

        public override void Pause()
        {
            SendCommand(ZoomPlayerCommandCodes.CallFunction, "fnPause");
        }

        public override void SetPosition(TimeSpan position)
        {
            SendCommand(ZoomPlayerCommandCodes.SetCurrentPosition, position.TotalSeconds.ToString("f3"));
        }

        private void SendCommand(ZoomPlayerCommandCodes command, string parameter)
        {
            try
            {
                string message = $"{((int) command):0000} {parameter}\r\n";
                byte[] data = Encoding.ASCII.GetBytes(message);

                _client?.GetStream().Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception in ZoomPlayerTimeSource.SendCommand: " + e.Message);
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
            _client?.Dispose();
            _clientLoop?.Interrupt();
            _clientLoop?.Abort();
        }
    }

    public enum ZoomPlayerMessageCodes
    {
        StateChanged = 1000,
        PositionUpdate = 1100,
        CurrentlyLoadedFile = 1800
    }

    public enum ZoomPlayerCommandCodes
    {
        SendTimelineUpdate = 1100, //0 = off, 1 = on, 2 = resend
        RequestPlayingFileName = 1800,
        SetCurrentPosition = 5000, // s.fff
        CallFunction = 5100, // fnPlay
    }

    public enum ZoomPlayerPlaybackStates
    {
        Closed = 0,
        Stopped = 1,
        Paused = 2,
        Playing = 3
    }
}