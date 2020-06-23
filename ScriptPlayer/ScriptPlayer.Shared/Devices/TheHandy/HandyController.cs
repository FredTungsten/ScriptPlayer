#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Diagnostics;
using ScriptPlayer.Shared.Scripts;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Reflection;
using System.Net.Http.Headers;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.ServiceModel.Channels;

namespace ScriptPlayer.Shared.Devices.TheHandy
{
    public class BlockingTaskQueue
    {
        private BlockingCollection<Task> _jobs = new BlockingCollection<Task>();

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private Thread _worker;

        public BlockingTaskQueue()
        {
            _worker = new Thread(new ThreadStart(OnStart));
            _worker.IsBackground = true;
            _worker.Start();
        }

        public void Enqueue(Task job)
        {
            _jobs.Add(job);
        }

        private void OnStart()
        {
            foreach (var job in _jobs.GetConsumingEnumerable(_tokenSource.Token))
            {
                job.RunSynchronously();
            }
        }

        public void Cancel()
        {
            _tokenSource.Cancel();
            _worker.Abort();
        }
    }

    public static class HandyHelper {
        private const string _connectionUrlBaseFormat = @"https://www.handyfeeling.com/api/v1/{0}/";
        private static string _connectionUrlWithId = null;
        private const string _defaultKey = "NO_KEY";
        public static bool IsDeviceIdSet => DeviceId != _defaultKey;

        public static string Default => _defaultKey;
        public static string ConnectionBaseUrl {
            get
            {
                if (_updateConnectionUrl)
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
                if (value != _deviceId)
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
            public float speed { get; set; }
        }

        class HandyUploadResponse
        {
            public bool success { get; set; }
            public bool? converted { get; set; }
            public string filename { get; set; }
            public string info { get; set; }
            public string orginalfile { get; set; }
            public int size { get; set; }
            public string url { get; set; }
            public string error { get; set; }
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

        class HandyOffset
        {
            // required
            public int offset { get; set; }
            // optional
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

        public bool UseLocalScriptServer => LocalScriptServer != null;
        public HandyScriptServer LocalScriptServer { get; private set; } = null;
        
        public bool Connected { get; private set; }

        private HttpClient _http;
        private DateTimeOffset _lastSyncAdjust = DateTimeOffset.Now;

        private bool _scriptLoaded => _loadedScript != null;
        private string _loadedScript; // script text as csv
        private string _scriptName; // path to script

        private long _offsetAverage; // holds calculated offset that gets added to current unix time in ms to estimate api server time


        private TimeSpan _currentTime = TimeSpan.FromSeconds(0);
        private bool _playing = false;


        // offset task to not spam server with api calls everytime the offset changes slightly
        private Task _updateOffsetTask = null;
        private int _newOffsetMs;
        private bool _resetOffsetTask = false;
        private object _updateOffsetLock = new object();

        // api call queue ensures correct order of api calls
        private BlockingTaskQueue _apiCallQueue = null;

        public HandyController(bool hostLocal)
        {
            if(hostLocal)
            {
                LocalScriptServer = new HandyScriptServer();
            }

            _apiCallQueue = new BlockingTaskQueue();
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        }

        public void Exit()
        {
            _apiCallQueue.Cancel();
            LocalScriptServer?.Exit();
        }

        private string UrlFor(string path)
        {
            return $"{HandyHelper.ConnectionBaseUrl}{path}";
        }

        public void StartLocalHttpServer() => LocalScriptServer?.Start();

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
                        CalcServerTimeOffset();
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
            var apiCall = new Task(() =>
            {
                var request = _http.GetAsync(url);
                Task call = request;
                if (resultCallback != null)
                    call = request.ContinueWith(r => resultCallback(r.Result));
                else
                {
    #if DEBUG
                    call = request.ContinueWith(r =>
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
                call.Wait(); // wait for response
            });
            _apiCallQueue.Enqueue(apiCall);
        }

        public void PrepareNewFunscript(string filePath, List<ScriptAction> actions)
        {
            string csv = GenerateCSVFromActions(actions);
            long scriptSize = Encoding.UTF8.GetByteCount(csv);
            // the maximum size for the script is 1MB
            if(scriptSize <= (1048576 * 0.995)) // 1MB - 5kb just in case
            {
                _loadedScript = csv;
                _scriptName = filePath;
                string filename = System.IO.Path.GetFileName(filePath);

                string scriptUrl = null;
                if (UseLocalScriptServer)
                {
                    LocalScriptServer.LoadedScript = csv;
                    scriptUrl = LocalScriptServer.ScriptHostUrl + "tmp.csv";
                }
                else
                {
                    var response = PostScriptToHandyfeeling(filename, csv);
                    if(response.success)
                    {
                        scriptUrl = response.url;
                    }
                    else
                    {
                        Debug.WriteLine("Failed to upload script");
                        MessageBox.Show($"Failed to upload script to handyfeeling.com.\n{response.error}\n{response.info}");
                        return;
                    }
                }

                SetSyncMode();
                SyncPrepare(new HandyPrepare()
                {
                    name = filename,
                    url = scriptUrl,
                    size = (int)scriptSize,
                    timeout = 20000
                });
            }
            else
            {
                Debug.WriteLine("Failed to load script larger than 1MB");
                MessageBox.Show("The script is to large for the Handy.");
                _loadedScript = null;
                _scriptName = null;
                if (UseLocalScriptServer)
                    LocalScriptServer.LoadedScript = null;
            }
        }

        private HandyUploadResponse PostScriptToHandyfeeling(string filename, string csv)
        {
            const string uploadUrl = "https://www.handyfeeling.com/api/sync/upload";
            string name = Path.GetFileNameWithoutExtension(filename);
            string csvFileName = $"{name}.csv";

            var requestContent = new MultipartFormDataContent();

            var fileContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
            //var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = "syncFile",
                FileName = "\"" + csvFileName + "\""
            };

            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            requestContent.Add(fileContent, name, csvFileName);



            var request = _http.PostAsync(uploadUrl, requestContent);
            var response = request.Result.Content.ReadAsAsync<HandyUploadResponse>().Result;
            return response;
        }

        public void SetScriptOffset(TimeSpan offset)
        {
            lock (_updateOffsetLock)
            {
                _newOffsetMs = (int)offset.TotalMilliseconds;
                if (!_updateOffsetTask?.IsCompleted ?? false)
                {
                    _resetOffsetTask = true;
                    return;
                }

                _resetOffsetTask = true;
                _updateOffsetTask = Task.Run(() =>
                {
                    while(_resetOffsetTask)
                    {
                        Debug.WriteLine("offset task waiting ...");
                        _resetOffsetTask = false;
                        Thread.Sleep(200);
                    }

                    Debug.WriteLine($"set offset to {_newOffsetMs}");
                    SyncOffset(new HandyOffset()
                    {
                        offset = _newOffsetMs
                    });
                });
            }
        }

        private void SyncOffset(HandyOffset offset)
        {
            string url = GetQuery("syncOffset", offset);
            Debug.WriteLine($"{nameof(SyncOffset)}: {url}");
            SendGetRequest(url);
        }

        private void ScaleScript(List<ScriptAction> actions)
        {
            // scale script across full range of the handy
            // some scripts only go from 5 to 95 or 10 to 90 this will scale
            // those scripts to the desired 0 - 100 range
            const int desiredMax = 100;
            const int desiredMin = 0;
            int maxPos = actions.Max(action => ((FunScriptAction)action).Position);
            int minPos = actions.Min(action => ((FunScriptAction)action).Position);

            if (maxPos < 100 || minPos > 0)
            {
                foreach (FunScriptAction action in actions)
                {
                    int pos = action.Position;
                    int scaledPos = desiredMin + ((pos - minPos)*(desiredMax - desiredMin) / (maxPos - minPos));
                    if (scaledPos <= 100 && scaledPos >= 0)
                        action.Position = (byte)scaledPos;
                }
            }
        }

        private string GenerateCSVFromActions(List<ScriptAction> actions)
        {
            StringBuilder builder = new StringBuilder(1024*1024);
            builder.AppendLine(@"""{""""type"""":""""handy""""}"",");
            
            ScaleScript(actions);

            foreach (FunScriptAction action in actions)
            {
                builder.AppendLine($"{action.TimeStamp.TotalMilliseconds},{action.Position}");
            }
            return builder.ToString();
        }

        public void Resync(TimeSpan time)
        {
            // I can't get this to work keeps returning "Machine timed out"
            // but seems to be working fine without resyncing

            //if (!_playing) return;
            //if (DateTime.Now - _lastSyncAdjust >= TimeSpan.FromSeconds(10) || Math.Abs((_currentTime - time).TotalMilliseconds) >= 500)
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


            // HACK: this resyncs whenever there is a jump greater than 500ms
            // this solves auto skip gaps
            if(_scriptLoaded && _playing) { 
                if (Math.Abs((_currentTime - time).TotalMilliseconds) >= 500)
                {
                    Debug.WriteLine("Resync HACK");
                    Play(false, time.TotalMilliseconds);
                    Play(true, time.TotalMilliseconds);
                }
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
                        // by default bool ToString() returns "True" or "False"
                        // which I think has to be lower case for the query
                        query[property.Name] = boolVal.ToString().ToLower();
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
            Debug.WriteLine($"{nameof(SetSyncMode)}: {url}");
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
        public void CalcServerTimeOffset()
        {
            // due too an api rate limit of I think 60 request per minute I chose just 10 attempts instead of 30...
            const int maxSyncAttempts = 10;
            long offset_agg = 0;
            for (int i = 0; i < maxSyncAttempts; i++)
            {
                var Tsend = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var result = _http.GetAsync(UrlFor("getServerTime")).Result;
                var Treceive = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var RTD = Treceive - Tsend;

                var Ts = result.Content.ReadAsAsync<HandyTimeResponse>().Result.serverTime;
                long Ts_est = Ts + (RTD / 2);

                offset_agg += Ts_est - Treceive;
            }
            _offsetAverage = (int)((double)offset_agg/(double)maxSyncAttempts);
        }

        private long GetServerTimeEstimate() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _offsetAverage;
    }
}
