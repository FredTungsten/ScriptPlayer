using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
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