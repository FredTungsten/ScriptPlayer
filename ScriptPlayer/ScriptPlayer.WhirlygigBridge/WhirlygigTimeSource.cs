using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ScriptPlayer.Shared;

namespace ScriptPlayer.WhirlygigBridge
{
    public class WhirlygigTimeSource : TimeSource
    {
        private Thread _clientLoop;
        public ManualTimeSource TimeSource { get; set; }

        public WhirlygigTimeSource()
        {
            TimeSource = new ManualTimeSource(new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                TimeSpan.FromMilliseconds(10)));

            _clientLoop = new Thread(ClientLoop);
            _clientLoop.Start();
        }

        private void ClientLoop()
        {
            while (true)
            {
                try
                {
                    TimeSource.Pause();

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
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    throw;
                }
            }
        }

        private void InterpretLine(string line)
        {
            if (line.StartsWith("S"))
            {
                TimeSource.Pause();
            }
            else if (line.StartsWith("C"))
            {
                
            }
            else if (line.StartsWith("P"))
            {
                string timeStamp = line.Substring(2).Trim();
                double seconds = double.Parse(timeStamp);
                TimeSpan position = TimeSpan.FromSeconds(seconds);
                TimeSource.SetPosition(position);
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
    }
}
