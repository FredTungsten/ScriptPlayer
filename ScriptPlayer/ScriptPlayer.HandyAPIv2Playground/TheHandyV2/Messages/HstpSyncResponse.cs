using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HstpSyncResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("rtd")]
        public int RoudtripDelay { get; set; }
    }
}