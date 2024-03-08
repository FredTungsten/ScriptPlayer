using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class WebHook
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("events")]
        public string Events { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("expires")]
        public int Epires { get; set; }
    }
}