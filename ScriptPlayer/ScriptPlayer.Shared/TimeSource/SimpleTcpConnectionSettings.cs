using System;

namespace ScriptPlayer.Shared
{
    public class SimpleTcpConnectionSettings
    {
        public string IpAndPort { get; set; }

        public void GetParameters(out string host, out int port)
        {
            Parse(IpAndPort, out host, out port);
        }

        public static bool Parse(string ipAndPort, out string host, out int port)
        {
            host = "";
            port = 0;

            int indexOf = ipAndPort.LastIndexOf(":", StringComparison.InvariantCultureIgnoreCase);

            if (indexOf <= 0)
                return false;

            if (!int.TryParse(ipAndPort.Substring(indexOf + 1), out int portNumber))
                return false;

            if (portNumber <= 0 || portNumber > 65535)
                return false;

            host = ipAndPort.Substring(0, indexOf);
            port = portNumber;

            return true;
        }
    }
}