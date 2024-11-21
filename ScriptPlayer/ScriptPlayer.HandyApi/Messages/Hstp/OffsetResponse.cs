using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    public class OffsetResponse
    {
        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}