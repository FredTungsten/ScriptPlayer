using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class HsspResponse
    {
        [JsonProperty("state")]
        public HsspState State { get; set; }

        [JsonProperty("slideMax")]
        public double SlideMax { get; set; }

        [JsonProperty("slideMin")]
        public double SlideMin { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }
    }
}