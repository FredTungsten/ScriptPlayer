using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class WebHookOpts
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("events")]
        public string Events { get; set; }
    }
}