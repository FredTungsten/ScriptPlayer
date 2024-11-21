using Newtonsoft.Json;

namespace ScriptPlayer.HandyApi.Messages
{
    public class SliderSettings
    {
        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }
    }
}