using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SlideSettingsMinMax : SlideSettings
    {
        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }
    }
}