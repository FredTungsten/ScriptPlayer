using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace ScriptPlayer.Shared.TheHandy
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
        private HandyController2 _controller2;
        private bool IsScriptLoaded => !string.IsNullOrWhiteSpace(LoadedScript);

        public event EventHandler ScriptDownloadFinished;

        public HandyScriptServer(HandyController2 handyController)
        {
            _controller2 = handyController;
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

                    string args = $"/C netsh http add urlacl url=\"{prefix}\" user=\"{Environment.UserName}\"";
                    ProcessStartInfo info = new ProcessStartInfo("cmd", args)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process.Start(info);

                    _server = new HttpListener();
                    _server.Prefixes.Add(prefix);
                    _server.Start();
                }
                catch(HttpListenerException ex)
                {
                    // Still can't start?
                    MessageBox.Show($"Error hosting script: \"{ex.Message}\" (Try as Administrator)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
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

                    _controller2?.OnOsdRequest("Script Download started", TimeSpan.FromSeconds(3), "ScriptServer");

                    HttpListenerRequest request = context.Request;
                    response = context.Response;
                }
                catch (Exception) { break; }

                // Construct a response.
                byte[] buffer;
                if (IsScriptLoaded)
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

                _controller2?.OnOsdRequest("Script Download finished", TimeSpan.FromSeconds(3), "ScriptServer");
                OnScriptDownloadFinished();
            }
            _server.Stop();
        }

        protected virtual void OnScriptDownloadFinished()
        {
            ScriptDownloadFinished?.Invoke(this, EventArgs.Empty);
        }
    }
}
