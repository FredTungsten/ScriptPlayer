using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ScriptPlayer.Shared
{
    public class WhirlygigTimeSource : TimeSource
    {
        public event EventHandler<string> FileOpened;

        private Thread _clientLoop;
        public ManualTimeSource TimeSource { get; set; }

        public WhirlygigTimeSource(ISampleClock clock)
        {
            TimeSource = new ManualTimeSource(clock);
            TimeSource.DurationChanged += TimeSourceOnDurationChanged;
            TimeSource.IsPlayingChanged += TimeSourceOnIsPlayingChanged;
            TimeSource.ProgressChanged += TimeSourceOnProgressChanged;

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
            while (true)
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(new IPEndPoint(IPAddress.Loopback, 2000));

                    using (NetworkStream stream = client.GetStream())
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
            }
        }

        private void InterpretLine(string line)
        {
            if (TimeSource.CheckAccess())
            {
                if (line.StartsWith("S"))
                {
                    TimeSource.Pause();
                }
                else if (line.StartsWith("C"))
                {
                    string file = line.Substring(2).Trim('\t', ' ', '\"');
                    OnFileOpened(file);
                }
                else if (line.StartsWith("P"))
                {
                    string timeStamp = line.Substring(2).Trim();
                    double seconds = double.Parse(timeStamp, CultureInfo.InvariantCulture);
                    TimeSpan position = TimeSpan.FromSeconds(seconds);
                    TimeSource.SetPosition(position);
                }
            }
            else
            {
                TimeSource.Dispatcher.Invoke(new Action(() => InterpretLine(line)));
            }
        }

        public override void Play()
        {
            Debug.WriteLine("Can't play");
        }

        public override void Pause()
        {
            Debug.WriteLine("Can't pause");
        }

        public override void TogglePlayback()
        {
            Debug.WriteLine("Can't toggle");
        }

        public override void SetPosition(TimeSpan position)
        {
            Debug.WriteLine("Can't set position");
        }

        public void SetDuration(TimeSpan duration)
        {
            TimeSource.SetDuration(duration);
        }

        protected virtual void OnFileOpened(string e)
        {
            FileOpened?.Invoke(this, e);
        }
    }
}
