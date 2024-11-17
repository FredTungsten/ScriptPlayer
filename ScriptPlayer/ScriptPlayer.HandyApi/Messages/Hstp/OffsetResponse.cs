using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3
{
    public class OffsetResponse
    {
        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}