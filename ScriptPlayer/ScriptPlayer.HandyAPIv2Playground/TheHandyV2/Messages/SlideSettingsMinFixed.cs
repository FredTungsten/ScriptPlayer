using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SlideSettingsMinFixed : SlideSettings
    {
        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("fixed")]
        public bool Fixed { get; set; }
    }
}