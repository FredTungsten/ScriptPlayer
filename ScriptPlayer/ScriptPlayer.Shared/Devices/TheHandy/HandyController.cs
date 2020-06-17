using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using System.Diagnostics;
using ScriptPlayer.Shared.Scripts;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Reflection;
using System.Net.Http.Headers;
using System.Windows;
using System.ServiceModel;

namespace ScriptPlayer.Shared.Devices.TheHandy
{
    public static class HandyHelper {
        private static readonly string _connectionUrlBaseFormat = @"https://www.handyfeeling.com/api/v1/{0}/";
        private static string _connectionUrlWithId = null;
        private const string _defaultKey = "NO_KEY";
        public static bool IsDeviceIdSet => DeviceId != _defaultKey;

        public static string Default => _defaultKey;
        public static string ConnectionBaseUrl { 
            get
            {
                if(_updateConnectionUrl)
                {
                    _updateConnectionUrl = false;
                    _connectionUrlWithId = string.Format(_connectionUrlBaseFormat, DeviceId);
                }
                return _connectionUrlWithId;
            }
        }

        private static bool _updateConnectionUrl = true;
        private static string _deviceId = _defaultKey;
        public static string DeviceId { 
            get => _deviceId;
            set
            {
                if(value != _deviceId)
                    _updateConnectionUrl = true;
                _deviceId = value;
            }
        }
    }

    // reference: https://app.swaggerhub.com/apis/alexandera/handy-api/1.0.0#/
    // implementing the handy as a device doesn't really work because of it's unique videosync api
    // implementing it as a timesource would make sense but prevent using other timesources...

    // hopeffully in the future the handy will receives an api to send commands to it via bluetooth or LAN 
    // in which case it would integrate nicely into ScriptPlayer
    public class HandyController 
    {
        class HandyResponse
        {
            public bool success { get; set; }
            public bool connected { get; set; }
            public string cmd { get; set; }
            public string error { get; set; }
            public int setOffset { get; set; }
            public int adjustment { get; set; }
            public string version { get; set; }
            public string latest { get; set; }
            public int mode { get; set; }
            public float position { get; set; }
            public int stroke { get; set; }
            public int speed { get; set; }
        }

        class HandyPlay
        {
            // required
            public bool play { get; set; }
            // optional
            public long? serverTime { get; set; }
            public int? time { get; set; }
            public int? timeout { get; set; }
        }

        class HandyPrepare
        {
            // required
            public string url { get; set; } // url to funscript converted to csv can be local ip
            // optional
            public string name { get; set; } // name of scipt
            public int? size { get; set; } // max size 1MB in bytes
            public int? timeout { get; set; }
        }

        class HandyAdjust
        {
            // required
            public int currentTime { get; set; }
            public long serverTime { get; set; } 
            // optional
            public float? filter { get; set; }
            public int? timeout { get; set; } 
        }

        // TODO: make serve script port configurable
        public int ServeScriptPort { get; set; } = 80;
        public bool Connected { get; private set; }

        private HttpClient _http;
        private HttpListener _server;
        private DateTimeOffset _lastSyncAdjust = DateTimeOffset.Now;

        private bool _scriptLoaded => _loadedScript != null;
        private string _loadedScript; // script text as csv
        private string _scriptName; // path to script

        Thread _serveScriptThread; // thread running the http server hosting the script

        private long _timeOfUpdateServerTime; // holds local time when update was done
        private long _offsetAverage; // holds calculated offset that gets added to current unix time in ms to estimate api server time

        public string ScriptHostUrl => $"http://{GetLocalIp()}:{ServeScriptPort}/script/";

        private TimeSpan _currentTime = TimeSpan.FromSeconds(0);
        private bool _playing = false;

        public HandyController()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _server = new HttpListener();
            _serveScriptThread = new Thread(ServeScript);
            _serveScriptThread.Start();
        }

        private string GetLocalIp()
        {
            // TODO: this isn't great but hopefully works for alot of people?
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

            MessageBox.Show("Failed to find local ip. fuck.");
            return "";
        }

        // host current loaded script on {local-ip}:{ServeScriptPort}/script/
        // handy needs to be in the same network for this to work also needs administrator
        // aswell as no firewall blocking access / windows firewall is blocking by default ...
        private void ServeScript()
        {
            _server.Prefixes.Add($"http://*:{ServeScriptPort}/script/");
            try
            {
                _server.Start(); 
            }
            catch(HttpListenerException ex)
            {
                // ACCESS DENIED
                // probably needs administrator
                MessageBox.Show($"Error hosting script: \"{ex.Message}\" (Try as Administrator)", "Error");
                return;
            }
            if(MessageBox.Show($"Hosting handy script at: {ScriptHostUrl}script.csv\n(Press \"Yes\" to test in browser.)\n"
                + "Try accessing the server with another device in the same network (maybe your phone) to test that no firewall is blocking it otherwise the Handy will also not be able to get the script.",
                "Host",
                MessageBoxButton.YesNo,
                MessageBoxImage.Asterisk) 
                == MessageBoxResult.Yes)
            {
                Process.Start($"{ScriptHostUrl}script.csv");
            }

            Debug.WriteLine("hosting scripts @ " + ScriptHostUrl);
            while(true)
            {
                Debug.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = _server.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                
                // Construct a response.
                byte[] buffer;
                if(_scriptLoaded)
                {
                    string responseString = _loadedScript;
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

        private string UrlFor(string path)
        {
            return $"{HandyHelper.ConnectionBaseUrl}{path}";
        }

        public void CheckConnected(Action<bool> successCallback = null) => GetStatus(successCallback);

        public void GetStatus(Action<bool> successCallback = null)
        {
            SendGetRequest(UrlFor("getStatus"), async response =>
            {
                if(response.IsSuccessStatusCode)
                {
                    var status = await response.Content.ReadAsAsync<HandyResponse>();
                    if(status.success)
                    {
                        UpdateServerTime();
                        SetSyncMode();
                        Connected = true;
                        Debug.WriteLine("Successfully connected");
                    }
                    else
                    {
                        MessageBox.Show($"Handy not connected.\nError: /getStatus: {status.error}");
                        Connected = false;
                    }
                    successCallback?.Invoke(Connected);
                }
            }, ignoreConnected:true);
        }

        private void SendGetRequest(string url, Action<HttpResponseMessage> resultCallback = null, bool ignoreConnected = false)
        {
            if (!ignoreConnected && !Connected) return;
            var result = _http.GetAsync(url);
            if (resultCallback != null)
                result.ContinueWith(r => resultCallback(r.Result));
            else
            {
#if DEBUG
                result.ContinueWith(r =>
                {
                    HandyResponse resp = r.Result.Content.ReadAsAsync<HandyResponse>().Result;
                    if (!resp.success)
                    {
                        Debug.WriteLine($"error: cmd:{resp.cmd} - {resp.error} - {url}");
                    }
                    else
                    {
                        Debug.WriteLine($"success: {url}");
                    }
                });
#endif
            }
        }

        public void PrepareNewFunscript(string fileName, List<ScriptAction> actions)
        {
            string csv = GenerateCSVFromActions(actions);
            long scriptSize = System.Text.Encoding.UTF8.GetByteCount(csv);
            if(scriptSize <= (1048576 * 0.99))
            {
                _loadedScript = csv;
                _scriptName = fileName;
                if (!_serveScriptThread.IsAlive)
                    _serveScriptThread = new Thread(ServeScript);
                SetSyncMode();
                SyncPrepare(new HandyPrepare()
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(_scriptName),
                    url = ScriptHostUrl + "tmp.csv",
                    //url = @"https://sweettecheu.s3.eu-central-1.amazonaws.com/scripts/admin/dataset.csv",
                    size = (int)scriptSize,
                    timeout = 20000
                });
            }
            else
            {
                // TODO: alert user
                Debug.WriteLine("Failed to load script larger than 1MB");
                MessageBox.Show("The script is to large for the Handy.");
                _loadedScript = null;
                _scriptName = null;
            }

        }

        private string GenerateCSVFromActions(List<ScriptAction> actions)
        {
            StringBuilder builder = new StringBuilder(1024*1024);
            builder.AppendLine(@"""{""""type"""":""""handy""""}"",");
            foreach (var action in actions)
            {
                builder.AppendLine($"{action.TimeStamp.TotalMilliseconds},{((FunScriptAction)action).Position}"); // TODO: convert position into 0 - 100 range
            }
            return builder.ToString();
        }

        public void Resync(TimeSpan time)
        {
            // I can't get this to work keeps returning "Machine timed out"
            // but seems to be working fine without resyncing

            //if (!_playing) return;
            //if (DateTime.Now - _lastSyncAdjust >= TimeSpan.FromSeconds(10) || (_currentTime.TotalMilliseconds - time.TotalMilliseconds) >= 500)
            //{
            //    Debug.WriteLine($"Resync time: {time.TotalMilliseconds}");
            //    Debug.WriteLine($"Current time: {_currentTime.TotalMilliseconds}");
            //    _lastSyncAdjust = DateTime.Now;
            //    // adjust time
            //    SyncAdjust(new HandyAdjust()
            //    {
            //        filter = 0.5f,
            //        currentTime = (int)time.TotalMilliseconds,
            //        serverTime = GetServerTimeEstimate(),
            //        timeout = 5000
            //    });
            //}
            _currentTime = time;
        }

        // /syncPrepare
        private void SyncPrepare(HandyPrepare prep)
        {
            string url = GetQuery("syncPrepare", prep);
            Debug.WriteLine($"{nameof(SyncPrepare)}: {url}");
            SendGetRequest(url);
        }

        public void Play(bool playing, double currentTimeMs)
        {
            if (playing == _playing || !_scriptLoaded) return;
            _playing = playing;
            SyncPlay(new HandyPlay()
            {
                play = playing,
                serverTime = GetServerTimeEstimate(),
                time = (int)currentTimeMs
            });
        }

        // /syncPlay
        private void SyncPlay(HandyPlay play)
        {
            string url = GetQuery("syncPlay", play);
            Debug.WriteLine($"{nameof(SyncPlay)}: {url}");
            SendGetRequest(url);
        }

        private string GetQuery(string path, object queryObject)
        {
            // there is probably a better way to do the same thing
            var query = HttpUtility.ParseQueryString(string.Empty);
            PropertyInfo[] properties = queryObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object val = property.GetValue(queryObject);
                if(val != null)
                {
                    if(val is bool boolVal)
                    {
                        query[property.Name] = val.ToString().ToLower();
                    }
                    else
                    {
                        query[property.Name] = val.ToString();
                    }
                }
            }
            
            if (query.Count == 0) return UrlFor(path);

            return $"{UrlFor(path)}?{query}";
        }

        // /syncAdjustTimestamp
        private void SyncAdjust(HandyAdjust adjust)
        {
            string url = GetQuery("syncAdjustTimestamp", adjust);
            Debug.WriteLine($"{nameof(SyncAdjust)}: {url}");
            SendGetRequest(url);
        }

        private void SetSyncMode()
        {
            string url = GetQuery("setMode", new { mode = 4 });
            Debug.WriteLine($"{nameof(SyncAdjust)}: {url}");
            SendGetRequest(url);
        }

        // /getServerTime
        /*
           --Guide for server time sync
           Ask server X times about the server time (Ts). A higher value results in longer syncing time but higher accuracy. A good value is to use 30 messages (X = 30).
           Each time a message is received track the Round Trip Delay (RTD) of the message by timing message send time (Tsend) and message receive time (Treceive). Calculate RTD = Treceive – Tsend.
           Calculate the estimated server time when the message is received (Ts_est) by adding half the RTD time to the received value server time value (Ts). Ts_est = Ts + RTD/2.
           Calculate the offset between estimated server time (Ts_est) and client time (Tc). Upon receive Tc == Treceive => offset = Ts_est - Treceive. Add the offset to the aggregated offset value (offset_agg). offset_agg = offset_agg + offset.
           When all messages are received calculate the average offset (offset_avg) by dividing aggregated offset (offset_agg) values by the number of messages sent (X). offset_avg = offset_agg / X

           --Calculating server time
           When sending serverTime (Ts) to Handy calculate the Ts by using the average offset (offset_avg) and the current client time (Tc) when sending a message to Handy. Ts = Tc + offset_avg
        */
        class HandyTimeResponse
        {
            public long serverTime { get; set; }
        }
        public void UpdateServerTime()
        {
            // due too an api rate limit of I think 60 request per minute I chose just 10 attempts instead of 30...
            const int maxSyncAttempts = 10; 
            long Ts_est = 0;
            long offset_agg = 0;
            for (int i = 0; i < maxSyncAttempts; i++)
            {
                var Tsend = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var result = _http.GetAsync(UrlFor("getServerTime")).Result;
                var Treceive = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var RTD = Treceive - Tsend;

                var Ts = result.Content.ReadAsAsync<HandyTimeResponse>().Result.serverTime;
                Ts_est = Ts + (RTD / 2);

                offset_agg += Ts_est - Treceive;
            }
            _offsetAverage = (int)((double)offset_agg/(double)maxSyncAttempts);

            _timeOfUpdateServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private long GetServerTimeEstimate()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _offsetAverage;
        }
    }
}
