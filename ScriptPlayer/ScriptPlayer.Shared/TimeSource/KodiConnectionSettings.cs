using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared
{
    public class KodiConnectionSettings
    {
        public string User { get; set; }
        public string Password { get; set; }

        public string Ip { get; set; }
        public int HttpPort { get; set; }
        public int TcpPort { get; set; }

        public KodiConnectionSettings()
        {
            Ip = DefaultIp;
            HttpPort = DefaultHttpPort;
            TcpPort = DefaultTcpPort;
            User = DefaultUser;
            Password = DefaultPass;
        }

        private const int DefaultHttpPort = 8080; // this port can be changed through kodi's gui
        private const int DefaultTcpPort = 9090; // this port can only be changed in kodi's advancedsettings.xml https://kodi.wiki/view/Advancedsettings.xml#jsonrpc
        private const string DefaultUser = "kodi"; 
        private const string DefaultPass = "";
        private const string DefaultIp = "127.0.0.1";
    }
}
