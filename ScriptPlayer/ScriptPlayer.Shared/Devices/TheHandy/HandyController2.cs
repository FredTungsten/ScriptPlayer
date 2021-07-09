using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using ScriptPlayer.Shared.Interfaces;
using ScriptPlayer.Shared.Properties;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.Shared.TheHandy
{
    public class HandyController2 : DeviceController, ISyncBasedDevice, IDisposable
    {
        private static readonly TimeSpan MaxOffset = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan ResyncIntervall = TimeSpan.FromSeconds(10);

        public delegate void OsdRequestEventHandler(string text, TimeSpan duration, string designation = null);

        public event HandyController2.OsdRequestEventHandler OsdRequest;

        private HandyScriptServer LocalScriptServer { get; set; }

        public bool UseMultipleCommandQueues { get; set; } = true;

        public bool Connected { get; private set; }

        private bool IsScriptLoaded { get; set; }
        private bool IsCurrentScriptDownloaded { get; set; }

        private long _offsetAverage; // holds calculated offset that gets added to current unix time in ms to estimate api server time


        private TimeSpan _currentTime = TimeSpan.FromSeconds(0);
        private bool _playing;


        // offset task to not spam server with api calls everytime the offset changes slightly
        private Task _updateOffsetTask;
        private int _newOffsetMs;
        private bool _resetOffsetTask;
        private readonly object _updateOffsetLock = new object();

        // api call queue ensures correct order of api calls
        private readonly Dictionary<string, BlockingTaskQueue> _apiCallQueue = new Dictionary<string, BlockingTaskQueue>();
        private readonly object _dictionaryLock = new object();

        private HandyHost _host;
        private DateTime _lastTimeAdjustement = DateTime.MinValue;
        private DateTime _lastResync = DateTime.Now;

        public void UpdateSettings(string deviceId, HandyHost host, string localIp, int localPort)
        {
            if (HandyHelper.DeviceId != deviceId || !Connected)
            {
                HandyHelper.DeviceId = deviceId;
                UpdateConnectionStatus();
            }

            _host = host;

            switch (_host)
            {
                case HandyHost.Local:
                    {
                        if (LocalScriptServer == null)
                        {
                            LocalScriptServer = new HandyScriptServer(this);
                        }

                        if (LocalScriptServer.HttpServerRunning)
                        {
                            if (LocalScriptServer.LocalIp != localIp || LocalScriptServer.ServeScriptPort != localPort)
                            {
                                LocalScriptServer.Exit();
                            }
                        }

                        LocalScriptServer.LocalIp = localIp;
                        LocalScriptServer.ServeScriptPort = localPort;

                        if (!LocalScriptServer.HttpServerRunning)
                            LocalScriptServer.Start();

                        break;
                    }
                case HandyHost.HandyfeelingCom:
                    {
                        if (LocalScriptServer != null && LocalScriptServer.HttpServerRunning)
                        {
                            LocalScriptServer.Exit();
                            LocalScriptServer = null;
                        }

                        break;
                    }
            }
        }

        public HandyController2()
        {
            _apiCallQueue = new Dictionary<string, BlockingTaskQueue>();
        }

        private HttpClient GetClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            client.DefaultRequestHeaders.Add("X-Connection-Key", HandyHelper.DeviceId);
            return client;
        }

        private string UrlFor(string path)
        {
            return $"https://www.handyfeeling.com/api/handy/v2/{path}";
        }

        public void CheckConnected(Action<bool> successCallback = null) => UpdateConnectionStatus(successCallback);

        public void UpdateConnectionStatus(Action<bool> successCallback = null)
        {
            SendGetRequest(UrlFor("connected"), async response =>
            {
                if (!response.IsSuccessStatusCode)
                    return;

                Handy2ConnectedResult status = await response.Content.ReadAsAsync<Handy2ConnectedResult>();

                if (status.connected)
                {
                    CalcServerTimeOffset();
                    SetMode(Handy2Mode.Hssp);
                    
                    OnHandyConnected();
                }
                else
                {
                    OnHandyDisconnected();
                }

                successCallback?.Invoke(Connected);

            }, ignoreConnected: true);
        }

        private void OnHandyDisconnected()
        {
            OnOsdRequest("The Handy is not connected", TimeSpan.FromSeconds(5), "HandyStatus");
            Connected = false;

            OnDeviceRemoved(this);
        }

        private void OnHandyConnected()
        {
            Connected = true;
            OnOsdRequest("The Handy is connected", TimeSpan.FromSeconds(2), "HandyStatus");
            Debug.WriteLine("Successfully connected");

            OnDeviceFound(this);
        }

        private void SendPutRequest<T>(string url, object data, Action<T> onSuccess) where T : Handy2Response
        {
            SendPutRequest(url, data, message =>
            {
                T resp = message.Content.ReadAsAsync<T>().Result;
                Handy2Response response = resp as Handy2Response;

                if (response.error != null)
                {
                    HandleError(url, response.error);
                }
                else
                {
                    onSuccess?.Invoke(resp);
                }
            });
        }

        private void SendGetRequest<T>(string url, Action<T> onSuccess) where T : Handy2Response
        {
            SendGetRequest(url, message =>
            {
                T resp = message.Content.ReadAsAsync<T>().Result;
                Handy2Response response = resp as Handy2Response;
                
                if (response.error != null)
                {
                    HandleError(url, response.error);
                }
                else
                {
                    onSuccess?.Invoke(resp);
                }
            }, false);
        }

        private void HandleError(string url, Handy2Error error)
        {
            Debug.WriteLine($"error: cmd:{error.code} - {error.message} - {url}");

            OnOsdRequest($"Error: {error}", TimeSpan.FromSeconds(3), "HandyError");

            if (error.code == 1001)
                OnHandyDisconnected();
        }

        private void ShowRateLimits(HttpResponseHeaders headers)
        {
            string rateLimit = GetFirstHeaderOrEmpty(headers, "X-RateLimit-Limit");
            string rateLimitRemaining = GetFirstHeaderOrEmpty(headers, "X-RateLimit-Remaining");
            string rateLimitReset = GetFirstHeaderOrEmpty(headers, "X-RateLimit-Reset");

            // Doesn't work somehow ...
            //Debug.WriteLine($"RateLimits: {rateLimitRemaining}/{rateLimit} until {rateLimitReset}");
        }

        private string GetFirstHeaderOrEmpty(HttpResponseHeaders headers, string name)
        {
            if (headers.Contains(name))
                return headers.GetValues(name).FirstOrDefault() ?? "";

            return "";
        }

        private void SendPutRequest(string url, object data, Action<HttpResponseMessage> resultCallback = null, bool ignoreConnected = false)
        {
            if (!ignoreConnected && !Connected)
                return;

            DateTime sendTime = DateTime.Now;

            Task apiCall = new Task(() =>
            {
                using (var client = GetClient())
                {
                    Task<HttpResponseMessage> request;

                    if (data != null)
                        request = client.PutAsJsonAsync(url, data);
                    else
                        request = client.PutAsync(url, null);

                    Task call;

                    TimeSpan duration = DateTime.Now - sendTime;

                    if (resultCallback != null)
                    {
                        Debug.WriteLine($"finished: {url} [{duration.TotalMilliseconds:F0}ms]");
                        call = request.ContinueWith(r => resultCallback(r.Result));
                    }
                    else
                    {
                        call = request.ContinueWith(r =>
                        {
                            Handy2Response resp = r.Result.Content.ReadAsAsync<Handy2Response>().Result;
                            ShowRateLimits(r.Result.Headers);

                            if (resp.error != null)
                            {
                                HandleError(url, resp.error);
                            }
                            else
                            {
                                Debug.WriteLine($"success: {url} [{duration.TotalMilliseconds:F0}ms]");
                            }
                        });
                    }
                    call.Wait(); // wait for response
                }
            });

            string command = ExtractCommand(url);
            Enqueue(command, apiCall);
        }

        private void SendGetRequest(string url, Action<HttpResponseMessage> resultCallback = null, bool ignoreConnected = false, bool waitForAnswer = true)
        {
            if (!ignoreConnected && !Connected)
                return;

            DateTime sendTime = DateTime.Now;

            Task apiCall = new Task(() =>
            {
                using (var client = GetClient())
                {
                    Task<HttpResponseMessage> request = client.GetAsync(url);

                    Task call;

                    TimeSpan duration = DateTime.Now - sendTime;

                    if (resultCallback != null)
                    {
                        Debug.WriteLine($"finished: {url} [{duration.TotalMilliseconds:F0}ms]");
                        call = request.ContinueWith(r => resultCallback(r.Result));
                    }
                    else
                    {
                        call = request.ContinueWith(r =>
                        {
                            Handy2Response resp = r.Result.Content.ReadAsAsync<Handy2Response>().Result;
                            ShowRateLimits(r.Result.Headers);

                            if (resp.error != null)
                            {
                                HandleError(url, resp.error);
                            }
                            else
                            {
                                Debug.WriteLine($"success: {url} [{duration.TotalMilliseconds:F0}ms]");
                            }
                        });
                    }
                    call.Wait(); // wait for response
                }
            });

            string command = ExtractCommand(url);
            Enqueue(command, apiCall);
        }

        private string ExtractCommand(string url)
        {
            Uri uri = new Uri(url);
            return uri.Segments.Last();
        }

        private void Enqueue(string command, Task apiCall)
        {
            if (!UseMultipleCommandQueues)
                command = "default";
            // else
            //    Debug.WriteLine("Command: " + command);

            lock (_dictionaryLock)
            {
                if (!_apiCallQueue.ContainsKey(command))
                    _apiCallQueue.Add(command, new BlockingTaskQueue());

                _apiCallQueue[command].Enqueue(apiCall);
            }
        }

        public void SetScript(string scriptTitle, IEnumerable<FunScriptAction> actions)
        {
            IsCurrentScriptDownloaded = false;

            string csvData = GenerateCsvFromActions(actions.ToList());
            long scriptSize = Encoding.UTF8.GetByteCount(csvData);

            string filename = $"{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}";

            Debug.WriteLine("Script-Size: " + scriptSize);

            // the maximum size for the script is 1MB
            if (scriptSize <= 1024 * (1024 - 5)) // 1MB - 5kb just in case
            {
                string scriptUrl = null;

                switch (_host)
                {
                    case HandyHost.Local:
                        {
                            LocalScriptServer.LoadedScript = csvData;
                            IsScriptLoaded = true;
                            scriptUrl = $"{LocalScriptServer.ScriptHostUrl}{filename}.csv";
                            break;
                        }
                    case HandyHost.HandyfeelingCom:
                        {
                            HandyUploadResponse response = PostScriptToHandyfeeling($"{filename}.csv", csvData);

                            if (response.success)
                            {
                                scriptUrl = response.url;
                                IsScriptLoaded = true;
                            }
                            else
                            {
                                Debug.WriteLine("Failed to upload script");
                                MessageBox.Show($"Failed to upload script to handyfeeling.com.\n{response.error}\n{response.info}");
                                return;
                            }

                            break;
                        }
                }

                SetMode(Handy2Mode.Hssp);
                HsspSetup(scriptUrl, "");
            }
            else
            {
                Debug.WriteLine("Failed to load script larger than 1MB");
                OnOsdRequest("The script is to large for the Handy.", TimeSpan.FromSeconds(10), "TheHandyScriptError");

                IsScriptLoaded = false;

                if (_host == HandyHost.Local)
                    LocalScriptServer.LoadedScript = null;
            }
        }

        private HandyUploadResponse PostScriptToHandyfeeling(string filename, string csv)
        {
            const string uploadUrl = "https://www.handyfeeling.com/api/sync/upload";
            string name = Path.GetFileNameWithoutExtension(filename);
            string csvFileName = $"{name}_{DateTime.UtcNow:yyyyMMddHHmmssf}.csv";

            var requestContent = new MultipartFormDataContent();

            var fileContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "syncFile",
                FileName = "\"" + csvFileName + "\""
            };

            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            requestContent.Add(fileContent, name, csvFileName);

            using (var client = GetClient())
            {
                var request = client.PostAsync(uploadUrl, requestContent);
                var response = request.Result.Content.ReadAsAsync<HandyUploadResponse>().Result;
                return response;
            }
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
                    while (_resetOffsetTask)
                    {
                        Debug.WriteLine("offset task waiting ...");
                        _resetOffsetTask = false;
                        Thread.Sleep(200);
                    }

                    Debug.WriteLine($"set offset to {_newOffsetMs}");
                    SyncOffset(_newOffsetMs);
                });
            }
        }

        private void SyncOffset(int offset)
        {
            string url = UrlFor("hssp/offset");
            Debug.WriteLine($"{nameof(SyncOffset)}: {url}");
            SendPutRequest(url, new Handy2SetOffsetRequest{ offset = offset});
        }

        private static void ScaleScript(List<FunScriptAction> actions)
        {
            // scale script across full range of the handy
            // some scripts only go from 5 to 95 or 10 to 90 this will scale
            // those scripts to the desired 0 - 100 range
            const int desiredMax = 100;
            const int desiredMin = 0;
            int maxPos = actions.Max(action => action.Position);
            int minPos = actions.Min(action => action.Position);

            if (maxPos < 100 || minPos > 0)
            {
                foreach (FunScriptAction action in actions)
                {
                    int pos = action.Position;
                    int scaledPos = desiredMin + ((pos - minPos) * (desiredMax - desiredMin) / (maxPos - minPos));
                    if (scaledPos <= 100 && scaledPos >= 0)
                        action.Position = (byte)scaledPos;
                }
            }
        }

        public static string GenerateCsvFromActions(List<FunScriptAction> actions)
        {
            StringBuilder builder = new StringBuilder(1024 * 1024);
            //builder.Append(@"""{""""type"""":""""handy""""}"",");
            builder.Append("#");

            ScaleScript(actions);

            foreach (FunScriptAction action in actions)
            {
                builder.Append($"\n{action.TimeStamp.TotalMilliseconds:F0},{action.Position}");
            }
            return builder.ToString();
        }

        public void Resync(TimeSpan time)
        {
            if (IsScriptLoaded && _playing)
            {
                TimeSpan diff = (EstimateCurrentTime() - time).Abs();

                if (diff > MaxOffset)
                {
                    //Hard resync because time is out of sync
                    Debug.WriteLine($"Resync (Offset = {diff.TotalMilliseconds})");
                    ResyncNow(time, true);
                }
                else if (DateTime.Now - _lastResync > ResyncIntervall)
                {
                    //Soft resync to "remind" Handy where it should be
                    ResyncNow(time, false);
                }
            }

            UpdateCurrentTime(time);
        }

        private void ResyncNow(TimeSpan time, bool hard)
        {
            if (!IsScriptLoaded || !IsCurrentScriptDownloaded)
                return;

            HsspPlay(time, null);

            //HsspSync();

            //if (hard)
            //{
            //    HsspPlay(time, AfterHardSync);
            //}
            //else
            //{
            //    // Handy doesn't respond  to this command so I'll just let it run into a (very short) timeout
            //    // https://www.reddit.com/r/handySupport/comments/hlljii/timeout_on_syncadjusttimestamp/

            //    //HsspPlay(_playing, time);
            //    // Update shoud be fixed in FW 2.12

            //    HsspSync();
            //}

            _lastResync = DateTime.Now;
        }

        //private void AfterHardSync(Handy2Response handyResponse)
        //{
        //    if (_playing)
        //        HsspPlay(true, EstimateCurrentTime(), null);
        //    else
        //        Debug.WriteLine("HandyController.AfterHardSync, but _playing = false");
        //}

        private TimeSpan EstimateCurrentTime()
        {
            if (_lastTimeAdjustement != DateTime.MinValue)
            {
                TimeSpan elapsedSinceLastUpdate = DateTime.Now - _lastTimeAdjustement;
                return _currentTime + elapsedSinceLastUpdate;
            }

            return _currentTime;
        }

        private void UpdateCurrentTime(TimeSpan time)
        {
            _currentTime = time;
            _lastTimeAdjustement = DateTime.Now;
        }

        public void SetMinMax(int min, int max)
        {
            SetSlide(new Handy2SetSlideRequest
            {
                min = Math.Min(1.0, Math.Max(0.0, min / 100.0)),
                max = Math.Min(1.0, Math.Max(0.0, max / 100.0)),
            });
        }

        private void SetSlide(Handy2SetSlideRequest stroke)
        {
            string url = UrlFor("slide");
            SendPutRequest(url, stroke);
        }

        ///<remarks>/syncPrepare</remarks> 
        private void HsspSetup(string scriptUrl, string hash)
        {
            Handy2HsspSetup setup = new Handy2HsspSetup
            {
                url = scriptUrl
            };

            string url = UrlFor("hssp/setup");
            Debug.WriteLine($"{nameof(HsspSetup)}: {url}");
            SendPutRequest<Handy2Response>(url, setup, HsspSetupFinished);
        }

        private void HsspSetupFinished(Handy2Response resp)
        {
            Handy2HsspSetupResult result = (Handy2HsspSetupResult) resp.result;

            TimeSpan time = EstimateCurrentTime();
            Debug.WriteLine($"success: (HsspSetup -> {result}), resyncing @ " + time.ToString("g"));
            OnOsdRequest("Handy finished downloading Script", TimeSpan.FromSeconds(3), "Handy");

            IsCurrentScriptDownloaded = true;

            ResyncNow(time, true);
        }

        public void Play(bool playing, TimeSpan progress)
        {
            Debug.WriteLine($"HandyController.Play: {playing} @ {progress}");
            if (playing == _playing || !IsScriptLoaded || !IsCurrentScriptDownloaded)
            {
                if (playing == _playing)
                    Debug.WriteLine("HandyController.Play, but playing == _playing");
                else
                    Debug.WriteLine("HandyController.Play, but !IsScriptLoading");

                return;
            }

            _playing = playing;
            Debug.WriteLine("HandyController.Play, _playing = " + _playing);

            if (playing)
                HsspPlay(progress, null);
            else
                HsspStop();
        }

        private void HsspStop()
        {
            string url = UrlFor("hssp/stop");
            SendPutRequest(url, null);
        }

        private void HsspPlay(TimeSpan progress, Action<Handy2Response> continueWith)
        {
            HsspPlay(new Handy2HsspPlayRequest
            {
                estimatedServerTime = GetServerTimeEstimate(),
                startTime = (long)progress.TotalMilliseconds
            }, continueWith);
        }

        ///<remarks>/syncPlay</remarks>
        private void HsspPlay(Handy2HsspPlayRequest play, Action<Handy2Response> continueWith)
        {
            string url = UrlFor("hssp/play");
            Debug.WriteLine($"{nameof(HsspPlay)}: {url}");
            SendPutRequest(url, play, continueWith);
        }

        private string GetQuery(string path, object queryObject)
        {
            // there is probably a better way to do the same thing
            var query = HttpUtility.ParseQueryString(string.Empty);

            PropertyInfo[] properties = queryObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object val = property.GetValue(queryObject);
                if (val == null)
                    continue;

                if (val is bool boolVal)
                {
                    query[property.Name] = boolVal.ToString().ToLower();
                }
                else
                {
                    query[property.Name] = val.ToString();
                }
            }

            if (query.Count == 0) return UrlFor(path);

            return $"{UrlFor(path)}?{query}";
        }

        ///<remarks>/syncAdjustTimestamp</remarks>
        private void HsspSync(int syncCount = 6)
        {
            string url = GetQuery("hssp/sync", new {syncCount = syncCount});
            Debug.WriteLine($"{nameof(HsspSync)}: {url}");
            SendGetRequest<Handy2Response>(url, AfterHsspSync);
        }

        private void AfterHsspSync(Handy2Response response)
        {
            Handy2SyncResult result = (Handy2SyncResult) response.result;
        }

        private void SetMode(Handy2Mode mode)
        {
            string url = UrlFor("mode");
            Debug.WriteLine($"{nameof(SetMode)}: {url}");
            SendPutRequest<Handy2ModeUpdateResponse>(url, new Handy2ModeUpdateRequest{mode = (int)mode}, AfterSetMode);
        }

        private void AfterSetMode(Handy2ModeUpdateResponse response)
        {
            Handy2ModeUpdateResult result = (Handy2ModeUpdateResult) response.result;
            HsspSync();
        }

        /// <summary>
        /// --Guide for server time sync
        /// Ask server X times about the server time(Ts). A higher value results in longer syncing time but higher accuracy.A good value is to use 30 messages (X = 30).
        /// Each time a message is received track the Round Trip Delay(RTD) of the message by timing message send time(Tsend) and message receive time(Treceive). Calculate RTD = Treceive – Tsend.
        /// Calculate the estimated server time when the message is received(Ts_est) by adding half the RTD time to the received value server time value(Ts). Ts_est = Ts + RTD/2.
        /// Calculate the offset between estimated server time(Ts_est) and client time(Tc). Upon receive Tc == Treceive => offset = Ts_est - Treceive.Add the offset to the aggregated offset value(offset_agg). offset_agg = offset_agg + offset.
        /// When all messages are received calculate the average offset(offset_avg) by dividing aggregated offset(offset_agg) values by the number of messages sent(X). offset_avg = offset_agg / X
        /// 
        /// --Calculating server time
        /// When sending serverTime(Ts) to Handy calculate the Ts by using the average offset(offset_avg) and the current client time(Tc) when sending a message to Handy.Ts = Tc + offset_avg
        /// </summary>
        /// <remarks>/getServerTime</remarks>
        public void CalcServerTimeOffset()
        {
            // due too an api rate limit of I think 60 request per minute I chose just 10 attempts instead of 30...
            const int maxSyncAttempts = 20;
            const int warmupAttempts = 2;

            long offsetAggregated = 0;

            long minDelay = long.MaxValue;
            long maxDelay = long.MinValue;

            using (var client = GetClient())
            {
                for (int i = 0; i < maxSyncAttempts; i++)
                {
                    long tSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    HttpResponseMessage result = client.GetAsync(UrlFor("servertime")).Result;
                    long tReceived = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long tTrip = tReceived - tSent;

                    long tServerResponse = result.Content.ReadAsAsync<Handy2ServerTimeResponse>().Result.serverTime;
                    long tServerEstimate = tServerResponse + (tTrip / 2);
                    long tOffset = tServerEstimate - tReceived;

                    if (i >= warmupAttempts)
                        offsetAggregated += tOffset;

                    if (tOffset > maxDelay)
                        maxDelay = tOffset;

                    if (tOffset < minDelay)
                        minDelay = tOffset;
                }
            }

            _offsetAverage = (int)(offsetAggregated / (double)(maxSyncAttempts - warmupAttempts));

            OnOsdRequest($"Handy Sync refreshed: {_offsetAverage}ms (min {minDelay}, max {maxDelay}", TimeSpan.FromSeconds(3), "HandySync");
        }

        private long GetServerTimeEstimate() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _offsetAverage;

        public virtual void OnOsdRequest(string text, TimeSpan duration, string designation)
        {
            OsdRequest?.Invoke(text, duration, designation);
        }

        public bool IsEnabled { get; set; }

        public string Name { get; set; } = "The Handy";

        public void Dispose()
        {
            lock (_dictionaryLock)
            {
                foreach (var kvp in _apiCallQueue)
                {
                    kvp.Value.Cancel();
                }

                _apiCallQueue.Clear();
            }

            LocalScriptServer?.Exit();
            _updateOffsetTask?.Dispose();
        }
    }


    public enum HandyHost
    {
        Local = 0,
        HandyfeelingCom = 1
    }

    [UsedImplicitly]
    internal class HandyUploadResponse
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
}
