using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HsspStateResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("state")]
        public HsspState State { get; set; }
    }
}