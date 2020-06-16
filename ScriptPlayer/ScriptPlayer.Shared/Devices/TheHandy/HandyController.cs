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

namespace ScriptPlayer.Shared.Devices.TheHandy
{
    public static class HandyHelper {
        private static readonly string _connectionUrlBaseFormat = @"https://www.handyfeeling.com/api/v1/{0}/";
        private static string _connectionUrlWithId = null;
        private const string _defaultKey = "NO_KEY";
        public static bool IsDeviceIdSet => DeviceId != _defaultKey;

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

        public int ServeScriptPort { get; set; } = 80;
        public bool Connected { get; private set; }

        private HttpClient _http;
        private HttpListener _server;
        private DateTime _lastSyncAdjust = DateTime.Now;

        private bool _scriptLoaded => _loadedScript != null;
        private string _loadedScript;
        private string _scriptName;

        Thread _serveScriptThread;

        private long _serverTime; // holds time that was calculated via GetServerTime
        private DateTime _timeOfUpdateServerTime; // holds local time when update was done
        private long _offsetAverage;

        public string ScriptHostUrl => $"http://{GetLocalIPAddress()}:{ServeScriptPort}/script/";

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

        private static string GetLocalIPAddress()
        {
            return "192.168.1.204"; // TODO
        }

        // host current loaded script on {local-ip}:{ServeScriptPort}/script/
        // handy needs to be in the same network for this to work also needs administrator
        // aswell as no firewall blocking access / windows firewall is blocking by default ...
        private void ServeScript()
        {
            _server.Prefixes.Add(ScriptHostUrl);
            try
            {
                _server.Start(); 
            }
            catch(HttpListenerException ex)
            {
                // ACCESS DENIED
                // probably needs administrator
                Debug.WriteLine($"Error hosting script: \"{ex.Message}\"");
                return;
            }

            Debug.WriteLine("hosting scripts @ " + ScriptHostUrl);
            while(true)
            {
                Debug.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = _server.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.
                if(_scriptLoaded)
                {
                    string responseString = _loadedScript;
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    // Get a response stream and write the response to it.
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/csv";
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    // You must close the output stream.
                    output.Close();
                }
            }
            _server.Stop();
        }

        private string UrlFor(string path)
        {
            return $"{HandyHelper.ConnectionBaseUrl}{path}";
        }


        public void CheckConnected() => GetStatus();

        public void GetStatus()
        {
            SendGetRequest(UrlFor("getStatus"), async response =>
            {
                if(response.IsSuccessStatusCode)
                {
                    var status = await response.Content.ReadAsAsync<HandyResponse>();
                    if(status.success)
                    {
                        Debug.WriteLine("Successfully connected");
                        Connected = true;
                        UpdateServerTime();
                        SetSyncMode();
                    }
                    else
                    {
                        Debug.WriteLine($"Error: /getStatus: {status.error}");
                        Connected = false;
                    }
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
                _loadedScript = null;
                _scriptName = null;
            }

        }

        private string GenerateCSVFromActions(List<ScriptAction> actions)
        {
            StringBuilder builder = new StringBuilder(1024*1024);
            //builder.AppendLine("#");
            builder.AppendLine(@"""{""""type"""":""""handy""""}"",");
            foreach (var action in actions)
            {
                builder.AppendLine($"{action.TimeStamp.TotalMilliseconds},{((FunScriptAction)action).Position}"); // TODO: convert position into 0 - 100 range
            }
            return builder.ToString();
        }

        public void Resync(TimeSpan time)
        {
            if (!_playing) return;
            if (DateTime.Now - _lastSyncAdjust >= TimeSpan.FromSeconds(10) || (_currentTime.TotalMilliseconds - time.TotalMilliseconds) >= 500)
            {
                Debug.WriteLine($"Resync time: {time.TotalMilliseconds}");
                Debug.WriteLine($"Current time: {_currentTime.TotalMilliseconds}");
                _lastSyncAdjust = DateTime.Now;
                // adjust time
                SyncAdjust(new HandyAdjust()
                {
                    currentTime = (int)time.TotalMilliseconds,
                    serverTime = GetServerTimeEstimate(),
                    timeout = 5000
                });
            }
            _currentTime = time;
        }

        // /syncPrepare
        private void SyncPrepare(HandyPrepare prep)
        {
            string url = GetQuery("syncPrepare", prep);
            Debug.WriteLine($"{nameof(SyncPrepare)}: {url}");
            SendGetRequest(url);
        }

        public void Play(bool playing, double currentTime)
        {
            if (playing == _playing) return;
            _playing = playing;
            SyncPlay(new HandyPlay()
            {
                play = playing,
                serverTime = GetServerTimeEstimate(),
                time = (int)currentTime
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
        class HandyTime
        {
            public long serverTime { get; set; }
        }
        public void UpdateServerTime()
        {
            var Tsend = DateTime.Now;
            var result = _http.GetAsync(UrlFor("getServerTime")).Result;
            var Treceive = DateTime.Now;
            var RTD = Treceive - Tsend;
            _serverTime = result.Content.ReadAsAsync<HandyTime>().Result.serverTime + (long)RTD.TotalMilliseconds; 

            // TODO: implement fancy algo
            //long Ts_est = 0;
            //long offset_agg = 0;
            //for(int i=0; i < 30; i++)
            //{
            //    var Tsend = DateTime.Now;
            //    var result = _http.GetAsync(UrlFor("getServerTime")).Result;
            //    var Treceive = DateTime.Now;
            //    if(Ts_est > 0)
            //    {

            //    }
            //    var RTD = Treceive - Tsend;

            //    var Ts = result.Content.ReadAsAsync<HandyTime>().Result.serverTime;
            //    Ts_est = Ts + (long)(RTD.TotalMilliseconds / 2.0);
            //    Thread.Sleep(50);
            //}
            _timeOfUpdateServerTime = DateTime.Now;
        }

        private long GetServerTimeEstimate()
        {
            var timePassed = DateTime.Now - _timeOfUpdateServerTime;
            return _serverTime + (long)timePassed.TotalMilliseconds;
        }
    }
}
