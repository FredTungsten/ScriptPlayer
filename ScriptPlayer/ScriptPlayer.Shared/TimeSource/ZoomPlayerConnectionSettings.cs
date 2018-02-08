using System;
using System.Diagnostics;
using System.Net;

namespace ScriptPlayer.Shared
{
    public class ZoomPlayerConnectionSettings
    {
        public string IpAndPort { get; set; }
        public const string DefaultEndpoint = "127.0.0.1:4769";

        public IPEndPoint ToEndpoint()
        {
            try
            {
                string ip;
                int port;

                if (IpAndPort.Contains(":"))
                {
                    int index = IpAndPort.IndexOf(":");
                    ip = IpAndPort.Substring(0, index);
                    port = int.Parse(IpAndPort.Substring(index + 1));
                }
                else
                {
                    ip = IpAndPort;
                    port = 2000;
                }

                return new IPEndPoint(IPAddress.Parse(ip), port);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not parse Whirligig Connection Settings: " + e.Message);
                return new IPEndPoint(IPAddress.Loopback, 4769);
            }
        }

        public ZoomPlayerConnectionSettings()
        {
            IpAndPort = DefaultEndpoint;
        }
    }
}