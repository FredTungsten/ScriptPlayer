using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3
{
    internal class HstpOffsetRequest
    {
        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}