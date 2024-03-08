using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptPlayer.Shared.TheHandyV2;

namespace ScriptPlayer.HandyAPIv2Playground.TheHandyV2
{
    public class HandyApiV2
    {
        private string _apiKey;
        private HttpClient _client;
        private Encoding _encoding;
        private string _apiUrl;

        public HandyApiV2(string apiKey, string apiUrl = null)
        {
            _apiKey = apiKey;

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add("X-Connection-Key", apiKey);

            if (string.IsNullOrEmpty(apiUrl))
                _apiUrl = "https://www.handyfeeling.com/api/handy/v2/";
            else
                _apiUrl = apiUrl;

            _encoding = new UTF8Encoding(false);
        }

        #region BasicModeCommands

        public async Task<Response<ModeUpdateResponse>> GetMode()
        {
            return await Get<ModeUpdateResponse>("mode");
        }

        public async Task<Response<ModeUpdateResponse>> PutMode(ModeUpdate mode)
        {
            return await Put<ModeUpdateResponse>("mode", mode);
        }

        public async Task<Response<ConnectedResponse>> GetConnected()
        {
            return await Get<ConnectedResponse>("connected");
        }

        public async Task<Response<InfoResponse>> GetInfo()
        {
            return await Get<InfoResponse>("info");
        }

        public async Task<Response<SettingsResponse>> GetSettings()
        {
            return await Get<SettingsResponse>("settings");
        }

        public async Task<Response<StatusResponse>> GetStatus()
        {
            return await Get<StatusResponse>("status");
        }

        #endregion

        #region Hamp Mode

        public async Task<Response<HampStartResponse>> HampStart()
        {
            return await Put<HampStartResponse>("hamp/start");
        }

        public async Task<Response<HampStopResponse>> HampStop()
        {
            return await Put<HampStopResponse>("hamp/stop");
        }

        public async Task<Response<HampVelocityPercentResponse>> HampGetVelocity()
        {
            return await Get<HampVelocityPercentResponse>("hamp/velocity");
        }

        public async Task<Response<RpcResult>> HampPutVelocity(HampVelocityPercent velocity)
        {
            return await Put<RpcResult>("hamp/velocity", velocity);
        }

        public async Task<Response<HampStateResponse>> HampGetState()
        {
            return await Get<HampStateResponse>("hamp/state");
        }

        #endregion

        #region HDSP

        public async Task<Response<HdspResponse>> HdspPutXava(NextXava value)
        {
            return await Put<HdspResponse>("hdsp/xava", value);
        }

        public async Task<Response<HdspResponse>> HdspPutXpva(NextXpva value)
        {
            return await Put<HdspResponse>("hdsp/xpva", value);
        }

        public async Task<Response<HdspResponse>> HdspPutXpvp(NextXpvp value)
        {
            return await Put<HdspResponse>("hdsp/xpvp", value);
        }

        public async Task<Response<HdspResponse>> HdspPutXat(NextXat value)
        {
            return await Put<HdspResponse>("hdsp/xat", value);
        }

        public async Task<Response<HdspResponse>> HdspPutXpt(NextXpt value)
        {
            return await Put<HdspResponse>("hdsp/xpt", value);
        }

        #endregion

        #region HSSP

        public async Task<Response<HsspPlayResponse>> HsspPlay(HsspPlay value)
        {
            return await Put<HsspPlayResponse>("hssp/play", value);
        }

        public async Task<Response<RpcResult>> HsspStop()
        {
            return await Put<RpcResult>("hssp/stop");
        }

        public async Task<Response<HsspPlayResponse>> HsspSetup(Setup setup)
        {
            return await Put<HsspPlayResponse>("hssp/setup", setup);
        }

        public async Task<Response<LoopSettingResponse>> HsspGetLoop()
        {
            return await Get<LoopSettingResponse>("hssp/loop");
        }

        public async Task<Response<RpcResult>> HsspSetLoop(LoopSettingUpdate loop)
        {
            return await Put<RpcResult>("hssp/loop", loop);
        }

        public async Task<Response<HsspStateResponse>> HsspGetState()
        {
            return await Get<HsspStateResponse>("hssp/state");
        }

        #endregion

        #region HSTP

        public async Task<Response<DeviceTimeResponse>> HstpGetTime()
        {
            return await Get<DeviceTimeResponse>("hstp/time");
        }

        public async Task<Response<OffsetResponse>> HstpGetOffset()
        {
            return await Get<OffsetResponse>("hstp/offset");
        }

        public async Task<Response<RpcResult>> HstpPutOffset(OffsetUpdate offset)
        {
            return await Put<RpcResult>("hstp/offset", offset);
        }

        public async Task<Response<RoudtripDelayResponse>> HstpGetRtd()
        {
            return await Get<RoudtripDelayResponse>("hstp/rtd");
        }

        public async Task<Response<HstpSyncResponse>> HstpGetSync(int syncCount = 30, int outliers = 6)
        {
            return await Get<HstpSyncResponse>($"hstp/sync?syncCount={syncCount}&outliers={outliers}");
        }

        #endregion

        #region slide

        public async Task<Response<SlideResponse>> GetSlide()
        {
            return await Get<SlideResponse>("slide");
        }

        public async Task<Response<SlideUpdateResponse>> PutSlide(SlideSettings settings)
        {
            return await Put<SlideUpdateResponse>("slide", settings);
        }

        public async Task<Response<PositionAbsoluteResponse>> GetSlidePositionAbsolute()
        {
            return await Get<PositionAbsoluteResponse>("slide/position/absolute");
        }

        #endregion

        #region TimeSync

        public async Task<Response<ServerTimeResponse>> GetServerTime()
        {
            return await Get<ServerTimeResponse>("servertime");
        }

        #endregion

        private async Task<Response<T>> Put<T>(string relativeUrl, object data = null) where T: class 
        {
            return await Exchange<T>(relativeUrl, true, data);
        }

        private async Task<Response<T>> Get<T>(string relativeUrl) where T : class
        {
            return await Exchange<T>(relativeUrl, false, null);
        }

        private async Task<Response<T>> Exchange<T>(string relativeUrl, bool put, object data) where T : class
        {
            HttpResponseMessage responseMessage;
            Uri uri = GetUri(relativeUrl);
            
            if (put)
            {
                HttpContent content = null;
                if (data != null)
                {
                    string json = JsonConvert.SerializeObject(data);
                    content = new StringContent(json, _encoding, "application/json");
                }

                responseMessage = await _client.PutAsync(uri, content);
            }
            else
            {
                responseMessage = await _client.GetAsync(uri);
            }

            if(responseMessage.StatusCode != HttpStatusCode.OK)
                throw new Exception("HTTP Status <> 200 OK");

            Response<T> response = new Response<T>
            {
                RateLimitPerMinute = TryToParseHeaderToInt(responseMessage.Headers, "X-RateLimit-Limit", 0),
                RateLimitRemaining = TryToParseHeaderToInt(responseMessage.Headers, "X-RateLimit-Remaining", 0),
                MsUntilRateLimitReset = TryToParseHeaderToInt(responseMessage.Headers, "X-RateLimit-Reset", 0)
            };
            
            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            JObject obj = JObject.Parse(responseContent);

            if (obj.ContainsKey("error"))
            {
                response.Error = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
            }
            else
            {
                response.Data = JsonConvert.DeserializeObject<T>(responseContent);
            }

            return response;
        }

        private Uri GetUri(string relativeUrl)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(_apiUrl);
            if (!_apiUrl.EndsWith("/"))
                builder.Append("/");

            if (relativeUrl.StartsWith("/"))
                builder.Append(relativeUrl.Substring(1));
            else
                builder.Append(relativeUrl);

            return new Uri(builder.ToString(), UriKind.Absolute);
        }

        private int TryToParseHeaderToInt(HttpResponseHeaders headers, string name, int fallback)
        {
            if (!headers.TryGetValues(name, out IEnumerable<string> values))
                return fallback;

            foreach (string value in values)
            {
                if (!int.TryParse(value, out int intValue))
                    continue;

                return intValue;
            }

            return fallback;
        }
    }

    public class Response
    {
        public ErrorResponse Error { get; set; }

        public object RawData { get; set; }

        public int RateLimitPerMinute { get; set; }

        public int RateLimitRemaining { get; set; }

        public int MsUntilRateLimitReset { get; set; }
    }

    public class Response<T> : Response where T:class
    {
        public T Data
        {
            get => RawData as T;
            set => RawData = value;
        }
    }
}
