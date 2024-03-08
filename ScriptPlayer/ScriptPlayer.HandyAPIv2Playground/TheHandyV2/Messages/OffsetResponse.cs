using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class OffsetResponse
    {
        [JsonProperty("result")]
        public GenericResult Result { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}