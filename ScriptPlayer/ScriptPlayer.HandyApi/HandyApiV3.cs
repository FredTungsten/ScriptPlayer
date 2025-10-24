using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ScriptPlayer.HandyApi.Messages;

namespace ScriptPlayer.HandyApi
{
    /// <summary>
    /// Api Doks:
    /// https://www.handyfeeling.com/api/handy-rest/v3/docs/
    /// https://ohdoki.notion.site/Handy-API-v3-ea6c47749f854fbcabcc40c729ea6df4
    /// </summary>
    public class HandyApiV3
    {
        private string _apiKey;
        private HttpClient _client;
        private Encoding _encoding;
        private string _apiUrl;

        public HandyApiV3(string apiKey, string apiUrl = null)
        {
            _apiKey = apiKey;

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add("X-Connection-Key", apiKey);

            // This value is specific to ScriptPlayer
            // It is a non-privileged Application-ID that can't be used with privileged API endpoints (e.g. /auth)
            // If you "borrow" this code, pleas at least register you own application: 

            // https://ohdoki.notion.site/Handy-API-v3-ea6c47749f854fbcabcc40c729ea6df4#8ca21fcf0e094c5287358bc7e8080a98
            // https://user.handyfeeling.com/

            _client.DefaultRequestHeaders.Add("X-Api-Key", ".oBFYi~F2Ahyn~H3Q9zGMEHLGPSnCx7b");

            if (string.IsNullOrEmpty(apiUrl))
                _apiUrl = "https://www.handyfeeling.com/api/handy-rest/v3/";
            else
                _apiUrl = apiUrl;

            _encoding = new UTF8Encoding(false);
        }

        #region BasicModeCommands

        public async Task<Response<GetModeResult>> GetMode()
        {
            return await Get<GetModeResult>("mode");
        }

        public async Task<Response<ModeResponse>> PutMode(HandyModes mode)
        {
            PutModeRequest request = new PutModeRequest { Mode = (int)mode };
            return await Put<ModeResponse>("mode", request);
        }

        public async Task<Response<ConnectedResponse>> GetConnected()
        {
            return await Get<ConnectedResponse>("connected");
        }

        public async Task<Response<InfoResponse>> GetInfo()
        {
            return await Get<InfoResponse>("info");
        }

        #endregion

        #region HSSP

        public async Task<Response<HsspStateResponse>> HsspGetState()
        {
            return await Get<HsspStateResponse>("hssp/state");
        }

        public async Task<Response<HsspStateResponse>> HsspSetup(HsspSetupRequest setup)
        {
            return await Put<HsspStateResponse>("hssp/setup", setup);
        }


        public async Task<Response<HsspStateResponse>> HsspPlay(HsspPlayRequest value)
        {
            return await Put<HsspStateResponse>("hssp/play", value);
        }

        public async Task<Response<HsspStateResponse>> HsspStop()
        {
            return await Put<HsspStateResponse>("hssp/stop");
        }

        public async Task<Response<HsspStateResponse>> HsspSyncTime(HsspSyncTimeRequest sync)
        {
            return await Put<HsspStateResponse>("hssp/synctime", sync);
        }


        #endregion

        #region HSTP

        public async Task<Response<DeviceTimeResponse>> HstpGetInfo()
        {
            return await Get<DeviceTimeResponse>("hstp/info");
        }

        public async Task<Response<OffsetResponse>> HstpGetOffset()
        {
            return await Get<OffsetResponse>("hstp/offset");
        }

        public async Task<Response<string>> HstpPutOffset(HstpOffsetRequest offset)
        {
            return await Put<string>("hstp/offset", offset);
        }

        public async Task<Response<string>> HstpClockSync()
        {
            return await Put<string>("hstp/offset");
        }


        #endregion

        #region utils

        public async Task<ServertimeResponse> GetServerTime()
        {
            return await GetRaw<ServertimeResponse>("servertime");
        }

        #endregion

        #region HAMP

        public async Task<Response<HampStateResponse>> HampPutVelocity(double velocity)
        {
            return await Put<HampStateResponse>("hamp/velocity", new HampVelocityRequest { Velocity = velocity });
        }

        public async Task<Response<HampStateResponse>> HampStart()
        {
            return await Put<HampStateResponse>("hamp/start");
        }

        public async Task<Response<HampStateResponse>> HampStop()
        {
            return await Put<HampStateResponse>("hamp/stop");
        }

        public async Task<Response<HampStateResponse>> HampGetState()
        {
            return await Get<HampStateResponse>("hamp/state");
        }

        #endregion

        #region Slider

        public async Task<Response<SliderStateResponse>> GetSliderState()
        {
            return await Get<SliderStateResponse>("slider/state");
        }

        public async Task<Response<SliderStrokeResponse>> PutSliderStroke(SliderSettings settings)
        {
            return await Put<SliderStrokeResponse>("slider/stroke", settings);
        }

        public async Task<Response<SliderStrokeResponse>> GetSliderStroke()
        {
            return await Get<SliderStrokeResponse>("slider/stroke");
        }

        #endregion

        private async Task<Response<T>> Put<T>(string relativeUrl, object data = null) where T : class
        {
            return await Exchange<T>(relativeUrl, true, data);
        }

        private async Task<Response<T>> Get<T>(string relativeUrl) where T : class
        {
            return await Exchange<T>(relativeUrl, false, null);
        }

        private async Task<T> GetRaw<T>(string relativeUrl) where T : class
        {
            return await ExchangeRaw<T>(relativeUrl, false, null);
        }

        private async Task<T> ExchangeRaw<T>(string relativeUrl, bool put, object data) where T : class
        {
            HttpResponseMessage responseMessage;
            Uri uri = GetUri(relativeUrl);

            Debug.WriteLine("Handy API call: " + relativeUrl);

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

            if (responseMessage.StatusCode != HttpStatusCode.OK)
                throw new Exception("HTTP Status <> 200 OK");

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(responseContent);
        }

        private async Task<Response<T>> Exchange<T>(string relativeUrl, bool put, object data) where T : class
        {
            HttpResponseMessage responseMessage;
            Uri uri = GetUri(relativeUrl);

            Debug.WriteLine("Handy API call: " + relativeUrl);

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

            if (responseMessage.StatusCode != HttpStatusCode.OK)
                throw new Exception("HTTP Status <> 200 OK");

            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            
            Response<T> response = JsonConvert.DeserializeObject<Response<T>>(responseContent);

            response.RateLimitPerMinute = TryToParseHeaderToInt(responseMessage.Headers, "X-RateLimit-Limit", 0);
            response.RateLimitRemaining = TryToParseHeaderToInt(responseMessage.Headers, "X-RateLimit-Remaining", 0);
            response.MsUntilRateLimitReset = TryToParseHeaderToInt(responseMessage.Headers, "X-RateLimit-Reset", 0);

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
        public DeviceError Error { get; set; }

        public object RawData { get; set; }

        public int RateLimitPerMinute { get; set; }

        public int RateLimitRemaining { get; set; }

        public int MsUntilRateLimitReset { get; set; }
    }

    public class Response<T> : Response where T : class
    {
        public T Result
        {
            get => RawData as T;
            set => RawData = value;
        }
    }
}
