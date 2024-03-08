using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class OffsetUpdate
    {
        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}