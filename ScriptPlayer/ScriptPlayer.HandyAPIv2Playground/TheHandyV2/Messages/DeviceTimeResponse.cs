using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class DeviceTimeResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("time")]
        public int Timestamp { get; set; }
    }
}