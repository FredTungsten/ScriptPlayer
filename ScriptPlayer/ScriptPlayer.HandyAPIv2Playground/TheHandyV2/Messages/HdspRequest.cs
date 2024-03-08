using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HdspRequest
    {
        [JsonProperty("stopOnTarget")]
        public bool StopOnTarget { get; set; }

        [JsonProperty("immediateResponse")]
        public bool ImmediateResponse { get; set; }
    }
}