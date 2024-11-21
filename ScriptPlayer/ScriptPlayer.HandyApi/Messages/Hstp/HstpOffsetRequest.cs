using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    public class HstpOffsetRequest
    {
        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}