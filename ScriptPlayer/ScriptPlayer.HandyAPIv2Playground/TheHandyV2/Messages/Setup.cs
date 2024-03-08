using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class Setup
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }
    }
}