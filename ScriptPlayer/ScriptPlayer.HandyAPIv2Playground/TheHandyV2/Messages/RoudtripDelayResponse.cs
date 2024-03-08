using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class RoudtripDelayResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("rtd")]
        public int RoudtripDelay { get; set; }
    }
}