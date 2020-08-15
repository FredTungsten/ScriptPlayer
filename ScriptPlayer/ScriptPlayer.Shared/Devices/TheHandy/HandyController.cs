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
using ScriptPlayer.Shared.Devices.Interfaces;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.Shared.TheHandy
{
    // reference: https://app.swaggerhub.com/apis/alexandera/handy-api/1.0.0#/
    // implementing the handy as a device doesn't really work because of it's unique videosync api
    // implementing it as a timesource would make sense but prevent using other timesources...

    // hopefully in the future the handy will receive an api to send commands to it via bluetooth or LAN 
    // in which case it would integrate nicely into ScriptPlayer

    public class HandyController : DeviceController, ISyncBasedDevice, IDisposable
    {
        private static readonly TimeSpan MaxOffset = TimeSpan.FromMilliseconds(500);

        public delegate void OsdRequestEventHandler(string text, TimeSpan duration, string designation = null);

        public event OsdRequestEventHandler OsdRequest;

        private HandyScriptServer LocalScriptServer { get; set; }

        public bool Connected { get; private set; }

        private readonly HttpClient _http;

        private bool IsScriptLoaded => _loadedScript != null;
        private string _loadedScript; // script text as csv

        private long _offsetAverage; // holds calculated offset that gets added to current unix time in ms to estimate api server time


        private TimeSpan _currentTime = TimeSpan.FromSeconds(0);
        private bool _playing;


        // offset task to not spam server with api calls everytime the offset changes slightly
        private Task _updateOffsetTask;
        private int _newOffsetMs;
        private bool _resetOffsetTask;
        private readonly object _updateOffsetLock = new object();

        // api call queue ensures correct order of api calls
        private readonly BlockingTaskQueue _apiCallQueue;
        private HandyHost _host;

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
                    if(LocalScriptServer == null)
                    {
                        LocalScriptServer = new HandyScriptServer();
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

        public HandyController()
        {
            _apiCallQueue = new BlockingTaskQueue();

            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        }

        private string UrlFor(string path)
        {
            return $"{HandyHelper.ConnectionBaseUrl}{path}";
        }

        public void CheckConnected(Action<bool> successCallback = null) => UpdateConnectionStatus(successCallback);

        public void UpdateConnectionStatus(Action<bool> successCallback = null)
        {
            SendGetRequest(UrlFor("getStatus"), async response =>
            {
                if (!response.IsSuccessStatusCode)
                    return;

                HandyResponse status = await response.Content.ReadAsAsync<HandyResponse>();

                if(status.success)
                {
                    CalcServerTimeOffset();
                    SetSyncMode();

                    OnHandyConnected();
                }
                else
                {
                    OnHandyDisconnected();
                }

                successCallback?.Invoke(Connected);

            }, ignoreConnected:true);
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

        private void SendGetRequest(string url, Action<HttpResponseMessage> resultCallback = null, bool ignoreConnected = false)
        {
            if (!ignoreConnected && !Connected)
                return;

            Task apiCall = new Task(() =>
            {
                Task<HttpResponseMessage> request = _http.GetAsync(url);

                Task call; 

                if (resultCallback != null)
                {
                    call = request.ContinueWith(r => resultCallback(r.Result));
                }
                else
                {
#if DEBUG
                    call = request.ContinueWith(r =>
                    {
                        HandyResponse resp = r.Result.Content.ReadAsAsync<HandyResponse>().Result;
                        if (!resp.success)
                        {
                            Debug.WriteLine($"error: cmd:{resp.cmd} - {resp.error} - {url}");

                            if (resp.error == "No machine connected")
                                OnHandyDisconnected();
                        }
                        else
                        {
                            Debug.WriteLine($"success: {url}");
                        }
                    });
#else
                    call = request;
#endif
                }
                call.Wait(); // wait for response
            });

            _apiCallQueue.Enqueue(apiCall);
        }

        public void SetScript(string scriptTitle, IEnumerable<FunScriptAction> actions)
        {
            string csvData = GenerateCsvFromActions(actions.ToList());
            long scriptSize = Encoding.UTF8.GetByteCount(csvData);

            // the maximum size for the script is 1MB
            if(scriptSize <= 1024 * (1024 - 25)) // 1MB - 5kb just in case
            {
                _loadedScript = csvData;
                string scriptUrl = null;

                switch (_host)
                {
                    case HandyHost.Local:
                    {
                        LocalScriptServer.LoadedScript = csvData;
                        scriptUrl = LocalScriptServer.ScriptHostUrl + "tmp.csv";
                        break;
                    }
                    case HandyHost.HandyfeelingCom:
                    {
                        HandyUploadResponse response = PostScriptToHandyfeeling(scriptTitle + ".csv", csvData);

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

                        break;
                    }
                }

                SetSyncMode();
                SyncPrepare(new HandyPrepare
                {
                    name = scriptTitle,
                    url = scriptUrl,
                    size = (int)scriptSize,
                    timeout = 20000
                });
            }
            else
            {
                Debug.WriteLine("Failed to load script larger than 1MB");
                OnOsdRequest("The script is to large for the Handy.", TimeSpan.FromSeconds(10), "TheHandyScriptError");

                _loadedScript = null;

                if(_host == HandyHost.Local)
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
            
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
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
                    SyncOffset(new HandyOffset
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

        private void ScaleScript(List<FunScriptAction> actions)
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
                    int scaledPos = desiredMin + ((pos - minPos)*(desiredMax - desiredMin) / (maxPos - minPos));
                    if (scaledPos <= 100 && scaledPos >= 0)
                        action.Position = (byte)scaledPos;
                }
            }
        }

        private string GenerateCsvFromActions(List<FunScriptAction> actions)
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
            if(IsScriptLoaded && _playing)
            {
                TimeSpan diff = (_currentTime - time).Abs();
                
                if(diff > MaxOffset)
                {
                    Debug.WriteLine($"Resync (Offset = {diff.TotalMilliseconds})");
                    Play(false, time);
                    Play(true, time);
                }
            }

            _currentTime = time;
        }

        public void StepStroke(bool stepUp)
        {
            SendGetRequest(GetQuery("stepStroke", new
            {
                step = stepUp,
                timeout = 5000
            }), async response =>
            {
                if (!response.IsSuccessStatusCode)
                    return;

                HandyResponse status = await response.Content.ReadAsAsync<HandyResponse>();

                if (status.success)
                {
                    OnOsdRequest("Stroke Length: " + status.stroke, TimeSpan.FromSeconds(2), "HandyStrokeLength");
                }
            });
        }

        public void SetStroke(int percent)
        {
            SetStroke(new HandySetStroke
            {
                timeout = 4000,
                type = "%",
                value = percent
            });
        }

        private void SetStroke(HandySetStroke stroke)
        {
            string url = GetQuery("setStroke", stroke);
            SendGetRequest(url);
        }

        ///<remarks>/syncPrepare</remarks> 
        private void SyncPrepare(HandyPrepare prep)
        {
            string url = GetQuery("syncPrepare", prep);
            Debug.WriteLine($"{nameof(SyncPrepare)}: {url}");
            SendGetRequest(url);
        }

        public void Play(bool playing, TimeSpan progress)
        {
            if (playing == _playing || !IsScriptLoaded) return;
            _playing = playing;
            SyncPlay(new HandyPlay
            {
                play = playing,
                serverTime = GetServerTimeEstimate(),
                time = (int)progress.TotalMilliseconds
            });
        }

        ///<remarks>/syncPlay</remarks>
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
                if (val == null)
                    continue;
                
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
            
            if (query.Count == 0) return UrlFor(path);

            return $"{UrlFor(path)}?{query}";
        }

        ///<remarks>/syncAdjustTimestamp</remarks>
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
            const int maxSyncAttempts = 10;
            long offsetAggregated = 0;

            for (int i = 0; i < maxSyncAttempts; i++)
            {
                long tSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                HttpResponseMessage result = _http.GetAsync(UrlFor("getServerTime")).Result;
                long tReceived = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long tTrip = tReceived - tSent;

                long tServerResponse = result.Content.ReadAsAsync<HandyTimeResponse>().Result.serverTime;
                long tServerEstimate = tServerResponse + (tTrip / 2);

                offsetAggregated += tServerEstimate - tReceived;
            }

            _offsetAverage = (int)(offsetAggregated/(double)maxSyncAttempts);
        }

        private long GetServerTimeEstimate() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _offsetAverage;

        protected virtual void OnOsdRequest(string text, TimeSpan duration, string designation)
        {
            OsdRequest?.Invoke(text, duration, designation);
        }

        public bool IsEnabled { get; set; }

        public string Name { get; set; } = "The Handy";

        public void Dispose()
        {
            _apiCallQueue.Cancel();
            LocalScriptServer?.Exit();
            _http?.Dispose();
            _updateOffsetTask?.Dispose();
        }
    }

    public enum HandyHost
    {
        Local = 0,
        HandyfeelingCom = 1
    }
}
