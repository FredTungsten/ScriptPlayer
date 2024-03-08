using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SlideSettingsMaxFixed : SlideSettings
    {
        [JsonProperty("max")]
        public double Max { get; set; }

        [JsonProperty("fixed")]
        public bool Fixed { get; set; }
    }
}