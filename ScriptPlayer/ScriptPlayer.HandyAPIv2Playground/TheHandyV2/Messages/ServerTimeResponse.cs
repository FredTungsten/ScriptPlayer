using Newtonsoft.Json;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public class ServerTimeResponse
    {
        [JsonProperty("serverTime")]
        public int ServerTime { get; set; }
    }
}