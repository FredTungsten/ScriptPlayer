using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace ScriptPlayer.Shared.Devices.TheHandy
{
    public class HandyScriptServer
    {
        public int ServeScriptPort { get; set; } = 80;
        public string LocalIp { get; set; }
        public string ScriptHostUrl => $"http://{LocalIp}:{ServeScriptPort}/script/";
        public bool HttpServerRunning => _serveScriptThread != null && _serveScriptThread.IsAlive;
        public string LoadedScript { get; set; }
        
        private HttpListener _server;
        private Thread _serveScriptThread; // thread running the http server hosting the script
        private bool _scriptLoaded => !string.IsNullOrWhiteSpace(LoadedScript);

        public HandyScriptServer()
        {
            LocalIp = GetLocalIp();
        }

        private string GetLocalIp()
        {
            // TODO: this isn't great but hopefully works for a lot of people?
            var host = Dns.GetHostEntry(Dns.GetHostName());
            string foundIp = null;
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    foundIp = ip.ToString();
                    if (foundIp.StartsWith("192"))
                        return foundIp;
                }
            }

            // return the last found ipv4 address if there is none starting with 192
            if (!string.IsNullOrWhiteSpace(foundIp))
                return foundIp;

            return "failed to find ip";
        }

        public void Start()
        {
            if (_serveScriptThread == null || !_serveScriptThread.IsAlive)
            {
                _serveScriptThread = new Thread(ServeScript);
                _serveScriptThread.Start();
            }
        }

        public void Exit()
        {
            if (_serveScriptThread.IsAlive)
            {
                try
                {
                    _server.Close();
                    _serveScriptThread.Abort();
                }
                catch (Exception) { }
            }
        }

        // host current loaded script on {local-ip}:{ServeScriptPort}/script/
        // handy needs to be in the same network for this to work also needs administrator
        // aswell as no firewall blocking access / windows firewall is blocking by default ...
        private void ServeScript()
        {
            string prefix = $"http://+:{ServeScriptPort}/script/";

            _server = new HttpListener();
            _server.Prefixes.Add(prefix);

            try
            {
                _server.Start();
            }
            catch (HttpListenerException)
            {
                try
                {
                    // Launch a command prompt as admin and add prefix to URL-ACL for future use

                    ProcessStartInfo info = new ProcessStartInfo("cmd", $"/C netsh http add urlacl url=\"{prefix}\" user=\"{Environment.UserName}\"");
                    info.UseShellExecute = true;
                    info.Verb = "runas";
                    info.WindowStyle = ProcessWindowStyle.Hidden;

                    Process.Start(info);

                    _server = new HttpListener();
                    _server.Prefixes.Add(prefix);
                    _server.Start();
                }
                catch(HttpListenerException ex)
                {
                    // ACCESS DENIED
                    // probably needs administrator
                    MessageBox.Show($"Error hosting script: \"{ex.Message}\" (Try as Administrator)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (MessageBox.Show($"Hosting handy script at: {ScriptHostUrl}script.csv\n(Press \"Yes\" to test in browser.)\n"
                + "Try accessing the server with another device in the same network (maybe your phone) to test that no firewall is blocking it otherwise the Handy will also not be able to get the script.",
                "Host",
                MessageBoxButton.YesNo,
                MessageBoxImage.Asterisk)
                == MessageBoxResult.Yes)
            {
                Process.Start($"{ScriptHostUrl}script.csv");
            }

            Debug.WriteLine("hosting scripts @ " + ScriptHostUrl);

            while (true)
            {
                Debug.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerResponse response;
                try
                {
                    HttpListenerContext context = _server.GetContext();
                    HttpListenerRequest request = context.Request;
                    response = context.Response;
                }
                catch (Exception) { break; }

                // Construct a response.
                byte[] buffer;
                if (_scriptLoaded)
                {
                    string responseString = LoadedScript;
                    buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentType = "text/csv";
                }
                else
                {
                    buffer = Encoding.UTF8.GetBytes("No script loaded.\nLoad a script and refresh to download.\nThe handy will fetch the script the same way.");
                    response.ContentType = "text/plain";
                }
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            _server.Stop();
        }

    }
}
