using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class SettingsResponse
    {
        [JsonProperty("slideMin")]
        public double SlideMin { get; set; }

        [JsonProperty("slideMax")]
        public double SlideMax { get; set; }
    }
}