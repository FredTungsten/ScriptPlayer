using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HampStateResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("state")]
        public HampState State { get; set; }
    }
}