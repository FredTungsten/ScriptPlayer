using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3
{
    public class HampStateResponse
    {
        [JsonProperty("play_state")]
        public int PlayState { get; set; }

        [JsonProperty("velocity")]
        public double Velocity { get; set; }

        [JsonProperty("direction")]
        public bool Direction { get; set; }
    }
}