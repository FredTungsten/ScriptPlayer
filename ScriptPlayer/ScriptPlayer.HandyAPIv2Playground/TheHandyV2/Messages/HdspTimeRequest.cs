using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HdspTimeRequest
    {
        [JsonProperty("stopOnTarget")]
        public bool StopOnTarget { get; set; }

        [JsonProperty("immediateResponse")]
        public bool ImmediateResponse { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }
    }
}