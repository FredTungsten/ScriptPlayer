using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class NextXpt
    {
        [JsonProperty("stopOnTarget")]
        public bool StopOnTarget { get; set; }

        [JsonProperty("immediateResponse")]
        public bool ImmediateResponse { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("position")]
        public double Position { get; set; }
    }
}