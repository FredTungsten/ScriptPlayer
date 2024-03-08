using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class OtaLatest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("branch")]
        public string Branch { get; set; }
    }
}