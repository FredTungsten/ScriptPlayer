using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HampResponse
    {
        [JsonProperty("state")]
        public HampState State { get; set; }

        [JsonProperty("slideMax")]
        public double SlideMax { get; set; }

        [JsonProperty("slideMin")]
        public double SlideMin { get; set; }

        [JsonProperty("velocity")]
        public int Velocity { get; set; }
    }
}