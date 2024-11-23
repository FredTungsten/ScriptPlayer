using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using ScriptPlayer.HandyApi.Messages;
using ScriptPlayer.HandyApi.ScriptServer;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Interfaces;
using ScriptPlayer.Shared.Scripts;
using ScriptPlayer.Shared.TheHandy;

namespace ScriptPlayer.HandyApi
{
    /// <summary>
    /// Handy Controller based on Apiv3
    /// </summary>
    public class HandyController : DeviceController, ISyncBasedDevice, IDisposable
    {
        private static readonly TimeSpan MaxOffset = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan ResyncIntervall = TimeSpan.FromSeconds(10);

        public delegate void OsdRequestEventHandler(string text, TimeSpan duration, string designation = null);

        public event OsdRequestEventHandler OsdRequest;

        public bool UseMultipleCommandQueues { get; set; } = true;

        public bool Connected { get; private set; }

        public event EventHandler ScriptLoaded;

        private long _offsetAverage; // holds calculated offset that gets added to current unix time in ms to estimate api server time
        
        private TimeSpan _currentTime = TimeSpan.FromSeconds(0);
        private bool _playing;


        // offset task to not spam server with api calls everytime the offset changes slightly
        private Task _updateOffsetTask;
        private int _newOffsetMs;
        private bool _resetOffsetTask;
        private readonly object _updateOffsetLock = new object();

        private Task _updateRangeTask;
        private bool _resetRangeTask;
        private readonly object _updateRangeLock = new object();

        // api call queue ensures correct order of api calls
        private readonly Dictionary<string, BlockingTaskQueue> _apiCallQueue = new Dictionary<string, BlockingTaskQueue>();
        private readonly object _dictionaryLock = new object();

        private DateTime _lastTimeAdjustement = DateTime.MinValue;
        private DateTime _lastResync = DateTime.Now;
        private bool _shouldBePlaying;

        private bool _isScriptLoadedOnDevice;
        private string _scriptTitle;

        private HandyApiV3 _api;
        private byte _newRangeMin;
        private byte _newRangeMax;

        public void UpdateSettings(string deviceId)
        {
            _api = new HandyApiV3(deviceId);

            if (HandyHelper.DeviceId != deviceId || !Connected)
            {
                HandyHelper.DeviceId = deviceId;
                UpdateConnectionStatus();
            }
        }

        public HandyController()
        {
            _apiCallQueue = new Dictionary<string, BlockingTaskQueue>();
        }

        private HttpClient GetClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            return client;
        }

        private string UrlFor(string path)
        {
            return $"{HandyHelper.ConnectionBaseUrl}{path}";
        }

        public void CheckConnected(Action<bool> successCallback = null) => UpdateConnectionStatus(successCallback);

        public void UpdateConnectionStatus(Action<bool> successCallback = null)
        {
            Continue(_api.GetInfo(), GetInfoResponse);
        }

        private void GetInfoResponse(InfoResponse response)
        {
            ShowFirmwareStatusOsd(response.FirmwareStatus);
            CalcServerTimeOffset();
            SetSyncMode();
            OnHandyConnected();
        }

        private void ShowFirmwareStatusOsd(int firmwareStatus)
        {
            switch(firmwareStatus)
            {
                case 0: //up-to-date
                    break;
                case 1:
                    OnOsdRequest("There is an optional firmware update available for your Handy", TimeSpan.FromSeconds(5), "HandyFirmware");
                    break;
                case 2:
                    OnOsdRequest("There is a REQUIRED firmware update available for your Handy!", TimeSpan.FromSeconds(10), "HandyFirmware");
                    break;
            }
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

        private async void Continue<T>(Task<Response<T>> call, Action<T> onSuccess = null, Action<DeviceError> onFailure = null) where T : class
        {
            Response<T> response = await call;

            if (response.Error != null)
            {
                string error = $"Handy Error\r\n" +
                    $"Code = {response.Error.Code}\r\n" +
                    $"Name = {response.Error.Name}\r\n" +
                    $"Message = {response.Error.Message}\r\n" +
                    $"Connected = {response.Error.Connected}";

                Debug.WriteLine(error);

                if (response.Error.Connected == false)
                    OnHandyDisconnected();

                onFailure?.Invoke(response.Error);
            }
            else
                onSuccess?.Invoke(response.Result);
        }

        private void Enqueue(string command, Task apiCall)
        {
            if (!UseMultipleCommandQueues)
              command = "default";

            lock (_dictionaryLock)
            {
                if(!_apiCallQueue.ContainsKey(command))
                    _apiCallQueue.Add(command, new BlockingTaskQueue());

                _apiCallQueue[command].Enqueue(apiCall);
            }
        }

        public bool IsScriptLoaded(string title)
        {
            return _scriptTitle == title && IsScriptLoaded();
        }

        public bool IsScriptLoaded()
        {
            return _isScriptLoadedOnDevice;
        }

        public void ClearScript()
        {
            _scriptTitle = "";
            Play(false, TimeSpan.Zero);
            _isScriptLoadedOnDevice = false;
        }

        public void SetScript(string scriptTitle, IEnumerable<FunScriptAction> actions)
        {
            ClearScript();

            _scriptTitle = scriptTitle;
            string csvData = HandyScriptServer.GenerateCsvFromActions(actions.ToList());
            long scriptSize = Encoding.UTF8.GetByteCount(csvData);

            string filename = $"{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}";

            Debug.WriteLine("Script-Size: " + scriptSize);

            // the maximum size for the script is 1MB
            if (scriptSize <= 1024 * (1024 - 5)) // 1MB - 5kb just in case
            {
                string scriptUrl = null;

                HandyHostingUploadResponse response = PostScriptToHandyfeeling($"{filename}.csv", csvData);

                if (!string.IsNullOrEmpty(response.Url))
                {
                    scriptUrl = response.Url;
                }
                else
                {
                    Debug.WriteLine("Failed to upload script");
                    MessageBox.Show($"Failed to upload script to handyfeeling.com.\n{response.Error}");
                    return;
                }

                SetSyncMode();
                                
                SyncPrepare(scriptUrl);
            }
            else
            {
                Debug.WriteLine("Failed to load script larger than 1MB");
                OnOsdRequest("The script is to large for the Handy.", TimeSpan.FromSeconds(10), "TheHandyScriptError");

                _isScriptLoadedOnDevice = false;
            }
        }

        private HandyHostingUploadResponse PostScriptToHandyfeeling(string filename, string csv)
        {
            try
            {
                const string uploadUrl = "https://www.handyfeeling.com/api/hosting/v2/upload";
                string name = Path.GetFileNameWithoutExtension(filename);
                string csvFileName = $"{name}_{DateTime.UtcNow:yyyyMMddHHmmssf}.csv";

                var requestContent = new MultipartFormDataContent();

                var fileContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "file",
                    FileName = "\"" + csvFileName + "\""
                };

                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                requestContent.Add(fileContent, name, csvFileName);

                using (var client = GetClient())
                {
                    var request = client.PostAsync(uploadUrl, requestContent);
                    var responseString = request.Result.Content.ReadAsStringAsync().Result;
                    HandyHostingUploadResponse response = JsonConvert.DeserializeObject<HandyHostingUploadResponse>(responseString);
                    return response;
                }
            }
            catch (Exception ex)
            {
                return new HandyHostingUploadResponse
                {
                    Url = null,
                    Error = new DeviceError
                    {
                        Message = ex.Message
                    }
                };
            }
        }

        public void SetStrokeZone(byte min, byte max)
        {
            if (max == 99)
                max = 100;

            _newRangeMin = min;
            _newRangeMax = max;

            lock (_updateRangeLock)
            {
                _resetRangeTask = true;

                if (!_updateRangeTask?.IsCompleted ?? false)
                    return;

                //Set value must be stable
                TimeSpan preSleep = TimeSpan.FromMilliseconds(200);

                //Don't spam
                TimeSpan postSleep = TimeSpan.FromMilliseconds(200);

                _updateRangeTask = Task.Run(() =>
                {
                    Thread.Sleep(preSleep);

                    while (_resetRangeTask)
                    {
                        Debug.WriteLine("range task waiting ...");
                        _resetRangeTask = false;
                        Thread.Sleep(postSleep);
                    }

                    Debug.WriteLine($"set range to {_newRangeMin}-{_newRangeMax}");
                    SetSliderSettings(_newRangeMin, _newRangeMax);
                });
            }
        }

        private void SetSliderSettings(int min, int max)
        {
            Continue(_api.PutSliderStroke(new SliderSettings
            {
                Min = min / 100.0,
                Max = max / 100.0
            }));
        }

        public void SetScriptOffset(TimeSpan offset)
        {
            lock (_updateOffsetLock)
            {
                _newOffsetMs = -(int)offset.TotalMilliseconds;
                _resetOffsetTask = true;
                if (!_updateOffsetTask?.IsCompleted ?? false)
                    return;
                
                //Set value must be stable
                TimeSpan preSleep = TimeSpan.FromMilliseconds(200);

                //Don't spam
                TimeSpan postSleep = TimeSpan.FromMilliseconds(200);

                _updateOffsetTask = Task.Run(() =>
                {
                    Thread.Sleep(preSleep);

                    while (_resetOffsetTask)
                    {
                        Debug.WriteLine("offset task waiting ...");
                        _resetOffsetTask = false;
                        Thread.Sleep(postSleep);
                    }

                    
                    Debug.WriteLine($"set offset to {_newOffsetMs}");
                    SyncOffset(_newOffsetMs);
                });
            }
        }

        
        private void SyncOffset(int offset)
        {
            Continue(_api.HstpPutOffset(new HstpOffsetRequest
            {
                Offset = offset
            }));
        }
        

        public void Resync(TimeSpan time)
        {
            if (_isScriptLoadedOnDevice && _playing)
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
            if (!_isScriptLoadedOnDevice)
                return;

            if (hard)
            {
                SyncPlay(false, time, AfterHardSync);
            }
            else
            {
                // Handy doesn't respond  to this command so I'll just let it run into a (very short) timeout
                // https://www.reddit.com/r/handySupport/comments/hlljii/timeout_on_syncadjusttimestamp/

                //SyncPlay(_playing, time);
                // Update shoud be fixed in FW 2.12

                SyncAdjust(new HsspSyncTimeRequest
                {
                    CurrentTime = (int)time.TotalMilliseconds,
                    ServerTime = GetServerTimeEstimate(),
                    Filter = 1.0f
                });
            }

            _lastResync = DateTime.Now;
        }

        private void AfterHardSync(HsspStateResponse handyResponse)
        {
            if (_playing || _shouldBePlaying)
                SyncPlay(true, EstimateCurrentTime(), null);
            else
                Debug.WriteLine("HandyController.AfterHardSync, but _playing = false");
        }

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

        ///<remarks>/syncPrepare</remarks> 
        private void SyncPrepare(string scriptUrl)
        {
            Continue(_api.HsspSetup(new HsspSetupUrlRequest
            {
                Url = scriptUrl
            }), SyncPrepareFinished, SyncPrepareFailed);
        }

        private void SyncPrepareFailed(DeviceError error)
        {
            OnOsdRequest("Script Download failed: " + error.Message, TimeSpan.FromSeconds(3), "Handy");
        }

        private void SyncPrepareFinished(HsspStateResponse resp)
        {
            _isScriptLoadedOnDevice = true;
            TimeSpan time = EstimateCurrentTime();
            Debug.WriteLine($"success: (SyncPrepare), resyncing @ " + time.ToString("g"));
            OnOsdRequest("Handy finished downloading Script", TimeSpan.FromSeconds(3), "Handy");
            OnScriptLoaded();
            ResyncNow(time, true);
        }

        public void Play(bool playing, TimeSpan progress)
        {
            Debug.WriteLine($"HandyController.Play: {playing} @ {progress}");

            bool wasAlreadyPlayingRight = _shouldBePlaying == playing;
            _shouldBePlaying = playing;
            
            if (!_isScriptLoadedOnDevice)
            {
                Debug.WriteLine("HandyController.Play, but !IsScriptLoading");
                return;
            }
            
            if (playing == _playing && wasAlreadyPlayingRight)
            {
                Debug.WriteLine("HandyController.Play, but playing == _playing (and was already)");
                return;
            }

            _playing = playing;
            Debug.WriteLine("HandyController.Play, _playing = " + _playing);

            SyncPlay(playing, progress, null);
        }

        private void SyncPlay(bool playing, TimeSpan progress, Action<HsspStateResponse> continueWith)
        {
            if (playing)
            {
                Continue(_api.HsspPlay(new HsspPlayRequest
                {
                    Loop = false,
                    PlaybackRate = 1.0,
                    ServerTime = GetServerTimeEstimate(),
                    StartTime = (int)progress.TotalMilliseconds,
                }), continueWith);
            }
            else
            {
                Continue(_api.HsspStop(), continueWith);
            }
        }

        
        ///<remarks>/syncAdjustTimestamp</remarks>
        private void SyncAdjust(HsspSyncTimeRequest adjust)
        {
            Continue(_api.HsspSyncTime(adjust), null);
        }
        

        private void SetSyncMode()
        {
            Continue(_api.PutMode(HandyModes.Hssp), null);
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
        public async void CalcServerTimeOffset()
        {
            // due too an api rate limit of I think 60 request per minute I chose just 10 attempts instead of 30...
            const int maxSyncAttempts = 20;
            const int warmupAttempts = 2;

            long offsetAggregated = 0;
            long pingAggregated = 0;

            long minDelay = long.MaxValue;
            long maxDelay = long.MinValue;

            long minPing = long.MaxValue;
            long maxPing = long.MinValue;
            
            for (int i = 0; i < maxSyncAttempts; i++)
            {
                long tSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var response = await _api.GetServerTime();
                long tReceived = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long tTrip = tReceived - tSent;              

                long tServerResponse = response.ServerTime;
                long tServerEstimate = tServerResponse + (tTrip / 2);
                long tOffset = tServerEstimate - tReceived;

                if (i < warmupAttempts)
                    continue;
                    
                offsetAggregated += tOffset;
                pingAggregated += tTrip;
                    
                if (tOffset > maxDelay)
                    maxDelay = tOffset;

                if (tOffset < minDelay)
                    minDelay = tOffset;

                if (tTrip > maxPing)
                    maxPing = tTrip;

                if (tTrip < minPing)
                    minPing = tTrip;
            }

            _offsetAverage = (int)(offsetAggregated / (double)(maxSyncAttempts - warmupAttempts));
            long offsetJitter = maxDelay - minDelay;

            long pingAverag = (int) (pingAggregated / (double) (maxSyncAttempts - warmupAttempts));
            long pingJitter = maxPing - minPing;

            OnOsdRequest($"Handy Sync refreshed: offset {_offsetAverage}ms, ping {minPing}-{maxPing}ms", TimeSpan.FromSeconds(3), "HandySync");
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
            
            _updateOffsetTask?.Dispose();
        }

        protected virtual void OnScriptLoaded()
        {
            ScriptLoaded?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum HandyHost
    {
        Local = 0,
        HandyfeelingCom = 1
    }

    public enum HandyConnectionMode
    {
        ApiV1,
        ApiV2,
    }
}
