using Newtonsoft.Json;

namespace ScriptPlayer.HandyAPIv3Playground.TheHandyV3
{
    public class SliderStrokeResponse
    {
        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }

        [JsonProperty("min_absolute")]
        public double MinAbsolute { get; set; }

        [JsonProperty("max_absolute")]
        public double MaxAbsolute { get; set; }
    }
}